
using System.Security.Claims;
using EOP.Gestion.Core.CasosDeUso.Presupuestos;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Entidades.Presupuestos;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Interfaces.Presupuestos;
using EOP.Gestion.Core.ServiciosDominio.Presupuestos;
using EOP.Gestion.Infrastructure.Consultas.Clientes;
using EOP.Gestion.Infrastructure.Consultas.Presupuestos;
using EOP.Gestion.Infrastructure.Consultas.Usuarios;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Usuarios;
using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.ViewModels.Presupuestos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

public class PresupuestosControllerPostTests : IDisposable
{
    private const string IdentidadMicrosoft = "entra-usuario-prueba";

    private readonly SqliteConnection _conexion;
    private readonly EopDbContext _contexto;
    private readonly RepositorioPresupuestosFalso _repositorio;
    private readonly PresupuestosController _controller;

    private readonly int _clienteId;
    private readonly int _tipoProyectoId;
    private readonly int _usuarioId;

    public PresupuestosControllerPostTests()
    {
        // Creamos una base de datos temporal para cada prueba.
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();

        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlite(_conexion)
            .Options;

        _contexto = new EopDbContext(opciones);
        _contexto.Database.EnsureCreated();

        var cliente = new Cliente
        {
            Nombre = "Cliente de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456",
            Activo = true
        };

        var usuario = new Usuario
        {
            NombreCompleto = "Ana Pereira",
            CorreoCorporativo = "ana@eop.com.uy",
            EntraObjectId = IdentidadMicrosoft,
            Rol = RolUsuario.Operador,
            Activo = true
        };

        _contexto.AddRange(cliente, usuario);
        _contexto.SaveChanges();

        _clienteId = cliente.Id;
        _usuarioId = usuario.Id;

        _tipoProyectoId = _contexto.TiposProyecto
            .Single(tipo => tipo.Nombre == "Obra")
            .Id;

        // Usamos un repositorio falso porque la secuencia real
        // de presupuestos funciona con SQL Server.
        _repositorio = new RepositorioPresupuestosFalso();

        var casoDeUso = new RegistrarPresupuesto(
            _repositorio,
            new ConsultaReferenciasPresupuesto(_contexto),
            new RepositorioUsuarios(_contexto),
            TimeProvider.System);

        // Simulamos la identidad autenticada por Microsoft.
        var identidad = new ClaimsIdentity(
            new[]
            {
                new Claim("oid", IdentidadMicrosoft),
                new Claim(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier",
                    IdentidadMicrosoft)
            },
            "Pruebas");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identidad)
        };

        _controller = new PresupuestosController(
            new ConsultarClientes(_contexto),
            new ConsultarTiposProyectoParaSeleccion(_contexto),
            casoDeUso,
            new ConsultarPresupuestos(_contexto),
            new ConsultarUsuarios(_contexto),
            new ConsultarPresupuestoDetalle(_contexto))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                new ProveedorTempDataDePrueba())
        };
    }

    private CrearPresupuestoViewModel CrearModeloValido()
    {
        return new CrearPresupuestoViewModel
        {
            NombreProyecto = "Instalación solar",
            ClienteId = _clienteId,
            TipoProyectoId = _tipoProyectoId,
            PlazoVigenciaDias = 30
        };
    }

    [Fact]
    public async Task CrearPost_DatosValidos_RedireccionaYAsignaResponsable()
    {
        var modelo = CrearModeloValido();

        IActionResult resultado = await _controller.Crear(modelo);

        // Después de guardar debe redirigir al formulario.
        var redireccion =
            Assert.IsType<RedirectToActionResult>(resultado);

        Assert.Equal("Crear", redireccion.ActionName);

        // Comprobamos que el caso de uso preparó correctamente
        // el presupuesto que se envió al repositorio.
        Presupuesto guardado =
            Assert.IsType<Presupuesto>(_repositorio.PresupuestoGuardado);

        Assert.Equal(_clienteId, guardado.ClienteId);
        Assert.Equal(_usuarioId, guardado.ResponsableId);
        Assert.Equal(EstadoPresupuesto.Borrador, guardado.Estado);
        Assert.StartsWith("PRES-", guardado.Codigo);

        Assert.Contains(
            guardado.Codigo,
            Assert.IsType<string>(_controller.TempData["MensajeExito"]));
    }

    [Fact]
    public async Task CrearPost_ModeloInvalido_VuelveAlFormulario()
    {
        var modelo = CrearModeloValido();

        // Simulamos un error que detectaría la validación
        // de ASP.NET Core antes de ejecutar la acción.
        _controller.ModelState.AddModelError(
            nameof(modelo.NombreProyecto),
            "El nombre del proyecto es obligatorio.");

        IActionResult resultado = await _controller.Crear(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);

        Assert.Same(modelo, vista.Model);
        Assert.False(_controller.ModelState.IsValid);
        Assert.NotEmpty(modelo.Clientes);
        Assert.NotEmpty(modelo.TiposProyecto);
        Assert.Null(_repositorio.PresupuestoGuardado);
    }

    [Fact]
    public async Task CrearPost_ClienteInactivo_NoGuarda()
    {
        var cliente = await _contexto.Clientes.SingleAsync();
        cliente.Activo = false;
        await _contexto.SaveChangesAsync();

        var modelo = CrearModeloValido();

        IActionResult resultado = await _controller.Crear(modelo);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Null(_repositorio.PresupuestoGuardado);
    }

    [Fact]
    public async Task CrearPost_TipoInexistente_NoGuarda()
    {
        var modelo = CrearModeloValido();
        modelo.TipoProyectoId = 999999;

        IActionResult resultado = await _controller.Crear(modelo);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Null(_repositorio.PresupuestoGuardado);
    }

    [Fact]
    public async Task CrearPost_SinIdentidadMicrosoft_NoGuarda()
    {
        // Simulamos una solicitud sin identidad reconocible.
        _controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity());

        var modelo = CrearModeloValido();

        IActionResult resultado = await _controller.Crear(modelo);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Null(_repositorio.PresupuestoGuardado);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    // Repositorio falso: registra el objeto recibido sin SQL Server.
    private sealed class RepositorioPresupuestosFalso
        : IRepositorioPresupuestos
    {
        public Presupuesto? PresupuestoGuardado { get; private set; }

        public Task AgregarConCodigoAsignadoAsync(
            Presupuesto presupuesto,
            CancellationToken cancellationToken = default)
        {
            presupuesto.NumeroCorrelativo = 1;

            presupuesto.Codigo = GeneradorCodigoPresupuesto.Generar(
                presupuesto.FechaCreacion,
                presupuesto.NombreProyecto,
                1);

            PresupuestoGuardado = presupuesto;

            return Task.CompletedTask;
        }
    }

    // Permite que el Controller use TempData sin una sesión real.
    private sealed class ProveedorTempDataDePrueba : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
