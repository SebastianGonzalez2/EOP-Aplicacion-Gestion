
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;

namespace EOP.Gestion.Core.Interfaces.Presupuestos;

public interface IRepositorioMargenPresupuesto
{
    Task<ResultadoRegistrarMargen> RegistrarSiPuedeEditarAsync(int presupuestoId,decimal porcentaje,IReadOnlyCollection<int> tiposBase,Usuario usuario,CancellationToken cancellationToken);
}
