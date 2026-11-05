using EOP.Gestion.Core.Interfaces.Clientes;

namespace EOP.Gestion.Core.CasosDeUso.Clientes;

using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.ServiciosDominio.Clientes;
using EOP.Gestion.Core.ServiciosDominio.Compartidos;
public class ValidarDatosCliente
{
    // Repositorio que permite comprobar si un RUT ya está registrado.
    private readonly IRepositorioClientes _repositorioClientes;

    // Recibimos el repositorio mediante el constructor.
    public ValidarDatosCliente(IRepositorioClientes repositorioClientes)
    {
        _repositorioClientes = repositorioClientes;
    }

    // Comprueba los datos del cliente antes de registrarlo.
    public Task<List<string>> ValidarAsync(Cliente cliente,CancellationToken cancellationToken = default)
    {
        return ValidarAsync(cliente, errorWhatsApp: null, cancellationToken);
    }

    // Variante para cuando quien nos llama ya intentó armar el número de WhatsApp y falló.
    // En errorWhatsApp nos pasa el motivo concreto, para mostrar ese mensaje en lugar del
    // genérico de formato internacional, que no le serviría de nada a quien escribió 099123.
    // Igual seguimos revisando el resto de los datos, así el usuario ve todos los errores juntos.
    public async Task<List<string>> ValidarAsync(Cliente cliente,string? errorWhatsApp,CancellationToken cancellationToken = default)
    {
        // Guardamos los mensajes de los errores encontrados.
        List<string> errores = new();

        // Comprobamos que el nombre sea obligatorio.
        if (string.IsNullOrWhiteSpace(cliente.Nombre))
        {
            errores.Add("El nombre del cliente es obligatorio.");
        }

        // Normalizamos el RUT para compararlo sin separadores.
        string rutNormalizado = ValidadorRut.Normalizar(cliente.Rut);

        // Comprobamos que el RUT sea válido.
        if (!ValidadorRut.EsValido(rutNormalizado))
        {
            errores.Add("El RUT ingresado no es válido.");
        }
        else if (await _repositorioClientes.ExistePorRutAsync(rutNormalizado, cancellationToken))
        {
            // Si el RUT es válido, consultamos si ya existe.
            errores.Add("Ya existe un cliente registrado con ese RUT.");
        }

        if (!ValidadorCorreos.TieneAlMenosUnCorreo(cliente.CorreoContacto,cliente.CorreoFacturacion))
        {
            errores.Add("Debe informar al menos un correo electrónico.");
        }

        // Comprobamos que el correo de contacto tenga un formato válido.
        if (!ValidadorCorreos.TieneFormatoValido(cliente.CorreoContacto))
        {
            errores.Add("El correo de contacto no tiene un formato válido.");
        }

        // Comprobamos que el correo de facturación tenga un formato válido.
        if (!ValidadorCorreos.TieneFormatoValido(cliente.CorreoFacturacion))
        {
            errores.Add("El correo de facturación no tiene un formato válido.");
        }

        if (errorWhatsApp != null)
        {
            errores.Add(errorWhatsApp);
        }
        // Comprobamos que el número de WhatsApp tenga un formato internacional válido.
        else if (!ValidadorWhatsApp.EsValido(cliente.NumeroWhatsApp))
        {
            errores.Add("El número de WhatsApp debe tener un formato internacional válido, por ejemplo +59899123456.");
        }

        // Si todos los datos son válidos, guardamos los valores normalizados
        // en el objeto Cliente para que puedan persistirse correctamente.
        if (errores.Count == 0)
        {
            cliente.Rut = rutNormalizado;
            cliente.NumeroWhatsApp = ValidadorWhatsApp.Normalizar(cliente.NumeroWhatsApp);
        }


        return errores;
    }




}