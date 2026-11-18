namespace EOP.Gestion.Core.Entidades.Presupuestos
{
    // Los tipos de concepto son opciones fijas: no hay pantallas para crearlos ni
    // modificarlos. Los guardamos en una tabla y no en un enum para que la base
    // garantice las relaciones y para poder mostrar el nombre sin duplicarlo.
    //
    // Los Id son lo que queda guardado en la base, así que nunca se cambian ni se
    // reutilizan. Los usamos desde las reglas de negocio para saber qué datos aplican
    // a cada tipo (por ejemplo, si es Material o si es el único Margen del presupuesto).
    public class TipoConcepto
    {
        public const int MaterialId = 1;
        public const int ManoDeObraId = 2;
        public const int AlquilerId = 3;
        public const int TrasladoId = 4;
        public const int CostosIndirectosId = 5;
        public const int MargenId = 6;

        public static bool AdmiteImporteDirecto(int tipoId)
        {
            return tipoId is ManoDeObraId or AlquilerId or TrasladoId or CostosIndirectosId;
        }

        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
    }
}
