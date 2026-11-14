namespace EOP.Gestion.Core.Comun
{
    // Único lugar donde el sistema convierte fechas entre UTC y la hora de Uruguay.
    //
    // La regla es: en la base de datos las fechas se guardan SIEMPRE en UTC (DateTime.UtcNow),
    // y se pasan a hora de Uruguay solo cuando hace falta mostrarlas, armar el código de un
    // presupuesto o interpretar el día que eligió el usuario en un filtro.
    //
    // Usamos la zona horaria de Uruguay a propósito y no la del servidor: Azure App Service
    // suele correr en UTC, así que depender de la hora del servidor daría resultados distintos
    // en la PC del desarrollador y en producción.
    public static class ZonaHorariaUruguay
    {
        // Linux, macOS y Windows (con ICU, que es lo normal en .NET 10 y en Azure) conocen el
        // identificador IANA. El de Windows queda como respaldo por si faltara el primero.
        private const string IdentificadorIana = "America/Montevideo";
        private const string IdentificadorWindows = "Montevideo Standard Time";

        private static readonly TimeZoneInfo Zona = BuscarZona();

        private static TimeZoneInfo BuscarZona()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(IdentificadorIana);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(IdentificadorWindows);
            }
        }

        // Pasa una fecha guardada en UTC a la hora de Uruguay.
        // Ojo: EF Core devuelve las fechas con Kind "Unspecified", así que acá una fecha sin Kind
        // se interpreta como UTC, que es como la guardamos. Solo se convierte una vez: no hay que
        // llamar a este método con una fecha que ya esté en hora de Uruguay.
        public static DateTime ConvertirUtcALocal(DateTime fechaUtc)
        {
            DateTime fechaConKindUtc;

            if (fechaUtc.Kind == DateTimeKind.Local)
            {
                // Una fecha Local se pasa a UTC primero, porque ConvertTimeFromUtc no la acepta.
                fechaConKindUtc = fechaUtc.ToUniversalTime();
            }
            else
            {
                fechaConKindUtc = DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(fechaConKindUtc, Zona);
        }

        // Dado un día que eligió el usuario (hora de Uruguay), devuelve el intervalo UTC que lo cubre:
        // desde las 00:00 de ese día (inclusive) hasta las 00:00 del día siguiente (exclusive).
        // Se consulta con "fecha >= InicioUtc && fecha < FinUtcExclusivo", así un presupuesto creado
        // justo a medianoche cae en un solo día y no en dos. Se ignora la hora de la fecha recibida.
        public static (DateTime InicioUtc, DateTime FinUtcExclusivo) ObtenerIntervaloUtcDelDia(DateTime diaLocal)
        {
            DateTime inicioDelDiaLocal = diaLocal.Date;
            DateTime inicioDelDiaSiguienteLocal = inicioDelDiaLocal.AddDays(1);

            DateTime inicioUtc = ConvertirLocalAUtc(inicioDelDiaLocal);
            DateTime finUtcExclusivo = ConvertirLocalAUtc(inicioDelDiaSiguienteLocal);

            return (inicioUtc, finUtcExclusivo);
        }

        private static DateTime ConvertirLocalAUtc(DateTime fechaLocal)
        {
            DateTime fechaSinKind = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified);

            // Si algún cambio de horario histórico se "comió" esa hora (por ejemplo las 00:00),
            // avanzamos hasta la primera hora que sí existió en Uruguay.
            while (Zona.IsInvalidTime(fechaSinKind))
            {
                fechaSinKind = fechaSinKind.AddHours(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(fechaSinKind, Zona);
        }
    }
}
