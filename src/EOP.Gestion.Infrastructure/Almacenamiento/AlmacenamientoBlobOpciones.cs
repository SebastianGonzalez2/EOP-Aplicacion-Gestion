using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace EOP.Gestion.Infrastructure.Almacenamiento;

/// <summary>
/// Datos de configuración para llegar a Azure Blob Storage, que se leen de la sección
/// "AlmacenamientoBlob" de la configuración.
/// </summary>
/// <remarks>
/// Acá NO hay claves de la cuenta ni cadenas de conexión (RNF-2 / RNF-11): la cuenta tiene
/// deshabilitado el acceso por clave compartida. La aplicación se identifica ante Azure con
/// una identidad de Microsoft Entra ID (ver <see cref="FabricaContenedorBlob"/>), que es
/// distinta del usuario que inició sesión en EOP.
/// Los valores de esta clase no son secretos, así que pueden ir en appsettings.json.
/// </remarks>
public sealed class AlmacenamientoBlobOpciones
{
    public const string NombreSeccion = "AlmacenamientoBlob";

    // Nombre de la cuenta de almacenamiento, por ejemplo "eopstorage245".
    public string NombreCuenta { get; init; } = string.Empty;

    // Nombre del contenedor privado, por ejemplo "documentos-eop".
    public string NombreContenedor { get; init; } = string.Empty;

    // true en Azure App Service: la aplicación usa su identidad administrada.
    // false en desarrollo: usa la sesión de Visual Studio, de Azure CLI, etc.
    public bool UsarIdentidadAdministrada { get; init; }

    // Solo si la identidad administrada es "asignada por el usuario": su Client Id.
    // Si es la del propio App Service (asignada por el sistema), se deja vacío.
    public string? ClientIdIdentidadAdministrada { get; init; }

    // Tenant de Microsoft Entra ID donde está la cuenta de almacenamiento (el institucional).
    // Es distinto del tenant donde está el registro de la aplicación que usan los usuarios para
    // iniciar sesión. En desarrollo se guarda en User Secrets; si queda vacío, se usa el
    // tenant predeterminado de la cuenta con la que se inició sesión en Visual Studio / Azure CLI.
    public string? TenantId { get; init; }

    // Reglas de Azure para los nombres: cuenta de 3 a 24 caracteres entre minúsculas y números;
    // contenedor de 3 a 63 entre minúsculas, números y guiones (sin guiones al principio, al final ni seguidos).
    private static readonly Regex FormatoNombreCuenta = new("^[a-z0-9]{3,24}$", RegexOptions.Compiled);
    private static readonly Regex FormatoNombreContenedor = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    /// <summary>
    /// Lee la sección "AlmacenamientoBlob" y valida que los datos tengan sentido.
    /// Lanza <see cref="InvalidOperationException"/> con un mensaje que dice qué falta.
    /// </summary>
    public static AlmacenamientoBlobOpciones Leer(IConfiguration configuration)
    {
        IConfigurationSection seccion = configuration.GetSection(NombreSeccion);

        // Si no dice nada, usamos la opción más segura para producción: la identidad administrada.
        bool usarIdentidadAdministrada = true;
        string? textoUsarIdentidadAdministrada = seccion["UsarIdentidadAdministrada"];
        if (!string.IsNullOrWhiteSpace(textoUsarIdentidadAdministrada))
        {
            if (!bool.TryParse(textoUsarIdentidadAdministrada, out usarIdentidadAdministrada))
            {
                throw new InvalidOperationException(
                    $"'{NombreSeccion}:UsarIdentidadAdministrada' debe ser true o false.");
            }
        }

        var opciones = new AlmacenamientoBlobOpciones
        {
            NombreCuenta = (seccion["NombreCuenta"] ?? string.Empty).Trim(),
            NombreContenedor = (seccion["NombreContenedor"] ?? string.Empty).Trim(),
            UsarIdentidadAdministrada = usarIdentidadAdministrada,
            ClientIdIdentidadAdministrada = seccion["ClientIdIdentidadAdministrada"],
            TenantId = seccion["TenantId"]
        };

        opciones.Validar();
        return opciones;
    }

    private void Validar()
    {
        // Validamos el formato porque estos nombres arman la dirección de la cuenta:
        // si tuvieran cualquier otro carácter, la aplicación podría terminar hablando con otro servidor.
        if (!FormatoNombreCuenta.IsMatch(NombreCuenta))
        {
            throw new InvalidOperationException(
                $"Falta '{NombreSeccion}:NombreCuenta' o no es un nombre válido de cuenta de almacenamiento " +
                "(3 a 24 caracteres: minúsculas y números).");
        }

        bool contenedorTieneLargoValido = NombreContenedor.Length >= 3 && NombreContenedor.Length <= 63;
        if (!contenedorTieneLargoValido || !FormatoNombreContenedor.IsMatch(NombreContenedor))
        {
            throw new InvalidOperationException(
                $"Falta '{NombreSeccion}:NombreContenedor' o no es un nombre válido de contenedor " +
                "(3 a 63 caracteres: minúsculas, números y guiones).");
        }
    }
}
