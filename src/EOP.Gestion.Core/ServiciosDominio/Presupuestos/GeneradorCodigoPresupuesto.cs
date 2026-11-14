using System.Globalization;
using EOP.Gestion.Core.Comun;

namespace EOP.Gestion.Core.ServiciosDominio.Presupuestos
{
    // Arma el código interno de un presupuesto: PRES-AAAAMMDD-NOMBRE DEL PROYECTO-#### (RF-01).
    // Es una regla pura: no sabe de la base de datos, así que se prueba sin infraestructura.
    // De dónde sale el correlativo es problema del repositorio, no de esta clase.
    public static class GeneradorCodigoPresupuesto
    {
        public const int CorrelativoMinimo = 1;

        // El correlativo tiene cuatro cifras, así que el último es el 9999.
        public const int CorrelativoMaximo = 9999;

        // Arma el código a partir de la fecha de creación tal como se guarda en la base (UTC):
        // la pasa a hora de Uruguay antes de tomar el día. Si no, un presupuesto creado el 14/11
        // a las 22:00 en Uruguay (01:00 UTC del 15/11) saldría con la fecha del 15.
        public static string GenerarDesdeFechaUtc(DateTime fechaCreacionUtc, string nombreProyecto, int correlativo)
        {
            DateTime fechaCreacionEnUruguay = ZonaHorariaUruguay.ConvertirUtcALocal(fechaCreacionUtc);

            return Generar(fechaCreacionEnUruguay, nombreProyecto, correlativo);
        }

        // Recibe la fecha que va escrita en el código, o sea, el día en Uruguay.
        public static string Generar(DateTime fechaCreacion, string nombreProyecto, int correlativo)
        {
            if (string.IsNullOrWhiteSpace(nombreProyecto))
            {
                throw new ArgumentException("El nombre del proyecto es obligatorio para generar el código.", nameof(nombreProyecto));
            }

            if (correlativo < CorrelativoMinimo || correlativo > CorrelativoMaximo)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(correlativo),
                    $"El correlativo debe estar entre {CorrelativoMinimo} y {CorrelativoMaximo} (cuatro cifras).");
            }

            string fechaConFormato = fechaCreacion.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string nombreParaElCodigo = NormalizarNombre(nombreProyecto);
            string correlativoConCuatroCifras = correlativo.ToString("D4", CultureInfo.InvariantCulture);

            return "PRES-" + fechaConFormato + "-" + nombreParaElCodigo + "-" + correlativoConCuatroCifras;
        }

        // El formato escribe NOMBRE DEL PROYECTO en mayúsculas, así que lo ponemos en mayúsculas y
        // sin espacios sobrantes. El resto de los caracteres se deja tal cual: los requerimientos
        // no dicen que haya que quitarlos.
        public static string NormalizarNombre(string nombreProyecto)
        {
            string[] palabras = nombreProyecto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            string nombreSinEspaciosExtra = string.Join(" ", palabras);

            return nombreSinEspaciosExtra.ToUpperInvariant();
        }
    }
}
