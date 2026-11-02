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

        // Revisa lo que el usuario escribe en el campo del número, antes de agregarle el prefijo.
        // Sacamos los separadores que Normalizar conoce, y lo que queda tienen que ser solo dígitos.
        // Usamos IsAsciiDigit y no IsDigit porque IsDigit también acepta dígitos de otros alfabetos (٠١٢).
        public static bool TieneSoloCaracteresDeNumeroNacional(string? numero)
        {
            string numeroNormalizado = Normalizar(numero);

            return numeroNormalizado.Length > 0 && numeroNormalizado.All(char.IsAsciiDigit);
        }






    }
}
