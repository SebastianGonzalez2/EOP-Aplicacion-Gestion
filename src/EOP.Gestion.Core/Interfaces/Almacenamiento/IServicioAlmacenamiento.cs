using EOP.Gestion.Core.DTOs.Almacenamiento;

namespace EOP.Gestion.Core.Interfaces.Almacenamiento;

// Core dice QUÉ necesita hacer con los archivos privados (guardarlos y recuperarlos), e
// Infrastructure resuelve CÓMO (hoy, con Azure Blob Storage). Así los casos de uso no se
// enteran de que existe Azure y se pueden probar con un servicio falso.
//
// En la base de datos se guarda solamente la referencia que devuelve GuardarAsync (RNF-6):
// nunca el archivo.
//
// Este servicio NO decide quién puede ver o subir un archivo, ni qué formatos o tamaños se
// aceptan: eso lo verifica cada caso de uso en el servidor (RNF-4) antes de llamarlo.
// Tampoco entrega URLs: los archivos son privados (RNF-5) y siempre se leen a través de la
// aplicación.
public interface IServicioAlmacenamiento
{
    // Guarda el archivo como uno nuevo y devuelve su referencia, que es lo que hay que
    // guardar en la base. Nunca pisa otro archivo: cada llamada genera un nombre nuevo.
    // Si los datos son inválidos lanza ArgumentException; si Azure falla lanza
    // AlmacenamientoNoDisponibleException.
    Task<string> GuardarAsync(ArchivoParaGuardar archivo,CancellationToken cancellationToken = default);

    // Recupera el archivo de esa referencia, o devuelve null si no existe.
    // Si la referencia no tiene el formato que genera GuardarAsync lanza ArgumentException;
    // si Azure falla lanza AlmacenamientoNoDisponibleException.
    Task<ArchivoDescargado?> ObtenerAsync(string referencia,CancellationToken cancellationToken = default);
}
