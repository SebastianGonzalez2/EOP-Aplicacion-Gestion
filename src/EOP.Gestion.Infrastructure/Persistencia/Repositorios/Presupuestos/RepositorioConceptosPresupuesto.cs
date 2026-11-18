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
}
