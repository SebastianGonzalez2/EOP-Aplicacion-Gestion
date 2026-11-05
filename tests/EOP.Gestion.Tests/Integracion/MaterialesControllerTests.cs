
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.ViewModels.Materiales;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Usuarios;
using EOP.Gestion.Core.Comun;
using Microsoft.AspNetCore.Authorization;
using EOP.Gestion.Core.CasosDeUso.Materiales;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Materiales;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;

namespace EOP.Gestion.Tests.Integracion
{
    public class MaterialesControllerTests
    {
        [Fact]
        public async Task RegistrarGet_CargaProveedoresExistentes()
        {
            // Creamos una base SQLite temporal en memoria.
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);

            await contexto.Database.EnsureCreatedAsync();

            // Registramos dos proveedores de prueba.
            var proveedorZeta = DatosDePruebaProveedores.CrearProveedor(
                "Zeta", "215521750017");

            var proveedorAlfa = DatosDePruebaProveedores.CrearProveedor(
                "Alfa", "210000000019");

            contexto.Proveedores.AddRange(proveedorZeta, proveedorAlfa);
            await contexto.SaveChangesAsync();

            // Preparamos el Controller con su servicio de consulta.
            var consulta = new ConsultarProveedoresParaSeleccion(contexto);

            // El controlador ahora también necesita el caso de uso
            // que se encarga de registrar materiales.
            var registrarMaterial = new RegistrarMaterial(
                new RepositorioMateriales(contexto),
                new RepositorioProveedores(contexto));

            var controller = new MaterialesController(
                consulta,
                registrarMaterial)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Ejecutamos el método GET del Controller.
            IActionResult resultado = await controller.Registrar();

            // Comprobamos que devuelve la vista con el ViewModel.
            ViewResult vista = Assert.IsType<ViewResult>(resultado);

            RegistrarMaterialViewModel modelo =
                Assert.IsType<RegistrarMaterialViewModel>(vista.Model);

            // Comprobamos que contiene los proveedores ordenados.
            Assert.Collection(modelo.Proveedores,
                primero =>
                {
                    Assert.Equal("Alfa", primero.Text);
                    Assert.Equal(proveedorAlfa.Id.ToString(), primero.Value);
                },
                segundo =>
                {
                    Assert.Equal("Zeta", segundo.Text);
                    Assert.Equal(proveedorZeta.Id.ToString(), segundo.Value);
                });
        }



        [Fact]
        public async Task RegistrarPost_ProveedorInexistente_RechazaElRegistro()
        {
            // Creamos una base de datos temporal.
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);

            await contexto.Database.EnsureCreatedAsync();

            // Guardamos un proveedor real para la prueba.
            var proveedor = DatosDePruebaProveedores.CrearProveedor();

            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();

            // Preparamos el Controller con su servicio de consulta.
            var consulta = new ConsultarProveedoresParaSeleccion(contexto);

            var registrarMaterial = new RegistrarMaterial(
                new RepositorioMateriales(contexto),
                new RepositorioProveedores(contexto));

            var controller = new MaterialesController(
                consulta,
                registrarMaterial)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Enviamos datos correctos, pero con un proveedor inexistente.
            var modelo = new RegistrarMaterialViewModel
            {
                Nombre = "Cable de cobre",
                Unidad = UnidadMedida.Metro,
                ProveedorId = 999,
                PrecioUnitario = 150,
                Moneda = "UYU"
            };

            // Ejecutamos el POST.
            IActionResult resultado = await controller.Registrar(modelo);

            // Debe volver al formulario con un error.
            ViewResult vista = Assert.IsType<ViewResult>(resultado);

            Assert.Same(modelo, vista.Model);
            Assert.False(controller.ModelState.IsValid);

