using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Almacenamiento;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Extensions.Logging;

namespace EOP.Gestion.Tests.Unitarias;

/// <summary>
/// Prueban ServicioAlmacenamientoBlob con el SDK real de Azure pero contra un Azure simulado
/// (no se conectan a internet ni usan credenciales reales).
/// </summary>
public class ServicioAlmacenamientoBlobTests
{
    private static readonly byte[] ContenidoDePrueba = Encoding.UTF8.GetBytes("contenido confidencial de prueba");

    private static ArchivoParaGuardar CrearArchivo(
        string carpeta = "facturas",
        string nombre = "Factura Marzo.pdf",
        string tipo = "application/pdf",
        Stream? contenido = null) =>
        new(carpeta, nombre, tipo, contenido ?? new MemoryStream(ContenidoDePrueba));

    // ---------- Guardar ----------

    [Fact]
    public async Task Guardar_ArchivoValido_SubeElContenidoYDevuelveLaReferencia()
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        string referencia = await servicio.GuardarAsync(CrearArchivo());

        Assert.Matches(new Regex("^facturas/[0-9a-f]{32}\\.pdf$"), referencia);

        var pedido = Assert.Single(azure.Pedidos);
        Assert.Equal(HttpMethod.Put, pedido.Metodo);
        Assert.Equal($"/documentos-eop/{referencia}", pedido.Direccion.AbsolutePath);
        Assert.Equal(ContenidoDePrueba, pedido.Cuerpo);
        Assert.Equal("application/pdf", pedido.Encabezados["x-ms-blob-content-type"]);
        // Nunca pisa un archivo existente.
        Assert.Equal("*", pedido.Encabezados["If-None-Match"]);
    }

    [Fact]
    public async Task Guardar_AutenticaConTokenDeEntraIdPorHttpsSinFirmasNiClaves()
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        await servicio.GuardarAsync(CrearArchivo());

        var pedido = Assert.Single(azure.Pedidos);
        Assert.Equal(Uri.UriSchemeHttps, pedido.Direccion.Scheme);
        // Bearer = token de Entra ID. Si fuera clave compartida diría "SharedKey".
        Assert.Equal($"Bearer {AzureBlobSimulado.TokenDePrueba}", pedido.Encabezados["Authorization"]);
        // Sin firma de acceso compartido (SAS) en la dirección: no se generan URLs de acceso.
        Assert.DoesNotContain("sig=", pedido.Direccion.Query);
    }

    [Theory]
    [InlineData("Informe.PDF", ".pdf")]
    [InlineData("plano.dwg", ".dwg")]
    [InlineData("archivo.tar.gz", ".gz")]
    [InlineData("sin-extension", "")]
    [InlineData("raro.<script>", "")]
    [InlineData("termina-en-punto.", "")]
    public async Task Guardar_ConservaSoloLaExtensionValidaEnMinusculas(string nombreOriginal, string extensionEsperada)
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        string referencia = await servicio.GuardarAsync(CrearArchivo(nombre: nombreOriginal));

        Assert.EndsWith(extensionEsperada, referencia);
        // El nombre que puso el usuario nunca forma parte de la referencia.
        Assert.Matches(new Regex($"^facturas/[0-9a-f]{{32}}{Regex.Escape(extensionEsperada)}$"), referencia);
    }

    [Fact]
    public async Task Guardar_DosVeces_GeneraReferenciasDistintas()
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        string primera = await servicio.GuardarAsync(CrearArchivo());
        string segunda = await servicio.GuardarAsync(CrearArchivo());

        Assert.NotEqual(primera, segunda);
    }

    [Fact]
    public async Task Guardar_ArchivoNulo_LanzaArgumentNullException()
    {
        var servicio = new AzureBlobSimulado().CrearServicio();

        await Assert.ThrowsAsync<ArgumentNullException>(() => servicio.GuardarAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Facturas")]
    [InlineData("../otra")]
    [InlineData("a/b")]
    [InlineData("con espacio")]
    [InlineData("-guion")]
    public async Task Guardar_CarpetaInvalida_LanzaArgumentExceptionSinLlamarAAzure(string carpeta)
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.GuardarAsync(CrearArchivo(carpeta: carpeta)));

        Assert.Empty(azure.Pedidos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Guardar_NombreOriginalVacio_LanzaArgumentExceptionSinLlamarAAzure(string nombre)
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.GuardarAsync(CrearArchivo(nombre: nombre)));

        Assert.Empty(azure.Pedidos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pdf")]
    [InlineData("application/pdf\r\nX-Inyectado: 1")]
    public async Task Guardar_TipoContenidoInvalido_LanzaArgumentExceptionSinLlamarAAzure(string tipo)
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.GuardarAsync(CrearArchivo(tipo: tipo)));

        Assert.Empty(azure.Pedidos);
    }

    [Fact]
    public async Task Guardar_ArchivoVacio_LanzaArgumentExceptionSinLlamarAAzure()
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.GuardarAsync(CrearArchivo(contenido: new MemoryStream())));

        Assert.Empty(azure.Pedidos);
    }

    [Fact]
    public async Task Guardar_ContenidoNoLegible_LanzaArgumentExceptionSinLlamarAAzure()
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();
        var cerrado = new MemoryStream(ContenidoDePrueba);
        cerrado.Dispose();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.GuardarAsync(CrearArchivo(contenido: cerrado)));

        Assert.Empty(azure.Pedidos);
    }

    // ---------- Obtener ----------

    [Fact]
    public async Task Obtener_ArchivoExistente_DevuelveContenidoYTipo()
    {
        var azure = new AzureBlobSimulado();
        azure.Responder = _ => AzureBlobSimulado.RespuestaConArchivo(ContenidoDePrueba, "application/pdf");
        var servicio = azure.CrearServicio();
        string referencia = $"facturas/{Guid.NewGuid():N}.pdf";

        var descargado = await servicio.ObtenerAsync(referencia);

        Assert.NotNull(descargado);
        Assert.Equal("application/pdf", descargado.TipoContenido);
        using var memoria = new MemoryStream();
        await using (descargado.Contenido)
        {
            await descargado.Contenido.CopyToAsync(memoria);
        }
        Assert.Equal(ContenidoDePrueba, memoria.ToArray());

        var pedido = Assert.Single(azure.Pedidos);
        Assert.Equal(HttpMethod.Get, pedido.Metodo);
        Assert.Equal($"/documentos-eop/{referencia}", pedido.Direccion.AbsolutePath);
        Assert.Equal($"Bearer {AzureBlobSimulado.TokenDePrueba}", pedido.Encabezados["Authorization"]);
        Assert.DoesNotContain("sig=", pedido.Direccion.Query);
    }

    [Fact]
    public async Task Obtener_SinTipoDeContenido_UsaTipoGenerico()
    {
        var azure = new AzureBlobSimulado();
        azure.Responder = _ => AzureBlobSimulado.RespuestaConArchivo(ContenidoDePrueba, tipoContenido: null);
        var servicio = azure.CrearServicio();

        var descargado = await servicio.ObtenerAsync($"facturas/{Guid.NewGuid():N}");

        Assert.NotNull(descargado);
        Assert.Equal("application/octet-stream", descargado.TipoContenido);
        await descargado.Contenido.DisposeAsync();
    }

    [Fact]
    public async Task Obtener_ReferenciaInexistente_DevuelveNull()
    {
        var azure = new AzureBlobSimulado();
        azure.Responder = _ => AzureBlobSimulado.Respuesta(HttpStatusCode.NotFound, "BlobNotFound");
        var servicio = azure.CrearServicio();

        var descargado = await servicio.ObtenerAsync($"facturas/{Guid.NewGuid():N}.pdf");

        Assert.Null(descargado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../facturas/" + "0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("facturas/no-es-un-guid.pdf")]
    [InlineData("facturas/0123456789abcdef0123456789abcdef.pdf/../otro")]
    [InlineData("0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("Facturas/0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("https://cuenta.blob.core.windows.net/documentos-eop/facturas/0123456789abcdef0123456789abcdef.pdf")]
    public async Task Obtener_ReferenciaConFormatoInvalido_LanzaArgumentExceptionSinLlamarAAzure(string referencia)
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ObtenerAsync(referencia));

        Assert.Empty(azure.Pedidos);
    }

    [Fact]
    public async Task Obtener_ReferenciaNula_LanzaArgumentException()
    {
        var servicio = new AzureBlobSimulado().CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ObtenerAsync(null!));
    }

    // ---------- Fallas de Azure ----------

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "AuthorizationPermissionMismatch")]
    [InlineData(HttpStatusCode.Forbidden, "AuthorizationFailure")]
    [InlineData(HttpStatusCode.Unauthorized, "InvalidAuthenticationInfo")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "ServerBusy")]
    [InlineData(HttpStatusCode.InternalServerError, "InternalError")]
    [InlineData(HttpStatusCode.NotFound, "ContainerNotFound")]
    public async Task Guardar_AzureRespondeConError_LanzaAlmacenamientoNoDisponibleYLoRegistraSinDatosSensibles(HttpStatusCode estado, string codigo)
    {
        var azure = new AzureBlobSimulado();
        azure.Responder = _ => AzureBlobSimulado.Respuesta(estado, codigo);
        var servicio = azure.CrearServicio();

        var excepcion = await Assert.ThrowsAsync<AlmacenamientoNoDisponibleException>(
            () => servicio.GuardarAsync(CrearArchivo()));

        Assert.NotNull(excepcion.InnerException);
        AfirmarErrorRegistradoSinDatosSensibles(azure, estado, codigo, "guardar");
        // El mensaje para el usuario es genérico: nada del detalle de Azure.
        Assert.DoesNotContain(codigo, excepcion.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "AuthorizationPermissionMismatch")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "ServerBusy")]
    [InlineData(HttpStatusCode.NotFound, "ContainerNotFound")]
    public async Task Obtener_AzureRespondeConError_LanzaAlmacenamientoNoDisponible(HttpStatusCode estado, string codigo)
    {
        var azure = new AzureBlobSimulado();
        azure.Responder = _ => AzureBlobSimulado.Respuesta(estado, codigo);
        var servicio = azure.CrearServicio();

        // Un contenedor inexistente también es 404, pero NO es "archivo no encontrado": es un problema de configuración.
        await Assert.ThrowsAsync<AlmacenamientoNoDisponibleException>(
            () => servicio.ObtenerAsync($"facturas/{Guid.NewGuid():N}.pdf"));

        AfirmarErrorRegistradoSinDatosSensibles(azure, estado, codigo, "obtener");
    }

    [Fact]
    public async Task Guardar_FallaLaAutenticacion_LanzaAlmacenamientoNoDisponibleSinFiltrarElDetalle()
    {
        var azure = new AzureBlobSimulado { Credencial = new AzureBlobSimulado.CredencialQueFalla() };
        var servicio = azure.CrearServicio();

        var excepcion = await Assert.ThrowsAsync<AlmacenamientoNoDisponibleException>(
            () => servicio.GuardarAsync(CrearArchivo()));

        Assert.DoesNotContain("detalle interno", excepcion.Message);
        var entrada = Assert.Single(azure.Logs.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.Contains("CredentialUnavailableException", entrada.Mensaje);
        Assert.DoesNotContain("detalle interno", entrada.Mensaje);
        // Si no hay credencial no se llegó a enviar ningún pedido a Azure.
        Assert.Empty(azure.Pedidos);
    }

    [Fact]
    public async Task Obtener_FallaLaAutenticacion_LanzaAlmacenamientoNoDisponible()
    {
        var azure = new AzureBlobSimulado { Credencial = new AzureBlobSimulado.CredencialQueFalla() };
        var servicio = azure.CrearServicio();

        await Assert.ThrowsAsync<AlmacenamientoNoDisponibleException>(
            () => servicio.ObtenerAsync($"facturas/{Guid.NewGuid():N}.pdf"));
    }

    [Fact]
    public async Task Guardar_CancelacionDelUsuario_NoSeConvierteEnFallaDeAlmacenamiento()
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servicio.GuardarAsync(CrearArchivo(), cancelacion.Token));
    }

    [Fact]
    public async Task Guardar_Correcto_RegistraLaOperacionSinContenidoNiToken()
    {
        var azure = new AzureBlobSimulado();
        var servicio = azure.CrearServicio();

        string referencia = await servicio.GuardarAsync(CrearArchivo());

        var entrada = Assert.Single(azure.Logs.Entradas);
        Assert.Equal(LogLevel.Information, entrada.Nivel);
        Assert.Contains(referencia, entrada.Mensaje);
        Assert.DoesNotContain("confidencial", entrada.Mensaje);
        Assert.DoesNotContain(AzureBlobSimulado.TokenDePrueba, entrada.Mensaje);
    }

    private static void AfirmarErrorRegistradoSinDatosSensibles(AzureBlobSimulado azure, HttpStatusCode estado, string codigo, string operacion)
    {
        var entrada = Assert.Single(azure.Logs.Entradas, e => e.Nivel == LogLevel.Error);
        Assert.Contains(operacion, entrada.Mensaje);
        Assert.Contains(((int)estado).ToString(), entrada.Mensaje);
        Assert.Contains(codigo, entrada.Mensaje);
        // Ni el contenido del archivo ni el token ni la excepción completa (que traería el mensaje de Azure).
        Assert.DoesNotContain("confidencial", entrada.Mensaje);
        Assert.DoesNotContain(AzureBlobSimulado.TokenDePrueba, entrada.Mensaje);
        Assert.Null(entrada.Excepcion);
    }
}
