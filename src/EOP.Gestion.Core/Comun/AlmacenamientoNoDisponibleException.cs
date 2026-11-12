namespace EOP.Gestion.Core.Comun;

/// <summary>
/// El almacenamiento de archivos no pudo completar la operación por un problema técnico:
/// no hay conexión, Azure respondió con error o la aplicación no tiene permiso.
/// </summary>
/// <remarks>
/// Existe para que los casos de uso puedan atrapar la falla (RNF-13) sin conocer el SDK de
/// Azure: Core no referencia a Azure. El mensaje es genérico a propósito, así que se puede
/// mostrar al usuario; el detalle técnico queda en los logs y en <see cref="Exception.InnerException"/>.
/// </remarks>
public class AlmacenamientoNoDisponibleException : Exception
{
    public AlmacenamientoNoDisponibleException(string mensaje, Exception? causa)
        : base(mensaje, causa)
    {
    }
}
