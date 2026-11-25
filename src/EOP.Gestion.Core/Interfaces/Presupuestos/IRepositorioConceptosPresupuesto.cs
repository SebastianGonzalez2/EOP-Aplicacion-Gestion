using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.Interfaces.Presupuestos;

public interface IRepositorioConceptosPresupuesto
{
    Task<Presupuesto?> ObtenerPresupuestoAsync(int presupuestoId, CancellationToken cancellationToken);

    // Verifica en la base (con el estado y rol actuales del usuario) si puede editar el presupuesto.
    Task<bool> PuedeEditarEnBaseAsync(int presupuestoId, Usuario usuario, CancellationToken cancellationToken);

    // Lee el estado persistido del presupuesto (conceptos y base de margen) sin seguimiento para
    // inicializar el borrador en trabajo y calcular su huella de concurrencia (US-003, RF-05).
    Task<BorradorPresupuestoDto?> ObtenerBorradorBaseAsync(int presupuestoId, CancellationToken cancellationToken);

    // Calcula la huella SHA-256 actual de lo persistido en SQL Server sin seguimiento.
    Task<string?> ObtenerHuellaPersistidaAsync(int presupuestoId, CancellationToken cancellationToken);

    // Lee una referencia del catálogo junto con su material y proveedor sin seguimiento.
    // Solo lectura: nunca crea ni modifica registros del catálogo (DT-11, RF-20).
    Task<ReferenciaCatalogoPresupuestoDto?> ObtenerReferenciaCatalogoAsync(
        int referenciaId, CancellationToken cancellationToken);

    // Obtiene el nombre visible de un tipo de concepto fijo (Material, Mano de obra, etc.).
    Task<string?> ObtenerNombreTipoConceptoAsync(int tipoId, CancellationToken cancellationToken);

    // US-003 (RF-05): persiste de forma atómica los cambios pendientes del borrador dentro de una
    // única transacción de SQL Server, verificando permisos, estado editable, concurrencia,
    // referencias del catálogo e invariantes de negocio.
    Task<ResultadoPersistenciaBorrador> GuardarBorradorSiPuedeEditarAsync(
        BorradorPresupuestoDto borrador, Usuario usuario, CancellationToken cancellationToken);
}
