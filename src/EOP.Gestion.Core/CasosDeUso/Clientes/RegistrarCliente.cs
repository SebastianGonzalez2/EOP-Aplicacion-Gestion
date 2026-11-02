
using EOP.Gestion.Core.DTOs.Clientes;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;

namespace EOP.Gestion.Core.CasosDeUso.Clientes;

public class RegistrarCliente
{
    // Caso de uso que valida los datos del cliente.
    private readonly ValidarDatosCliente _validarDatosCliente;

    // Repositorio que permite guardar el cliente.
    private readonly IRepositorioClientes _repositorioClientes;

    // Servicio que arma el número internacional según las reglas de cada país.
    private readonly IServicioTelefonos _servicioTelefonos;

    // Recibimos las dependencias mediante el constructor.
    public RegistrarCliente(ValidarDatosCliente validarDatosCliente,IRepositorioClientes repositorioClientes,IServicioTelefonos servicioTelefonos)
    {
        _validarDatosCliente = validarDatosCliente;
        _repositorioClientes = repositorioClientes;
        _servicioTelefonos = servicioTelefonos;
    }


    // Valida y registra un nuevo cliente.
    // El país y el número nacional son datos del formulario: vienen aparte porque no se
    // guardan. Lo que se guarda es el número internacional que armamos con ellos, en Cliente.NumeroWhatsApp.
    public async Task<List<string>> EjecutarAsync(Cliente cliente,string? codigoPaisWhatsApp,string? numeroNacionalWhatsApp,CancellationToken cancellationToken = default)
    {
        ResultadoNumeroInternacional whatsApp = _servicioTelefonos.ConvertirAInternacional(codigoPaisWhatsApp, numeroNacionalWhatsApp);

        if (whatsApp.NumeroInternacional != null)
        {
            cliente.NumeroWhatsApp = whatsApp.NumeroInternacional;
        }

        // Reutilizamos las validaciones implementadas en T19.
        List<string> errores = await _validarDatosCliente.ValidarAsync(cliente, whatsApp.Error, cancellationToken);

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
