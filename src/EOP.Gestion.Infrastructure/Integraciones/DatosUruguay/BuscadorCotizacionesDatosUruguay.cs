using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Cotizaciones;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;

/// <summary>
/// Encuentra la cotización que corresponde a una fecha: la publicada ese día o, si ese día no
/// hubo publicación, la última válida anterior (RF-29, RF-077, RF-103, RF-104).
/// </summary>
/// <remarks>
/// La regla para que una publicación sirva es:
///   1. su fecha es igual o anterior a la fecha pedida (nunca una posterior), y
///   2. su valor es mayor que cero.
/// Entre todas las que sirven, se elige la de fecha más reciente. No se calculan promedios ni
/// estimaciones, y no se usa ningún otro proveedor.
///
/// La búsqueda se hace en dos pasos para no descargar años de datos en cada consulta:
///
///   Paso 1 — un rango corto: desde 14 días antes hasta la fecha pedida. Alcanza para los fines de
///   semana, los feriados y los feriados largos como Carnaval o Turismo. Casi siempre se resuelve
///   acá, con una sola solicitud.
///
///   Paso 2 — si en ese rango no hubo ninguna válida, NO concluimos que no existe (puede haber una
///   más vieja). Seguimos hacia atrás sin límite inferior, página por página, hasta encontrar una
///   válida o hasta que el proveedor diga que no quedan más resultados. Solo en ese último caso
///   respondemos que no hay publicación.
/// </remarks>
public sealed class BuscadorCotizacionesDatosUruguay
{
    // Días hacia atrás que cubre el primer rango (paso 1).
    public const int DiasDelRangoInicial = 14;

    // Cuántas publicaciones pedimos por página. Un rango de 15 días entra entero en una sola página.
    public const int CantidadPorPagina = 100;

    // Tope de páginas por búsqueda, sumando los dos pasos. El proveedor permite 20 consultas cada
    // 10 segundos por IP; con este tope una búsqueda nunca se acerca a ese límite ni se queda
    // pidiendo páginas para siempre si el proveedor respondiera algo raro. 10 páginas de 100
    // publicaciones hábiles son unos cuatro años hacia atrás.
    public const int MaximoDePaginasPorBusqueda = 10;

    private readonly ClienteDatosUruguay _cliente;
    private readonly ILogger<BuscadorCotizacionesDatosUruguay> _logger;

