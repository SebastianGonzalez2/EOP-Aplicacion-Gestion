using EOP.Gestion.Core.Entidades.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

// S2-T23: reglas básicas de la entidad Presupuesto (RF-01 y RF-02).
public class PresupuestoTests
{
    [Fact]
    public void PresupuestoNuevo_EmpiezaEnBorrador()
    {
        Assert.Equal(EstadoPresupuesto.Borrador, new Presupuesto().Estado);
    }

    [Fact]
    public void PresupuestoNuevo_NoTieneCodigoTodavia()
    {
        // El código lo asigna el sistema al guardar; nadie lo escribe a mano.
        var presupuesto = new Presupuesto();

        Assert.Equal(string.Empty, presupuesto.Codigo);
        Assert.Equal(0, presupuesto.NumeroCorrelativo);
    }

    [Fact]
    public void EstadoPresupuesto_TieneExactamenteLosOchoEstadosDeRF02()
    {
        string[] estadosEnOrden = Enum.GetValues<EstadoPresupuesto>().Select(estado => estado.ToString()).ToArray();

        Assert.Equal(
            [
                "Borrador",
                "ParaRevisionInterna",
                "EnviadoPendienteDeRespuestaDelCliente",
                "Aprobado",
                "Rechazado",
                "SolicitudDeModificacionDelCliente",
                "Vencido",
                "Cancelado"
            ],
            estadosEnOrden);
    }

    [Fact]
    public void EstadoPresupuesto_NingunEstadoVale0_ParaQueUnEstadoOlvidadoLoRechaceLaBase()
    {
        Assert.DoesNotContain(Enum.GetValues<EstadoPresupuesto>(), estado => (int)estado == 0);
    }
}