            // Comprobamos que no se guardó ningún material.
            Assert.Empty(await contexto.Materiales.ToListAsync());
        }



        [Fact]
        public async Task RegistrarPost_DatosInvalidos_NoGuardaMaterial()
        {
            // Creamos una base de datos temporal para la prueba.
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Preparamos el Controller.
            var consulta = new ConsultarProveedoresParaSeleccion(contexto);

            var registrarMaterial = new RegistrarMaterial(
                new RepositorioMateriales(contexto),
                new RepositorioProveedores(contexto));

            var controller = new MaterialesController(
                consulta,
                registrarMaterial)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Simulamos un formulario con un precio inválido.
            var modelo = new RegistrarMaterialViewModel
            {
                Nombre = "Cable de cobre",
                Unidad = UnidadMedida.Metro,
                ProveedorId = 1,
                PrecioUnitario = 0,
                Moneda = "UYU"
            };

            // Simulamos el error de validación que detectaría ASP.NET Core.
            controller.ModelState.AddModelError(
                nameof(modelo.PrecioUnitario),
                "El precio unitario debe ser mayor que cero.");

            // Ejecutamos el POST.
            IActionResult resultado = await controller.Registrar(modelo);

            // Debe volver al formulario conservando los datos.
            ViewResult vista = Assert.IsType<ViewResult>(resultado);

            Assert.Same(modelo, vista.Model);
            Assert.False(controller.ModelState.IsValid);

            // No debe haberse registrado ningún material.
            Assert.Empty(await contexto.Materiales.ToListAsync());
        }



        // Esta prueba comprueba que el formulario realmente guarde
        // el material y su primera relación con el proveedor.
        [Fact]
        public async Task RegistrarPost_DatosValidos_GuardaMaterialYPrimeraRelacion()
        {
            // Creamos una base SQLite temporal.
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Registramos el proveedor que seleccionaremos.
            var proveedor = DatosDePruebaProveedores.CrearProveedor();

            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();

            // Preparamos las dependencias del controlador.
            var consulta = new ConsultarProveedoresParaSeleccion(contexto);

            var registrarMaterial = new RegistrarMaterial(
                new RepositorioMateriales(contexto),
                new RepositorioProveedores(contexto));

            var httpContext = new DefaultHttpContext();

            var controller = new MaterialesController(
                consulta,
                registrarMaterial)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },

                // TempData es necesario porque el controlador guarda
                // el mensaje que se mostrará después de redirigir.
                TempData = new TempDataDictionary(
                    httpContext,
                    new ProveedorTempDataDePrueba())
            };

            var modelo = new RegistrarMaterialViewModel
            {
                Nombre = "Cable de cobre",
                Unidad = UnidadMedida.Metro,
                ProveedorId = proveedor.Id,
                PrecioUnitario = 150,
                Moneda = "UYU"
            };

            // Ejecutamos el registro.
            IActionResult resultado = await controller.Registrar(modelo);

            // Si todo salió bien, debe redirigir al formulario.
            RedirectToActionResult redireccion =
                Assert.IsType<RedirectToActionResult>(resultado);

            Assert.Equal("Registrar", redireccion.ActionName);

            // Comprobamos que guardó un solo material.
            Material material = Assert.Single(
                await contexto.Materiales.ToListAsync());

            Assert.Equal("Cable de cobre", material.Nombre);
            Assert.Equal(UnidadMedida.Metro, material.Unidad);

            // También debe haberse guardado la primera relación
            // con el proveedor, su precio, moneda y fecha.
            MaterialProveedor relacion = Assert.Single(
                await contexto.MaterialProveedores.ToListAsync());

            Assert.Equal(material.Id, relacion.MaterialId);
            Assert.Equal(proveedor.Id, relacion.ProveedorId);
            Assert.Equal(150m, relacion.PrecioUnitario);
            Assert.Equal(Moneda.UYU, relacion.Moneda);
            Assert.NotEqual(default(DateTime), relacion.FechaUltimaActualizacion);

            // El usuario debe recibir la confirmación del registro.
            Assert.Equal(
                "Material registrado correctamente.",
                controller.TempData["MensajeExito"]);
        }



        // Un material con el mismo nombre normalizado
        // no debe volver a registrarse.
        [Fact]
        public async Task RegistrarPost_NombreDuplicado_NoGuardaOtroMaterial()
        {
            using var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            using var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            var proveedor = DatosDePruebaProveedores.CrearProveedor();

            contexto.Proveedores.Add(proveedor);

            // Ya existe este material en el catálogo.
            contexto.Materiales.Add(new Material
            {
                Nombre = "Cable de cobre",
                Unidad = UnidadMedida.Metro
            });

            await contexto.SaveChangesAsync();

            var consulta = new ConsultarProveedoresParaSeleccion(contexto);

            var registrarMaterial = new RegistrarMaterial(
                new RepositorioMateriales(contexto),
                new RepositorioProveedores(contexto));

            var controller = new MaterialesController(
                consulta,
                registrarMaterial)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // El nombre es el mismo, aunque cambien
            // las mayúsculas y los espacios.
            var modelo = new RegistrarMaterialViewModel
            {
                Nombre = "  CABLE   DE   COBRE  ",
                Unidad = UnidadMedida.Metro,
                ProveedorId = proveedor.Id,
                PrecioUnitario = 150,
                Moneda = "UYU"
            };

            IActionResult resultado = await controller.Registrar(modelo);

            // Debe volver al formulario con un error.
            ViewResult vista = Assert.IsType<ViewResult>(resultado);

            Assert.Same(modelo, vista.Model);
            Assert.False(controller.ModelState.IsValid);

            // No puede haber registrado un segundo material.
            Assert.Equal(1, await contexto.Materiales.CountAsync());

            // Tampoco puede haber guardado una relación nueva.
            Assert.Empty(await contexto.MaterialProveedores.ToListAsync());
        }



        [Fact]
        public void MaterialesController_TieneLosPermisosCorrectos()
        {
            // Obtenemos el atributo Authorize del controlador.
            var atributos = typeof(MaterialesController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true);

            var autorizacion = Assert.IsType<AuthorizeAttribute>(
                Assert.Single(atributos));

            // Comprobamos los roles permitidos.
            Assert.NotNull(autorizacion.Roles);

            string[] rolesPermitidos = autorizacion.Roles.Split(',');

            Assert.Contains(nameof(RolUsuario.Administrador), rolesPermitidos);
            Assert.Contains(nameof(RolUsuario.Supervisor), rolesPermitidos);

            // El Operador no puede registrar materiales.
            Assert.DoesNotContain(nameof(RolUsuario.Operador), rolesPermitidos);

            // Deben estar habilitados únicamente esos dos roles.
            Assert.Equal(2, rolesPermitidos.Length);
        }



        [Fact]
        public void RegistrarPost_EstaProtegidoContraCSRF()
        {
            // Buscamos el método Registrar que recibe un ViewModel.
            // Así identificamos el POST y no el GET, porque ambos
            // métodos tienen el mismo nombre.
            var metodo = typeof(MaterialesController).GetMethod(
                nameof(MaterialesController.Registrar),
                new[] { typeof(RegistrarMaterialViewModel) });

            Assert.NotNull(metodo);

            // Comprobamos que el método recibe solicitudes POST.
            var atributoPost = metodo.GetCustomAttributes(
                typeof(HttpPostAttribute), true);

            Assert.Single(atributoPost);

            // Comprobamos que valida el token antifalsificación.
            var atributoAntiforgery = metodo.GetCustomAttributes(
                typeof(ValidateAntiForgeryTokenAttribute), true);

            Assert.Single(atributoAntiforgery);
        }



        // Implementación sencilla de TempData para las pruebas.
        // No necesitamos una sesión real del navegador.
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
                // No necesitamos guardar datos entre solicitudes
                // porque esta prueba ejecuta una sola acción.
            }
        }
    }
}
