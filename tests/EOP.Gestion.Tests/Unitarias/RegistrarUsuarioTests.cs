using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Tests.Compartido;
using static EOP.Gestion.Tests.Compartido.RepositorioUsuariosConTurnoEnMemoria;

namespace EOP.Gestion.Tests.Unitarias;

// Las reglas del alta de usuarios (US-043 / RF-70), sin base de datos ni web. El índice único y las
// solicitudes simultáneas se prueban contra SQL Server en RegistrarUsuarioSqlServerTests.
public class RegistrarUsuarioTests
{
    private const string NombreValido = "Ana Pérez";
    private const string CorreoValido = "ana.perez@eop.com.uy";
    private const string MensajeCorreoYaRegistrado = "El correo corporativo ya se encuentra registrado.";

    private readonly RepositorioUsuariosConTurnoEnMemoria _repositorio = new();
    private readonly RegistrarUsuario _registrarUsuario;

    public RegistrarUsuarioTests()
    {
        _registrarUsuario = new RegistrarUsuario(_repositorio);
    }

    private void AssertNoSeGuardoNada()
    {
        Assert.Empty(_repositorio.Usuarios);
        Assert.DoesNotContain(PasoAgregar, _repositorio.Pasos);
    }

    // ---------- Alta correcta ----------

    [Fact]
    public async Task DatosValidos_GuardaElUsuarioActivoYPendienteDeVinculacion()
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, CorreoValido, RolUsuario.Operador);

        Assert.Empty(errores);

        Usuario guardado = Assert.Single(_repositorio.Usuarios);
        Assert.Equal(NombreValido, guardado.NombreCompleto);
        Assert.Equal(CorreoValido, guardado.CorreoCorporativo);
        Assert.Equal(RolUsuario.Operador, guardado.Rol);
        Assert.True(guardado.Activo);
        Assert.Null(guardado.EntraObjectId);
    }

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Supervisor)]
    [InlineData(RolUsuario.Operador)]
    public async Task CadaRolFijo_SeGuardaTalCualSeEligio(RolUsuario rol)
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, CorreoValido, rol);

        Assert.Empty(errores);
        Assert.Equal(rol, Assert.Single(_repositorio.Usuarios).Rol);
    }

    [Fact]
    public async Task EspaciosYMayusculas_SeNormalizanAntesDeGuardar()
    {
        await _registrarUsuario.EjecutarAsync("  Ana Pérez  ", "  Ana.Perez@EOP.com.UY  ", RolUsuario.Operador);

        Usuario guardado = Assert.Single(_repositorio.Usuarios);
        Assert.Equal(NombreValido, guardado.NombreCompleto);
        Assert.Equal(CorreoValido, guardado.CorreoCorporativo);
    }

    [Fact]
    public async Task ValoresEnElLimiteDeLasColumnas_SeAceptan()
    {
        string nombreDe150 = new('a', 150);
        string correoDe254 = new string('a', 254 - "@eop.com.uy".Length) + "@eop.com.uy";

        List<string> errores = await _registrarUsuario.EjecutarAsync(nombreDe150, correoDe254, RolUsuario.Operador);

        Assert.Empty(errores);
        Assert.Single(_repositorio.Usuarios);
    }

    // ---------- Datos obligatorios e inválidos ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NombreVacio_SeRechaza(string? nombre)
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync(nombre, CorreoValido, RolUsuario.Operador);

        Assert.Equal(["El nombre completo es obligatorio."], errores);
        AssertNoSeGuardoNada();
    }

    [Fact]
    public async Task NombreDemasiadoLargo_SeRechaza()
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync(new string('a', 151), CorreoValido, RolUsuario.Operador);

        Assert.Equal(["El nombre completo no puede superar los 150 caracteres."], errores);
        AssertNoSeGuardoNada();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CorreoVacio_SeRechazaSinConsultarLaBase(string? correo)
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, correo, RolUsuario.Operador);

        Assert.Equal(["El correo corporativo es obligatorio."], errores);
        Assert.Empty(_repositorio.Pasos);
    }

    [Theory]
    [InlineData("ana.perez")]
    [InlineData("ana.perez@")]
    [InlineData("@eop.com.uy")]
    [InlineData("ana perez@eop.com.uy")]
    [InlineData("ana@eop@com.uy")]
    [InlineData("Ana Perez <ana.perez@eop.com.uy>")]
    public async Task CorreoConFormatoInvalido_SeRechazaSinConsultarLaBase(string correo)
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, correo, RolUsuario.Operador);

        Assert.Equal(["El correo corporativo no tiene un formato válido."], errores);
        Assert.Empty(_repositorio.Pasos);
    }

    [Fact]
    public async Task CorreoDemasiadoLargo_SeRechaza()
    {
        string correoDe255 = new string('a', 255 - "@eop.com.uy".Length) + "@eop.com.uy";

        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, correoDe255, RolUsuario.Operador);

        Assert.Equal(["El correo corporativo no tiene un formato válido."], errores);
        AssertNoSeGuardoNada();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task RolQueNoEsDeLosTresFijos_SeRechaza(int rolInexistente)
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, CorreoValido, (RolUsuario)rolInexistente);

        Assert.Equal(["Seleccioná un rol válido."], errores);
        AssertNoSeGuardoNada();
    }

    [Fact]
    public async Task VariosDatosInvalidos_InformaTodosLosErroresJuntos()
    {
        List<string> errores = await _registrarUsuario.EjecutarAsync("", "sin-arroba", (RolUsuario)99);

        Assert.Equal(3, errores.Count);
        AssertNoSeGuardoNada();
    }

    // ---------- Correo duplicado ----------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CorreoDeOtroUsuario_SeRechazaAunqueEsteInactivo(bool activo)
    {
        _repositorio.Usuarios.Add(new Usuario { Id = 1, NombreCompleto = "Otra Persona", CorreoCorporativo = CorreoValido, Rol = RolUsuario.Administrador, Activo = activo });

        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, CorreoValido, RolUsuario.Operador);

        Assert.Equal([MensajeCorreoYaRegistrado], errores);
        Assert.Single(_repositorio.Usuarios);
        Assert.DoesNotContain(PasoAgregar, _repositorio.Pasos);
    }

    [Fact]
    public async Task OtraSolicitudRegistraElCorreoDespuesDeLaConsultaPrevia_InformaLaDuplicacion()
    {
        _repositorio.OtraSolicitudGanaLaCarrera = true;

        List<string> errores = await _registrarUsuario.EjecutarAsync(NombreValido, CorreoValido, RolUsuario.Operador);

        Assert.Equal([MensajeCorreoYaRegistrado], errores);
        Assert.Equal([PasoObtenerPorCorreo, PasoAgregar], _repositorio.Pasos);
    }

    // ---------- Fallas de infraestructura ----------

    [Fact]
    public async Task FallaAlGuardar_SePropagaYNoSeInformaComoDuplicado()
    {
        _repositorio.ErrorAlAgregar = new InvalidOperationException("Se cayó la base");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _registrarUsuario.EjecutarAsync(NombreValido, CorreoValido, RolUsuario.Operador));
    }
}
