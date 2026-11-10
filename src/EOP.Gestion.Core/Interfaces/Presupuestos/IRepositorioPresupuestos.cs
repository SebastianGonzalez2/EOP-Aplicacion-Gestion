using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Core.Interfaces.Presupuestos
{
    public interface IRepositorioPresupuestos
    {
        // Guarda un presupuesto nuevo y le asigna su código interno (RF-01).
        //
        // Pide el siguiente correlativo global a la base, arma el código con
        // GeneradorCodigoPresupuesto y completa Codigo y NumeroCorrelativo del presupuesto que
        // recibe. El que llama NO los informa: el código nunca lo elige el usuario.
        // Si el presupuesto no se puede guardar (por ejemplo, el cliente no existe) lanza la
        // excepción de la base y no queda nada guardado.
        Task AgregarConCodigoAsignadoAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default);
    }
}
