using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Materiales;
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

// S2-T20 (US-071): editar un material y sus referencias de precio existentes, recorriendo
// controller, caso de uso, repositorio y base (SQLite en memoria).
public class EditarMaterialTests : IAsyncLifetime
{
    private static readonly DateTime FechaVieja = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private EopDbContext _contexto = null!;
    private int _materialId;
    private int _otroMaterialId;
    private int _relacionAId;
    private int _relacionBId;
    private int _relacionDeOtroMaterialId;

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        _contexto = new EopDbContext(new DbContextOptionsBuilder<EopDbContext>().UseSqlite(_conexion).Options);
        await _contexto.Database.EnsureCreatedAsync();

        var proveedorA = DatosDePruebaProveedores.CrearProveedor("Proveedor A", "215521750017");
        var proveedorB = DatosDePruebaProveedores.CrearProveedor("Fivisa", "210000000019");
        var material = new Material { Nombre = "Cable de cobre", Unidad = UnidadMedida.Metro };
        var otroMaterial = new Material { Nombre = "Caño de PVC", Unidad = UnidadMedida.Metro };
        _contexto.Proveedores.AddRange(proveedorA, proveedorB);
        _contexto.Materiales.AddRange(material, otroMaterial);
        await _contexto.SaveChangesAsync();

        var relacionA = CrearRelacion(material.Id, proveedorA.Id, 100m, Moneda.UYU);
        var relacionB = CrearRelacion(material.Id, proveedorB.Id, 50m, Moneda.USD);
        var relacionDeOtroMaterial = CrearRelacion(otroMaterial.Id, proveedorA.Id, 70m, Moneda.UYU);
        _contexto.MaterialProveedores.AddRange(relacionA, relacionB, relacionDeOtroMaterial);
        await _contexto.SaveChangesAsync();

