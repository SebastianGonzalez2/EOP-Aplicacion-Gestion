using System;
using System.Collections.Generic;
using System.Text;

namespace EOP.Gestion.Core.ServiciosDominio.Clientes
{
    public static class ValidadorRut
    {

        public static string Normalizar(string? rut) {

            if (string.IsNullOrWhiteSpace(rut))
            { //si esta vacio o es null, devuelvo vacio
                return string.Empty;
            }
            return rut.Replace(".", "").Replace("-", "").Replace(" ", ""); //elimino puntos, guiones y espacios , DE ESTO 21.456.789-0012 A ESTO 214567890012
        }

        private static bool tieneDoceDigitos(string rut) {

            if (rut.Length != 12)
            {
                return false;
            }

            foreach (char caracter in rut) { 

                if (caracter < '0' || caracter > '9') { //esto tambien toma q no sea signo ni letra
                    return false;
                }
            
            }
            return true;
        }

        // Calcula la suma necesaria para verificar el último dígito del RUT.
        private static int CalcularSumaPonderada(string rut)
        {
            int[] factores = { 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int suma = 0;
            // Recorremos los primeros 11 dígitos.
            for (int i = 0; i < 11; i++)
            {
                int digito = rut[i] - '0'; // Convertimos el carácter a número restando '0'.

                suma += digito * factores[i];
            }
            return suma;
        }

        // Calcula el dígito verificador a partir de los primeros 11 dígitos.
        private static int CalcularDigitoVerificador(string rut)
        {
            // Obtenemos la suma de las multiplicaciones.
            int suma = CalcularSumaPonderada(rut);
            // Calculamos el resto de dividir la suma entre 11.
            int resto = suma % 11;
            // Calculamos el posible dígito verificador.
            int digito = 11 - resto;
            // Si da 11, el dígito verificador es 0.
            if (digito == 11){
                return 0;
            }
            // Si da 10, no existe un dígito verificador válido.
            if (digito == 10){
                return -1;
            }
            return digito;
        }



        public static bool EsValido(string? rut) { 
        
            string rutNormalizado = Normalizar(rut); //elimina los separadores del RUT

            if (!tieneDoceDigitos(rutNormalizado)){ //verifica que tenga 12 dígitos numéricos
                return false;
            }
            
            int digitoCalculado= CalcularDigitoVerificador(rutNormalizado); //obtiene el dígito verificador esperado

            int digitoIngresado = rutNormalizado[11] - '0'; // Convertimos el carácter a número restando '0'.
            
            return digitoCalculado == digitoIngresado; //true si coincide con el ingresado, o false si no coincide
        }








    }
}
