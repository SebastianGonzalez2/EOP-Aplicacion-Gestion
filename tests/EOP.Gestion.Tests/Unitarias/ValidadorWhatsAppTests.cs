using EOP.Gestion.Core.ServiciosDominio.Clientes;
using System;
using System.Collections.Generic;
using System.Text;

namespace EOP.Gestion.Tests.Unitarias
{
    public class ValidadorWhatsAppTests
    {

        [Fact]
        public void Normalizar_DebeEliminarSeparadores() //Probamos con un numero de whatsapp con espacios, guiones, puntos y parentesis, para verificar que los saque
        {
            string numeroIngresado = "+598 (99) 123-456";
            string resultado = ValidadorWhatsApp.Normalizar(numeroIngresado);

            Assert.Equal("+59899123456", resultado);
        }


        [Fact]
        public void EsValido_DebeAceptarNumeroInternacional() //Probamos con un numero de whatsapp en formato internacional, que es el formato valido
        {
            string numero = "+59899123456";
            bool resultado = ValidadorWhatsApp.EsValido(numero);

            Assert.True(resultado);
        }


        [Theory]
        [InlineData("59899123456")]          // Falta el signo +.
        [InlineData("+598ABC123")]           // Contiene letras.
        [InlineData("+09899123456")]         // Comienza con 0 después del +.
        [InlineData("+598123456789012345")]  // Tiene demasiados dígitos.
        public void EsValido_DebeRechazarNumerosIncorrectos(string numero) //Probamos con varios numeros de whatsapp en formato incorrecto
        {
            bool resultado = ValidadorWhatsApp.EsValido(numero);

            Assert.False(resultado);
        }




    }
}
