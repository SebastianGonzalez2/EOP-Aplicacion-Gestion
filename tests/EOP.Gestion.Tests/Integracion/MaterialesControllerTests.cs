
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using EOP.Gestion.WebApp.Controllers;
using EOP.Gestion.WebApp.ViewModels.Materiales;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Core.Entidades.Usuarios;
using Microsoft.AspNetCore.Authorization;

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

            var controller = new MaterialesController(consulta)
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

            var controller = new MaterialesController(consulta)
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

            var controller = new MaterialesController(consulta)
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


    }
}
