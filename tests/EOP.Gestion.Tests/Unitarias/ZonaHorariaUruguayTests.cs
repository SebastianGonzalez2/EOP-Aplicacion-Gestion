using EOP.Gestion.Core.Comun;

namespace EOP.Gestion.Tests.Unitarias;

// FIX-UTC-FECHAS: las fechas se guardan en UTC y se interpretan en hora de Uruguay (UTC-3, sin horario de verano).
public class ZonaHorariaUruguayTests
{
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ConvertirUtcALocal_RestaTresHorasYTratalaFechaSinKindComoUtc(DateTimeKind kind)
    {
        DateTime fechaUtc = DateTime.SpecifyKind(new DateTime(2026, 11, 15, 1, 0, 0), kind);

        DateTime local = ZonaHorariaUruguay.ConvertirUtcALocal(fechaUtc);

        Assert.Equal(new DateTime(2026, 11, 14, 22, 0, 0), local);
    }

    [Fact]
    public void ConvertirUtcALocal_FechaConKindLocalSePasaPrimeroAUtc()
    {
        // Una fecha Local tiene que equivaler al mismo instante, sin importar la zona de la PC que corre la prueba.
        DateTime instanteUtc = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        DateTime comoLocal = instanteUtc.ToLocalTime();

        DateTime local = ZonaHorariaUruguay.ConvertirUtcALocal(comoLocal);

        Assert.Equal(new DateTime(2026, 7, 10, 9, 0, 0), local);
    }

    [Fact]
    public void ConvertirUtcALocal_ElResultadoNoDependeDeLaZonaDelServidor()
    {
        // Mismo instante, dos formas de escribirlo: el resultado en Uruguay debe ser idéntico.
        DateTime comoUtc = new(2026, 1, 20, 3, 0, 0, DateTimeKind.Utc);
        DateTime comoSinKind = new(2026, 1, 20, 3, 0, 0);

        Assert.Equal(ZonaHorariaUruguay.ConvertirUtcALocal(comoUtc), ZonaHorariaUruguay.ConvertirUtcALocal(comoSinKind));
    }

    [Fact]
    public void ObtenerIntervaloUtcDelDia_EmpiezaALas0300UtcYTermina24HorasDespues()
    {
        var intervalo = ZonaHorariaUruguay.ObtenerIntervaloUtcDelDia(new DateTime(2026, 11, 14));

        Assert.Equal(new DateTime(2026, 11, 14, 3, 0, 0), intervalo.InicioUtc);
        Assert.Equal(new DateTime(2026, 11, 15, 3, 0, 0), intervalo.FinUtcExclusivo);
        Assert.Equal(DateTimeKind.Utc, intervalo.InicioUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, intervalo.FinUtcExclusivo.Kind);
    }

    [Fact]
    public void ObtenerIntervaloUtcDelDia_IgnoraLaHoraDeLaFechaRecibida()
    {
        var sinHora = ZonaHorariaUruguay.ObtenerIntervaloUtcDelDia(new DateTime(2026, 11, 14));
        var conHora = ZonaHorariaUruguay.ObtenerIntervaloUtcDelDia(new DateTime(2026, 11, 14, 22, 45, 0));

        Assert.Equal(sinHora, conHora);
    }

    [Fact]
    public void ObtenerIntervaloUtcDelDia_DiasConsecutivosNoSeSuperponenNiDejanHuecos()
    {
        var hoy = ZonaHorariaUruguay.ObtenerIntervaloUtcDelDia(new DateTime(2026, 11, 14));
        var manana = ZonaHorariaUruguay.ObtenerIntervaloUtcDelDia(new DateTime(2026, 11, 15));

        Assert.Equal(hoy.FinUtcExclusivo, manana.InicioUtc);
    }

    [Fact]
    public void ObtenerIntervaloUtcDelDia_ElIntervaloVuelveASerElMismoDiaEnUruguay()
    {
        var intervalo = ZonaHorariaUruguay.ObtenerIntervaloUtcDelDia(new DateTime(2026, 11, 14));

        Assert.Equal(new DateTime(2026, 11, 14, 0, 0, 0), ZonaHorariaUruguay.ConvertirUtcALocal(intervalo.InicioUtc));
        Assert.Equal(new DateTime(2026, 11, 15, 0, 0, 0), ZonaHorariaUruguay.ConvertirUtcALocal(intervalo.FinUtcExclusivo));
    }
}
