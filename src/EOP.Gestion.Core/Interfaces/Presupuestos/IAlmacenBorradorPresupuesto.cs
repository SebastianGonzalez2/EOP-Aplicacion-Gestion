using EOP.Gestion.Core.DTOs.Presupuestos;

namespace EOP.Gestion.Core.Interfaces.Presupuestos;

// Almacén temporal de cambios pendientes de un presupuesto durante la elaboración (US-003, RF-05).
//
// Los cambios viven separados de SQL Server y aislados por usuario y presupuesto hasta que el
// usuario presiona explícitamente «Guardar borrador» o los descarta (preparado para S3-T15).
public interface IAlmacenBorradorPresupuesto
{
    BorradorPresupuestoDto? Obtener(int presupuestoId, int usuarioId);

    void Guardar(int presupuestoId, int usuarioId, BorradorPresupuestoDto borrador);

    void Eliminar(int presupuestoId, int usuarioId);

    bool TieneCambiosPendientes(int presupuestoId, int usuarioId);
}
