namespace EOP.Gestion.Core.DTOs.Proveedores;

// Cómo terminó el intento de guardar un Proveedor.
//
// Existe por el caso de dos usuarios que registran el mismo proveedor a la vez: los dos pasan la
// validación y recién la base descubre el duplicado. Eso no es una falla del sistema sino una
// respuesta esperable, así que el repositorio la devuelve como un resultado más. Además, Core se
// entera de lo que pasó sin tener que conocer las excepciones de EF Core ni las de SQL Server.
public enum ResultadoAgregarProveedor
{
    Agregado = 1,
    NombreDuplicado = 2,
    RutDuplicado = 3
}
