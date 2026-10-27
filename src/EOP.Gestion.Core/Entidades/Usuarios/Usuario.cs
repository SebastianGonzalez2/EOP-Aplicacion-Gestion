using System;
using System.Collections.Generic;
using System.Text;

namespace EOP.Gestion.Core.Entidades.Usuarios
{
    public class Usuario
    {
        public int Id { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string CorreoCorporativo { get; set; } = string.Empty;

        public RolUsuario Rol { get; set; }

        public bool Activo { get; set; }

        //identificador extern de Microsoft Entra ID, sirve para vincular con un usuaio de Microsoft Entra ID
        public string? EntraObjectId { get; set; }
    }

}
