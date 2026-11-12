using System.Text.RegularExpressions;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Almacenamiento;
using EOP.Gestion.Core.Interfaces.Almacenamiento;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Infrastructure.Almacenamiento;

/// <summary>
/// Guarda y recupera archivos privados en un contenedor de Azure Blob Storage.
/// </summary>
public sealed class ServicioAlmacenamientoBlob : IServicioAlmacenamiento
{
    //esto es un comentario de prueba para ver si se sube a git
    private const string TipoContenidoPorDefecto = "application/octet-stream";

    private const string MensajeAlmacenamientoNoDisponible =
        "No se pudo acceder al almacenamiento de archivos. Intentá de nuevo más tarde.";

    // La carpeta: partes en minúsculas, números y guiones. Sin barras ni puntos, para que nadie
    // pueda "salirse" de su carpeta con algo como "../".
    private static readonly Regex FormatoCarpeta = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    // La extensión: punto + hasta 10 letras o números (".pdf", ".dwg", ".xlsx").
    private static readonly Regex FormatoExtension = new(@"^\.[a-z0-9]{1,10}$", RegexOptions.Compiled);

    // Tipo MIME simple, como "application/pdf". Se valida porque va en un encabezado HTTP.
    private static readonly Regex FormatoTipoContenido = new(@"^[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]*/[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]*$", RegexOptions.Compiled);

    // Es exactamente la forma de lo que genera GuardarAsync: carpeta/32 caracteres hexadecimales + extensión opcional.
    private static readonly Regex FormatoReferencia = new(@"^[a-z0-9]+(-[a-z0-9]+)*/[0-9a-f]{32}(\.[a-z0-9]{1,10})?$", RegexOptions.Compiled);

    //BlobContainerClient bien facil es un cliente de Azure que nos permite interactuar con un contenedor de blobs osea un contenedor de archivos en Azure.
    //Con este cliente podemos subir, descargar, eliminar y listar archivos en el contenedor de blob
    private readonly BlobContainerClient _contenedor;

    //El logger es para registrar eventos, errores y mensajes de información en la aplicación.
    //Nos ayuda a monitorear el comportamiento del servicio de almacenamiento y a depurar problemas.
    private readonly ILogger<ServicioAlmacenamientoBlob> _logger;

    public ServicioAlmacenamientoBlob(
        BlobContainerClient contenedor,
        ILogger<ServicioAlmacenamientoBlob> logger)
    {
        _contenedor = contenedor;
        _logger = logger;
    }

    //este metodo lo que hace basicamente es guardar un archivo en Azure Blob Storage y devolver una referencia única para ese archivo.
    public async Task<string> GuardarAsync(
        ArchivoParaGuardar archivo,
        CancellationToken cancellationToken = default)
    {
        // 1. Validamos los datos antes de hablar con Azure.
        ValidarArchivoParaGuardar(archivo);

        // 2. Armamos el nombre con el que va a quedar guardado.
        string referencia = CrearReferenciaNueva(archivo);

        // 3. Subimos el archivo.
        try
        {
            // BlobClient es un cliente de Azure que representa un archivo específico dentro del contenedor de blob. 
            // Lo que hacemos aca bien facil es obtener un cliente para el archivo que vamos a subir, usando la referencia que generamos.
            // Un cliente es como un "manejador" que nos permite interactuar con ese archivo en particular: subirlo, descargarlo, eliminarlo, etc.
            BlobClient blob = _contenedor.GetBlobClient(referencia);


            // esto de opciones de subida bien facil es para configurar cómo queremos subir el archivo.
            // Por ejemplo, le decimos que tipo de contenido tiene (como "application/pdf") y
            // que no queremos sobrescribir un archivo que ya exista con el mismo nombre.
            var opcionesDeSubida = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = archivo.TipoContenido },

                // "If-None-Match: *" significa "guardalo solo si todavía no existe". Con el nombre
                // aleatorio ya es casi imposible pisar otro archivo, pero así queda garantizado:
                // los archivos originales no se sobrescriben (las bajas son lógicas).
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
            };

            await blob.UploadAsync(archivo.Contenido, opcionesDeSubida, cancellationToken);

            _logger.LogInformation(
                "Archivo guardado en Blob Storage. Carpeta: {Carpeta}. Referencia: {Referencia}.",
                archivo.Carpeta,
                referencia);

