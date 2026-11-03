using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.DTOs.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Tests.Compartido;
using static EOP.Gestion.Tests.Compartido.RepositorioUsuariosConTurnoEnMemoria;

namespace EOP.Gestion.Tests.Unitarias;

// Las reglas del cambio de rol (US-044 / RF-72), sin base de datos ni web.
// Lo que pasa con solicitudes simultáneas se prueba contra SQL Server
// real en ModificarRolConcurrenciaSqlServerTests.
public class ModificarRolUsuarioTests
{
    private readonly RepositorioUsuariosConTurnoEnMemoria _repositorio = new();
    private readonly ModificarRolUsuario _modificarRolUsuario;

    public ModificarRolUsuarioTests()
    {
        _modificarRolUsuario = new ModificarRolUsuario(_repositorio);
    }

    private Usuario AgregarUsuario(int id, RolUsuario rol, bool activo = true)
    {
        var usuario = new Usuario
        {
            Id = id,
            NombreCompleto = "Usuario de prueba",
            CorreoCorporativo = $"usuario{id}@eop.com.uy",
            Rol = rol,
            Activo = activo
        };

        _repositorio.Usuarios.Add(usuario);
        return usuario;
    }

    // ---------- Cambios válidos ----------

    [Fact]
    public async Task OperadorASupervisor_ModificaElRol()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario operador = AgregarUsuario(2, RolUsuario.Operador);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(operador.Id, RolUsuario.Supervisor);

