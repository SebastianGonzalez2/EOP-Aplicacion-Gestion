using System.Data;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;

public class RepositorioConceptosPresupuesto : IRepositorioConceptosPresupuesto
{
    private readonly EopDbContext _contexto;

    public RepositorioConceptosPresupuesto(EopDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken)
    {
        return _contexto.Presupuestos.AsNoTracking()
            .SingleOrDefaultAsync(presupuesto => presupuesto.Id == presupuestoId, cancellationToken);
    }

    public async Task<ResultadoGuardarConcepto> AgregarSiPuedeEditarAsync(
        ConceptoPresupuesto concepto, Usuario usuario, CancellationToken cancellationToken)
    {
        // Volvemos a comprobar el acceso dentro de la transacción para que el estado,
        // el responsable y los permisos del usuario no cambien antes de guardar.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        var usuarioActual = await _contexto.Usuarios.AsNoTracking()
            .SingleOrDefaultAsync(actual => actual.Id == usuario.Id, cancellationToken);
        var presupuesto = await ObtenerPresupuestoAsync(concepto.PresupuestoId, cancellationToken);

        if (usuarioActual == null || usuarioActual.EntraObjectId != usuario.EntraObjectId
            || presupuesto == null || !PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuarioActual))
        {
            return ResultadoGuardarConcepto.EdicionNoPermitida;
        }

        if (concepto.TipoConceptoId == TipoConcepto.MaterialId)
        {
            var referenciaSeleccionada = await (
                from referencia in _contexto.MaterialProveedores.AsNoTracking()
                join material in _contexto.Materiales.AsNoTracking() on referencia.MaterialId equals material.Id
                where referencia.Id == concepto.MaterialProveedorId
                select new { Material = material, Referencia = referencia }
            ).SingleOrDefaultAsync(cancellationToken);

            if (referenciaSeleccionada == null)
            {
                return ResultadoGuardarConcepto.ReferenciaInexistente;
            }

            bool duplicado = await _contexto.ConceptosPresupuesto.AnyAsync(
                fila => fila.PresupuestoId == concepto.PresupuestoId
                    && fila.MaterialProveedorId == concepto.MaterialProveedorId,
                cancellationToken);
            if (duplicado)
            {
                return ResultadoGuardarConcepto.ReferenciaDuplicada;
            }

            concepto.CopiarReferencia(referenciaSeleccionada.Material, referenciaSeleccionada.Referencia);
        }

