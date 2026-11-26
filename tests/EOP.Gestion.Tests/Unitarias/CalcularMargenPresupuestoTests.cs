using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Cotizaciones;
using EOP.Gestion.Core.DTOs.Presupuestos;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Interfaces.Cotizaciones;
using Xunit;

namespace EOP.Gestion.Tests.Unitarias;

public class CalcularMargenPresupuestoTests
{
    [Fact]
    public async Task EjecutarAsync_SinMargen_NoConsultaCotizacion()
    {
        var servicio = new ServicioCotizacionesFalso();
        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 100m, Moneda.USD)
        };

        var resultado = await casoDeUso.EjecutarAsync(filas);

        Assert.Null(resultado.MensajeError);
        Assert.Null(resultado.Cotizacion);
        Assert.Equal(0, servicio.CantidadConsultas);
    }

    [Fact]
    public async Task EjecutarAsync_BaseEnUYU_NoConsultaCotizacion()
    {
        var servicio = new ServicioCotizacionesFalso();
        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 10000m, Moneda.UYU),

            CrearMargen(10m, TipoConcepto.ManoDeObraId)
        };

        var resultado = await casoDeUso.EjecutarAsync(filas);

        Assert.Null(resultado.MensajeError);
        Assert.Null(resultado.Cotizacion);
        Assert.Equal(1000m, filas[1].Subtotal);
        Assert.Equal(Moneda.UYU, filas[1].Moneda);
        Assert.Equal(0, servicio.CantidadConsultas);
    }

    [Fact]
    public async Task EjecutarAsync_BaseEnUSD_ConvierteAMonedaNacional()
    {
        var servicio = new ServicioCotizacionesFalso
        {
            Cotizacion = CrearCotizacion(40m)
        };

        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 100m, Moneda.USD),

            CrearMargen(10m, TipoConcepto.ManoDeObraId)
        };

        var resultado = await casoDeUso.EjecutarAsync(filas);

        Assert.Null(resultado.MensajeError);
        Assert.Equal(servicio.Cotizacion, resultado.Cotizacion);
        Assert.Equal(400m, filas[1].Subtotal);
        Assert.Equal(Moneda.UYU, filas[1].Moneda);
        Assert.Equal(1, servicio.CantidadConsultas);
    }

    [Fact]
    public async Task EjecutarAsync_BaseMixta_CalculaMargenCorrectamente()
    {
        var servicio = new ServicioCotizacionesFalso
        {
            Cotizacion = CrearCotizacion(40m)
        };

        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 10000m, Moneda.UYU),

            CrearConcepto(
                TipoConcepto.TrasladoId, 100m, Moneda.USD),

            CrearMargen(
                10m,
                TipoConcepto.ManoDeObraId,
                TipoConcepto.TrasladoId)
        };

        var resultado = await casoDeUso.EjecutarAsync(filas);

        Assert.Null(resultado.MensajeError);
        Assert.Equal(1400m, filas[2].Subtotal);
        Assert.Equal(Moneda.UYU, filas[2].Moneda);
        Assert.Equal(1, servicio.CantidadConsultas);
    }

    [Fact]
    public async Task EjecutarAsync_USDFueraDeLaBase_NoConsultaCotizacion()
    {
        var servicio = new ServicioCotizacionesFalso();
        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 10000m, Moneda.UYU),

            CrearConcepto(
                TipoConcepto.TrasladoId, 500m, Moneda.USD),

            CrearMargen(10m, TipoConcepto.ManoDeObraId)
        };

        var resultado = await casoDeUso.EjecutarAsync(filas);

        Assert.Null(resultado.MensajeError);
        Assert.Equal(1000m, filas[2].Subtotal);
        Assert.Equal(0, servicio.CantidadConsultas);
    }

    [Fact]
    public async Task EjecutarAsync_SinCotizacion_InformaError()
    {
        var servicio = new ServicioCotizacionesFalso();
        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 100m, Moneda.USD),

            CrearMargen(10m, TipoConcepto.ManoDeObraId)
        };

        var resultado = await casoDeUso.EjecutarAsync(filas);

        Assert.NotNull(resultado.MensajeError);
        Assert.Null(resultado.Cotizacion);
        Assert.Null(filas[1].Subtotal);
        Assert.Equal(2, filas.Count);
    }

    [Fact]
    public async Task EjecutarAsync_FallaServicio_ConservaLosConceptos()
    {
        var servicio = new ServicioCotizacionesFalso
        {
            SimularError = true
        };

        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 100m, Moneda.USD),

            CrearMargen(10m, TipoConcepto.ManoDeObraId)
        };

        var resultado = await casoDeUso.EjecutarAsync(filas);

        Assert.NotNull(resultado.MensajeError);
        Assert.Null(resultado.Cotizacion);
        Assert.Null(filas[1].Subtotal);

        Assert.Equal(2, filas.Count);
        Assert.Equal(100m, filas[0].Subtotal);
        Assert.Equal(Moneda.USD, filas[0].Moneda);
        Assert.Equal(10m, filas[1].PorcentajeMargen);
    }

    [Fact]
    public async Task EjecutarAsync_CambiaCotizacion_RecalculaMargen()
    {
        var servicio = new ServicioCotizacionesFalso
        {
            Cotizacion = CrearCotizacion(40m)
        };

        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 100m, Moneda.USD),

            CrearMargen(10m, TipoConcepto.ManoDeObraId)
        };

        await casoDeUso.EjecutarAsync(filas);

        Assert.Equal(400m, filas[1].Subtotal);

        servicio.Cotizacion = CrearCotizacion(42m);

        await casoDeUso.EjecutarAsync(filas);

        Assert.Equal(420m, filas[1].Subtotal);
    }

    [Fact]
    public async Task EjecutarAsync_CambiaConcepto_RecalculaMargen()
    {
        var servicio = new ServicioCotizacionesFalso();
        var casoDeUso = new CalcularMargenPresupuesto(servicio);

        var filas = new List<FilaConceptoDto>
        {
            CrearConcepto(
                TipoConcepto.ManoDeObraId, 10000m, Moneda.UYU),

            CrearMargen(10m, TipoConcepto.ManoDeObraId)
        };

        await casoDeUso.EjecutarAsync(filas);

        Assert.Equal(1000m, filas[1].Subtotal);

        filas[0] = filas[0] with
        {
            PrecioUnitario = 20000m
        };

        await casoDeUso.EjecutarAsync(filas);

        Assert.Equal(2000m, filas[1].Subtotal);
    }

    private static FilaConceptoDto CrearConcepto(
        int tipoId,
        decimal importe,
        Moneda moneda)
    {
        return new FilaConceptoDto(
            tipoId,
            tipoId,
            "Concepto de prueba",
            null,
            "Concepto de prueba",
            null,
            null,
            null,
            null,
            null,
            importe,
            moneda);
    }

    private static FilaConceptoDto CrearMargen(
        decimal porcentaje,
        params int[] tiposBase)
    {
        return new FilaConceptoDto(
            100,
            TipoConcepto.MargenId,
            "Margen",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null)
        {
            PorcentajeMargen = porcentaje,
            TiposBaseMargen = tiposBase
        };
    }

    private static CotizacionUsd CrearCotizacion(decimal valor)
    {
        return new CotizacionUsd(
            valor,
            new DateOnly(2026, 11, 25),
            new DateOnly(2026, 11, 26));
    }

    private class ServicioCotizacionesFalso
        : IServicioCotizaciones
    {
        public CotizacionUsd? Cotizacion { get; set; }

        public bool SimularError { get; set; }

        public int CantidadConsultas { get; private set; }

        public Task<CotizacionUsd?> ObtenerCotizacionVigenteAsync(
            CancellationToken cancellationToken = default)
        {
            CantidadConsultas++;

            if (SimularError)
            {
                throw new CotizacionNoDisponibleException(
                    MotivoCotizacionNoDisponible.FallaDeConexion,
                    null);
            }

            return Task.FromResult(Cotizacion);
        }

        public Task<CotizacionUsd?> ObtenerCotizacionHistoricaAsync(
            DateOnly fecha,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Cotizacion);
        }
    }
}