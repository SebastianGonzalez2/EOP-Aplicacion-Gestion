
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;

namespace EOP.Gestion.Tests.Unitarias;

// S3-T16 (US-003): comprobamos que solamente puedan editar
// los usuarios autorizados y cuando el presupuesto esté
// en uno de los estados que permiten modificaciones.
public class PermisosConceptosPresupuestoTests
{
    [Theory]
    [InlineData(EstadoPresupuesto.Borrador, true)]
    [InlineData(EstadoPresupuesto.ParaRevisionInterna, true)]
    [InlineData(EstadoPresupuesto.EnviadoPendienteDeRespuestaDelCliente, false)]
    [InlineData(EstadoPresupuesto.Aprobado, false)]
    [InlineData(EstadoPresupuesto.Rechazado, false)]
    [InlineData(EstadoPresupuesto.SolicitudDeModificacionDelCliente, true)]
    [InlineData(EstadoPresupuesto.Vencido, false)]
    [InlineData(EstadoPresupuesto.Cancelado, false)]
    public void Estados_SoloPermitenEditarCuandoCorresponde(
        EstadoPresupuesto estado, bool esperado)
    {
        var presupuesto = new Presupuesto
        {
            Estado = estado,
            ResponsableId = 10
        };

        var operador = new Usuario
        {
            Id = 10,
            Rol = RolUsuario.Operador,
            Activo = true
        };

        var supervisor = new Usuario
        {
            Id = 20,
            Rol = RolUsuario.Supervisor,
            Activo = true
        };

        var administrador = new Usuario
        {
            Id = 30,
            Rol = RolUsuario.Administrador,
            Activo = true
        };

        Assert.Equal(esperado,
            PermisosConceptosPresupuesto.PuedeEditar(presupuesto, operador));

        Assert.Equal(esperado,
            PermisosConceptosPresupuesto.PuedeEditar(presupuesto, supervisor));

        Assert.Equal(esperado,
            PermisosConceptosPresupuesto.PuedeEditar(presupuesto, administrador));
    }

    [Theory]
    [InlineData(RolUsuario.Operador, false)]
    [InlineData(RolUsuario.Supervisor, true)]
    [InlineData(RolUsuario.Administrador, true)]
    public void UsuarioNoResponsable_RespetaLosPermisosDelRol(
        RolUsuario rol, bool esperado)
    {
        var presupuesto = new Presupuesto
        {
            Estado = EstadoPresupuesto.Borrador,
            ResponsableId = 10
        };

        var usuario = new Usuario
        {
            Id = 20,
            Rol = rol,
            Activo = true
        };

        bool puedeEditar =
            PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario);

        Assert.Equal(esperado, puedeEditar);
    }

    [Theory]
    [InlineData(RolUsuario.Operador)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Administrador)]
    public void UsuarioInactivo_NoPuedeEditar(RolUsuario rol)
    {
        var presupuesto = new Presupuesto
        {
            Estado = EstadoPresupuesto.Borrador,
            ResponsableId = 10
        };

        var usuario = new Usuario
        {
            Id = 10,
            Rol = rol,
            Activo = false
        };

        bool puedeEditar =
            PermisosConceptosPresupuesto.PuedeEditar(presupuesto, usuario);

        Assert.False(puedeEditar);
    }
}
