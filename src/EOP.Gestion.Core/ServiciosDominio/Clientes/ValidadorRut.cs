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



        // Controla que cada parte del RUT tenga un valor posible. El RUT es AA BBBBBB CCC D:
        // prefijo, número de serie, sufijo y dígito verificador.
        //
        // Sin esto, 000000000000 pasaba como válido, porque la cuenta del dígito verificador da justo 0.
        // La DGI explicó qué es cada parte pero no publica los valores válidos, por eso estos
        // rangos salen de fuentes públicas. El detalle está en DT-04 de docs/decisiones-tecnicas.md.
        private static bool TieneEstructuraValida(string rut)
        {
            // Acá ya sabemos que son 12 dígitos, así que estos cortes y el Parse no pueden fallar.
            int prefijo = int.Parse(rut.Substring(0, 2));
            string numeroDeSerie = rut.Substring(2, 6);
            string sufijo = rut.Substring(8, 3);

            // El prefijo es la oficina de la DGI donde se inscribió la empresa.
            if (prefijo < 1 || prefijo > 22)
            {
                return false;
            }

            // El número de serie es secuencial y arranca en 1, nunca puede ser todo ceros.
            if (numeroDeSerie == "000000")
            {
                return false;
            }

            // Según la DGI el sufijo es una constante, y esa constante es 001.
            if (sufijo != "001")
            {
                return false;
            }

            return true;
        }

        public static bool EsValido(string? rut) {
        
            string rutNormalizado = Normalizar(rut); //elimina los separadores del RUT

            if (!tieneDoceDigitos(rutNormalizado)){ //verifica que tenga 12 dígitos numéricos
                return false;
            }
            
            if (!TieneEstructuraValida(rutNormalizado)){ //verifica que el prefijo, la serie y el sufijo tengan valores posibles
                return false;
            }

            int digitoCalculado= CalcularDigitoVerificador(rutNormalizado); //obtiene el dígito verificador esperado

            int digitoIngresado = rutNormalizado[11] - '0'; // Convertimos el carácter a número restando '0'.
            
            return digitoCalculado == digitoIngresado; //true si coincide con el ingresado, o false si no coincide
        }








    }
}
