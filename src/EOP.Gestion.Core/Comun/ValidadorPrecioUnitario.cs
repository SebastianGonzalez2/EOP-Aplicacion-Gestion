namespace EOP.Gestion.Core.Comun
{
    // S2-T22: regla única para el precio unitario de una relación material-proveedor.
    // La usan los tres casos de uso (registrar material, agregar proveedor y editar precios)
    // y los tres formularios, así todos aceptan y rechazan exactamente los mismos valores.
    //
    // Los límites salen de la columna PrecioUnitario, que es decimal(28,10): 28 dígitos en
    // total, 10 de ellos decimales. Si dejáramos pasar un valor que no entra, SQL Server
    // fallaría al guardar (o redondearía en silencio) y el usuario vería un error genérico
    // en vez de un mensaje claro. Es un límite técnico, no una regla comercial.
    public static class ValidadorPrecioUnitario
    {
        // Cantidad de decimales que guarda la columna.
        public const int CantidadMaximaDeDecimales = 10;

        // 18 dígitos enteros y 10 decimales: el mayor valor que entra en decimal(28,10).
        public const decimal ValorMaximo = 999999999999999999.9999999999m;

        public const string MensajeNoPositivo = "El precio unitario debe ser mayor que cero.";

        public const string MensajeDemasiadoGrande = "El precio unitario es demasiado grande: admite hasta 18 dígitos enteros.";

        public const string MensajeDemasiadosDecimales = "El precio unitario admite como máximo 10 decimales.";

        // Devuelve el mensaje de error si el precio no es válido, o null si lo es.
        public static string? ObtenerError(decimal? precioUnitario)
        {
            // Sin precio, o con cero o negativo: debe ser estrictamente mayor que cero.
            if (!precioUnitario.HasValue || precioUnitario.Value <= 0)
            {
                return MensajeNoPositivo;
            }

            if (precioUnitario.Value > ValorMaximo)
            {
                return MensajeDemasiadoGrande;
            }

            // Si al redondear a 10 decimales el valor cambia, es que traía más decimales de
            // los que guarda la base. Los ceros de más ("5,00000000000") no cuentan, porque
            // no cambian el valor.
            decimal precioRedondeado = Math.Round(precioUnitario.Value, CantidadMaximaDeDecimales);

            if (precioRedondeado != precioUnitario.Value)
            {
                return MensajeDemasiadosDecimales;
            }

            return null;
        }
    }
}