    public BuscadorCotizacionesDatosUruguay(
        ClienteDatosUruguay cliente,
        ILogger<BuscadorCotizacionesDatosUruguay> logger)
    {
        _cliente = cliente;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve la cotización válida para <paramref name="fechaSolicitada"/>, o null si el proveedor
    /// confirma que no existe ninguna publicación válida en o antes de esa fecha.
    /// </summary>
    /// <exception cref="CotizacionNoDisponibleException">Falló la comunicación con el proveedor.</exception>
    public async Task<CotizacionUsd?> BuscarUltimaCotizacionValidaAsync(
        DateOnly fechaSolicitada,
        CancellationToken cancellationToken)
    {
        // Lo compartimos entre los dos pasos para respetar el tope de páginas de toda la búsqueda.
        var contador = new ContadorDePaginas();

        // Paso 1: el rango corto.
        DateOnly inicioDelRango = fechaSolicitada.AddDays(-DiasDelRangoInicial);
        RegistroCotizacionDatosUruguay? encontrada = await BuscarEnRangoAsync(
            inicioDelRango, fechaSolicitada, contador, cancellationToken);

        // Paso 2: si el rango corto no alcanzó, seguimos hacia atrás desde el día anterior al rango.
        if (encontrada == null)
        {
            _logger.LogInformation(
                "No hay cotización válida entre {Desde} y {Hasta}; se busca una publicación anterior.",
                inicioDelRango, fechaSolicitada);

            DateOnly diaAnteriorAlRango = inicioDelRango.AddDays(-1);
            encontrada = await BuscarHaciaAtrasAsync(diaAnteriorAlRango, fechaSolicitada, contador, cancellationToken);
        }

        if (encontrada == null)
        {
            // El proveedor respondió bien y confirmó que no hay más resultados: no hay publicación.
            _logger.LogWarning(
                "datosUruguay no tiene ninguna cotización válida en o antes del {FechaSolicitada}.",
                fechaSolicitada);
            return null;
        }

        return new CotizacionUsd(encontrada.Valor, encontrada.Fecha, fechaSolicitada);
    }

    // Paso 1: recorre todas las páginas del rango y se queda con la válida de fecha más reciente.
    // No depende del orden en que vengan: compara las fechas.
    private async Task<RegistroCotizacionDatosUruguay?> BuscarEnRangoAsync(
        DateOnly desde,
        DateOnly hasta,
        ContadorDePaginas contador,
        CancellationToken cancellationToken)
    {
        RegistroCotizacionDatosUruguay? mejor = null;
        int desplazamiento = 0;
        bool hayMasPaginas = true;

        while (hayMasPaginas)
        {
            ContarPaginaPedida(contador, hasta);

            PaginaCotizacionesDatosUruguay pagina = await _cliente.ObtenerPaginaAsync(
                desde, hasta, CantidadPorPagina, desplazamiento, cancellationToken);

            foreach (RegistroCotizacionDatosUruguay registro in pagina.Registros)
            {
                if (!EsValida(registro, hasta))
                {
                    continue;
                }

                if (mejor == null || registro.Fecha > mejor.Fecha)
                {
                    mejor = registro;
                }
            }

            desplazamiento = desplazamiento + pagina.Registros.Count;
            hayMasPaginas = pagina.HayMasPaginas;
        }

        return mejor;
    }

    // Paso 2: pide las publicaciones hasta "hasta", sin límite hacia atrás, página por página.
    // El proveedor las devuelve de la más reciente a la más antigua, así que la primera válida
    // que aparece es la más reciente de todas y podemos cortar ahí sin pedir más páginas.
    private async Task<RegistroCotizacionDatosUruguay?> BuscarHaciaAtrasAsync(
        DateOnly hasta,
        DateOnly fechaSolicitada,
        ContadorDePaginas contador,
        CancellationToken cancellationToken)
    {
        int desplazamiento = 0;
        bool hayMasPaginas = true;

        while (hayMasPaginas)
        {
            ContarPaginaPedida(contador, fechaSolicitada);

            PaginaCotizacionesDatosUruguay pagina = await _cliente.ObtenerPaginaAsync(
                null, hasta, CantidadPorPagina, desplazamiento, cancellationToken);

            // Cortar en la primera válida solo es correcto si el orden es el prometido.
            // Si no lo fuera, podríamos devolver una cotización que no es la última: mejor avisar.
            VerificarOrdenDescendente(pagina, fechaSolicitada);

            foreach (RegistroCotizacionDatosUruguay registro in pagina.Registros)
            {
                if (EsValida(registro, fechaSolicitada))
                {
                    return registro;
                }
            }

            desplazamiento = desplazamiento + pagina.Registros.Count;
            hayMasPaginas = pagina.HayMasPaginas;
        }

        return null;
    }

    // Aplica las dos condiciones de la regla. Si descarta algo, lo deja registrado, porque una
    // cotización cero o una fecha futura indicarían un problema del proveedor que conviene ver.
    private bool EsValida(RegistroCotizacionDatosUruguay registro, DateOnly fechaSolicitada)
    {
        if (registro.Fecha > fechaSolicitada)
        {
            _logger.LogWarning(
                "Se descartó la cotización del {FechaPublicacion} porque es posterior a la fecha solicitada {FechaSolicitada}.",
                registro.Fecha, fechaSolicitada);
            return false;
        }

        if (registro.Valor <= 0)
        {
            _logger.LogWarning(
                "Se descartó la cotización del {FechaPublicacion} porque su valor ({Valor}) no es mayor que cero.",
                registro.Fecha, registro.Valor);
            return false;
        }

        return true;
    }

    private void VerificarOrdenDescendente(PaginaCotizacionesDatosUruguay pagina, DateOnly fechaSolicitada)
    {
        for (int indice = 1; indice < pagina.Registros.Count; indice++)
        {
            DateOnly fechaAnterior = pagina.Registros[indice - 1].Fecha;
            DateOnly fechaActual = pagina.Registros[indice].Fecha;

            if (fechaActual > fechaAnterior)
            {
                _logger.LogError(
                    "datosUruguay devolvió las cotizaciones fuera de orden al buscar la del {FechaSolicitada}.",
                    fechaSolicitada);
                throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.RespuestaInvalida, null);
            }
        }
    }

    // Suma una página a la cuenta de la búsqueda, y corta si ya se llegó al tope.
    private void ContarPaginaPedida(ContadorDePaginas contador, DateOnly fechaSolicitada)
    {
        if (contador.PaginasPedidas >= MaximoDePaginasPorBusqueda)
        {
            // Quedan páginas sin revisar: no sabemos si más atrás hay una cotización válida,
            // así que NO podemos responder "no hay publicación". Lo informamos como falla.
            _logger.LogError(
                "Se revisaron {CantidadPaginas} páginas sin encontrar una cotización válida para el {FechaSolicitada} y quedan más; la búsqueda se detuvo.",
                MaximoDePaginasPorBusqueda, fechaSolicitada);
            throw new CotizacionNoDisponibleException(MotivoCotizacionNoDisponible.BusquedaIncompleta, null);
        }

        contador.PaginasPedidas = contador.PaginasPedidas + 1;
    }

    // Lleva la cuenta de las páginas pedidas en una búsqueda. Es una clase (y no un int suelto)
    // para que los dos pasos compartan la misma cuenta.
    private sealed class ContadorDePaginas
    {
        public int PaginasPedidas { get; set; }
    }
}
