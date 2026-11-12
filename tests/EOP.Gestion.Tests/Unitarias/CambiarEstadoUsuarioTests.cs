using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Tests.Compartido;
using static EOP.Gestion.Tests.Compartido.RepositorioUsuariosConTurnoEnMemoria;

namespace EOP.Gestion.Tests.Unitarias;

// Las reglas del cambio de estado (S2-T07 / US-046 / RF-75), sin base de datos ni web.
// Las solicitudes simultáneas se prueban contra SQL Server real en CambiarEstadoConcurrenciaSqlServerTests.
// Las restricciones de los responsables de proyectos son de S2-T08 y no se prueban acá.
public class CambiarEstadoUsuarioTests
{
    private readonly RepositorioUsuariosConTurnoEnMemoria _repositorio = new();
    private readonly CambiarEstadoUsuario _cambiarEstadoUsuario;

    public CambiarEstadoUsuarioTests()
    {
        _cambiarEstadoUsuario = new CambiarEstadoUsuario(_repositorio);
    }

    private Usuario AgregarUsuario(int id, RolUsuario rol, bool activo = true)
    {
        var usuario = new Usuario
        {
            Id = id,
            NombreCompleto = "Usuario de prueba",
            CorreoCorporativo = $"usuario{id}@eop.com.uy",
            Rol = rol,
            Activo = activo,
            EntraObjectId = $"objeto-{id}"
        };

        _repositorio.Usuarios.Add(usuario);
        return usuario;
    }

    // ---------- Inactivar y reactivar ----------

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task UsuarioActivo_SeInactivaSinEliminarloNiTocarSusDemasDatos(RolUsuario rol)
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario usuario = AgregarUsuario(2, rol);

        ResultadoCambiarEstadoUsuario resultado = await _cambiarEstadoUsuario.EjecutarAsync(usuario.Id, nuevoEstadoActivo: false);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UsuarioInactivado, resultado);
        Assert.False(usuario.Activo);
        Assert.Equal(2, _repositorio.Usuarios.Count);
        Assert.Equal(rol, usuario.Rol);
        Assert.Equal("objeto-2", usuario.EntraObjectId);
        Assert.Equal("usuario2@eop.com.uy", usuario.CorreoCorporativo);
    }

    [Fact]
    public async Task UsuarioInactivo_SePuedeReactivar()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario usuario = AgregarUsuario(2, RolUsuario.Supervisor, activo: false);

        ResultadoCambiarEstadoUsuario resultado = await _cambiarEstadoUsuario.EjecutarAsync(usuario.Id, nuevoEstadoActivo: true);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UsuarioReactivado, resultado);
        Assert.True(usuario.Activo);
        Assert.Equal(RolUsuario.Supervisor, usuario.Rol);
    }

    [Fact]
    public async Task CambiarElEstadoDeUnUsuario_NoAfectaALosDemas()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario inactivado = AgregarUsuario(2, RolUsuario.Operador);
        Usuario otro = AgregarUsuario(3, RolUsuario.Operador);

        await _cambiarEstadoUsuario.EjecutarAsync(inactivado.Id, nuevoEstadoActivo: false);

        Assert.True(otro.Activo);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UsuarioQueYaEstabaEnEseEstado_NoSeToca(bool estadoActual)
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario usuario = AgregarUsuario(2, RolUsuario.Operador, activo: estadoActual);

        ResultadoCambiarEstadoUsuario resultado = await _cambiarEstadoUsuario.EjecutarAsync(usuario.Id, nuevoEstadoActivo: estadoActual);

        Assert.Equal(ResultadoCambiarEstadoUsuario.YaTeniaEseEstado, resultado);
        Assert.Equal(estadoActual, usuario.Activo);
        Assert.DoesNotContain(PasoActualizarEstado, _repositorio.Pasos);
    }

    [Fact]
    public async Task UsuarioQueNoExiste_SeInformaYNoSeGuardaNada()
    {
        ResultadoCambiarEstadoUsuario resultado = await _cambiarEstadoUsuario.EjecutarAsync(999, nuevoEstadoActivo: false);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UsuarioNoEncontrado, resultado);
        Assert.Equal([PasoBloquear, PasoObtener, PasoLiberar], _repositorio.Pasos);
    }

    // ---------- Último Administrador activo ----------

    [Fact]
    public async Task UltimoAdministradorActivo_NoSePuedeInactivar()
    {
        Usuario administrador = AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Supervisor);
        AgregarUsuario(3, RolUsuario.Administrador, activo: false);

        ResultadoCambiarEstadoUsuario resultado = await _cambiarEstadoUsuario.EjecutarAsync(administrador.Id, nuevoEstadoActivo: false);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UltimoAdministradorActivo, resultado);
        Assert.True(administrador.Activo);
        Assert.Equal([PasoBloquear, PasoObtener, PasoContar, PasoLiberar], _repositorio.Pasos);
    }

    [Fact]
    public async Task AdministradorConOtroAdministradorActivo_SePuedeInactivar()
    {
        Usuario administrador = AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Administrador);

        ResultadoCambiarEstadoUsuario resultado = await _cambiarEstadoUsuario.EjecutarAsync(administrador.Id, nuevoEstadoActivo: false);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UsuarioInactivado, resultado);
        Assert.False(administrador.Activo);
    }

    [Fact]
    public async Task AdministradorInactivoSinOtrosAdministradores_SePuedeReactivar()
    {
        Usuario administrador = AgregarUsuario(1, RolUsuario.Administrador, activo: false);

        ResultadoCambiarEstadoUsuario resultado = await _cambiarEstadoUsuario.EjecutarAsync(administrador.Id, nuevoEstadoActivo: true);

        Assert.Equal(ResultadoCambiarEstadoUsuario.UsuarioReactivado, resultado);
        Assert.True(administrador.Activo);
    }

    [Theory]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task UsuarioQueNoEsAdministrador_SePuedeInactivarAunqueHayaUnSoloAdministrador(RolUsuario rol)
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario usuario = AgregarUsuario(2, rol);

        await _cambiarEstadoUsuario.EjecutarAsync(usuario.Id, nuevoEstadoActivo: false);

        Assert.False(usuario.Activo);
        Assert.DoesNotContain(PasoContar, _repositorio.Pasos);
    }

    // ---------- Cuando la operación falla ----------

    [Fact]
    public async Task SiFallaAlGuardar_SePropagaElErrorSeConservaElEstadoYSeLiberaElTurno()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario operador = AgregarUsuario(2, RolUsuario.Operador);
        _repositorio.ErrorAlActualizar = new InvalidOperationException("Se cayó la base");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _cambiarEstadoUsuario.EjecutarAsync(operador.Id, nuevoEstadoActivo: false));

        Assert.True(operador.Activo);
        Assert.DoesNotContain(PasoConfirmar, _repositorio.Pasos);
        Assert.Equal(PasoLiberar, _repositorio.Pasos.Last());
    }

    // ---------- Orden de los pasos ----------

    [Fact]
    public async Task AlInactivarUnAdministrador_PrimeroEsperaElTurnoDespuesLeeYRecienAlFinalGuarda()
    {
        // Es el mismo orden que exige ModificarRolUsuario: si contara antes de tener el
        // turno, dos solicitudes simultáneas podrían inactivar a los dos últimos administradores.
        Usuario administrador = AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Administrador);

        await _cambiarEstadoUsuario.EjecutarAsync(administrador.Id, nuevoEstadoActivo: false);

        Assert.Equal(
            [PasoBloquear, PasoObtener, PasoContar, PasoActualizarEstado, PasoConfirmar, PasoLiberar],
            _repositorio.Pasos);
    }
}
