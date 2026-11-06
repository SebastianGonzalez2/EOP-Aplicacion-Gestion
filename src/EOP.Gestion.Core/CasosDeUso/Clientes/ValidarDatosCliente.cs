using EOP.Gestion.Core.Interfaces.Clientes;

namespace EOP.Gestion.Core.CasosDeUso.Clientes;

using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.ServiciosDominio.Clientes;
using EOP.Gestion.Core.ServiciosDominio.Compartidos;
public class ValidarDatosCliente
{
    // El mismo largo que tiene la columna en ClienteConfiguracion. Si lo dejáramos pasar,
    // SQL Server rechazaría el INSERT y el usuario vería un error genérico en vez de este mensaje.
    public const int LargoMaximoNombre = 200;

    // Es público porque RegistrarCliente lo vuelve a usar cuando el RUT se repite en el momento
    // de guardar: el usuario tiene que ver el mismo mensaje en los dos casos.
    public const string MensajeRutDuplicado = "Ya existe un cliente registrado con ese RUT.";

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

        // Sacamos los espacios del principio y del final antes de medir el nombre: "  Acme  " es
        // el mismo cliente que "Acme", y no queremos que esos espacios cuenten para el largo
        // ni que queden guardados (después estorbarían en las búsquedas por nombre).
        string nombreSinEspaciosEnLosExtremos = QuitarEspaciosDeLosExtremos(cliente.Nombre);

        // Comprobamos que el nombre sea obligatorio.
        if (nombreSinEspaciosEnLosExtremos.Length == 0)
        {
            errores.Add("El nombre del cliente es obligatorio.");
        }
        else if (nombreSinEspaciosEnLosExtremos.Length > LargoMaximoNombre)
        {
            errores.Add($"El nombre del cliente no puede superar los {LargoMaximoNombre} caracteres.");
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
            errores.Add(MensajeRutDuplicado);
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
            cliente.Nombre = nombreSinEspaciosEnLosExtremos;
            cliente.Rut = rutNormalizado;
            cliente.NumeroWhatsApp = ValidadorWhatsApp.Normalizar(cliente.NumeroWhatsApp);
        }


        return errores;
    }

    // Aunque Nombre se declara como string no nulo, el formulario podría no mandarlo.
    // Tratamos ese caso como un nombre vacío, así lo frena la validación de obligatorio.
    private static string QuitarEspaciosDeLosExtremos(string? texto)
    {
        if (texto == null)
        {
            return string.Empty;
        }

        return texto.Trim();
    }


}