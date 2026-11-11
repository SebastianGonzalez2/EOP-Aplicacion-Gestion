namespace EOP.Gestion.Core.DTOs.Almacenamiento;

/// <summary>
/// Lo que un caso de uso le entrega al servicio de almacenamiento para guardar un archivo.
/// </summary>
/// <param name="Carpeta">
/// Agrupa los archivos por tema, por ejemplo "facturas" o "documentos-proyecto".
/// Solo letras minúsculas, números y guiones. La define el caso de uso, no el usuario.
/// </param>
/// <param name="NombreOriginal">
/// Nombre que tenía el archivo al subirlo. Solo se usa para conservar su extensión;
/// el nombre guardado lo genera el servicio. El caso de uso debe guardar este nombre
/// en la base si quiere mostrarlo después.
/// </param>
/// <param name="TipoContenido">Tipo MIME del archivo, por ejemplo "application/pdf".</param>
/// <param name="Contenido">Los bytes del archivo. El servicio lo lee, pero no lo cierra.</param>
public sealed record ArchivoParaGuardar(
    string Carpeta,
    string NombreOriginal,
    string TipoContenido,
    Stream Contenido);
