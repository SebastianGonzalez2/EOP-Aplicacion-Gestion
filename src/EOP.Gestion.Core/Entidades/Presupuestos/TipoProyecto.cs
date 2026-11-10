namespace EOP.Gestion.Core.Entidades.Presupuestos
{
    // Los tipos de proyecto son opciones fijas: no hay pantallas para crearlos ni modificarlos
    // (RF-01 y alcance.md).
    //
    // Los guardamos en una tabla y no en un enum porque la lista definitiva todavía está
    // PENDIENTE DE VALIDAR CON EOP (RF-01). Así, cuando EOP la confirme, se carga con una
    // migración y no hace falta tocar esta clase ni el Presupuesto. Hasta entonces la tabla queda
    // vacía a propósito: no inventamos valores.
    public class TipoProyecto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
    }
}
