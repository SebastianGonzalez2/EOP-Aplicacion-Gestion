
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.Entidades.Catalogo;
using EOP.Gestion.Infrastructure.Consultas.Materiales;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion
{
    public class ConsultarMaterialesTests
    {
        [Fact]
        public async Task SinFiltros_DevuelveTodosLosMaterialesOrdenados()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            // Sin filtros deben aparecer todos los materiales.
            var resultado = await consulta.EjecutarAsync(null, null, null);

            Assert.Equal(3, resultado.Count);

            Assert.Equal(
                new[] { "Cable de aluminio", "Cable de cobre", "Pintura blanca" },
                resultado.Select(material => material.Nombre));
        }


        [Fact]
        public async Task BuscarPorNombre_IgnoraMayusculasYEspaciosExtra()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            // Comprobamos que la búsqueda normalice el texto.
            var resultado = await consulta.EjecutarAsync(
                "  cAbLe   DE   cObRe  ",
                null,
                null);

            var material = Assert.Single(resultado);

            Assert.Equal("Cable de cobre", material.Nombre);
        }


        [Fact]
        public async Task FiltrarPorUnidad_DevuelveSoloMaterialesDeEsaUnidad()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            var resultado = await consulta.EjecutarAsync(
                null,
                UnidadMedida.Litro,
                null);

            var material = Assert.Single(resultado);

            Assert.Equal("Pintura blanca", material.Nombre);
            Assert.Equal(UnidadMedida.Litro, material.Unidad);
        }


        [Fact]
        public async Task FiltrarPorProveedor_DevuelveMaterialesRelacionados()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            // Fivisa vende Cable de cobre y Pintura blanca.
            var resultado = await consulta.EjecutarAsync(
                null,
                null,
                datos.FivisaId);

            Assert.Equal(2, resultado.Count);

            Assert.Contains(resultado,
                material => material.Nombre == "Cable de cobre");

            Assert.Contains(resultado,
                material => material.Nombre == "Pintura blanca");

            Assert.DoesNotContain(resultado,
                material => material.Nombre == "Cable de aluminio");
        }


        [Fact]
        public async Task CombinarFiltros_DevuelveSoloMaterialesQueCumplenTodos()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            // Buscamos cables medidos en metros que venda Fivisa.
            var resultado = await consulta.EjecutarAsync(
                "Cable",
                UnidadMedida.Metro,
                datos.FivisaId);

            var material = Assert.Single(resultado);

            Assert.Equal("Cable de cobre", material.Nombre);
            Assert.Single(material.Referencias);
            Assert.Equal("Fivisa", material.Referencias[0].ProveedorNombre);
        }


        [Fact]
        public async Task ConsultarMaterial_DevuelvePreciosMonedasYFechasDeSusProveedores()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            var resultado = await consulta.EjecutarAsync(
                "Cable de cobre",
                null,
                null);

            var material = Assert.Single(resultado);

            // Sin filtrar por proveedor, mostramos todas las
            // referencias de precios que tiene el material.
            Assert.Equal(2, material.Referencias.Count);

            var referenciaFivisa = Assert.Single(
                material.Referencias.Where(referencia =>
                    referencia.ProveedorNombre == "Fivisa"));

            Assert.Equal(150m, referenciaFivisa.PrecioUnitario);
            Assert.Equal(Moneda.UYU, referenciaFivisa.Moneda);
            Assert.Equal(
                new DateTime(2026, 11, 1, 10, 0, 0),
                referenciaFivisa.FechaUltimaActualizacion);

            var referenciaBarraca = Assert.Single(
                material.Referencias.Where(referencia =>
                    referencia.ProveedorNombre == "Barraca Central"));

            Assert.Equal(7.50m, referenciaBarraca.PrecioUnitario);
            Assert.Equal(Moneda.USD, referenciaBarraca.Moneda);
            Assert.Equal(
                new DateTime(2026, 11, 2, 11, 0, 0),
                referenciaBarraca.FechaUltimaActualizacion);
        }


        [Fact]
        public async Task FiltrarPorProveedor_MuestraSoloLaReferenciaDelProveedorSeleccionado()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            // Buscamos el material filtrando por Fivisa.
            var resultado = await consulta.EjecutarAsync(
                "Cable de cobre",
                null,
                datos.FivisaId);

            // Debe encontrar el material.
            var material = Assert.Single(resultado);

            Assert.Equal("Cable de cobre", material.Nombre);

            // Debe mostrar solamente la referencia de Fivisa.
            var referencia = Assert.Single(material.Referencias);

            Assert.Equal("Fivisa", referencia.ProveedorNombre);
            Assert.Equal(150m, referencia.PrecioUnitario);
            Assert.Equal(Moneda.UYU, referencia.Moneda);

            Assert.Equal(
                new DateTime(2026, 11, 1, 10, 0, 0),
                referencia.FechaUltimaActualizacion);
        }


        [Fact]
        public async Task FiltrosSinCoincidencias_DevuelveListaVacia()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var consulta = new ConsultarMateriales(contexto);

            // La pintura existe, pero se vende por litro, no por metro.
            var resultado = await consulta.EjecutarAsync(
                "Pintura",
                UnidadMedida.Metro,
                datos.FivisaId);

            Assert.Empty(resultado);
        }


        [Fact]
        public async Task LasReferenciasDeUnMaterialSeOrdenanPorNombreDeProveedor()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var resultado = await new ConsultarMateriales(contexto)
                .EjecutarAsync("Cable de cobre", null, null);

            var material = Assert.Single(resultado);

            Assert.Equal(
                new[] { "Barraca Central", "Fivisa" },
                material.Referencias.Select(referencia => referencia.ProveedorNombre));
        }


        [Fact]
        public async Task MaterialSinReferencias_AparecesSinPreciosYNoApareceAlFiltrarPorProveedor()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            contexto.Materiales.Add(new Material
            {
                Nombre = "Cinta aisladora",
                Unidad = UnidadMedida.Unidad
            });

            await contexto.SaveChangesAsync();

            var consulta = new ConsultarMateriales(contexto);

            var sinFiltros = await consulta.EjecutarAsync("Cinta", null, null);
            var material = Assert.Single(sinFiltros);
            Assert.Empty(material.Referencias);

            var porProveedor = await consulta.EjecutarAsync(
                "Cinta", null, datos.FivisaId);

            Assert.Empty(porProveedor);
        }


        [Fact]
        public async Task ConsultarProveedoresParaSeleccion_DevuelveTodosOrdenadosPorNombre()
        {
            var datos = await CrearCatalogoDePruebaAsync();
            using var conexion = datos.Conexion;
            using var contexto = datos.Contexto;

            var proveedores = await new ConsultarProveedoresParaSeleccion(contexto)
                .EjecutarAsync();

            Assert.Equal(
                new[] { "Barraca Central", "Fivisa" },
                proveedores.Select(proveedor => proveedor.Nombre));

            Assert.Equal(datos.BarracaId, proveedores[0].Id);
            Assert.Equal(datos.FivisaId, proveedores[1].Id);
        }


        // Preparamos el mismo catálogo para todas las pruebas.
        // Cada prueba recibe su propia base SQLite en memoria,
        // así los resultados no dependen de otras pruebas.
        private static async Task<(
            SqliteConnection Conexion,
            EopDbContext Contexto,
            int FivisaId,
            int BarracaId)> CrearCatalogoDePruebaAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var opciones = new DbContextOptionsBuilder<EopDbContext>()
                .UseSqlite(conexion)
                .Options;

            var contexto = new EopDbContext(opciones);
            await contexto.Database.EnsureCreatedAsync();

            // Registramos dos proveedores.
            var fivisa = DatosDePruebaProveedores.CrearProveedor(
                "Fivisa", "215521750017");

            var barraca = DatosDePruebaProveedores.CrearProveedor(
                "Barraca Central", "210000000019");

            contexto.Proveedores.AddRange(fivisa, barraca);

            // Registramos tres materiales.
            var cableCobre = new Material
            {
                Nombre = "Cable de cobre",
                Unidad = UnidadMedida.Metro
            };

            var cableAluminio = new Material
            {
                Nombre = "Cable de aluminio",
                Unidad = UnidadMedida.Metro
            };

            var pintura = new Material
            {
                Nombre = "Pintura blanca",
                Unidad = UnidadMedida.Litro
            };

            contexto.Materiales.AddRange(
                cableCobre,
                cableAluminio,
                pintura);

            await contexto.SaveChangesAsync();

            // Un material puede tener precios de distintos proveedores.
            // Los precios y fechas son datos de prueba, no valores reales.
            contexto.MaterialProveedores.AddRange(
                new MaterialProveedor
                {
                    MaterialId = cableCobre.Id,
                    ProveedorId = fivisa.Id,
                    PrecioUnitario = 150m,
                    Moneda = Moneda.UYU,
                    FechaUltimaActualizacion = new DateTime(2026, 11, 1, 10, 0, 0)
                },
                new MaterialProveedor
                {
                    MaterialId = cableCobre.Id,
                    ProveedorId = barraca.Id,
                    PrecioUnitario = 7.50m,
                    Moneda = Moneda.USD,
                    FechaUltimaActualizacion = new DateTime(2026, 11, 2, 11, 0, 0)
                },
                new MaterialProveedor
                {
                    MaterialId = cableAluminio.Id,
                    ProveedorId = barraca.Id,
                    PrecioUnitario = 123m,
                    Moneda = Moneda.UYU,
                    FechaUltimaActualizacion = new DateTime(2026, 11, 3, 12, 0, 0)
                },
                new MaterialProveedor
                {
                    MaterialId = pintura.Id,
                    ProveedorId = fivisa.Id,
                    PrecioUnitario = 180m,
                    Moneda = Moneda.UYU,
                    FechaUltimaActualizacion = new DateTime(2026, 11, 4, 13, 0, 0)
                }
            );

            await contexto.SaveChangesAsync();

            return (conexion, contexto, fivisa.Id, barraca.Id);
        }
    }
}
