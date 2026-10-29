using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;

namespace EOP.Gestion.Core.ServiciosDominio.Clientes
{
    public static class ValidadorCorreosCliente
    {

        public static bool TieneAlMenosUnCorreo(string? correoContacto, string? correoFacturacion) {  //verifica si al menos tiene un correo
            return !string.IsNullOrWhiteSpace(correoContacto) || !string.IsNullOrWhiteSpace(correoFacturacion);
        }

        public static bool TieneFormatoValido(string? correo) {

            if (string.IsNullOrWhiteSpace(correo)) {
                return true;
            }

            return correo.Length <= 254 && MailAddress.TryCreate(correo, out var direccion) && direccion is not null && direccion.Address == correo;
            //verifica que sea un formato válido de correo electrónico 
        }

    }
}
