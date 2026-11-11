
namespace EOP.Gestion.Core.Interfaces.Presupuestos
{
    public interface IConsultaReferenciasPresupuesto
    {
        // Comprueba que el cliente exista y esté activo.
        Task<bool> ClienteEstaActivoAsync(int clienteId,CancellationToken cancellationToken = default);

        // Comprueba que el tipo de proyecto exista.
        Task<bool> TipoProyectoExisteAsync(int tipoProyectoId,CancellationToken cancellationToken = default);
    }
}
