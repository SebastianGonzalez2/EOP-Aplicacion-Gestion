using EOP.Gestion.Core.CasosDeUso.Proveedores;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Core.Interfaces.Proveedores;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Infrastructure.Persistencia.Repositorios.Proveedores;
using EOP.Gestion.Infrastructure.Telefonia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.EntityFrameworkCore;

namespace EOP.Gestion.Tests.Integracion;

// El alta completa contra un SQL Server real, con el esquema que generan las migraciones.
//
// Esto no se puede probar con SQLite: los códigos de error, los nombres de los índices en el
// mensaje, los límites de largo y las esperas entre dos transacciones son propios de SQL Server,
// y es justamente lo que RepositorioProveedores tiene que reconocer.
//
// Cada "solicitud" usa su propio DbContext y por lo tanto su propia conexión, como dos pedidos
// HTTP distintos de la aplicación real.
public class RegistrarProveedorSqlServerTests : IAsyncLifetime
{
    private const string PaisValido = "UY";
    private const string NumeroValido = "098 765 432";
    private const int RepeticionesDeLaCarrera = 15;
    private const int SolicitudesSimultaneas = 4;

    private readonly string _cadenaConexion = ServidorSqlDePrueba.CrearCadenaParaBaseNueva();

    public async Task InitializeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!ServidorSqlDePrueba.EstaDisponible)
        {
            return;
        }

        using EopDbContext contexto = CrearContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    private EopDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<EopDbContext>()
            .UseSqlServer(_cadenaConexion)
            .Options;

        return new EopDbContext(opciones);
    }

    private static RegistrarProveedor CrearCasoDeUso(IRepositorioProveedores repositorio)
    {
        return new RegistrarProveedor(new ValidarDatosProveedor(repositorio, new ServicioTelefonos()), repositorio);
    }

    private async Task GuardarAsync(Proveedor proveedor)
    {
        using EopDbContext contexto = CrearContexto();
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();
    }

    private async Task<ResultadoAgregarProveedor> AgregarConContextoNuevoAsync(Proveedor proveedor)
    {
        using EopDbContext contexto = CrearContexto();

        return await new RepositorioProveedores(contexto).AgregarAsync(proveedor);
    }

    private async Task ComprobarCantidadesAsync(int proveedoresEsperados, int cuentasEsperadas)
    {
        using EopDbContext contexto = CrearContexto();

        Assert.Equal(proveedoresEsperados, await contexto.Proveedores.CountAsync());
        Assert.Equal(cuentasEsperadas, await contexto.CuentasBancarias.CountAsync());
    }

    // ---------- Guardado completo ----------

    [FactConSqlServer]
    public async Task EjecutarAsync_DebeGuardarElProveedorNormalizadoConTodasSusCuentas()
    {
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor("Construcciones Pérez", "21.552.175-0017");
        proveedorNuevo.CorreoFacturacion = "facturas@ejemplo.com";
        proveedorNuevo.CuentasBancarias.Add(new CuentaBancaria
        {
            Banco = Banco.BancoSantander,
            NumeroCuenta = "000-222",
            Titular = "Otro titular",
            Moneda = Moneda.USD
        });

        using (EopDbContext contexto = CrearContexto())
        {
            List<string> errores = await CrearCasoDeUso(new RepositorioProveedores(contexto)).EjecutarAsync(proveedorNuevo, PaisValido, NumeroValido);

            Assert.Empty(errores);
        }

        using EopDbContext contextoDeLectura = CrearContexto();
        Proveedor guardado = await contextoDeLectura.Proveedores
            .Include(p => p.CuentasBancarias)
            .SingleAsync();

        Assert.Equal("Construcciones Pérez", guardado.Nombre);
        Assert.Equal("CONSTRUCCIONES PEREZ", guardado.NombreNormalizado);
        Assert.Equal("215521750017", guardado.Rut);
        Assert.Equal("+59898765432", guardado.Telefono);
        Assert.Equal("contacto@ejemplo.com", guardado.CorreoContacto);
        Assert.Equal("facturas@ejemplo.com", guardado.CorreoFacturacion);

        Assert.Equal(2, guardado.CuentasBancarias.Count);
        Assert.All(guardado.CuentasBancarias, cuenta => Assert.Equal(guardado.Id, cuenta.ProveedorId));

        CuentaBancaria segundaCuenta = guardado.CuentasBancarias.Single(cuenta => cuenta.NumeroCuenta == "000-222");
        Assert.Equal(Banco.BancoSantander, segundaCuenta.Banco);
        Assert.Equal("Otro titular", segundaCuenta.Titular);
        Assert.Equal(Moneda.USD, segundaCuenta.Moneda);
    }

    [FactConSqlServer]
    public async Task ProveedorRegistrado_DebeAparecerEnLaConsultaParaSeleccionUnaSolaVez()
    {
        Proveedor proveedorNuevo = DatosDePruebaProveedores.CrearProveedor("Fivisa S.A.", "215521750017");
        proveedorNuevo.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));

        ResultadoAgregarProveedor resultado = await AgregarConContextoNuevoAsync(proveedorNuevo);
        Assert.Equal(ResultadoAgregarProveedor.Agregado, resultado);

        using EopDbContext contexto = CrearContexto();
        List<ProveedorOpcionDto> opciones = await new ConsultarProveedoresParaSeleccion(contexto).EjecutarAsync();

        ProveedorOpcionDto opcion = Assert.Single(opciones);
        Assert.Equal("Fivisa S.A.", opcion.Nombre);
        Assert.Equal(proveedorNuevo.Id, opcion.Id);

        // Consultar el selector no puede crear ni cambiar registros (RF-23).
        await ComprobarCantidadesAsync(proveedoresEsperados: 1, cuentasEsperadas: 2);
    }

    // ---------- Reconocer los duplicados ----------

    [FactConSqlServer]
    public async Task AgregarAsync_ConNombreRepetido_DebeInformarloSinGuardarNadaNuevo()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa", "215521750017"));

        Proveedor proveedorRepetido = DatosDePruebaProveedores.CrearProveedor(" FÍVISA ", "210000000019");
        proveedorRepetido.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));

        ResultadoAgregarProveedor resultado = await AgregarConContextoNuevoAsync(proveedorRepetido);

        Assert.Equal(ResultadoAgregarProveedor.NombreDuplicado, resultado);
        await ComprobarCantidadesAsync(proveedoresEsperados: 1, cuentasEsperadas: 1);
    }

    [FactConSqlServer]
    public async Task AgregarAsync_ConRutRepetido_DebeInformarloSinGuardarNadaNuevo()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Primero", "215521750017"));

        ResultadoAgregarProveedor resultado = await AgregarConContextoNuevoAsync(DatosDePruebaProveedores.CrearProveedor("Segundo", "215521750017"));

        Assert.Equal(ResultadoAgregarProveedor.RutDuplicado, resultado);
        await ComprobarCantidadesAsync(proveedoresEsperados: 1, cuentasEsperadas: 1);
    }

    // Si el rechazado quedara anotado en el contexto, el próximo guardado del mismo pedido lo
    // intentaría insertar de nuevo y fallaría por un proveedor que ya no le importa a nadie.
    [FactConSqlServer]
    public async Task AgregarAsync_DespuesDeUnDuplicado_DebeDejarElContextoEnCondicionesDeSeguir()
    {
        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa", "215521750017"));

        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioProveedores(contexto);

        ResultadoAgregarProveedor rechazado = await repositorio.AgregarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa", "210000000019"));
        Assert.Equal(ResultadoAgregarProveedor.NombreDuplicado, rechazado);
        Assert.Empty(contexto.ChangeTracker.Entries());

        ResultadoAgregarProveedor aceptado = await repositorio.AgregarAsync(DatosDePruebaProveedores.CrearProveedor("Otro proveedor", "210000000019"));
        Assert.Equal(ResultadoAgregarProveedor.Agregado, aceptado);

        await ComprobarCantidadesAsync(proveedoresEsperados: 2, cuentasEsperadas: 2);
    }

    // ---------- No confundir otros errores con duplicados, ni guardar a medias ----------

    // La falla ocurre en la última cuenta, cuando el Proveedor y las cuentas anteriores ya se
    // insertaron dentro de la transacción. Esta es la prueba de que el guardado es todo o nada.
    // Saltamos la validación a propósito: acá el que tiene que frenar el dato es la base.
    [TheoryConSqlServer]
    [InlineData("restricción de la base", "   ", 1)]
    [InlineData("largo máximo de la columna", "", 201)]
    [InlineData("banco fuera de la lista", "Titular", 99)]
    public async Task AgregarAsync_SiFallaLaUltimaCuenta_NoDebeGuardarNiElProveedorNiLasOtrasCuentas(string motivo, string titular, int valor)
    {
        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor();
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));

        CuentaBancaria ultimaCuenta = DatosDePruebaProveedores.CrearCuenta("333");
        ultimaCuenta.Titular = titular;

        if (motivo == "largo máximo de la columna")
        {
            ultimaCuenta.Titular = new string('a', valor);
        }
        else if (motivo == "banco fuera de la lista")
        {
            ultimaCuenta.Banco = (Banco)valor;
        }

        proveedor.CuentasBancarias.Add(ultimaCuenta);

        await Assert.ThrowsAsync<DbUpdateException>(() => AgregarConContextoNuevoAsync(proveedor));

        await ComprobarCantidadesAsync(proveedoresEsperados: 0, cuentasEsperadas: 0);
    }

    // Es un duplicado de verdad para SQL Server (error 2601), pero no del nombre ni del RUT. Decirle
    // al usuario "ya existe ese proveedor" sería mentirle. Creamos un índice que solo existe en esta
    // base de prueba porque hoy la aplicación no tiene otro, y esta es la forma de provocar el caso.
    // Además, el Proveedor llega a insertarse antes de que falle la cuenta: tampoco tiene que quedar.
    [FactConSqlServer]
    public async Task AgregarAsync_ConOtroIndiceUnicoRepetido_NoDebeInformarloComoDuplicadoDeProveedor()
    {
        using (EopDbContext contexto = CrearContexto())
        {
            await contexto.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_SoloParaEstaPrueba ON CuentasBancarias (NumeroCuenta)");
        }

        await GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Primero", "215521750017"));

        Proveedor otroProveedor = DatosDePruebaProveedores.CrearProveedor("Segundo", "210000000019");

        DbUpdateException excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => AgregarConContextoNuevoAsync(otroProveedor));

        Assert.Contains("IX_SoloParaEstaPrueba", excepcion.GetBaseException().Message);
        await ComprobarCantidadesAsync(proveedoresEsperados: 1, cuentasEsperadas: 1);
    }

    // ---------- Concurrencia ----------

    // Provoca la carrera siempre igual, sin depender de la suerte: la validación corre de verdad y
    // no encuentra nada, y justo antes de guardar otro usuario se adelanta y guarda el suyo.
    private sealed class RepositorioConOtroUsuarioQueSeAdelanta : IRepositorioProveedores
    {
        private readonly IRepositorioProveedores _repositorioReal;
        private readonly Func<Task> _otroUsuarioGuarda;

        public RepositorioConOtroUsuarioQueSeAdelanta(IRepositorioProveedores repositorioReal, Func<Task> otroUsuarioGuarda)
        {
            _repositorioReal = repositorioReal;
            _otroUsuarioGuarda = otroUsuarioGuarda;
        }

        public Task<bool> ExistePorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken cancellationToken = default)
        {
            return _repositorioReal.ExistePorNombreNormalizadoAsync(nombreNormalizado, cancellationToken);
        }

        public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default)
        {
            return _repositorioReal.ExistePorRutAsync(rutNormalizado, cancellationToken);
        }

        public async Task<ResultadoAgregarProveedor> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
        {
            await _otroUsuarioGuarda();

            return await _repositorioReal.AgregarAsync(proveedor, cancellationToken);
        }
    }

    [TheoryConSqlServer]
    [InlineData("Fivisa", "210000010014", ValidarDatosProveedor.MensajeNombreDuplicado)]
    [InlineData("Otro nombre", "215521750017", ValidarDatosProveedor.MensajeRutDuplicado)]
    public async Task EjecutarAsync_SiOtroUsuarioGuardaDespuesDeLaValidacion_DebeInformarElDuplicadoConElMensajeDeSiempre(
        string nombreDelQueLlegaTarde, string rutDelQueLlegaTarde, string mensajeEsperado)
    {
        using EopDbContext contexto = CrearContexto();
        var repositorio = new RepositorioConOtroUsuarioQueSeAdelanta(
            new RepositorioProveedores(contexto),
            () => GuardarAsync(DatosDePruebaProveedores.CrearProveedor("Fivisa", "215521750017")));

        Proveedor proveedorQueLlegaTarde = DatosDePruebaProveedores.CrearProveedor(nombreDelQueLlegaTarde, rutDelQueLlegaTarde);
        proveedorQueLlegaTarde.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));

        List<string> errores = await CrearCasoDeUso(repositorio).EjecutarAsync(proveedorQueLlegaTarde, PaisValido, NumeroValido);

        // Comparar el mensaje exacto también demuestra que no se filtró nada del texto de SQL Server.
        Assert.Equal(mensajeEsperado, Assert.Single(errores));
        await ComprobarCantidadesAsync(proveedoresEsperados: 1, cuentasEsperadas: 1);
    }

    // Y ahora la carrera real: varias solicitudes salen a la vez con los mismos datos. Algunas pueden
    // quedar afuera en la validación y otras recién en la base; eso depende del momento. Lo que no
    // puede variar es el resultado: una guarda, las otras reciben un mensaje y ninguna explota.
    [FactConSqlServer]
    public async Task EjecutarAsync_ConVariasSolicitudesSimultaneas_DebeGuardarSoloUna()
    {
        for (int repeticion = 0; repeticion < RepeticionesDeLaCarrera; repeticion++)
        {
            await VaciarTablasAsync();

            var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var solicitudes = new List<Task<List<string>>>();

            for (int solicitud = 0; solicitud < SolicitudesSimultaneas; solicitud++)
            {
                solicitudes.Add(RegistrarDespuesDeLaLargadaAsync(largada.Task));
            }

            largada.SetResult();
            List<string>[] resultados = await Task.WhenAll(solicitudes);

            int cantidadSinErrores = 0;

            foreach (List<string> errores in resultados)
            {
                if (errores.Count == 0)
                {
                    cantidadSinErrores++;
                }
                else
                {
                    Assert.All(errores, error => Assert.StartsWith("Ya existe un proveedor registrado con ese", error));
                }
            }

            Assert.Equal(1, cantidadSinErrores);
            await ComprobarCantidadesAsync(proveedoresEsperados: 1, cuentasEsperadas: 2);
        }
    }

    private async Task<List<string>> RegistrarDespuesDeLaLargadaAsync(Task largada)
    {
        await largada;

        Proveedor proveedor = DatosDePruebaProveedores.CrearProveedor("Fivisa S.A.", "215521750017");
        proveedor.CuentasBancarias.Add(DatosDePruebaProveedores.CrearCuenta("222"));

        using EopDbContext contexto = CrearContexto();

        return await CrearCasoDeUso(new RepositorioProveedores(contexto)).EjecutarAsync(proveedor, PaisValido, NumeroValido);
    }

    private async Task VaciarTablasAsync()
    {
        using EopDbContext contexto = CrearContexto();

        await contexto.CuentasBancarias.ExecuteDeleteAsync();
        await contexto.Proveedores.ExecuteDeleteAsync();
    }
}
