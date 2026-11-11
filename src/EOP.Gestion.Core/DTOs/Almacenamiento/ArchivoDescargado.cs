namespace EOP.Gestion.Core.DTOs.Almacenamiento;

/// <summary>
/// Un archivo recuperado del almacenamiento privado.
/// </summary>
/// <param name="Contenido">
/// Los bytes del archivo, que se van leyendo a medida que se consumen. Quien lo recibe
/// tiene que cerrarlo (con <c>using</c>) cuando termina. Si se lo entrega a un
/// <c>FileStreamResult</c> de MVC, MVC lo cierra solo.
/// </param>
/// <param name="TipoContenido">Tipo MIME con el que se guardó, por ejemplo "application/pdf".</param>
public sealed record ArchivoDescargado(
    Stream Contenido,
    string TipoContenido);
