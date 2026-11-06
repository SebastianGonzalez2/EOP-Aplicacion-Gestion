using EOP.Gestion.Core.CasosDeUso.Clientes;
using EOP.Gestion.Core.Entidades.Clientes;
using EOP.Gestion.Core.Interfaces.Clientes;

namespace EOP.Gestion.Tests.Unitarias;

public class ValidarDatosClienteTests
{
    // Simulamos un repositorio que siempre encuentra el RUT.
    private class RepositorioClientesFalso : IRepositorioClientes
    {
        // Guarda el último RUT que recibió el repositorio.
        public string? RutConsultado { get; private set; }

        // Indica si el RUT existe. Por defecto simulamos que está registrado.
        public bool RutExiste { get; set; } = true;

        public Task<bool> ExistePorRutAsync(string rutNormalizado,CancellationToken cancellationToken = default)
        {
            // Guardamos el RUT recibido para comprobarlo en las pruebas.
            RutConsultado = rutNormalizado;
            return Task.FromResult(RutExiste);
        }

        public Task<bool> AgregarSiRutDisponibleAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            // Este repositorio falso se utiliza únicamente para probar
            // las validaciones de clientes, no para guardar información.
            throw new NotSupportedException("Este repositorio falso no permite guardar clientes.");
        }


    }

    [Fact]
    public async Task ValidarAsync_DebeRechazarRutDuplicado()
    {
        // Creamos el repositorio falso que simula un RUT existente.
        var repositorio = new RepositorioClientesFalso();
        // Creamos el validador utilizando ese repositorio.
        var validador = new ValidarDatosCliente(repositorio);
        // Creamos un cliente con datos correctos, pero con un RUT duplicado.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "21.552.175-0017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };

        // Ejecutamos todas las validaciones del cliente.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que se haya detectado el RUT duplicado.
        Assert.Contains("Ya existe un cliente registrado con ese RUT.", errores);

        // Comprobamos que el repositorio recibió el RUT sin puntos ni guiones.
        Assert.Equal("215521750017", repositorio.RutConsultado);
    }


    [Fact]
    public async Task ValidarAsync_DebeAceptarClienteValidoYNormalizarDatos()
    {
        // Simulamos que el RUT todavía no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        // Creamos el validador con nuestro repositorio falso.
        var validador = new ValidarDatosCliente(repositorio);

        // Creamos un cliente con datos válidos y separadores.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "21.552.175-0017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+598 (99) 123-456"
        };

        // Ejecutamos todas las validaciones.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que no se encontraron errores.
        Assert.Empty(errores);

        // Comprobamos que los datos quedaron normalizados.
        Assert.Equal("215521750017", cliente.Rut);
        Assert.Equal("+59899123456", cliente.NumeroWhatsApp);
    }

    [Fact]
    public async Task ValidarAsync_DebeRechazarNombreVacio()
    {
        // Simulamos que el RUT no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        var validador = new ValidarDatosCliente(repositorio);

        // Creamos un cliente sin nombre, pero con los demás datos válidos.
        var cliente = new Cliente
        {
            Nombre = "",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };

        // Ejecutamos las validaciones.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que detecte la ausencia del nombre.
        Assert.Contains("El nombre del cliente es obligatorio.", errores);
    }


    [Fact]
    public async Task ValidarAsync_DebeRechazarRutInvalido()
    {
        // Simulamos que el RUT no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        var validador = new ValidarDatosCliente(repositorio);

        // Creamos un cliente con el dígito verificador del RUT incorrecto.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "215521750018",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "+59899123456"
        };

        // Ejecutamos las validaciones.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que detecte el RUT inválido.
        Assert.Contains("El RUT ingresado no es válido.", errores);
    }


    [Fact]
    public async Task ValidarAsync_DebeRechazarAmbosCorreosVacios()
    {
        // Simulamos que el RUT no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        var validador = new ValidarDatosCliente(repositorio);

        // Creamos un cliente con datos válidos, pero sin correos.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "215521750017",
            CorreoContacto = null,
            CorreoFacturacion = null,
            NumeroWhatsApp = "+59899123456"
        };

        // Ejecutamos todas las validaciones.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que se exija al menos un correo.
        Assert.Contains("Debe informar al menos un correo electrónico.", errores);
    }

    [Fact]
    public async Task ValidarAsync_DebeRechazarCorreoContactoInvalido()
    {
        // Simulamos que el RUT no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        var validador = new ValidarDatosCliente(repositorio);

        // Creamos un cliente con el correo de contacto mal escrito.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente.gmail.com",
            NumeroWhatsApp = "+59899123456"
        };

        // Ejecutamos todas las validaciones.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que detecte el correo incorrecto.
        Assert.Contains(
            "El correo de contacto no tiene un formato válido.",
            errores);
    }


    [Fact]
    public async Task ValidarAsync_DebeRechazarCorreoFacturacionInvalido()
    {
        // Simulamos que el RUT no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        var validador = new ValidarDatosCliente(repositorio);

        // Creamos un cliente con el correo de facturación incorrecto.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "215521750017",
            CorreoContacto = null,
            CorreoFacturacion = "facturacion.gmail.com",
            NumeroWhatsApp = "+59899123456"
        };

        // Ejecutamos todas las validaciones.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que detecte el correo de facturación incorrecto.
        Assert.Contains(
            "El correo de facturación no tiene un formato válido.",
            errores);
    }

    [Fact]
    public async Task ValidarAsync_DebeRechazarWhatsAppInvalido()
    {
        // Simulamos que el RUT no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        var validador = new ValidarDatosCliente(repositorio);

        // Creamos un cliente con un número de WhatsApp sin código de país.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com",
            NumeroWhatsApp = "093966189"
        };

        // Ejecutamos todas las validaciones.
        List<string> errores = await validador.ValidarAsync(cliente);

        // Comprobamos que detecte el formato incorrecto de WhatsApp.
        Assert.Contains(
            "El número de WhatsApp debe tener un formato internacional válido, por ejemplo +59899123456.",
            errores);
    }

    [Fact]
    public async Task ValidarAsync_ConErrorDeWhatsAppInformado_DebeMostrarEseErrorSinElGenerico()
    {
        // Simulamos que el RUT no está registrado.
        var repositorio = new RepositorioClientesFalso
        {
            RutExiste = false
        };

        var validador = new ValidarDatosCliente(repositorio);

        // El cliente queda sin número porque no se pudo armar el internacional.
        var cliente = new Cliente
        {
            Nombre = "Empresa de prueba",
            Rut = "215521750017",
            CorreoContacto = "cliente@ejemplo.com"
        };

        // Ejecutamos las validaciones pasando el motivo por el que no se pudo armar.
        List<string> errores = await validador.ValidarAsync(cliente, "El número no es válido para Uruguay.");

        // Comprobamos que se muestre únicamente el motivo concreto.
        Assert.Equal(["El número no es válido para Uruguay."], errores);
    }

    private static Cliente CrearClienteValido(string nombre) => new()
    {
        Nombre = nombre,
        Rut = "215521750017",
        CorreoContacto = "cliente@ejemplo.com",
        NumeroWhatsApp = "+59899123456"
    };

    [Theory]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task ValidarAsync_ConNombreSoloDeEspacios_LoRechazaComoObligatorio(string nombre)
    {
        var validador = new ValidarDatosCliente(new RepositorioClientesFalso { RutExiste = false });

        List<string> errores = await validador.ValidarAsync(CrearClienteValido(nombre));

        Assert.Equal(["El nombre del cliente es obligatorio."], errores);
    }

    [Fact]
    public async Task ValidarAsync_ConNombreDelLargoMaximo_LoAcepta()
    {
        var validador = new ValidarDatosCliente(new RepositorioClientesFalso { RutExiste = false });

        List<string> errores = await validador.ValidarAsync(CrearClienteValido(new string('a', ValidarDatosCliente.LargoMaximoNombre)));

        Assert.Empty(errores);
    }

    [Fact]
    public async Task ValidarAsync_ConNombreMasLargoQueLaColumna_LoRechazaSinGuardarlo()
    {
        var validador = new ValidarDatosCliente(new RepositorioClientesFalso { RutExiste = false });
        Cliente cliente = CrearClienteValido(new string('a', ValidarDatosCliente.LargoMaximoNombre + 1));

        List<string> errores = await validador.ValidarAsync(cliente);

        Assert.Equal(["El nombre del cliente no puede superar los 200 caracteres."], errores);
    }

    // Los espacios de los extremos no cuentan para el largo y no se guardan.
    [Fact]
    public async Task ValidarAsync_QuitaLosEspaciosDeLosExtremosDelNombre_AntesDeMedirlo()
    {
        var validador = new ValidarDatosCliente(new RepositorioClientesFalso { RutExiste = false });
        string nombreDelLargoMaximo = new('a', ValidarDatosCliente.LargoMaximoNombre);
        Cliente cliente = CrearClienteValido($"  {nombreDelLargoMaximo}  ");

        List<string> errores = await validador.ValidarAsync(cliente);

        Assert.Empty(errores);
        Assert.Equal(nombreDelLargoMaximo, cliente.Nombre);
    }

    [Fact]
    public async Task ValidarAsync_ConErrores_NoModificaElNombreRecibido()
    {
        var validador = new ValidarDatosCliente(new RepositorioClientesFalso { RutExiste = false });
        Cliente cliente = CrearClienteValido("  Empresa  ");
        cliente.Rut = "215521750018";

        await validador.ValidarAsync(cliente);

        Assert.Equal("  Empresa  ", cliente.Nombre);
    }




}