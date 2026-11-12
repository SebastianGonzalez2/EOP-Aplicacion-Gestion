using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Consultas.Materiales;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.ViewModels.Materiales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// S2-T19 (US-010B): agregar una nueva relación material-proveedor desde el catálogo,
// recorriendo controller, caso de uso, repositorio y base (SQLite en memoria).
public class AgregarProveedorAMaterialTests : IAsyncLifetime
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private EopDbContext _contexto = null!;
    private int _materialId;
    private int _proveedorId;
    private int _otroProveedorId;

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        _contexto = new EopDbContext(new DbContextOptionsBuilder<EopDbContext>().UseSqlite(_conexion).Options);
        await _contexto.Database.EnsureCreatedAsync();

        var proveedor = DatosDePruebaProveedores.CrearProveedor("Proveedor A", "215521750017");
        var otroProveedor = DatosDePruebaProveedores.CrearProveedor("Proveedor B", "210000000019");
        var material = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        _contexto.Proveedores.AddRange(proveedor, otroProveedor);
        _contexto.Materiales.Add(material);
        await _contexto.SaveChangesAsync();

        // El material ya tiene su primera relación con el proveedor A, como exige US-041.
        _contexto.MaterialProveedores.Add(new MaterialProveedor
        {
            MaterialId = material.Id,
            ProveedorId = proveedor.Id,
            PrecioUnitario = 100m,
            Moneda = Moneda.UYU,
            FechaUltimaActualizacion = DateTime.UtcNow.AddDays(-10)
        });
        await _contexto.SaveChangesAsync();

        _materialId = material.Id;
        _proveedorId = proveedor.Id;
        _otroProveedorId = otroProveedor.Id;
    }

    public async Task DisposeAsync()
    {
        await _contexto.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    private sealed class ProveedorTempDataDePrueba : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private MaterialesController CrearController()
    {
        var httpContext = new DefaultHttpContext();

        return new(
            new ConsultarProveedoresParaSeleccion(_contexto),
            new RegistrarMaterial(new RepositorioMateriales(_contexto), new RepositorioProveedores(_contexto)),
            new ConsultarMateriales(_contexto),
            new AgregarProveedorAMaterial(new RepositorioMateriales(_contexto), new RepositorioProveedores(_contexto)),
            new EditarMaterial(new RepositorioMateriales(_contexto)))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new ProveedorTempDataDePrueba())
        };
    }

    private AgregarProveedorMaterialViewModel CrearModelo(int? proveedorId = null, decimal? precio = 250m, string moneda = "USD") => new()
    {
        MaterialId = _materialId,
        ProveedorId = proveedorId ?? _otroProveedorId,
        PrecioUnitario = precio,
        Moneda = moneda
    };

    private Task<int> ContarRelacionesAsync() => _contexto.MaterialProveedores.CountAsync();

    [Fact]
    public async Task AltaValida_GuardaLaRelacionConPrecioMonedaYFecha_YRedirigeAlCatalogo()
    {
        var controller = CrearController();
        DateTime antes = DateTime.UtcNow;

        IActionResult resultado = await controller.AgregarProveedor(CrearModelo());

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(MaterialesController.Index), redireccion.ActionName);
        Assert.Equal("Proveedor agregado correctamente al material.", controller.TempData["MensajeExito"]);

        MaterialProveedor nueva = await _contexto.MaterialProveedores.SingleAsync(relacion => relacion.ProveedorId == _otroProveedorId);
        Assert.Equal(_materialId, nueva.MaterialId);
        Assert.Equal(250m, nueva.PrecioUnitario);
        Assert.Equal(Moneda.USD, nueva.Moneda);
        Assert.True(nueva.FechaUltimaActualizacion >= antes);
        Assert.Equal(2, await ContarRelacionesAsync());
    }

    [Fact]
    public async Task AltaValida_NoModificaLaRelacionExistente()
    {
        await CrearController().AgregarProveedor(CrearModelo());

        MaterialProveedor existente = await _contexto.MaterialProveedores.SingleAsync(relacion => relacion.ProveedorId == _proveedorId);
        Assert.Equal(100m, existente.PrecioUnitario);
        Assert.Equal(Moneda.UYU, existente.Moneda);
    }

    [Fact]
    public async Task RelacionDuplicada_SeInformaYNoSeCreaOtra()
    {
        var controller = CrearController();
        var modelo = CrearModelo(proveedorId: _proveedorId);

        IActionResult resultado = await controller.AgregarProveedor(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        Assert.Contains(AgregarProveedorAMaterial.MensajeRelacionDuplicada,
            controller.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage));
        Assert.Equal(1, await ContarRelacionesAsync());
    }

    [Fact]
    public async Task ProveedorInexistente_SeRechazaSinCrearProveedorNiRelacion()
    {
        var controller = CrearController();

        IActionResult resultado = await controller.AgregarProveedor(CrearModelo(proveedorId: 999));

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(1, await ContarRelacionesAsync());
        Assert.Equal(2, await _contexto.Proveedores.CountAsync());
    }

    [Theory]
    [InlineData(null, "UYU")]
    [InlineData(0, "UYU")]
    [InlineData(-5, "USD")]
    [InlineData(100, "EUR")]
    [InlineData(100, "")]
    public async Task PrecioOMonedaInvalidos_SeRechazanSinGuardarNada(int? precio, string moneda)
    {
        var controller = CrearController();
        // Simulamos lo que haría el binder: las validaciones del ViewModel se evalúan antes de la acción.
        var modelo = CrearModelo(precio: precio, moneda: moneda);
        foreach (var error in ValidarViewModel(modelo))
        {
            controller.ModelState.AddModelError(error.Key, error.Value);
        }

        IActionResult resultado = await controller.AgregarProveedor(modelo);

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(1, await ContarRelacionesAsync());
    }

    private static IEnumerable<KeyValuePair<string, string>> ValidarViewModel(AgregarProveedorMaterialViewModel modelo)
    {
        var resultados = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            modelo, new System.ComponentModel.DataAnnotations.ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados.Select(resultado => new KeyValuePair<string, string>(
            resultado.MemberNames.FirstOrDefault() ?? string.Empty, resultado.ErrorMessage ?? string.Empty));
    }

    [Fact]
    public async Task MaterialInexistente_DevuelveNotFoundSinGuardar()
    {
        var modelo = CrearModelo();
        modelo.MaterialId = 999;

        IActionResult resultado = await CrearController().AgregarProveedor(modelo);

        Assert.IsType<NotFoundResult>(resultado);
        Assert.Equal(1, await ContarRelacionesAsync());
    }

    [Fact]
    public async Task ProveedorFivisa_SeRegistraConElPrecioManual()
    {
        // US-010B: Fivisa no tiene integración; es un proveedor más con precio ingresado a mano.
        var fivisa = DatosDePruebaProveedores.CrearProveedor("Fivisa", "211234560018");
        _contexto.Proveedores.Add(fivisa);
        await _contexto.SaveChangesAsync();

        await CrearController().AgregarProveedor(CrearModelo(proveedorId: fivisa.Id, precio: 75.5m, moneda: "UYU"));

        MaterialProveedor relacion = await _contexto.MaterialProveedores.SingleAsync(r => r.ProveedorId == fivisa.Id);
        Assert.Equal(75.5m, relacion.PrecioUnitario);
    }

    [Fact]
    public void AgregarProveedorPost_SoloPermiteSupervisorYAdministrador_YExigeTokenAntiforgery()
    {
        var metodo = typeof(MaterialesController).GetMethod(
            nameof(MaterialesController.AgregarProveedor), [typeof(AgregarProveedorMaterialViewModel)])!;

        var autorizacion = Assert.Single(metodo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("Supervisor,Administrador", autorizacion.Roles);
        Assert.NotEmpty(metodo.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));
        Assert.NotEmpty(metodo.GetCustomAttributes(typeof(HttpPostAttribute), true));
    }
}
