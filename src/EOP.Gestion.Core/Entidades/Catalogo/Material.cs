
namespace EOP.Gestion.Core.Entidades.Catalogo
{
    public class Material
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        // La unidad debe ser una de las opciones fijas
        // definidas en UnidadMedida.cs.
        public UnidadMedida Unidad { get; set; }
    }
}
