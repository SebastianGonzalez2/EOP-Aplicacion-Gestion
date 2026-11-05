using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;

namespace EOP.Gestion.Core.ServiciosDominio.Compartidos
{
    // Reglas de correo que no son de un módulo en particular: las usan Clientes, Proveedores y Usuarios.
    // Acá solo miramos el texto del correo. Saber si ya está registrado necesita consultar la base,
    // así que eso lo hace el caso de uso que lo precise, como RegistrarUsuario.
    public static class ValidadorCorreos
    {

        public static bool TieneAlMenosUnCorreo(string? correoContacto, string? correoFacturacion) {  //verifica si al menos tiene un correo
            return !string.IsNullOrWhiteSpace(correoContacto) || !string.IsNullOrWhiteSpace(correoFacturacion);
        }

        public static bool TieneFormatoValido(string? correo) {

            // Un correo vacío pasa a propósito: acá no decidimos si es obligatorio. Eso lo comprueba
            // aparte cada caso de uso, porque en Clientes y Proveedores uno de los dos puede faltar.
            if (string.IsNullOrWhiteSpace(correo)) {
                return true;
            }

            return correo.Length <= 254 && MailAddress.TryCreate(correo, out var direccion) && direccion is not null && direccion.Address == correo;
            //verifica que sea un formato válido de correo electrónico 
        }

    }
}