        _contexto.ConceptosPresupuesto.Add(concepto);
        try
        {
            await _contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excepcion) when (
            IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.ConceptosPresupuestoMaterialProveedor))
        {
            _contexto.Entry(concepto).State = EntityState.Detached;
            return ResultadoGuardarConcepto.ReferenciaDuplicada;
        }

        await transaccion.CommitAsync(cancellationToken);
        return ResultadoGuardarConcepto.Guardado;
    }

    // US-007: vincula o sustituye el material de una fila que ya existe. Todo pasa en una sola
    // transacción para que el permiso, la fila, la referencia y la duplicidad se miren con los
    // datos de ese mismo instante, igual que al agregar una fila nueva.
    public Task<ResultadoSeleccionarMaterial> SeleccionarMaterialSiPuedeEditarAsync(
        int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
        CancellationToken cancellationToken)
    {
        return CopiarReferenciaALaFilaAsync(
            presupuestoId, conceptoId, referenciaId, usuario, conservarMaterial: false, cancellationToken);
    }

    // S3-T12 (US-010A): cambia solo el proveedor de una fila que ya tiene material. Es el mismo
    // recorrido que la selección de US-007 (misma transacción, mismo bloqueo, mismo control de
    // duplicados), con la condición extra de que la referencia nueva sea del mismo material.
    public Task<ResultadoSeleccionarMaterial> CambiarProveedorSiPuedeEditarAsync(
        int presupuestoId, int conceptoId, int referenciaId, Usuario usuario,
        CancellationToken cancellationToken)
    {
        return CopiarReferenciaALaFilaAsync(
            presupuestoId, conceptoId, referenciaId, usuario, conservarMaterial: true, cancellationToken);
    }

    // Cuando conservarMaterial es true (S3-T12), la fila tiene que tener ya un material y la
    // referencia elegida tiene que ser de ese mismo material. Cuando es false (US-007), la fila
    // puede quedar con cualquier material del catálogo.
    private async Task<ResultadoSeleccionarMaterial> CopiarReferenciaALaFilaAsync(
        int presupuestoId, int conceptoId, int referenciaId, Usuario usuario, bool conservarMaterial,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);

        // Primero el acceso: si no tiene permiso no revelamos ni siquiera si la fila existe.
        if (!await UsuarioPuedeEditarAsync(presupuestoId, usuario, cancellationToken))
        {
            return ResultadoSeleccionarMaterial.EdicionNoPermitida;
        }

        var concepto = await ObtenerFilaBloqueadaAsync(presupuestoId, conceptoId, cancellationToken);
        if (concepto == null)
        {
            return ResultadoSeleccionarMaterial.ConceptoInexistente;
        }

        if (concepto.TipoConceptoId != TipoConcepto.MaterialId)
        {
            return ResultadoSeleccionarMaterial.ConceptoNoEsMaterial;
        }

        // S3-T12: sin material en la fila no hay un "otro proveedor" que elegir. Esto va antes de
        // mirar la referencia para dar el mensaje más útil: primero hay que elegir el material.
        if (conservarMaterial && concepto.MaterialId == null)
        {
            return ResultadoSeleccionarMaterial.ConceptoSinMaterial;
        }

        // Si la fila ya usa esta referencia no se copia nada de nuevo: volver a copiar pisaría
        // los valores históricos que la fila conserva con los actuales del catálogo (DT-11).
        if (concepto.MaterialProveedorId == referenciaId)
        {
            return ResultadoSeleccionarMaterial.SinCambios;
        }

        var referenciaSeleccionada = await ObtenerReferenciaConMaterialAsync(referenciaId, cancellationToken);
        if (referenciaSeleccionada == null)
        {
            return ResultadoSeleccionarMaterial.ReferenciaInexistente;
        }

        // S3-T12: el cambio de proveedor no puede cambiar el material. Se compara contra el material
        // que la fila ya tiene guardado, no contra lo que mande la pantalla (RNF-4, RNF-10).
        if (conservarMaterial && referenciaSeleccionada.Material.Id != concepto.MaterialId)
        {
            return ResultadoSeleccionarMaterial.ReferenciaDeOtroMaterial;
        }

        // RF-20: la misma combinación material-proveedor no puede estar en dos filas del
        // presupuesto. La fila que estamos modificando no cuenta como duplicada de sí misma.
        bool duplicado = await _contexto.ConceptosPresupuesto.AnyAsync(
            fila => fila.PresupuestoId == presupuestoId
                && fila.MaterialProveedorId == referenciaId
                && fila.Id != conceptoId,
            cancellationToken);
        if (duplicado)
        {
            return ResultadoSeleccionarMaterial.ReferenciaDuplicada;
        }

        // Se reemplazan solo los cuatro datos históricos y la referencia. ConceptoDetectado y
        // Cantidad no se tocan. El catálogo se leyó sin seguimiento, así que no se puede modificar.
        concepto.CopiarReferencia(referenciaSeleccionada.Material, referenciaSeleccionada.Referencia);
        try
        {
            await _contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excepcion) when (
            IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.ConceptosPresupuestoMaterialProveedor))
        {
            // Otra solicitud simultánea tomó la misma referencia entre la consulta y el guardado.
            _contexto.Entry(concepto).State = EntityState.Detached;
            return ResultadoSeleccionarMaterial.ReferenciaDuplicada;
        }

        await transaccion.CommitAsync(cancellationToken);
        return ResultadoSeleccionarMaterial.Seleccionado;
    }

    // S3-T12 (US-010A, RF-020): actualiza a pedido los valores de la referencia que la fila YA
    // tiene. Vuelve a copiar los cuatro datos históricos (DT-11) con los valores vigentes del
    // catálogo. No cambia el material, el proveedor, ConceptoDetectado ni la cantidad.
    //
    // La referencia no viene de la pantalla: es la que la fila ya guarda. Así esta acción no puede
    // usarse para cambiar de proveedor ni de material, y no hay un Id que alguien pueda manipular.
    public async Task<ResultadoSeleccionarMaterial> ActualizarReferenciaActualSiPuedeEditarAsync(
        int presupuestoId, int conceptoId, Usuario usuario, CancellationToken cancellationToken)
    {
        // Misma transacción, mismo control de acceso y mismo bloqueo que los otros dos flujos.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);

        if (!await UsuarioPuedeEditarAsync(presupuestoId, usuario, cancellationToken))
        {
            return ResultadoSeleccionarMaterial.EdicionNoPermitida;
        }

        var concepto = await ObtenerFilaBloqueadaAsync(presupuestoId, conceptoId, cancellationToken);
        if (concepto == null)
        {
            return ResultadoSeleccionarMaterial.ConceptoInexistente;
        }

        if (concepto.TipoConceptoId != TipoConcepto.MaterialId)
        {
            return ResultadoSeleccionarMaterial.ConceptoNoEsMaterial;
        }

        // Sin material (o sin referencia) no hay nada que actualizar.
        if (concepto.MaterialId == null || concepto.MaterialProveedorId == null)
        {
            return ResultadoSeleccionarMaterial.ConceptoSinMaterial;
        }

        var referenciaActual = await ObtenerReferenciaConMaterialAsync(
            concepto.MaterialProveedorId.Value, cancellationToken);
        if (referenciaActual == null)
        {
            return ResultadoSeleccionarMaterial.ReferenciaInexistente;
        }

        // Si los cuatro valores de la fila ya son los del catálogo no hay nada que actualizar.
        // Se avisa en vez de guardar igual, para que nadie crea que cambió algo.
        bool yaCoinciden = concepto.Concepto == referenciaActual.Material.Nombre
            && concepto.Unidad == referenciaActual.Material.Unidad
            && concepto.PrecioUnitario == referenciaActual.Referencia.PrecioUnitario
            && concepto.Moneda == referenciaActual.Referencia.Moneda;
        if (yaCoinciden)
        {
            return ResultadoSeleccionarMaterial.SinCambios;
        }

        // No hace falta mirar duplicados: la combinación material-proveedor de la fila no cambia.
        // El catálogo se leyó sin seguimiento, así que esta operación no puede modificarlo.
        concepto.CopiarReferencia(referenciaActual.Material, referenciaActual.Referencia);
        await _contexto.SaveChangesAsync(cancellationToken);

        await transaccion.CommitAsync(cancellationToken);
        return ResultadoSeleccionarMaterial.Seleccionado;
    }

    // Vuelve a comprobar el acceso con los datos de este instante (estado del presupuesto,
    // responsable, rol y usuario activo), dentro de la transacción.
    private async Task<bool> UsuarioPuedeEditarAsync(
        int presupuestoId, Usuario usuario, CancellationToken cancellationToken)
    {
        var usuarioActual = await _contexto.Usuarios.AsNoTracking()
            .SingleOrDefaultAsync(actual => actual.Id == usuario.Id, cancellationToken);
        var presupuesto = await ObtenerPresupuestoAsync(presupuestoId, cancellationToken);

        return usuarioActual != null && usuarioActual.EntraObjectId == usuario.EntraObjectId
            && presupuesto != null && PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuarioActual);
    }

    // La fila se busca por su Id Y por el presupuesto: así nadie puede modificar una fila de
    // otro presupuesto cambiando el Id en la URL. UPDLOCK reserva la fila para modificarla; sin
    // eso, dos sustituciones simultáneas de la misma fila se bloquearían entre sí (deadlock).
    private Task<ConceptoPresupuesto?> ObtenerFilaBloqueadaAsync(
        int presupuestoId, int conceptoId, CancellationToken cancellationToken)
    {
        return _contexto.ConceptosPresupuesto
            .FromSqlInterpolated($@"SELECT * FROM ConceptosPresupuesto WITH (UPDLOCK)
                WHERE Id = {conceptoId} AND PresupuestoId = {presupuestoId}")
            .SingleOrDefaultAsync(cancellationToken);
    }

    // Lee la referencia y su material sin seguimiento: el catálogo solo se lee, nunca se modifica.
    private Task<ReferenciaConMaterial?> ObtenerReferenciaConMaterialAsync(
        int referenciaId, CancellationToken cancellationToken)
    {
        return (
            from referencia in _contexto.MaterialProveedores.AsNoTracking()
            join material in _contexto.Materiales.AsNoTracking() on referencia.MaterialId equals material.Id
            where referencia.Id == referenciaId
            select new ReferenciaConMaterial(material, referencia)
        ).SingleOrDefaultAsync(cancellationToken);
    }

    private sealed record ReferenciaConMaterial(Material Material, MaterialProveedor Referencia);
}
