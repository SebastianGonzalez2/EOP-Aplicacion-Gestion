using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.ViewModels.Usuarios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

using EOP.Gestion.Infrastructure.Consultas.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;


namespace EOP.Gestion.Tests.Unitarias;

// Comprobamos qué le responde el Controller a cada resultado del caso de uso.
// La seguridad (rol, token antifalsificación, POST) se prueba pasando por la app
// entera en ValidacionSesionIniciadaIntegracionTests.ModificarRol.cs.
public class UsuariosControllerTests : IDisposable
{
    private const string MensajeExito = "MensajeExito";
    private const string MensajeError = "MensajeError";

    private readonly RepositorioUsuariosConTurnoEnMemoria _repositorio = new();
    private readonly UsuariosController _controller;

    private readonly SqliteConnection _conexion;
    private readonly EopDbContext _contexto;


    public UsuariosControllerTests()
    {
        // Creamos una conexión SQLite temporal para las pruebas.
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(_conexion)
            .Options;

        _contexto = new EopDbContext(opciones);

        var contextoHttp = new DefaultHttpContext();

        _controller = new UsuariosController(
            new ModificarRolUsuario(_repositorio),
            new CambiarEstadoUsuario(_repositorio),
            new RegistrarUsuario(_repositorio),
            new ConsultarUsuarios(_contexto))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = contextoHttp
            },
            TempData = new TempDataDictionary(
                contextoHttp,
                new ProveedorTempDataSinGuardar())
        };
    }



    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }


    private void AgregarUsuario(int id, RolUsuario rol, bool activo = true)
    {
        _repositorio.Usuarios.Add(new Usuario
        {
            Id = id,
            NombreCompleto = "Usuario de prueba",
            CorreoCorporativo = $"usuario{id}@eop.com.uy",
            Rol = rol,
            Activo = activo
        });
    }

    private static void AssertVuelveAlListado(IActionResult respuesta)
    {
        LocalRedirectResult redireccion = Assert.IsType<LocalRedirectResult>(respuesta);
        Assert.Equal("~/Usuarios", redireccion.Url);
    }

    [Fact]
    public async Task CambioValido_InformaElExitoYVuelveAlListado()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Operador);

        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 2, NuevoRol = "Supervisor" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("El rol del usuario se modificó correctamente.", _controller.TempData[MensajeExito]);
        Assert.Null(_controller.TempData[MensajeError]);
    }

    [Fact]
    public async Task UsuarioQueNoExiste_InformaElMotivoYVuelveAlListado()
    {
        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 999, NuevoRol = "Supervisor" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se pudo modificar el rol: el usuario no existe.", _controller.TempData[MensajeError]);
        Assert.Null(_controller.TempData[MensajeExito]);
    }

    [Fact]
    public async Task UltimoAdministradorActivo_InformaElMotivoYVuelveAlListado()
    {
        AgregarUsuario(1, RolUsuario.Administrador);

        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 1, NuevoRol = "Operador" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se puede modificar el rol: es el último Administrador activo del sistema.", _controller.TempData[MensajeError]);
        Assert.Null(_controller.TempData[MensajeExito]);
    }

    [Fact]
    public async Task RolQueSuperoLaValidacionDelFormularioPeroNoExiste_LoFrenaElCasoDeUso()
    {
        AgregarUsuario(1, RolUsuario.Operador);

        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 1, NuevoRol = "99" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se pudo modificar el rol: el rol seleccionado no es válido.", _controller.TempData[MensajeError]);
    }

    [Fact]
    public async Task DatosInvalidos_NoLlamaAlCasoDeUso()
    {
        AgregarUsuario(1, RolUsuario.Operador);
        _controller.ModelState.AddModelError(nameof(ModificarRolViewModel.NuevoRol), "Seleccioná un rol válido.");

        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 1 });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se pudo modificar el rol: los datos enviados no son válidos.", _controller.TempData[MensajeError]);
        Assert.Empty(_repositorio.Pasos);
    }

    // ---------- Cambio de estado (S2-T07 / US-046) ----------

    private const string MensajeAdvertencia = "MensajeAdvertencia";

    [Fact]
    public async Task CambiarEstado_Inactivar_InformaElExitoYVuelveAlListado()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Operador);

        IActionResult respuesta = await _controller.CambiarEstado(new CambiarEstadoViewModel { UsuarioId = 2, NuevoEstado = "Inactivo" });

        AssertVuelveAlListado(respuesta);
        Assert.StartsWith("El usuario se inactivó correctamente.", (string?)_controller.TempData[MensajeExito]);
        Assert.Null(_controller.TempData[MensajeError]);
        Assert.False(_repositorio.Usuarios.Single(u => u.Id == 2).Activo);
    }

    [Fact]
    public async Task CambiarEstado_Reactivar_InformaElExitoYVuelveAlListado()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Operador, activo: false);

        IActionResult respuesta = await _controller.CambiarEstado(new CambiarEstadoViewModel { UsuarioId = 2, NuevoEstado = "Activo" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("El usuario se reactivó correctamente.", _controller.TempData[MensajeExito]);
        Assert.True(_repositorio.Usuarios.Single(u => u.Id == 2).Activo);
    }

    [Fact]
    public async Task CambiarEstado_UsuarioQueYaTeniaEseEstado_AdvierteYNoCambiaNada()
    {
        AgregarUsuario(1, RolUsuario.Administrador);
        AgregarUsuario(2, RolUsuario.Operador, activo: false);

        IActionResult respuesta = await _controller.CambiarEstado(new CambiarEstadoViewModel { UsuarioId = 2, NuevoEstado = "Inactivo" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("El usuario ya se encontraba Inactivo. No se realizaron cambios.", _controller.TempData[MensajeAdvertencia]);
        Assert.Null(_controller.TempData[MensajeExito]);
    }

    [Fact]
    public async Task CambiarEstado_UsuarioQueNoExiste_InformaElMotivoYVuelveAlListado()
    {
        IActionResult respuesta = await _controller.CambiarEstado(new CambiarEstadoViewModel { UsuarioId = 999, NuevoEstado = "Inactivo" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se pudo cambiar el estado: el usuario no existe.", _controller.TempData[MensajeError]);
        Assert.Null(_controller.TempData[MensajeExito]);
    }

    [Fact]
    public async Task CambiarEstado_UltimoAdministradorActivo_InformaElMotivoYNoLoInactiva()
    {
        AgregarUsuario(1, RolUsuario.Administrador);

        IActionResult respuesta = await _controller.CambiarEstado(new CambiarEstadoViewModel { UsuarioId = 1, NuevoEstado = "Inactivo" });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se puede inactivar al usuario: es el último Administrador activo del sistema.", _controller.TempData[MensajeError]);
        Assert.Null(_controller.TempData[MensajeExito]);
        Assert.True(_repositorio.Usuarios.Single().Activo);
    }

    [Fact]
    public async Task CambiarEstado_DatosInvalidos_NoLlamaAlCasoDeUso()
    {
        AgregarUsuario(1, RolUsuario.Operador);
        _controller.ModelState.AddModelError(nameof(CambiarEstadoViewModel.NuevoEstado), "Seleccioná un estado válido.");

        IActionResult respuesta = await _controller.CambiarEstado(new CambiarEstadoViewModel { UsuarioId = 1 });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se pudo cambiar el estado: los datos enviados no son válidos.", _controller.TempData[MensajeError]);
        Assert.Empty(_repositorio.Pasos);
    }

    // ---------- Alta de usuarios (S1-T17) ----------

    private static RegistrarUsuarioViewModel DatosDeAltaValidos() => new()
    {
        NombreCompleto = "Ana Pérez",
        CorreoCorporativo = "ana.perez@eop.com.uy",
        Rol = "Supervisor"
    };

    private void AssertVuelveAMostrarElFormulario(IActionResult respuesta, RegistrarUsuarioViewModel modelo)
    {
        ViewResult vista = Assert.IsType<ViewResult>(respuesta);
        Assert.Same(modelo, vista.Model);
        Assert.Null(_controller.TempData[MensajeExito]);
    }

    [Fact]
    public void Registrar_Get_MuestraElFormularioVacioSinConsultarLaBase()
    {
        ViewResult vista = Assert.IsType<ViewResult>(_controller.Registrar());

        RegistrarUsuarioViewModel modelo = Assert.IsType<RegistrarUsuarioViewModel>(vista.Model);
        Assert.Empty(modelo.NombreCompleto);
        Assert.Empty(modelo.CorreoCorporativo);
        Assert.Null(modelo.Rol);
        Assert.Empty(_repositorio.Pasos);
    }

    [Fact]
    public async Task Registrar_DatosValidos_InformaElExitoYRedirigeAlFormulario()
    {
        IActionResult respuesta = await _controller.Registrar(DatosDeAltaValidos());

        RedirectToActionResult redireccion = Assert.IsType<RedirectToActionResult>(respuesta);
        Assert.Equal(nameof(UsuariosController.Registrar), redireccion.ActionName);
        Assert.StartsWith("El usuario se registró correctamente.", (string?)_controller.TempData[MensajeExito]);

        Usuario guardado = Assert.Single(_repositorio.Usuarios);
        Assert.Equal(RolUsuario.Supervisor, guardado.Rol);
    }

    [Fact]
    public async Task Registrar_ModeloInvalido_NoLlamaAlCasoDeUso()
    {
        RegistrarUsuarioViewModel modelo = new();
        _controller.ModelState.AddModelError(nameof(RegistrarUsuarioViewModel.Rol), "Seleccioná un rol.");

        IActionResult respuesta = await _controller.Registrar(modelo);

        AssertVuelveAMostrarElFormulario(respuesta, modelo);
        Assert.Empty(_repositorio.Pasos);
    }

    [Fact]
    public async Task Registrar_CorreoDuplicado_MuestraElMotivoEnElFormulario()
    {
        AgregarUsuario(1, RolUsuario.Operador);
        RegistrarUsuarioViewModel modelo = DatosDeAltaValidos();
        modelo.CorreoCorporativo = "usuario1@eop.com.uy";

        IActionResult respuesta = await _controller.Registrar(modelo);

        AssertVuelveAMostrarElFormulario(respuesta, modelo);
        Assert.Contains(
            _controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage == "El correo corporativo ya se encuentra registrado.");
        Assert.Single(_repositorio.Usuarios);
    }

    [Fact]
    public async Task Registrar_RolQueSuperoLaValidacionDelFormularioPeroNoExiste_LoFrenaElCasoDeUso()
    {
        RegistrarUsuarioViewModel modelo = DatosDeAltaValidos();
        modelo.Rol = "99";

        IActionResult respuesta = await _controller.Registrar(modelo);

        AssertVuelveAMostrarElFormulario(respuesta, modelo);
        Assert.Empty(_repositorio.Usuarios);
    }

    // TempData necesita un lugar donde guardarse entre pedidos. En estas pruebas
    // solo leemos lo que dejó el Controller, así que no hace falta guardarlo en ningún lado.
    private sealed class ProveedorTempDataSinGuardar : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
