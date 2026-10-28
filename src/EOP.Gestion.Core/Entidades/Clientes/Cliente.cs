using System;
using System.Collections.Generic;
using System.Text;

namespace EOP.Gestion.Core.Entidades.Clientes
{
    public class Cliente
    {


        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty; // string.Empty para que no sea nulo y se inicialice como cadena vacía
        public string Rut { get; set; } = string.Empty;
        public string? CorreoContacto { get; set; }
        public string? CorreoFacturacion { get; set; } //? para que pueda ser nulo
        public string NumeroWhatsApp { get; set; } = string.Empty;
        public bool Activo { get; set; } = true; // Inicializado como true por defecto



    }
}
