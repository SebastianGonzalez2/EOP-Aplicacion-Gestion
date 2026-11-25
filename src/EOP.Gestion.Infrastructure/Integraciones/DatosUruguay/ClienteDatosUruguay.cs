using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EOP.Gestion.Core.Comun;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;

/// <summary>
/// Habla con la API de datosUruguay: pide UNA página de cotizaciones del dólar, revisa que la
/// respuesta tenga sentido y la devuelve ya convertida a nuestros tipos.
/// </summary>
/// <remarks>
/// Es el único lugar del sistema que conoce la dirección, los parámetros y el formato JSON del
/// proveedor. Todo lo que puede salir mal en la comunicación (sin conexión, demora, códigos de
/// error, JSON roto) se traduce acá a <see cref="CotizacionNoDisponibleException"/>, para que el
/// resto de la aplicación no tenga que saber nada de HttpClient.
///
/// No decide qué cotización usar ni guarda nada en caché: eso lo hacen
/// <see cref="BuscadorCotizacionesDatosUruguay"/> y <see cref="ServicioCotizacionesDatosUruguay"/>.
///
/// La API es pública, gratuita y no usa clave, así que acá no hay ningún secreto (RNF-11).
/// </remarks>
public sealed class ClienteDatosUruguay
{
    // Dirección base de la API. Termina en "/" para que las rutas relativas se agreguen al final.
    public const string UrlBase = "https://datosuruguay.com/api/v1/";

    // Cuánto esperamos como máximo una respuesta. Si el proveedor tarda más, preferimos avisar
    // que la cotización no está disponible antes que dejar al usuario esperando (RNF-13).
    public static readonly TimeSpan TiempoMaximoDeEspera = TimeSpan.FromSeconds(10);

    private const string RutaCotizacionesUsd = "exchange-rates/usd";
    private const string FuenteEsperada = "BCU";
    private const string UnidadEsperada = "UYU/USD";
    private const string SerieEsperada = "usd";
    private const string FormatoFecha = "yyyy-MM-dd";

