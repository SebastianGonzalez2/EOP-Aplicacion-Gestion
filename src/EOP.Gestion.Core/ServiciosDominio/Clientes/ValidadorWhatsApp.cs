using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EOP.Gestion.Core.ServiciosDominio.Clientes
{
    public static class ValidadorWhatsApp
    {


        public static string Normalizar(string? numero) //metodo para normalizar el numero de whatsapp, eliminando espacios, guiones, puntos y parentesis
        {
 
            if (string.IsNullOrWhiteSpace(numero))
            {
                return string.Empty;
            }

            return numero.Replace(" ", "").Replace("-", "").Replace(".", "").Replace("(", "").Replace(")", "");
        }

        public static bool EsValido(string? numero) 
        {
            string numeroNormalizado = Normalizar(numero);

            return Regex.IsMatch(numeroNormalizado, @"^\+[1-9][0-9]{1,14}$"); //verifica que el numero de whatsapp sea valido, debe empezar con + y tener entre 1 y 14 digitos, el formato es compatible con E.164,
        }






    }
}
