
namespace EOP.Gestion.Core.Entidades.Catalogo
{
    // Las unidades de medida son fijas y no se pueden crear desde el sistema.
    // Se definen acá para que todos los materiales utilicen las mismas opciones.
    //
    // Ojo con los números: son lo que queda guardado en la base. Por eso las unidades nuevas
    // siguen numerando desde el 6 aunque visualmente queden agrupadas en otro orden, y un número
    // que ya existe nunca se cambia: si no, los materiales guardados pasarían a tener otra unidad.
    public enum UnidadMedida
    {
        // Las cinco del relevamiento original.
        Metro = 1,
        MetroCuadrado = 2,
        MetroCubico = 3,
        Kilogramo = 4,
        Litro = 5,

        // Longitud.
        Milimetro = 6,
        Centimetro = 7,
        Kilometro = 8,

        // Superficie.
        CentimetroCuadrado = 9,

        // Volumen.
        CentimetroCubico = 10,
        Mililitro = 11,

        // Peso.
        Gramo = 12,
        Tonelada = 13,

        // Para lo que se compra por pieza y no tiene una medida natural,
        // como un tablero eléctrico o una llave.
        Unidad = 14
    }
}
