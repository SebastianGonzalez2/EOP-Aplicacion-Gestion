using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace EOP.Gestion.Infrastructure.Almacenamiento;

/// <summary>
/// Arma el cliente de Azure que apunta al contenedor privado de documentos de EOP.
/// </summary>
public static class FabricaContenedorBlob
{
    /// <summary>
    /// Crea el cliente del contenedor. No se conecta a Azure todavía: eso pasa recién en la
    /// primera operación (guardar u obtener).
    /// </summary>
    public static BlobContainerClient Crear(AlmacenamientoBlobOpciones opciones)
    {
        // Siempre HTTPS (RNF-9). Es la dirección del contenedor, sin firma ni token incluidos:
        // la autenticación va aparte, en el encabezado de cada pedido.
        var direccionContenedor = new Uri(
            $"https://{opciones.NombreCuenta}.blob.core.windows.net/{opciones.NombreContenedor}");

        TokenCredential credencial = CrearCredencial(opciones);

        return new BlobContainerClient(direccionContenedor, credencial);
    }

    // La cuenta tiene deshabilitadas las claves compartidas, así que la aplicación no puede (ni debe)
    // usar claves ni cadenas de conexión: se identifica con Microsoft Entra ID, igual que una
    // persona, pero con una identidad técnica. Esa identidad necesita el rol "Storage Blob Data
    // Contributor" sobre el contenedor.
    //
    // No confundir con el usuario que inició sesión en EOP: él se autentica contra el registro
    // de aplicación de Entra ID (otro tenant); acá hablamos de la identidad de la aplicación frente
    // al almacenamiento. Cuáles de los usuarios de EOP pueden ver un archivo lo decide cada caso de uso.
    private static TokenCredential CrearCredencial(AlmacenamientoBlobOpciones opciones)
    {
        if (opciones.UsarIdentidadAdministrada)
        {
            // Producción (Azure App Service). Sin secretos: Azure le da el token a la aplicación.
            // Usamos solo la identidad administrada, y no la cadena completa de DefaultAzureCredential,
            // para que no pruebe otros mecanismos por error y para que falle rápido si no está configurada.
            if (string.IsNullOrWhiteSpace(opciones.ClientIdIdentidadAdministrada))
            {
                return new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
            }

            return new ManagedIdentityCredential(
                ManagedIdentityId.FromUserAssignedClientId(opciones.ClientIdIdentidadAdministrada));
        }

        // Desarrollo: DefaultAzureCredential prueba, entre otras, la sesión de Visual Studio y la de
        // Azure CLI ("az login"). Hay que iniciar sesión con la cuenta institucional que tiene el rol sobre el contenedor.
        var opcionesCredencial = new DefaultAzureCredentialOptions();

        // Como el tenant de la cuenta de almacenamiento no es el del registro de la aplicación,
        // se puede indicar explícitamente cuál usar.
        if (!string.IsNullOrWhiteSpace(opciones.TenantId))
        {
            opcionesCredencial.TenantId = opciones.TenantId;
        }

        return new DefaultAzureCredential(opcionesCredencial);
    }
}
