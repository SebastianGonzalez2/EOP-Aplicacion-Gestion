
using System.Data;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Infrastructure.Persistencia.Repositorios.Presupuestos;

public class RepositorioMargenPresupuesto : IRepositorioMargenPresupuesto
{
    private readonly EopDbContext _contexto;

    public RepositorioMargenPresupuesto(EopDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<ResultadoRegistrarMargen> RegistrarSiPuedeEditarAsync(
        int presupuestoId,
        decimal porcentaje,
        IReadOnlyCollection<int> tiposBase,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        // Guardamos el margen y sus tipos de base
        // dentro de una misma transacción.
        await using var transaccion =
            await _contexto.Database.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, cancellationToken);

        // Comprobamos nuevamente el usuario y sus permisos.
        var usuarioActual = await _contexto.Usuarios.AsNoTracking()
            .SingleOrDefaultAsync(
                actual => actual.Id == usuario.Id,
                cancellationToken);

        var presupuesto = await _contexto.Presupuestos.AsNoTracking()
            .SingleOrDefaultAsync(
                actual => actual.Id == presupuestoId,
                cancellationToken);

        if (usuarioActual == null
            || usuarioActual.EntraObjectId != usuario.EntraObjectId
            || presupuesto == null
            || !PermisosConceptosPresupuesto.PuedeEditar(
                presupuesto, usuarioActual))
        {
            return ResultadoRegistrarMargen.EdicionNoPermitida;
        }

        // RF-27: un presupuesto no puede tener dos márgenes.
        bool tieneMargen = await _contexto.ConceptosPresupuesto
            .AnyAsync(
                concepto => concepto.PresupuestoId == presupuestoId
                    && concepto.TipoConceptoId == TipoConcepto.MargenId,
                cancellationToken);

        if (tieneMargen)
        {
            return ResultadoRegistrarMargen.MargenExistente;
        }

        // Consultamos los tipos presentes en este presupuesto.
        // No confiamos únicamente en los Id enviados por el formulario.
        var tiposDisponibles = await _contexto.ConceptosPresupuesto
            .AsNoTracking()
            .Where(concepto =>
                concepto.PresupuestoId == presupuestoId
                && concepto.TipoConceptoId != TipoConcepto.MargenId)
            .Select(concepto => concepto.TipoConceptoId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (tiposBase.Any(tipo => !tiposDisponibles.Contains(tipo)))
        {
            return ResultadoRegistrarMargen.TiposNoDisponibles;
        }

        // Creamos el concepto Margen con su porcentaje.
        var margen = new ConceptoPresupuesto
        {
            PresupuestoId = presupuestoId,
            TipoConceptoId = TipoConcepto.MargenId,
            PorcentajeMargen = porcentaje
        };

        _contexto.ConceptosPresupuesto.Add(margen);

        try
        {
            // Guardamos primero el margen para obtener su Id.
            await _contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excepcion) when (
            IndicesUnicos.RepitioElIndice(
                excepcion,
                IndicesUnicos.ConceptosPresupuestoUnicoMargen))
        {
            // El índice único impide que se registre otro margen.
            _contexto.Entry(margen).State = EntityState.Detached;
            return ResultadoRegistrarMargen.MargenExistente;
        }

        // Guardamos cada tipo seleccionado en la tabla MargenesBaseTipo.
        foreach (int tipoId in tiposBase)
        {
            _contexto.MargenesBaseTipo.Add(new MargenBaseTipo
            {
                ConceptoPresupuestoId = margen.Id,
                TipoConceptoId = tipoId
            });
        }

        await _contexto.SaveChangesAsync(cancellationToken);

        // Confirmamos todos los cambios juntos.
        await transaccion.CommitAsync(cancellationToken);

        return ResultadoRegistrarMargen.Guardado;
    }
}
