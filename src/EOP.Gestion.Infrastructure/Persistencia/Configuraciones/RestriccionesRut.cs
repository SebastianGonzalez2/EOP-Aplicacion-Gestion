namespace EOP.Gestion.Infrastructure.Persistencia.Configuraciones;

// Proveedores y Clientes guardan el RUT de la misma forma (RF-23 y RF-89: 12 dígitos, sin
// separadores), así que la restricción CHECK que lo controla es la misma en las dos tablas.
// La dejamos en un solo lugar para que, si algún día hay que corregirla, no quede una
// versión arreglada y otra vieja.
internal static class RestriccionesRut
{
    // Arma la condición que mira el RUT carácter por carácter: el código de cada uno tiene
    // que estar entre 48 y 57, que son los códigos de '0' y '9'.
    //
    // La forma corta, [Rut] NOT LIKE '%[^0-9]%', parece lo mismo pero no lo es: LIKE compara
    // según el idioma configurado en la base, y para SQL Server caracteres como '²', '½' o
    // los dígitos árabes también entran en el rango 0-9 (lo comprobamos, y por eso hay una
    // prueba para cada uno). Comparando códigos, el resultado no depende de esa configuración.
    //
    // El LIKE con 12 guiones bajos (cada uno es "un carácter cualquiera") exige el largo
    // exacto. Tiene que estar sí o sí: en un RUT más corto, mirar una posición que no existe
    // da NULL, y una restricción CHECK deja pasar lo desconocido.
    internal static string CrearCondicionRutDeDoceDigitos()
    {
        string condicion = "[Rut] LIKE '____________'";

        for (int posicion = 1; posicion <= 12; posicion++)
        {
            condicion += $" AND UNICODE(SUBSTRING([Rut], {posicion}, 1)) BETWEEN 48 AND 57";
        }

        return condicion;
    }
}
