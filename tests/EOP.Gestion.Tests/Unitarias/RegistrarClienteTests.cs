using EOP.Gestion.Core.CasosDeUso.Clientes;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;
using EOP.Gestion.Infrastructure.Telefonia;

namespace EOP.Gestion.Tests.Unitarias;

public class RegistrarClienteTests
{
    // Simulamos la base de datos: nos alcanza con anotar lo que nos pidieron guardar.
    private class RepositorioClientesFalso : IRepositorioClientes
    {
        public Cliente? ClienteGuardado { get; private set; }

        // En false simula que otra solicitud registró el mismo RUT entre la validación y el guardado.
        public bool RutDisponibleAlGuardar { get; set; } = true;

        public Task<bool> ExistePorRutAsync(string rutNormalizado, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> AgregarSiRutDisponibleAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            if (RutDisponibleAlGuardar)
            {
                ClienteGuardado = cliente;
            }

            return Task.FromResult(RutDisponibleAlGuardar);
        }
    }

    private readonly RepositorioClientesFalso _repositorio = new();
    private readonly RegistrarCliente _registrarCliente;

    public RegistrarClienteTests()
    {
        _registrarCliente = new RegistrarCliente(
            new ValidarDatosCliente(_repositorio),
            _repositorio,
            new ServicioTelefonos());
    }

    private static Cliente CrearClienteValido() => new()
    {
        Nombre = "Empresa de prueba",
        Rut = "21.552.175-0017",
        CorreoContacto = "cliente@ejemplo.com"
    };

    [Fact]
    public async Task EjecutarAsync_DebeGuardarElNumeroInternacionalYNoElNacional()
    {
        List<string> errores = await _registrarCliente.EjecutarAsync(CrearClienteValido(), "UY", "099 123 456");

        Assert.Empty(errores);
        Assert.NotNull(_repositorio.ClienteGuardado);
        Assert.Equal("+59899123456", _repositorio.ClienteGuardado.NumeroWhatsApp);
    }

    [Fact]
    public async Task EjecutarAsync_DebeSeguirNormalizandoRutYDejandoAlClienteActivo()
    {
        await _registrarCliente.EjecutarAsync(CrearClienteValido(), "AR", "011 15 2345-6789");

        Assert.NotNull(_repositorio.ClienteGuardado);
        Assert.Equal("215521750017", _repositorio.ClienteGuardado.Rut);
        Assert.Equal("+5491123456789", _repositorio.ClienteGuardado.NumeroWhatsApp);
        Assert.True(_repositorio.ClienteGuardado.Activo);
    }

    [Theory]
    [InlineData("UY", "099123")]
    [InlineData("UY", "")]
    [InlineData("UY", "+59899123456")]
    [InlineData("ZZ", "099123456")]
    [InlineData(null, "099123456")]
    public async Task EjecutarAsync_ConTelefonoInvalido_NoDebeGuardarYDebeMostrarUnSoloError(string? pais, string numero)
    {
        List<string> errores = await _registrarCliente.EjecutarAsync(CrearClienteValido(), pais, numero);

        Assert.Single(errores);
        Assert.Null(_repositorio.ClienteGuardado);
    }

    [Fact]
    public async Task EjecutarAsync_ConTelefonoYRutInvalidos_DebeInformarLosDosErrores()
    {
        Cliente cliente = CrearClienteValido();
        cliente.Rut = "215521750018";

        List<string> errores = await _registrarCliente.EjecutarAsync(cliente, "UY", "099123");

        Assert.Contains("El RUT ingresado no es válido.", errores);
        Assert.Contains(errores, error => error.Contains("no es válido para Uruguay"));
        Assert.Null(_repositorio.ClienteGuardado);
    }

    [Fact]
    public async Task EjecutarAsync_ConTelefonoValidoYCorreoInvalido_NoDebeGuardar()
    {
        Cliente cliente = CrearClienteValido();
        cliente.CorreoContacto = "cliente.gmail.com";

        List<string> errores = await _registrarCliente.EjecutarAsync(cliente, "UY", "099123456");

        Assert.Contains("El correo de contacto no tiene un formato válido.", errores);
        Assert.Null(_repositorio.ClienteGuardado);
    }

    [Fact]
    public async Task EjecutarAsync_SiOtroGuardoElMismoRutAntes_InformaElDuplicadoYNoGuarda()
    {
        _repositorio.RutDisponibleAlGuardar = false;

        List<string> errores = await _registrarCliente.EjecutarAsync(CrearClienteValido(), "UY", "099123456");

        Assert.Equal([ValidarDatosCliente.MensajeRutDuplicado], errores);
        Assert.Null(_repositorio.ClienteGuardado);
    }
}
