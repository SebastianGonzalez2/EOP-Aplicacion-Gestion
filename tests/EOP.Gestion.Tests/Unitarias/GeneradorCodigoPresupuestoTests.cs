using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

// S2-T23: formato del código interno PRES-AAAAMMDD-NOMBRE DEL PROYECTO-#### (RF-01).
public class GeneradorCodigoPresupuestoTests
{
    private static readonly DateTime Fecha = new(2026, 11, 9, 15, 30, 0);

    [Fact]
    public void Generar_ArmaElCodigoConFechaNombreYCorrelativo()
    {
        string codigo = GeneradorCodigoPresupuesto.Generar(Fecha, "Instalación solar", 7);

        Assert.Equal("PRES-20261109-INSTALACIÓN SOLAR-0007", codigo);
    }

    [Theory]
    [InlineData(1, "0001")]
    [InlineData(42, "0042")]
    [InlineData(999, "0999")]
    [InlineData(1000, "1000")]
    [InlineData(9999, "9999")]
    public void Generar_ElCorrelativoSiempreTieneCuatroCifras(int correlativo, string esperado)
    {
        string codigo = GeneradorCodigoPresupuesto.Generar(Fecha, "Obra", correlativo);

        Assert.EndsWith("-" + esperado, codigo);
    }

    [Theory]
    [InlineData(2026, 1, 5, "20260105")]
    [InlineData(2026, 12, 31, "20261231")]
    public void Generar_LaFechaUsaAnioMesYDiaConDosCifras(int anio, int mes, int dia, string esperado)
    {
        string codigo = GeneradorCodigoPresupuesto.Generar(new DateTime(anio, mes, dia), "Obra", 1);

        Assert.StartsWith("PRES-" + esperado + "-", codigo);
    }

    [Theory]
    [InlineData("obra norte", "OBRA NORTE")]
    [InlineData("  obra    norte  ", "OBRA NORTE")]
    [InlineData("Año Ñandú", "AÑO ÑANDÚ")]
    public void Generar_ElNombreVaEnMayusculasYSinEspaciosSobrantes(string nombre, string nombreEsperado)
    {
        string codigo = GeneradorCodigoPresupuesto.Generar(Fecha, nombre, 1);

        Assert.Equal($"PRES-20261109-{nombreEsperado}-0001", codigo);
    }

    [Fact]
    public void Generar_EscribeElCorrelativoTalCualAunqueCambienLaFechaOElNombre()
    {
        // El correlativo lo decide quien llama: no se reinicia por fecha ni por nombre.
        string primero = GeneradorCodigoPresupuesto.Generar(new DateTime(2026, 11, 9), "Obra", 5);
        string segundo = GeneradorCodigoPresupuesto.Generar(new DateTime(2026, 11, 10), "Otra obra", 6);

        Assert.EndsWith("-0005", primero);
        Assert.EndsWith("-0006", segundo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Generar_ConNombreEnBlanco_LanzaExcepcion(string nombre)
    {
        Assert.Throws<ArgumentException>(() => GeneradorCodigoPresupuesto.Generar(Fecha, nombre, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void Generar_ConCorrelativoFueraDeCuatroCifras_LanzaExcepcion(int correlativo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeneradorCodigoPresupuesto.Generar(Fecha, "Obra", correlativo));
    }

    // FIX-UTC-FECHAS: la fecha del código es el día en Uruguay, aunque la base guarde UTC.
    [Theory]
    [InlineData(2026, 11, 9, 15, 30, "20261109")]   // 12:30 en Uruguay: mismo día
    [InlineData(2026, 11, 9, 3, 0, "20261109")]     // 00:00 en Uruguay: primer instante del día
    [InlineData(2026, 11, 9, 2, 59, "20261108")]    // 23:59 en Uruguay: todavía es el día anterior
    [InlineData(2026, 11, 15, 1, 0, "20261114")]    // 22:00 en Uruguay (el ejemplo del pedido)
    [InlineData(2027, 1, 1, 2, 0, "20261231")]      // cruza el cambio de año
    [InlineData(2026, 11, 14, 23, 59, "20261114")]  // 20:59 en Uruguay
    public void GenerarDesdeFechaUtc_UsaElDiaDeUruguay(int anio, int mes, int dia, int hora, int minuto, string fechaEsperada)
    {
        var fechaUtc = new DateTime(anio, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

        string codigo = GeneradorCodigoPresupuesto.GenerarDesdeFechaUtc(fechaUtc, "Obra", 1);

        Assert.Equal($"PRES-{fechaEsperada}-OBRA-0001", codigo);
    }

    [Fact]
    public void GenerarDesdeFechaUtc_AceptaLaFechaSinKindQueDevuelveEfCore()
    {
        var fechaSinKind = new DateTime(2026, 11, 15, 1, 0, 0);

        string codigo = GeneradorCodigoPresupuesto.GenerarDesdeFechaUtc(fechaSinKind, "Instalación solar", 7);

        Assert.Equal("PRES-20261114-INSTALACIÓN SOLAR-0007", codigo);
    }
}