    // Opciones "General" (estrictas) en lugar de las "Web" que ReadFromJsonAsync usa si no se le
    // pasa nada: las Web aceptan un número escrito como texto ("40.1"). El proveedor documenta que
    // el valor es siempre un número JSON, así que cualquier otra cosa es una respuesta inesperada.
    // Se crea una sola vez porque System.Text.Json guarda información interna en estas opciones.
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.General);

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClienteDatosUruguay> _logger;

    // El HttpClient nos lo da ASP.NET Core (IHttpClientFactory) ya configurado con la dirección
    // base y el tiempo máximo de espera: ver InyeccionDeDependencias.
    public ClienteDatosUruguay(HttpClient httpClient, ILogger<ClienteDatosUruguay> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Pide una página de cotizaciones publicadas entre <paramref name="desde"/> y
    /// <paramref name="hasta"/> (ambas incluidas). Si <paramref name="desde"/> es null, no hay
    /// límite hacia atrás. El proveedor las devuelve de la más reciente a la más antigua.
    /// </summary>
    /// <param name="cantidadPorPagina">Cuántas publicaciones pedir (la API acepta de 1 a 5000).</param>
    /// <param name="desplazamiento">Cuántas publicaciones saltear, para pedir las páginas siguientes.</param>
    /// <exception cref="CotizacionNoDisponibleException">Falló la comunicación o la respuesta no es válida.</exception>
    /// <exception cref="OperationCanceledException">Quien llamó canceló la operación.</exception>
    public async Task<PaginaCotizacionesDatosUruguay> ObtenerPaginaAsync(
        DateOnly? desde,
        DateOnly hasta,
        int cantidadPorPagina,
        int desplazamiento,
        CancellationToken cancellationToken)
    {
        string ruta = ArmarRuta(desde, hasta, cantidadPorPagina, desplazamiento);

        // RNF-12: registramos cada llamada al servicio externo. La ruta solo tiene fechas y números
        // de página, no hay ningún dato sensible.
        _logger.LogInformation("Consultando cotizaciones en datosUruguay: {Ruta}", ruta);

        HttpResponseMessage respuesta = await EnviarSolicitudAsync(ruta, cancellationToken);

        // "using" asegura que la respuesta se libere aunque salga una excepción más abajo.
        using (respuesta)
        {
            VerificarCodigoDeEstado(respuesta, ruta);

            RespuestaDatosUruguay? cuerpo = await LeerCuerpoAsync(respuesta, ruta, cancellationToken);

            return ConvertirEnPagina(cuerpo, ruta);
        }
    }

    // Arma algo como "exchange-rates/usd?from=2026-11-10&to=2026-11-24&limit=100&offset=0".
    // Las fechas se escriben siempre con cultura invariante, así no dependen del idioma del servidor.
    private static string ArmarRuta(DateOnly? desde, DateOnly hasta, int cantidadPorPagina, int desplazamiento)
    {
        var parametros = new List<string>();

        if (desde.HasValue)
        {
            parametros.Add("from=" + desde.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        }

        parametros.Add("to=" + hasta.ToString(FormatoFecha, CultureInfo.InvariantCulture));
        parametros.Add("limit=" + cantidadPorPagina.ToString(CultureInfo.InvariantCulture));
        parametros.Add("offset=" + desplazamiento.ToString(CultureInfo.InvariantCulture));

        return RutaCotizacionesUsd + "?" + string.Join("&", parametros);
    }

    private async Task<HttpResponseMessage> EnviarSolicitudAsync(string ruta, CancellationToken cancellationToken)
    {
        try
        {
            // GetAsync, por defecto, espera a tener la respuesta completa. Así el tiempo máximo de
            // espera del HttpClient cubre también la descarga del cuerpo, no solo los encabezados.
            return await _httpClient.GetAsync(ruta, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // La canceló quien llamó (por ejemplo, el usuario cerró la página). No es una falla del
            // proveedor, así que la dejamos seguir tal cual y no la registramos como error.
            throw;
        }
        catch (OperationCanceledException excepcion)
        {
            // Si nadie canceló pero igual se canceló, es porque se agotó el tiempo máximo de espera
            // del HttpClient (así lo informa .NET: TaskCanceledException con un TimeoutException adentro).
            _logger.LogWarning(excepcion,
                "datosUruguay no respondió dentro del tiempo máximo de espera ({Segundos} s). Ruta: {Ruta}",
                TiempoMaximoDeEspera.TotalSeconds, ruta);
            throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.TiempoDeEsperaAgotado, excepcion);
        }
        catch (HttpRequestException excepcion)
        {
            // No se pudo ni siquiera hablar con el servidor: sin internet, falla de DNS, conexión rechazada...
            _logger.LogWarning(excepcion, "No se pudo conectar con datosUruguay. Ruta: {Ruta}", ruta);
            throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.FallaDeConexion, excepcion);
        }
    }

    private void VerificarCodigoDeEstado(HttpResponseMessage respuesta, string ruta)
    {
        if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // HTTP 429: el proveedor limita la cantidad de consultas por IP (20 cada 10 segundos y
            // 300 por hora). No reintentamos acá: reintentar enseguida empeora el problema. Anotamos
            // cuánto pide esperar (Retry-After) para poder diagnosticarlo.
            string segundosDeEspera = "no informado";
            if (respuesta.Headers.RetryAfter != null && respuesta.Headers.RetryAfter.Delta.HasValue)
            {
                segundosDeEspera = respuesta.Headers.RetryAfter.Delta.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }

            _logger.LogWarning(
                "datosUruguay rechazó la consulta por exceso de solicitudes (HTTP 429). Pide esperar: {SegundosDeEspera} s. Ruta: {Ruta}",
                segundosDeEspera, ruta);
            throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.LimiteDeSolicitudesExcedido, null);
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            // Cualquier otro error (400, 500, 503...). Un 400 indicaría un error nuestro al armar la
            // ruta; igual, para quien pidió la cotización el resultado es el mismo: no se pudo obtener.
            _logger.LogError(
                "datosUruguay respondió con un código de error HTTP {CodigoHttp}. Ruta: {Ruta}",
                (int)respuesta.StatusCode, ruta);
            throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.ErrorDelProveedor, null);
        }
    }

    private async Task<RespuestaDatosUruguay?> LeerCuerpoAsync(
        HttpResponseMessage respuesta,
        string ruta,
        CancellationToken cancellationToken)
    {
        try
        {
            return await respuesta.Content.ReadFromJsonAsync<RespuestaDatosUruguay>(OpcionesJson, cancellationToken);
        }
        catch (JsonException excepcion)
        {
            // No es JSON, o tiene un tipo de dato inesperado (por ejemplo, un valor escrito como texto).
            _logger.LogError(excepcion, "datosUruguay respondió un JSON que no se puede interpretar. Ruta: {Ruta}", ruta);
            throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.RespuestaInvalida, excepcion);
        }
        catch (NotSupportedException excepcion)
        {
            // El proveedor respondió con un tipo de contenido que no es JSON (por ejemplo, una página HTML).
            _logger.LogError(excepcion, "datosUruguay respondió con un contenido que no es JSON. Ruta: {Ruta}", ruta);
            throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.RespuestaInvalida, excepcion);
        }
    }

    // Revisa que la respuesta tenga todo lo que necesitamos y la pasa a nuestros tipos.
    // Si falta algo, preferimos no usar la respuesta entera antes que adivinar.
    private PaginaCotizacionesDatosUruguay ConvertirEnPagina(RespuestaDatosUruguay? cuerpo, string ruta)
    {
        if (cuerpo == null || cuerpo.Datos == null || cuerpo.Meta == null || !cuerpo.Meta.HayMas.HasValue)
        {
            throw RespuestaInvalida(ruta, "faltan 'data', 'meta' o 'meta.has_more'");
        }

        // Si el proveedor cambiara la fuente o la unidad, los números significarían otra cosa.
        // Mejor rechazar la respuesta que usar una cotización equivocada.
        if (!string.Equals(cuerpo.Meta.Fuente, FuenteEsperada, StringComparison.OrdinalIgnoreCase))
        {
            throw RespuestaInvalida(ruta, "la fuente no es el BCU");
        }

        if (!string.Equals(cuerpo.Meta.Unidad, UnidadEsperada, StringComparison.OrdinalIgnoreCase))
        {
            throw RespuestaInvalida(ruta, "la unidad no es UYU/USD");
        }

        var registros = new List<RegistroCotizacionDatosUruguay>();

        foreach (PuntoDatosUruguay? punto in cuerpo.Datos)
        {
            registros.Add(ConvertirPunto(punto, ruta));
        }

        bool hayMasPaginas = cuerpo.Meta.HayMas.Value;

        // Si dice que hay más páginas pero esta vino vacía, pedir la siguiente nos dejaría dando
        // vueltas para siempre en la misma posición.
        if (hayMasPaginas && registros.Count == 0)
        {
            throw RespuestaInvalida(ruta, "anuncia más páginas pero la página vino vacía");
        }

        return new PaginaCotizacionesDatosUruguay(registros, hayMasPaginas);
    }

    private RegistroCotizacionDatosUruguay ConvertirPunto(PuntoDatosUruguay? punto, string ruta)
    {
        if (punto == null || punto.Fecha == null || !punto.Valor.HasValue)
        {
            throw RespuestaInvalida(ruta, "hay una publicación sin fecha o sin valor");
        }

        if (!string.Equals(punto.Serie, SerieEsperada, StringComparison.OrdinalIgnoreCase))
        {
            throw RespuestaInvalida(ruta, "hay una publicación que no es del dólar");
        }

        bool fechaValida = DateOnly.TryParseExact(
            punto.Fecha,
            FormatoFecha,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly fecha);

        if (!fechaValida)
        {
            throw RespuestaInvalida(ruta, "hay una publicación con una fecha que no tiene el formato AAAA-MM-DD");
        }

        // Acá NO descartamos valores cero o negativos: la página es válida igual, y esa decisión
        // le corresponde al buscador (que además lo deja registrado).
        return new RegistroCotizacionDatosUruguay(fecha, punto.Valor.Value);
    }

    private CotizacionNoDisponibleException RespuestaInvalida(string ruta, string problema)
    {
        _logger.LogError("datosUruguay respondió con datos incompletos o inesperados: {Problema}. Ruta: {Ruta}", problema, ruta);
        return new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.RespuestaInvalida, null);
    }
}