        Assert.Equal(ResultadoModificarRol.RolModificado, resultado);
        Assert.Equal(RolUsuario.Supervisor, operador.Rol);
    }

    [Theory]
    [InlineData(RolUsuario.Operador, RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor, RolUsuario.Operador)]
    [InlineData(RolUsuario.Supervisor, RolUsuario.Administrador)]
    public async Task CambioEntreRolesPermitidos_ModificaElRol(RolUsuario rolAnterior, RolUsuario nuevoRol)
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario usuario = AgregarUsuario(2, rolAnterior);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(usuario.Id, nuevoRol);

        Assert.Equal(ResultadoModificarRol.RolModificado, resultado);
        Assert.Equal(nuevoRol, usuario.Rol);
    }

    [Theory]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task AdministradorConOtroAdministradorActivo_SePuedeDegradar(RolUsuario nuevoRol)
    {
        Usuario administrador = AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Administrador);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(administrador.Id, nuevoRol);

        Assert.Equal(ResultadoModificarRol.RolModificado, resultado);
        Assert.Equal(nuevoRol, administrador.Rol);
    }

    // ---------- Rol inexistente ----------

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task RolQueNoEsDeLosTresFijos_SeRechazaSinTocarNada(int rolInexistente)
    {
        Usuario operador = AgregarUsuario(1, RolUsuario.Operador);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(operador.Id, (RolUsuario)rolInexistente);

        Assert.Equal(ResultadoModificarRol.RolNoPermitido, resultado);
        Assert.Equal(RolUsuario.Operador, operador.Rol);
        Assert.Empty(_repositorio.Pasos);
    }

    // ---------- Usuario inexistente ----------

    [Fact]
    public async Task UsuarioQueNoExiste_SeRechazaSinGuardarNada()
    {
        Usuario administrador = AgregarUsuario(1, RolUsuario.Administrador);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(999, RolUsuario.Supervisor);

        Assert.Equal(ResultadoModificarRol.UsuarioNoEncontrado, resultado);
        Assert.Equal(RolUsuario.Administrador, administrador.Rol);
        Assert.Equal([PasoBloquear, PasoObtener, PasoLiberar], _repositorio.Pasos);
    }

    // ---------- Último Administrador activo ----------

    [Theory]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task UltimoAdministradorActivo_NoSePuedeDegradarYConservaSuRol(RolUsuario nuevoRol)
    {
        Usuario ultimoAdministrador = AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Operador);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(ultimoAdministrador.Id, nuevoRol);

        Assert.Equal(ResultadoModificarRol.UltimoAdministradorActivo, resultado);
        Assert.Equal(RolUsuario.Administrador, ultimoAdministrador.Rol);
        Assert.DoesNotContain(PasoActualizar, _repositorio.Pasos);
    }

    [Fact]
    public async Task OtroAdministradorInactivo_NoCuenta_YElActivoSigueSiendoElUltimo()
    {
        Usuario administradorActivo = AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Administrador, activo: false);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(administradorActivo.Id, RolUsuario.Operador);

        Assert.Equal(ResultadoModificarRol.UltimoAdministradorActivo, resultado);
        Assert.Equal(RolUsuario.Administrador, administradorActivo.Rol);
    }

    [Fact]
    public async Task UltimoAdministradorActivo_QueSigueSiendoAdministrador_NoSeRechaza()
    {
        // La regla es no quedarse sin administradores, y esto no le quita el rol a nadie.
        Usuario ultimoAdministrador = AgregarUsuario(1, RolUsuario.Administrador);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(ultimoAdministrador.Id, RolUsuario.Administrador);

        Assert.Equal(ResultadoModificarRol.RolModificado, resultado);
        Assert.Equal(RolUsuario.Administrador, ultimoAdministrador.Rol);
    }

    [Fact]
    public async Task AdministradorInactivo_SePuedeDegradar_PorqueNoEraUnAdministradorActivo()
    {
        // Hay un único Administrador activo, pero no es este: la cantidad no cambia.
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario administradorInactivo = AgregarUsuario(2, RolUsuario.Administrador, activo: false);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(administradorInactivo.Id, RolUsuario.Operador);

        Assert.Equal(ResultadoModificarRol.RolModificado, resultado);
        Assert.Equal(RolUsuario.Operador, administradorInactivo.Rol);
    }

    [Fact]
    public async Task UsuarioNoAdministrador_SePuedeCambiar_AunqueSoloHayaUnAdministrador()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario supervisor = AgregarUsuario(2, RolUsuario.Supervisor);

        ResultadoModificarRol resultado = await _modificarRolUsuario.EjecutarAsync(supervisor.Id, RolUsuario.Operador);

        Assert.Equal(ResultadoModificarRol.RolModificado, resultado);
        Assert.DoesNotContain(PasoContar, _repositorio.Pasos);
    }

    // ---------- Cuando la operación falla ----------

    [Fact]
    public async Task SiFallaAlGuardar_SePropagaElErrorSeConservaElRolYSeLiberaElTurno()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        Usuario operador = AgregarUsuario(2, RolUsuario.Operador);
        _repositorio.ErrorAlActualizar = new InvalidOperationException("Se cayó la base");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _modificarRolUsuario.EjecutarAsync(operador.Id, RolUsuario.Supervisor));

        Assert.Equal(RolUsuario.Operador, operador.Rol);
        Assert.DoesNotContain(PasoConfirmar, _repositorio.Pasos);
        Assert.Equal(PasoLiberar, _repositorio.Pasos.Last());
    }

    // ---------- Orden de los pasos ----------

    [Fact]
    public async Task AlDegradar_PrimeroEsperaElTurnoDespuesLeeYRecienAlFinalGuarda()
    {
        // Este orden es la mitad de la protección ante solicitudes simultáneas: si
        // contara antes de tener el turno, podría decidir con un número desactualizado.
        Usuario administrador = AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Administrador);

        await _modificarRolUsuario.EjecutarAsync(administrador.Id, RolUsuario.Operador);

        Assert.Equal(
            [PasoBloquear, PasoObtener, PasoContar, PasoActualizar, PasoConfirmar, PasoLiberar],
            _repositorio.Pasos);
    }

    [Fact]
    public async Task AlRechazarAlUltimoAdministrador_NoConfirmaNiGuardaNada()
    {
        Usuario ultimoAdministrador = AgregarUsuario(1, RolUsuario.Administrador);

        await _modificarRolUsuario.EjecutarAsync(ultimoAdministrador.Id, RolUsuario.Operador);

        Assert.Equal([PasoBloquear, PasoObtener, PasoContar, PasoLiberar], _repositorio.Pasos);
    }
}
