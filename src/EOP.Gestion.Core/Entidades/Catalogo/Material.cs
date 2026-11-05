
namespace EOP.Gestion.Core.Entidades.Catalogo
{
    public class Material
    {
        private string _nombre = string.Empty;

        public int Id { get; set; }

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

        // Guardamos una versión normalizada del nombre
        // para poder detectar materiales duplicados.
        public string NombreNormalizado { get; private set; } = string.Empty;

        // La unidad debe ser una de las opciones fijas
        // definidas en UnidadMedida.cs.
        public UnidadMedida Unidad { get; set; }

        // Normalizamos el nombre ignorando mayúsculas,
        // minúsculas y espacios innecesarios.
        private static string NormalizarNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return string.Empty;
            }

            string[] palabras = nombre.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

            string nombreSinEspaciosExtra = string.Join(" ", palabras);

            return nombreSinEspaciosExtra.ToUpperInvariant();
        }
    }
}
