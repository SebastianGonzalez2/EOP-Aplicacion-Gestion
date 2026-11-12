
using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Core.DTOs.Presupuestos
{
    // Contiene solamente los datos que necesitamos
    // mostrar en el listado de presupuestos (US-001).
    public class PresupuestoListadoDto
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string NombreProyecto { get; set; } = string.Empty;

        public string NombreCliente { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public EstadoPresupuesto Estado { get; set; }

        public string NombreResponsable { get; set; } = string.Empty;
    }
}
