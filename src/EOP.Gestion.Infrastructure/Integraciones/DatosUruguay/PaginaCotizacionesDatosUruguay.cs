namespace EOP.Gestion.Infrastructure.Integraciones.DatosUruguay;

/// <summary>
/// Una página de resultados de datosUruguay, ya revisada: el JSON tenía la forma esperada,
/// la fuente es el BCU y la unidad es UYU/USD.
/// </summary>
/// <remarks>
/// Ojo: que la página sea válida no significa que cada cotización lo sea. Acá puede venir un
/// valor igual a cero o una fecha posterior a la pedida; eso lo descarta
/// <see cref="BuscadorCotizacionesDatosUruguay"/>, que es quien conoce la fecha solicitada.
/// </remarks>
/// <param name="Registros">Las publicaciones de la página, en el orden en que las mandó el proveedor.</param>
/// <param name="HayMasPaginas">true si el proveedor avisó que quedan más resultados.</param>
public sealed record PaginaCotizacionesDatosUruguay(
    IReadOnlyList<RegistroCotizacionDatosUruguay> Registros,
    bool HayMasPaginas);

/// <summary>Una publicación tal como vino del proveedor: su fecha y su valor en UYU por USD.</summary>
public sealed record RegistroCotizacionDatosUruguay(
    DateOnly Fecha,
    decimal Valor);
