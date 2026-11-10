namespace EOP.Gestion.Core.Entidades.Presupuestos
{
    // Un presupuesto de EOP (RF-01 y RF-02). Esta clase solo representa los datos básicos del alta;
    // los conceptos, materiales y el resto de los datos llegan en las tareas siguientes.
    public class Presupuesto
    {
        public int Id { get; set; }

        // Código interno con la forma PRES-AAAAMMDD-NOMBRE DEL PROYECTO-####.
        // Lo genera el sistema al guardar (nunca el usuario) y no cambia durante todo el
        // ciclo de vida del presupuesto, aunque después se modifique el nombre.
        public string Codigo { get; set; } = string.Empty;

        // El #### del código: correlativo global de presupuestos, de 1 a 9999.
        // Lo guardamos aparte para que la base garantice que no se repite.
        public int NumeroCorrelativo { get; set; }

        // Nombre del proyecto o trabajo que se va a presupuestar.
        public string NombreProyecto { get; set; } = string.Empty;

        // Todo presupuesto está asociado a un cliente.
        public int ClienteId { get; set; }

        // Todo presupuesto tiene un tipo de proyecto, elegido entre los tipos fijos.
        public int TipoProyectoId { get; set; }

        // Plazo de vigencia en días corridos. Empieza a contar desde el envío al cliente (RF-36).
        public int PlazoVigenciaDias { get; set; }

        // El usuario que creó el presupuesto. Se asigna solo, nadie lo elige (RF-01).
        public int ResponsableId { get; set; }

        // Todo presupuesto recién creado está en Borrador (RF-02).
        public EstadoPresupuesto Estado { get; set; } = EstadoPresupuesto.Borrador;

        // Momento en que se creó. El AAAAMMDD del código sale de esta fecha.
        public DateTime FechaCreacion { get; set; }
    }
}