            return referencia;
        }
        catch (Exception falla) when (EsFallaDeAzure(falla))
        {
            throw ConvertirFalla(falla, "guardar", referencia);
        }
    }

    //este metodo lo que hace basicamente es recuperar un archivo de Azure Blob Storage usando la referencia que se guardó en la base de datos.
    public async Task<ArchivoDescargado?> ObtenerAsync(
        string referencia,
        CancellationToken cancellationToken = default)
    {
        ValidarReferencia(referencia);

        try
        {
            BlobClient blob = _contenedor.GetBlobClient(referencia);

            // Descarga en streaming: el archivo se va leyendo a medida que se consume,
            // sin cargar todo el contenido (hasta 100 MB) en la memoria del servidor.
            Response<BlobDownloadStreamingResult> respuesta =
                await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);

            string tipoContenido = respuesta.Value.Details.ContentType;
            if (string.IsNullOrWhiteSpace(tipoContenido))
            {
                tipoContenido = TipoContenidoPorDefecto;
            }

            _logger.LogInformation(
                "Archivo recuperado de Blob Storage. Referencia: {Referencia}.",
                referencia);

            return new ArchivoDescargado(respuesta.Value.Content, tipoContenido);
        }
        catch (RequestFailedException falla) when (falla.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            // Que el archivo no exista es un resultado esperable, no una falla del servicio.
            // Ojo: se mira el código de error y no solo el 404, porque un contenedor
            // inexistente también responde 404 y eso sí es un problema de configuración.
            _logger.LogWarning(
                "No se encontró el archivo en Blob Storage. Referencia: {Referencia}.",
                referencia);

            return null;
        }
        catch (Exception falla) when (EsFallaDeAzure(falla))
        {
            throw ConvertirFalla(falla, "obtener", referencia);
        }
    }

    private static void ValidarArchivoParaGuardar(ArchivoParaGuardar archivo)
    {
        ArgumentNullException.ThrowIfNull(archivo);

        if (string.IsNullOrEmpty(archivo.Carpeta) || !FormatoCarpeta.IsMatch(archivo.Carpeta))
        {
            throw new ArgumentException(
                "La carpeta solo puede tener minúsculas, números y guiones (por ejemplo \"facturas\").",
                nameof(archivo));
        }

        if (string.IsNullOrWhiteSpace(archivo.NombreOriginal))
        {
            throw new ArgumentException("El nombre original del archivo es obligatorio.", nameof(archivo));
        }

        if (string.IsNullOrEmpty(archivo.TipoContenido) || !FormatoTipoContenido.IsMatch(archivo.TipoContenido))
        {
            throw new ArgumentException(
                "El tipo de contenido no es válido (por ejemplo \"application/pdf\").",
                nameof(archivo));
        }

        if (archivo.Contenido == null || !archivo.Contenido.CanRead)
        {
            throw new ArgumentException("El contenido del archivo no se puede leer.", nameof(archivo));
        }

        // Si el flujo permite saber su tamaño, rechazamos los archivos vacíos.
        if (archivo.Contenido.CanSeek && archivo.Contenido.Length == 0)
        {
            throw new ArgumentException("El archivo está vacío.", nameof(archivo));
        }
    }

    private static void ValidarReferencia(string referencia)
    {
        // La referencia viene de la base de datos. Validamos su forma para que nunca se pueda
        // pedir un nombre de blob arbitrario, ni siquiera por un error en el código que llama.
        if (string.IsNullOrEmpty(referencia) || !FormatoReferencia.IsMatch(referencia))
        {
            throw new ArgumentException(
                "La referencia del archivo no tiene un formato válido.",
                nameof(referencia));
        }
    }

    private static string CrearReferenciaNueva(ArchivoParaGuardar archivo)
    {
        // Del nombre que trae el usuario solo conservamos la extensión (y en minúsculas). El resto
        // del nombre puede traer espacios, tildes o caracteres raros, y no hace falta para nada.
        string extension = Path.GetExtension(archivo.NombreOriginal).ToLowerInvariant();
        if (!FormatoExtension.IsMatch(extension))
        {
            extension = string.Empty;
        }

        // Guid "N" = 32 caracteres hexadecimales, sin guiones: imposible de adivinar y único.
        string nombreUnico = Guid.NewGuid().ToString("N");

        return $"{archivo.Carpeta}/{nombreUnico}{extension}";
    }

    // Las fallas que esperamos de Azure: error HTTP del servicio (sin permiso, no disponible,
    // sin red, etc.) o imposibilidad de obtener el token de Microsoft Entra ID.
    private static bool EsFallaDeAzure(Exception falla)
    {
        return falla is RequestFailedException || falla is AuthenticationFailedException;
    }

    private AlmacenamientoNoDisponibleException ConvertirFalla(Exception falla, string operacion, string referencia)
    {
        // Registramos solo datos técnicos (RNF-12): el tipo de error, el estado HTTP y el código de Azure.
        // No registramos el mensaje de la excepción ni el contenido del archivo. Tampoco hay tokens ni
        // claves para filtrar, porque nunca se usan.
        int estadoHttp = 0;
        string codigoAzure = string.Empty;
        if (falla is RequestFailedException fallaDeAzure)
        {
            estadoHttp = fallaDeAzure.Status;
            codigoAzure = fallaDeAzure.ErrorCode ?? string.Empty;
        }

        _logger.LogError(
            "Falló la operación '{Operacion}' en Blob Storage. Referencia: {Referencia}. Tipo de error: {TipoError}. Estado HTTP: {EstadoHttp}. Código de Azure: {CodigoAzure}.",
            operacion,
            referencia,
            falla.GetType().Name,
            estadoHttp,
            codigoAzure);

        // Mensaje genérico, sin detalles de Azure (RNF-13: la aplicación informa y sigue funcionando).
        return new AlmacenamientoNoDisponibleException(MensajeAlmacenamientoNoDisponible, falla);
    }
}
