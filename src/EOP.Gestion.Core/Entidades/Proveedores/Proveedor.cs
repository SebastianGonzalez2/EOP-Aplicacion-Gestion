using System.Text;

namespace EOP.Gestion.Core.Entidades.Proveedores;

public class Proveedor
{
    private string _nombre = string.Empty;

    public int Id { get; set; }

    // Cada vez que se asigna el Nombre se recalcula también NombreNormalizado. Así las dos
    // propiedades no se pueden desincronizar, y nadie se tiene que acordar de actualizar
    // la segunda, ni en el alta ni cuando se corrija el nombre más adelante.
    public string Nombre
    {
        get
        {
            return _nombre;
        }
        set
        {
            _nombre = value;
            NombreNormalizado = NormalizarNombre(value);
        }
    }

    // No se puede repetir el nombre aunque cambien las mayúsculas, los espacios (RF-23) o los
    // tildes (decisión del equipo en S2-T10).
    // La base no puede comparar así por su cuenta: para ella "Fivisa  SA" y "Fivisa SA" son
    // textos distintos. Por eso guardamos esta versión normalizada en su propia columna y
    // el índice único va sobre ella, mientras Nombre conserva lo que escribió el usuario.
    // El set es privado porque este valor solo se calcula, nunca se asigna a mano.
    public string NombreNormalizado { get; private set; } = string.Empty;

    public string Rut { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    // Al menos uno de los dos correos tiene que estar informado, por eso son opcionales por
    // separado y la regla que los une está en ProveedorConfiguracion.
    public string? CorreoContacto { get; set; }

    public string? CorreoFacturacion { get; set; }

    // El lado "muchos" de la relación: un Proveedor puede tener varias cuentas. Es solo de
    // lectura para que nadie reemplace la lista entera y pierda las cuentas anteriores: las
    // cuentas nuevas se agregan con CuentasBancarias.Add(...).
    public List<CuentaBancaria> CuentasBancarias { get; } = new List<CuentaBancaria>();

    // Saca los espacios del principio y del final, deja un solo espacio entre palabras, quita
    // los tildes y pasa todo a mayúsculas. Los espacios entre palabras se conservan porque son
    // parte del nombre ("Fi Visa" no es "Fivisa").
    private static string NormalizarNombre(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return string.Empty;
        }

        // Pasarle null a Split significa "cortá por cualquier espacio en blanco", incluidos
        // tabulaciones y saltos de línea.
        string[] palabras = nombre.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        string nombreConEspaciosSimples = string.Join(" ", palabras);
        string nombreSinTildes = QuitarTildes(nombreConEspaciosSimples);

        // Invariant para que el resultado no dependa del idioma configurado en el servidor.
        return nombreSinTildes.ToUpperInvariant();
    }

    // FormD separa cada letra de su marca: la "é" pasa a ser una "e" seguida de un tilde suelto
    // (el carácter ́). Así los podemos sacar sin hacer una lista de vocales, y también
    // sirve para los textos que ya venían escritos con la letra y el tilde separados.
    //
    // Sacamos solamente el tilde: la ñ y la ü usan otras marcas y se quedan como están, porque
    // "Peña" y "Pena" son palabras distintas. FormC vuelve a unir esas letras con su marca.
    private static string QuitarTildes(string texto)
    {
        string textoSeparado = texto.Normalize(NormalizationForm.FormD);
        string textoSinTildes = textoSeparado.Replace("́", string.Empty);

        return textoSinTildes.Normalize(NormalizationForm.FormC);
    }
}
