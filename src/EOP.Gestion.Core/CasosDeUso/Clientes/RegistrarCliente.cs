
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;

namespace EOP.Gestion.Core.CasosDeUso.Clientes;

public class RegistrarCliente
{
    // Caso de uso que valida los datos del cliente.
    private readonly ValidarDatosCliente _validarDatosCliente;

    // Repositorio que permite guardar el cliente.
    private readonly IRepositorioClientes _repositorioClientes;

    // Recibimos las dependencias mediante el constructor.
    public RegistrarCliente(ValidarDatosCliente validarDatosCliente,IRepositorioClientes repositorioClientes)
    {
        _validarDatosCliente = validarDatosCliente;
        _repositorioClientes = repositorioClientes;
    }


    // Valida y registra un nuevo cliente.
    public async Task<List<string>> EjecutarAsync(Cliente cliente,CancellationToken cancellationToken = default)
    {
        // Reutilizamos las validaciones implementadas en T19.
        List<string> errores = await _validarDatosCliente.ValidarAsync(cliente, cancellationToken);

        // Si existen errores, no guardamos el cliente.
        if (errores.Count > 0)
        {
            return errores;
        }

        // Todo cliente nuevo debe comenzar activo.
        cliente.Activo = true;

        // Guardamos el cliente utilizando el repositorio.
        await _repositorioClientes.AgregarAsync(cliente, cancellationToken);

        // Devolvemos la lista vacía porque no hubo errores de validación.
        return errores;
    }
}