        _materialId = material.Id;
        _otroMaterialId = otroMaterial.Id;
        _relacionAId = relacionA.Id;
        _relacionBId = relacionB.Id;
        _relacionDeOtroMaterialId = relacionDeOtroMaterial.Id;
    }

    public async Task DisposeAsync()
    {
        await _contexto.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    private static MaterialProveedor CrearRelacion(int materialId, int proveedorId, decimal precio, Moneda moneda) => new()
    {
        MaterialId = materialId,
        ProveedorId = proveedorId,
        PrecioUnitario = precio,
        Moneda = moneda,
        FechaUltimaActualizacion = FechaVieja
    };

    private sealed class ProveedorTempDataDePrueba : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private MaterialesController CrearController()
    {
        var httpContext = new DefaultHttpContext();
        var repositorioMateriales = new RepositorioMateriales(_contexto);

        return new(
            new ConsultarProveedoresParaSeleccion(_contexto),
            new RegistrarMaterial(repositorioMateriales, new RepositorioProveedores(_contexto)),
            new ConsultarMateriales(_contexto),
            new AgregarProveedorAMaterial(repositorioMateriales, new RepositorioProveedores(_contexto)),
            new EditarMaterial(repositorioMateriales))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new ProveedorTempDataDePrueba())
        };
    }

    // El formulario tal como llegaría sin tocar nada, con los valores actuales del material.
    private EditarMaterialViewModel CrearModelo() => new()
    {
        MaterialId = _materialId,
        Nombre = "Cable de cobre",
        Unidad = UnidadMedida.Metro,
        Referencias =
        [
            new() { RelacionId = _relacionAId, PrecioUnitario = 100m, Moneda = "UYU" },
            new() { RelacionId = _relacionBId, PrecioUnitario = 50m, Moneda = "USD" }
        ]
    };

    // Lee la base con un contexto nuevo, para no ver lo que quedó en memoria del contexto de la prueba.
    private async Task<(Material Material, List<MaterialProveedor> Relaciones)> LeerEstadoAsync(int materialId)
    {
        using var contexto = new EopDbContext(new DbContextOptionsBuilder<EopDbContext>().UseSqlite(_conexion).Options);
        var material = await contexto.Materiales.AsNoTracking().SingleAsync(m => m.Id == materialId);
        var relaciones = await contexto.MaterialProveedores.AsNoTracking().Where(r => r.MaterialId == materialId).OrderBy(r => r.Id).ToListAsync();

        return (material, relaciones);
    }

    private static IEnumerable<string> ErroresGenerales(Controller controller) =>
        controller.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage);

    // ---------- GET ----------

    [Fact]
    public async Task Get_MuestraLosDatosActualesDelMaterialYSusReferencias()
    {
        IActionResult resultado = await CrearController().Editar(_materialId);

        var modelo = Assert.IsType<EditarMaterialViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(_materialId, modelo.MaterialId);
        Assert.Equal("Cable de cobre", modelo.Nombre);
        Assert.Equal(UnidadMedida.Metro, modelo.Unidad);
        Assert.Equal(2, modelo.Referencias.Count);

        // Vienen ordenadas por nombre de proveedor: Fivisa y después Proveedor A.
        Assert.Equal("Fivisa", modelo.Referencias[0].ProveedorNombre);
        Assert.Equal(_relacionBId, modelo.Referencias[0].RelacionId);
        Assert.Equal(50m, modelo.Referencias[0].PrecioUnitario);
        Assert.Equal("USD", modelo.Referencias[0].Moneda);
        Assert.Equal(FechaVieja, modelo.Referencias[0].FechaUltimaActualizacion);
        Assert.Equal("Proveedor A", modelo.Referencias[1].ProveedorNombre);
    }

    [Fact]
    public async Task Get_MaterialInexistente_DevuelveNotFound()
    {
        Assert.IsType<NotFoundResult>(await CrearController().Editar(999));
    }

    // ---------- POST: caminos válidos ----------

    [Fact]
    public async Task ModificarNombreYUnidad_ActualizaElMismoMaterialYConservaSusRelaciones()
    {
        var modelo = CrearModelo();
        modelo.Nombre = "  Cable de cobre 2,5 mm  ";
        modelo.Unidad = UnidadMedida.Kilogramo;
        var controller = CrearController();

        IActionResult resultado = await controller.Editar(modelo);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(MaterialesController.Index), redireccion.ActionName);
        Assert.Equal("Material actualizado correctamente.", controller.TempData["MensajeExito"]);

        var (material, relaciones) = await LeerEstadoAsync(_materialId);
        Assert.Equal(_materialId, material.Id);
        Assert.Equal("Cable de cobre 2,5 mm", material.Nombre);
        Assert.Equal("CABLE DE COBRE 2,5 MM", material.NombreNormalizado);
        Assert.Equal(UnidadMedida.Kilogramo, material.Unidad);

        // Las relaciones siguen siendo las mismas filas (mismos Ids) y sin cambios.
        Assert.Equal([_relacionAId, _relacionBId], relaciones.Select(r => r.Id));
        Assert.All(relaciones, relacion => Assert.Equal(FechaVieja, relacion.FechaUltimaActualizacion));
        Assert.Equal(2, await _contexto.Materiales.CountAsync());
    }

    [Fact]
    public async Task ModificarPrecio_GuardaElNuevoPrecioYActualizaLaFecha_SinTocarLasOtrasReferencias()
    {
        var modelo = CrearModelo();
        modelo.Referencias[0].PrecioUnitario = 125.5m;
        DateTime antes = DateTime.UtcNow;

        await CrearController().Editar(modelo);

        var (_, relaciones) = await LeerEstadoAsync(_materialId);
        var modificada = relaciones.Single(r => r.Id == _relacionAId);
        Assert.Equal(125.5m, modificada.PrecioUnitario);
        Assert.Equal(Moneda.UYU, modificada.Moneda);
        Assert.True(modificada.FechaUltimaActualizacion >= antes);

        var intacta = relaciones.Single(r => r.Id == _relacionBId);
        Assert.Equal(50m, intacta.PrecioUnitario);
        Assert.Equal(Moneda.USD, intacta.Moneda);
        Assert.Equal(FechaVieja, intacta.FechaUltimaActualizacion);
    }

    [Fact]
    public async Task ModificarSoloLaMoneda_ConservaElPrecioYActualizaLaFecha()
    {
        var modelo = CrearModelo();
        modelo.Referencias[0].Moneda = "USD";
        DateTime antes = DateTime.UtcNow;

        await CrearController().Editar(modelo);

        var (_, relaciones) = await LeerEstadoAsync(_materialId);
        var modificada = relaciones.Single(r => r.Id == _relacionAId);
        Assert.Equal(Moneda.USD, modificada.Moneda);
        Assert.Equal(100m, modificada.PrecioUnitario);
        Assert.True(modificada.FechaUltimaActualizacion >= antes);
    }

    [Fact]
    public async Task GuardarSinCambiosEconomicos_NoModificaLasFechas()
    {
        await CrearController().Editar(CrearModelo());

        var (_, relaciones) = await LeerEstadoAsync(_materialId);
        Assert.All(relaciones, relacion => Assert.Equal(FechaVieja, relacion.FechaUltimaActualizacion));
    }

    [Fact]
    public async Task PrecioDeFivisa_SeActualizaManualmente()
    {
        var modelo = CrearModelo();
        modelo.Referencias[1].PrecioUnitario = 55m;

        await CrearController().Editar(modelo);

        var (_, relaciones) = await LeerEstadoAsync(_materialId);
        Assert.Equal(55m, relaciones.Single(r => r.Id == _relacionBId).PrecioUnitario);
    }

    [Theory]
    [InlineData("CABLE DE COBRE")]
    [InlineData("  cable   de   cobre ")]
    public async Task ConservarElNombreConOtraEscritura_NoChocaConElPropioMaterial(string nombre)
    {
        var modelo = CrearModelo();
        modelo.Nombre = nombre;

        IActionResult resultado = await CrearController().Editar(modelo);

        Assert.IsType<RedirectToActionResult>(resultado);
    }

    [Fact]
    public async Task EditarElMaterial_NoAlteraLosDatosDeOtrosMateriales()
    {
        var modelo = CrearModelo();
        modelo.Nombre = "Otro nombre";
        modelo.Referencias[0].PrecioUnitario = 999m;

        await CrearController().Editar(modelo);

        var (otro, relaciones) = await LeerEstadoAsync(_otroMaterialId);
        Assert.Equal("Caño de PVC", otro.Nombre);
        Assert.Equal(70m, Assert.Single(relaciones).PrecioUnitario);
    }

    // ---------- POST: caminos inválidos (sin cambios parciales) ----------

    // Verifica que no quedó NADA modificado: ni el material ni ninguna de sus relaciones.
    private async Task AssertNadaCambioAsync()
    {
        var (material, relaciones) = await LeerEstadoAsync(_materialId);
        Assert.Equal("Cable de cobre", material.Nombre);
        Assert.Equal(UnidadMedida.Metro, material.Unidad);
        Assert.Equal([100m, 50m], relaciones.Select(r => r.PrecioUnitario));
        Assert.Equal([Moneda.UYU, Moneda.USD], relaciones.Select(r => r.Moneda));
        Assert.All(relaciones, relacion => Assert.Equal(FechaVieja, relacion.FechaUltimaActualizacion));
    }

    [Theory]
    [InlineData("Caño de PVC")]
    [InlineData("  CAÑO   de pvc ")]
    public async Task NombreDuplicadoDeOtroMaterial_SeRechazaSinCambiosParciales(string nombreDuplicado)
    {
        var modelo = CrearModelo();
        modelo.Nombre = nombreDuplicado;
        modelo.Unidad = UnidadMedida.Litro;
        modelo.Referencias[0].PrecioUnitario = 999m;
        var controller = CrearController();

        IActionResult resultado = await controller.Editar(modelo);

        Assert.Same(modelo, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Contains(RegistrarMaterial.MensajeNombreDuplicado, ErroresGenerales(controller));
        await AssertNadaCambioAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NombreVacio_SeRechazaSinCambios(string nombre)
    {
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, nombre, UnidadMedida.Metro, []);

        Assert.Contains("El nombre del material es obligatorio.", errores);
        await AssertNadaCambioAsync();
    }

    [Fact]
    public async Task NombreDemasiadoLargo_SeRechazaSinCambios()
    {
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, new string('a', RegistrarMaterial.LargoMaximoNombre + 1), UnidadMedida.Metro, []);

        Assert.Single(errores);
        await AssertNadaCambioAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(999)]
    [InlineData(0)]
    public async Task UnidadInvalida_SeRechazaSinCambios(int? unidad)
    {
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, "Nuevo nombre", (UnidadMedida?)unidad, []);

        Assert.Contains("Seleccioná una unidad de medida válida.", errores);
        await AssertNadaCambioAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task PrecioInvalido_SeRechazaYSeConservaElValorAnterior(int? precio)
    {
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, "Nuevo nombre", UnidadMedida.Litro,
        [
            new() { RelacionId = _relacionAId, PrecioUnitario = 500m, Moneda = Moneda.USD },
            new() { RelacionId = _relacionBId, PrecioUnitario = precio, Moneda = Moneda.USD }
        ]);

        Assert.Equal(["El precio unitario debe ser mayor que cero."], errores);
        await AssertNadaCambioAsync();
    }

    [Fact]
    public async Task MonedaInvalida_SeRechazaSinCambios()
    {
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, "Nuevo nombre", UnidadMedida.Litro,
            [new() { RelacionId = _relacionAId, PrecioUnitario = 500m, Moneda = (Moneda)99 }]);

        Assert.Equal(["La moneda debe ser UYU o USD."], errores);
        await AssertNadaCambioAsync();
    }

    [Fact]
    public async Task ControllerConMonedaQueNoEsUyuNiUsd_SeRechazaSinCambios()
    {
        var modelo = CrearModelo();
        modelo.Nombre = "Nuevo nombre";
        modelo.Referencias[0].Moneda = "EUR";
        var controller = CrearController();

        IActionResult resultado = await controller.Editar(modelo);

        Assert.IsType<ViewResult>(resultado);
        Assert.Contains("La moneda debe ser UYU o USD.", ErroresGenerales(controller));
        await AssertNadaCambioAsync();
    }

    [Fact]
    public async Task ReferenciaDeOtroMaterial_SeRechazaYNoSeTocaNada()
    {
        // Alguien arma el POST a mano con el Id de una relación que pertenece a otro material.
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, "Nuevo nombre", UnidadMedida.Litro,
        [
            new() { RelacionId = _relacionAId, PrecioUnitario = 500m, Moneda = Moneda.USD },
            new() { RelacionId = _relacionDeOtroMaterialId, PrecioUnitario = 1m, Moneda = Moneda.USD }
        ]);

        Assert.Equal(["Una de las referencias de precio no pertenece a este material."], errores);
        await AssertNadaCambioAsync();
        var (_, relacionesDelOtro) = await LeerEstadoAsync(_otroMaterialId);
        Assert.Equal(70m, Assert.Single(relacionesDelOtro).PrecioUnitario);
    }

    [Fact]
    public async Task ReferenciaInexistente_SeRechazaSinCambios()
    {
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, "Nuevo nombre", UnidadMedida.Litro,
            [new() { RelacionId = 99999, PrecioUnitario = 5m, Moneda = Moneda.UYU }]);

        Assert.Single(errores);
        await AssertNadaCambioAsync();
    }

    [Fact]
    public async Task ReferenciaRepetida_SeRechazaSinCambios()
    {
        var errores = await CrearCasoDeUso().EjecutarAsync(_materialId, "Nuevo nombre", UnidadMedida.Litro,
        [
            new() { RelacionId = _relacionAId, PrecioUnitario = 5m, Moneda = Moneda.UYU },
            new() { RelacionId = _relacionAId, PrecioUnitario = 6m, Moneda = Moneda.UYU }
        ]);

        Assert.Equal(["Una referencia de precio llegó repetida."], errores);
        await AssertNadaCambioAsync();
    }

    [Fact]
    public async Task MaterialInexistente_Post_DevuelveNotFound()
    {
        var modelo = CrearModelo();
        modelo.MaterialId = 999;

        Assert.IsType<NotFoundResult>(await CrearController().Editar(modelo));
    }

    [Fact]
    public async Task ModelStateInvalido_VuelveAMostrarElFormularioConProveedoresYFechas()
    {
        var modelo = CrearModelo();
        var controller = CrearController();
        controller.ModelState.AddModelError(nameof(EditarMaterialViewModel.Nombre), "Error de prueba");

        IActionResult resultado = await controller.Editar(modelo);

        Assert.Same(modelo, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("Proveedor A", modelo.Referencias[0].ProveedorNombre);
        Assert.Equal("Fivisa", modelo.Referencias[1].ProveedorNombre);
        Assert.Equal(FechaVieja, modelo.Referencias[0].FechaUltimaActualizacion);
        await AssertNadaCambioAsync();
    }

    // ---------- Seguridad ----------

    [Theory]
    [InlineData(typeof(HttpGetAttribute), new[] { typeof(int) })]
    [InlineData(typeof(HttpPostAttribute), new[] { typeof(EditarMaterialViewModel) })]
    public void Editar_SoloPermiteSupervisorYAdministrador_EnGetYEnPost(Type verboHttp, Type[] parametros)
    {
        var metodo = typeof(MaterialesController).GetMethod(nameof(MaterialesController.Editar), parametros)!;

        var autorizacion = Assert.Single(metodo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("Supervisor,Administrador", autorizacion.Roles);
        Assert.NotEmpty(metodo.GetCustomAttributes(verboHttp, true));
    }

    [Fact]
    public void EditarPost_ExigeTokenAntiforgery()
    {
        var metodo = typeof(MaterialesController).GetMethod(nameof(MaterialesController.Editar), [typeof(EditarMaterialViewModel)])!;

        Assert.NotEmpty(metodo.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));
    }

    [Fact]
    public void ViewModel_ProveedorYFechaNoSeEnlazanDesdeElFormulario()
    {
        // Si se enlazaran, un formulario manipulado podría mostrar otro proveedor o otra fecha.
        foreach (string propiedad in new[] { nameof(EditarReferenciaPrecioViewModel.ProveedorNombre), nameof(EditarReferenciaPrecioViewModel.FechaUltimaActualizacion) })
        {
            var atributos = typeof(EditarReferenciaPrecioViewModel).GetProperty(propiedad)!
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ModelBinding.BindNeverAttribute), true);
            Assert.NotEmpty(atributos);
        }
    }

    private EditarMaterial CrearCasoDeUso() => new(new RepositorioMateriales(_contexto));
}
