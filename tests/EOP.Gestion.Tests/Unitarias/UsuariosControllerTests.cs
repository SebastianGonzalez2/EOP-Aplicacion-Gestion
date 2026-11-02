using EOP.Gestion.Core.CasosDeUso.Usuarios;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.ViewModels.Usuarios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EOP.Gestion.Tests.Unitarias;

// Comprobamos qué le responde el Controller a cada resultado del caso de uso.
// La seguridad (rol, token antifalsificación, POST) se prueba pasando por la app
// entera en ValidacionSesionIniciadaIntegracionTests.ModificarRol.cs.
public class UsuariosControllerTests
{
    private const string MensajeExito = "MensajeExito";
    private const string MensajeError = "MensajeError";

    private readonly RepositorioUsuariosConTurnoEnMemoria _repositorio = new();
    private readonly UsuariosController _controller;

    public UsuariosControllerTests()
    {
        var contextoHttp = new DefaultHttpContext();

        _controller = new UsuariosController(new ModificarRolUsuario(_repositorio))
        {
            ControllerContext = new ControllerContext { HttpContext = contextoHttp },
            TempData = new TempDataDictionary(contextoHttp, new ProveedorTempDataSinGuardar())
        };
    }

    private void AgregarUsuario(int id, RolUsuario rol)
    {
        _repositorio.Usuarios.Add(new Usuario
        {
            Id = id,
            NombreCompleto = "Usuario de prueba",
            CorreoCorporativo = $"usuario{id}@eop.com.uy",
            Rol = rol,
            Activo = true
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

        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 2, NuevoRol = RolUsuario.Supervisor });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("El rol del usuario se modificó correctamente.", _controller.TempData[MensajeExito]);
        Assert.Null(_controller.TempData[MensajeError]);
    }

    [Fact]
    public async Task UsuarioQueNoExiste_InformaElMotivoYVuelveAlListado()
    {
        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 999, NuevoRol = RolUsuario.Supervisor });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se pudo modificar el rol: el usuario no existe.", _controller.TempData[MensajeError]);
        Assert.Null(_controller.TempData[MensajeExito]);
    }

    [Fact]
    public async Task UltimoAdministradorActivo_InformaElMotivoYVuelveAlListado()
    {
        AgregarUsuario(1, RolUsuario.Administrador);

        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 1, NuevoRol = RolUsuario.Operador });

        AssertVuelveAlListado(respuesta);
        Assert.Equal("No se puede modificar el rol: es el último Administrador activo del sistema.", _controller.TempData[MensajeError]);
        Assert.Null(_controller.TempData[MensajeExito]);
    }

    [Fact]
    public async Task RolQueSuperoLaValidacionDelFormularioPeroNoExiste_LoFrenaElCasoDeUso()
    {
        AgregarUsuario(1, RolUsuario.Operador);

        IActionResult respuesta = await _controller.ModificarRol(new ModificarRolViewModel { UsuarioId = 1, NuevoRol = (RolUsuario)99 });

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
