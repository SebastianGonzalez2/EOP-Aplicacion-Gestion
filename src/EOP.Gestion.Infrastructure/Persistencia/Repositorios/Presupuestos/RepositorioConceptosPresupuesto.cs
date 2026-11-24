using System.Data;
using EOP.Gestion.Core.DTOs.Presupuestos;
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

    public async Task<bool> PuedeEditarEnBaseAsync(
        int presupuestoId, Usuario usuario, CancellationToken cancellationToken)
    {
        Usuario? usuarioActual = await _contexto.Usuarios.AsNoTracking()
            .SingleOrDefaultAsync(actual => actual.Id == usuario.Id, cancellationToken);

        Presupuesto? presupuesto = await ObtenerPresupuestoAsync(presupuestoId, cancellationToken);

        if (usuarioActual == null
            || usuarioActual.EntraObjectId != usuario.EntraObjectId
            || presupuesto == null)
        {
            return false;
        }

        return PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuarioActual);
    }

    // Construye el punto de partida del borrador a partir de los datos realmente guardados en SQL Server.
    // Todo se lee con AsNoTracking para que consultar o iniciar una edición nunca modifique la base.
    public async Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(
        int presupuestoId, CancellationToken cancellationToken)
    {
        bool existePresupuesto = await _contexto.Presupuestos.AsNoTracking()
            .AnyAsync(presupuesto => presupuesto.Id == presupuestoId, cancellationToken);

        if (!existePresupuesto)
        {
            return null;
        }

        List<ConceptoPresupuesto> conceptosPersistidos = await _contexto.ConceptosPresupuesto
            .AsNoTracking()
            .Where(concepto => concepto.PresupuestoId == presupuestoId)
            .OrderBy(concepto => concepto.Id)
            .ToListAsync(cancellationToken);

        List<int> idsConceptos = new List<int>();
        foreach (ConceptoPresupuesto concepto in conceptosPersistidos)
        {
            idsConceptos.Add(concepto.Id);
        }

        List<MargenBaseTipo> margenesBasePersistidos = new List<MargenBaseTipo>();
        if (idsConceptos.Count > 0)
        {
            margenesBasePersistidos = await _contexto.MargenesBaseTipo
                .AsNoTracking()
                .Where(baseMargen => idsConceptos.Contains(baseMargen.ConceptoPresupuestoId))
                .OrderBy(baseMargen => baseMargen.ConceptoPresupuestoId)
                .ThenBy(baseMargen => baseMargen.TipoConceptoId)
                .ToListAsync(cancellationToken);
        }

        Dictionary<int, string> nombresTipos = await _contexto.TiposConcepto
            .AsNoTracking()
            .ToDictionaryAsync(tipo => tipo.Id, tipo => tipo.Nombre, cancellationToken);

        // El nombre del proveedor se consulta solo para mostrarlo en la tabla; los cuatro datos
        // históricos del material (nombre, unidad, precio y moneda) salen siempre del concepto (DT-11).
        List<int> idsReferencias = new List<int>();
        foreach (ConceptoPresupuesto concepto in conceptosPersistidos)
        {
            if (concepto.MaterialProveedorId.HasValue)
            {
                idsReferencias.Add(concepto.MaterialProveedorId.Value);
            }
        }

        Dictionary<int, string> proveedoresPorReferencia = new Dictionary<int, string>();
        if (idsReferencias.Count > 0)
        {
            var filasProveedores = await (
                from referencia in _contexto.MaterialProveedores.AsNoTracking()
                join proveedor in _contexto.Proveedores.AsNoTracking()
                    on referencia.ProveedorId equals proveedor.Id
                where idsReferencias.Contains(referencia.Id)
                select new
                {
                    ReferenciaId = referencia.Id,
                    ProveedorNombre = proveedor.Nombre
                }
            ).ToListAsync(cancellationToken);

            foreach (var filaProveedor in filasProveedores)
            {
                proveedoresPorReferencia[filaProveedor.ReferenciaId] = filaProveedor.ProveedorNombre;
            }
        }

        var borrador = new BorradorPresupuestoDto
        {
            PresupuestoId = presupuestoId,
            HuellaEstadoBase = CalculadorHuellaBorrador.Calcular(
                conceptosPersistidos, margenesBasePersistidos)
        };

        foreach (ConceptoPresupuesto concepto in conceptosPersistidos)
        {
            string nombreTipo = string.Empty;
            if (nombresTipos.TryGetValue(concepto.TipoConceptoId, out string? encontradoTipo))
            {
                nombreTipo = encontradoTipo;
            }

            string? nombreProveedor = null;
            if (concepto.MaterialProveedorId.HasValue
                && proveedoresPorReferencia.TryGetValue(concepto.MaterialProveedorId.Value, out string? encontradoProveedor))
            {
                nombreProveedor = encontradoProveedor;
            }

            var tiposBase = new List<int>();
            foreach (MargenBaseTipo baseMargen in margenesBasePersistidos)
            {
                if (baseMargen.ConceptoPresupuestoId == concepto.Id)
                {
                    tiposBase.Add(baseMargen.TipoConceptoId);
                }
            }

            borrador.Conceptos.Add(new ConceptoBorradorDto
            {
                Id = concepto.Id,
                TipoConceptoId = concepto.TipoConceptoId,
                TipoNombre = nombreTipo,
                ConceptoDetectado = concepto.ConceptoDetectado,
                Concepto = concepto.Concepto,
                MaterialId = concepto.MaterialId,
                MaterialProveedorId = concepto.MaterialProveedorId,
                ProveedorNombre = nombreProveedor,
                Cantidad = concepto.Cantidad,
                Unidad = concepto.Unidad,
                PrecioUnitario = concepto.PrecioUnitario,
                Moneda = concepto.Moneda,
                PorcentajeMargen = concepto.PorcentajeMargen,
                TiposBaseMargen = tiposBase,
                EsNuevo = false,
                FueModificado = false
            });
        }

        return borrador;
    }

    // Lee la referencia, su material y su proveedor sin seguimiento: el catálogo solo se lee,
    // nunca se crea ni se modifica desde el presupuesto (DT-11, RF-20).
    public Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(
        int referenciaId, CancellationToken cancellationToken)
    {
        return (
            from referencia in _contexto.MaterialProveedores.AsNoTracking()
            join material in _contexto.Materiales.AsNoTracking()
                on referencia.MaterialId equals material.Id
            join proveedor in _contexto.Proveedores.AsNoTracking()
                on referencia.ProveedorId equals proveedor.Id
            where referencia.Id == referenciaId
            select new ReferenciaCatalogoPresupuestoDto(
                material.Id,
                referencia.Id,
                material.Nombre,
                material.Unidad,
                proveedor.Nombre,
                referencia.PrecioUnitario,
                referencia.Moneda)
        ).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken)
    {
        return _contexto.TiposConcepto.AsNoTracking()
            .Where(tipo => tipo.Id == tipoId)
            .Select(tipo => (string?)tipo.Nombre)
            .SingleOrDefaultAsync(cancellationToken);
    }

    // US-003 (RF-05): persiste de forma atómica todos los cambios pendientes del borrador.
    //
    // Todo ocurre dentro de una única transacción con UPDLOCK sobre la fila del Presupuesto:
    // - Dos guardados simultáneos sobre el mismo presupuesto se ejecutan en orden sin deadlocks.
    // - Volvemos a comprobar usuario activo, rol, responsabilidad y estado editable en ese instante.
    // - Verificamos la huella del estado base para detectar ediciones concurrentes sin pisar datos.
    // - Nunca modificamos Presupuestos ni el catálogo (Materiales, MaterialProveedores, Proveedores).
    // - Nunca tocamos filas persistidas que no fueron modificadas, preservando sus valores históricos.
    public async Task<ResultadoGuardarBorrador> GuardarBorradorSiPuedeEditarAsync(
        BorradorPresupuestoDto borrador,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);

        Presupuesto? presupuesto = await ObtenerPresupuestoBloqueadoAsync(
            borrador.PresupuestoId, cancellationToken);

        Usuario? usuarioActual = await _contexto.Usuarios.AsNoTracking()
            .SingleOrDefaultAsync(actual => actual.Id == usuario.Id, cancellationToken);

        if (usuarioActual == null
            || usuarioActual.EntraObjectId != usuario.EntraObjectId
            || presupuesto == null
            || !PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuarioActual))
        {
            return ResultadoGuardarBorrador.EdicionNoPermitida;
        }

        List<ConceptoPresupuesto> conceptosPersistidos = await _contexto.ConceptosPresupuesto
            .Where(concepto => concepto.PresupuestoId == borrador.PresupuestoId)
            .OrderBy(concepto => concepto.Id)
            .ToListAsync(cancellationToken);

        List<int> idsPersistidos = new List<int>();
        foreach (ConceptoPresupuesto concepto in conceptosPersistidos)
        {
            idsPersistidos.Add(concepto.Id);
        }

        List<MargenBaseTipo> margenesBasePersistidos = new List<MargenBaseTipo>();
        if (idsPersistidos.Count > 0)
        {
            margenesBasePersistidos = await _contexto.MargenesBaseTipo
                .AsNoTracking()
                .Where(baseMargen => idsPersistidos.Contains(baseMargen.ConceptoPresupuestoId))
                .OrderBy(baseMargen => baseMargen.ConceptoPresupuestoId)
                .ThenBy(baseMargen => baseMargen.TipoConceptoId)
                .ToListAsync(cancellationToken);
        }

        // Control de concurrencia optimista: si algo cambió en la base desde que empezó la edición,
        // rechazamos el guardado para no perder ni duplicar datos en silencio.
        string huellaActual = CalculadorHuellaBorrador.Calcular(
            conceptosPersistidos, margenesBasePersistidos);

        if (!string.Equals(huellaActual, borrador.HuellaEstadoBase, StringComparison.Ordinal))
        {
            return ResultadoGuardarBorrador.ConflictoConcurrencia;
        }

        if (!borrador.TieneCambiosPendientes)
        {
            return ResultadoGuardarBorrador.SinCambios;
        }

        // Volvemos a validar el borrador completo dentro de la transacción.
        if (ValidadorBorradorPresupuesto.ValidarBorrador(borrador) != null)
        {
            return ResultadoGuardarBorrador.DatosInvalidos;
        }

        Dictionary<int, ConceptoPresupuesto> persistidosPorId = new Dictionary<int, ConceptoPresupuesto>();
        foreach (ConceptoPresupuesto concepto in conceptosPersistidos)
        {
            persistidosPorId[concepto.Id] = concepto;
        }

        // Separamos las filas nuevas y las filas modificadas; las filas sin cambios no se tocan.
        var conceptosModificados = new List<ConceptoBorradorDto>();
        var conceptosNuevosSinMargen = new List<ConceptoBorradorDto>();
        ConceptoBorradorDto? margenNuevo = null;

        var referenciasAValidar = new Dictionary<int, int>();
        var referenciasFinalesDeCambios = new HashSet<int>();
        var referenciasFinalesDelPresupuesto = new HashSet<int>();
        var tiposPresentesSinMargen = new HashSet<int>();

        bool yaExisteMargenPersistido = false;
        foreach (ConceptoPresupuesto persistido in conceptosPersistidos)
        {
            if (persistido.TipoConceptoId == TipoConcepto.MargenId)
            {
                yaExisteMargenPersistido = true;
            }
            else
            {
                tiposPresentesSinMargen.Add(persistido.TipoConceptoId);
            }
        }

        foreach (ConceptoBorradorDto fila in borrador.Conceptos)
        {
            if (fila.MaterialProveedorId.HasValue)
            {
                if (!referenciasFinalesDelPresupuesto.Add(fila.MaterialProveedorId.Value))
                {
                    return ResultadoGuardarBorrador.ReferenciaDuplicada;
                }
            }

            if (fila.EsNuevo)
            {
                if (fila.TipoConceptoId == TipoConcepto.MargenId)
                {
                    if (yaExisteMargenPersistido || margenNuevo != null)
                    {
                        return ResultadoGuardarBorrador.MargenExistente;
                    }

                    margenNuevo = fila;
                }
                else
                {
                    conceptosNuevosSinMargen.Add(fila);
                    tiposPresentesSinMargen.Add(fila.TipoConceptoId);
                }

                if (fila.MaterialProveedorId.HasValue && fila.MaterialId.HasValue)
                {
                    referenciasAValidar[fila.MaterialProveedorId.Value] = fila.MaterialId.Value;
                    referenciasFinalesDeCambios.Add(fila.MaterialProveedorId.Value);
                }
            }
            else if (fila.FueModificado)
            {
                if (!persistidosPorId.TryGetValue(fila.Id, out ConceptoPresupuesto? entidadExistente))
                {
                    return ResultadoGuardarBorrador.ConflictoConcurrencia;
                }

                // Actualmente solo las filas de tipo Material se modifican al seleccionar o cambiar
                // su referencia de proveedor o actualizar sus valores históricos.
                if (entidadExistente.TipoConceptoId != TipoConcepto.MaterialId
                    || fila.TipoConceptoId != TipoConcepto.MaterialId)
                {
                    return ResultadoGuardarBorrador.DatosInvalidos;
                }

                conceptosModificados.Add(fila);

                if (fila.MaterialProveedorId.HasValue && fila.MaterialId.HasValue)
                {
                    referenciasAValidar[fila.MaterialProveedorId.Value] = fila.MaterialId.Value;
                    referenciasFinalesDeCambios.Add(fila.MaterialProveedorId.Value);
                }
            }
        }

        // Validamos que los tipos seleccionados como base del margen nuevo existan en el presupuesto.
        if (margenNuevo != null)
        {
            foreach (int tipoBaseId in margenNuevo.TiposBaseMargen)
            {
                if (!tiposPresentesSinMargen.Contains(tipoBaseId))
                {
                    return ResultadoGuardarBorrador.TiposBaseMargenNoDisponibles;
                }
            }
        }

        // Comprobamos sin seguimiento que todas las referencias del catálogo elegidas existan y
        // correspondan exactamente al material indicado (integridad referencial y sin tocar el catálogo).
        if (referenciasAValidar.Count > 0)
        {
            List<int> idsReferenciasAValidar = new List<int>(referenciasAValidar.Keys);

            Dictionary<int, int> materialesPorReferenciaEnBase = await _contexto.MaterialProveedores
                .AsNoTracking()
                .Where(referencia => idsReferenciasAValidar.Contains(referencia.Id))
                .ToDictionaryAsync(
                    referencia => referencia.Id,
                    referencia => referencia.MaterialId,
                    cancellationToken);

            foreach (KeyValuePair<int, int> par in referenciasAValidar)
            {
                if (!materialesPorReferenciaEnBase.TryGetValue(par.Key, out int materialIdEnBase)
                    || materialIdEnBase != par.Value)
                {
                    return ResultadoGuardarBorrador.ReferenciaInexistente;
                }
            }
        }

        // Si alguna fila modificada cede una referencia que otra fila del mismo borrador pasa a usar
        // (por ejemplo, un intercambio de proveedores entre dos filas), limpiamos primero las
        // referencias en conflicto dentro de la misma transacción para evitar choques transitorios
        // con el índice único filtrado IX_ConceptosPresupuesto_PresupuestoId_MaterialProveedorId.
        var filasParaDespejar = new List<ConceptoPresupuesto>();
        foreach (ConceptoBorradorDto filaModificada in conceptosModificados)
        {
            ConceptoPresupuesto entidad = persistidosPorId[filaModificada.Id];
            if (entidad.MaterialProveedorId.HasValue
                && entidad.MaterialProveedorId != filaModificada.MaterialProveedorId
                && referenciasFinalesDeCambios.Contains(entidad.MaterialProveedorId.Value))
            {
                filasParaDespejar.Add(entidad);
            }
        }

        try
        {
            if (filasParaDespejar.Count > 0)
            {
                foreach (ConceptoPresupuesto entidad in filasParaDespejar)
                {
                    entidad.MaterialId = null;
                    entidad.MaterialProveedorId = null;
                }

                await _contexto.SaveChangesAsync(cancellationToken);
            }

            // Aplicamos las actualizaciones sobre las filas persistidas que cambiaron.
            // Conservamos ConceptoDetectado y Cantidad sin tocarlos.
            foreach (ConceptoBorradorDto filaModificada in conceptosModificados)
            {
                ConceptoPresupuesto entidad = persistidosPorId[filaModificada.Id];
                entidad.MaterialId = filaModificada.MaterialId;
                entidad.MaterialProveedorId = filaModificada.MaterialProveedorId;
                entidad.Concepto = filaModificada.Concepto;
                entidad.Unidad = filaModificada.Unidad;
                entidad.PrecioUnitario = filaModificada.PrecioUnitario;
                entidad.Moneda = filaModificada.Moneda;
            }

            // Insertamos las filas nuevas que no son Margen.
            foreach (ConceptoBorradorDto filaNueva in conceptosNuevosSinMargen)
            {
                var nuevaEntidad = new ConceptoPresupuesto
                {
                    PresupuestoId = borrador.PresupuestoId,
                    TipoConceptoId = filaNueva.TipoConceptoId,
                    Concepto = filaNueva.Concepto,
                    ConceptoDetectado = filaNueva.ConceptoDetectado,
                    MaterialId = filaNueva.MaterialId,
                    MaterialProveedorId = filaNueva.MaterialProveedorId,
                    Cantidad = filaNueva.Cantidad,
                    Unidad = filaNueva.Unidad,
                    PrecioUnitario = filaNueva.PrecioUnitario,
                    Moneda = filaNueva.Moneda
                };

                _contexto.ConceptosPresupuesto.Add(nuevaEntidad);
            }

            // Si hay un margen nuevo, guardamos primero los conceptos para obtener el Id del margen
            // y luego registramos sus tipos base en la misma transacción atómica.
            if (margenNuevo != null)
            {
                var entidadMargen = new ConceptoPresupuesto
                {
                    PresupuestoId = borrador.PresupuestoId,
                    TipoConceptoId = TipoConcepto.MargenId,
                    PorcentajeMargen = margenNuevo.PorcentajeMargen
                };

                _contexto.ConceptosPresupuesto.Add(entidadMargen);
                await _contexto.SaveChangesAsync(cancellationToken);

                foreach (int tipoBaseId in margenNuevo.TiposBaseMargen)
                {
                    _contexto.MargenesBaseTipo.Add(new MargenBaseTipo
                    {
                        ConceptoPresupuestoId = entidadMargen.Id,
                        TipoConceptoId = tipoBaseId
                    });
                }

                await _contexto.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await _contexto.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateException excepcion) when (
            IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.ConceptosPresupuestoMaterialProveedor))
        {
            _contexto.ChangeTracker.Clear();
            return ResultadoGuardarBorrador.ReferenciaDuplicada;
        }
        catch (DbUpdateException excepcion) when (
            IndicesUnicos.RepitioElIndice(excepcion, IndicesUnicos.ConceptosPresupuestoUnicoMargen))
        {
            _contexto.ChangeTracker.Clear();
            return ResultadoGuardarBorrador.MargenExistente;
        }

        await transaccion.CommitAsync(cancellationToken);
        return ResultadoGuardarBorrador.Guardado;
    }

    // Bloquea la fila del presupuesto durante la transacción con UPDLOCK y sin seguimiento
    // (AsNoTracking): serializa guardados simultáneos y garantiza que nunca se altere el presupuesto.
    private Task<Presupuesto?> ObtenerPresupuestoBloqueadoAsync(
        int presupuestoId, CancellationToken cancellationToken)
    {
        return _contexto.Presupuestos
            .FromSqlInterpolated($"SELECT * FROM Presupuestos WITH (UPDLOCK) WHERE Id = {presupuestoId}")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }
}
