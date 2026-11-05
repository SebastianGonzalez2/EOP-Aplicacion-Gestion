using System.Net;
using System.Text.RegularExpressions;
using EOP.Gestion.Core.Comun;
using EOP.Gestion.Core.DTOs.Proveedores;
using EOP.Gestion.Core.Entidades.Proveedores;
using EOP.Gestion.Infrastructure.Consultas.Proveedores;
using EOP.Gestion.Infrastructure.Persistencia;
using EOP.Gestion.Tests.Compartido;
using Microsoft.Extensions.DependencyInjection;

namespace EOP.Gestion.Tests.Integracion;

// El POST real del alta (S2-T13), de punta a punta: formulario, model binding, Controller, caso de
// uso, repositorio y base. Cada prueba lee después la base, porque el éxito no es que la pantalla
// diga "correctamente" sino que el proveedor esté guardado.
public partial class FormularioRegistrarProveedorTests
{
    private const string MensajeDeExito = "El proveedor se registró correctamente.";

    // ---------- Registro correcto ----------

    [Fact]
    public async Task Post_ConUnaCuenta_GuardaTodosLosDatosNormalizados()
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["Nombre"] = "Construcciones Pérez";
        datos["Rut"] = "21.552.175-0017";
        datos["NumeroNacionalTelefono"] = "099 123 456";
        datos["CorreoContacto"] = "contacto@ejemplo.com";
        datos["CorreoFacturacion"] = "facturas@ejemplo.com";

        HttpResponseMessage respuesta = await EnviarAltaAsync(CrearCliente("Administrador"), datos);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        Proveedor guardado = Assert.Single(await _fabrica.LeerProveedoresAsync());
        Assert.Equal("Construcciones Pérez", guardado.Nombre);
        Assert.Equal("CONSTRUCCIONES PEREZ", guardado.NombreNormalizado);
        Assert.Equal("215521750017", guardado.Rut);
        Assert.Equal("+59899123456", guardado.Telefono);
        Assert.Equal("contacto@ejemplo.com", guardado.CorreoContacto);
        Assert.Equal("facturas@ejemplo.com", guardado.CorreoFacturacion);

        CuentaBancaria cuenta = Assert.Single(guardado.CuentasBancarias);
        Assert.Equal(guardado.Id, cuenta.ProveedorId);
        Assert.Equal(Banco.BancoRepublicaOrientalDelUruguay, cuenta.Banco);
        Assert.Equal("000111", cuenta.NumeroCuenta);
        Assert.Equal("Titular 1", cuenta.Titular);
        Assert.Equal(Moneda.UYU, cuenta.Moneda);
    }

    // US-009, "múltiples Cuentas": todas quedan en el mismo Proveedor, sin mezclar sus datos.
    [Fact]
    public async Task Post_ConVariasCuentas_LasGuardaTodasEnUnSoloProveedor()
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        AgregarCuenta(datos, 1, "8", "000222", "Segundo titular", "2");
        AgregarCuenta(datos, 2, "11", "000333", "Tercer titular", "1");

        HttpResponseMessage respuesta = await EnviarAltaAsync(CrearCliente("Administrador"), datos);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        Proveedor guardado = Assert.Single(await _fabrica.LeerProveedoresAsync());
        Assert.Equal(3, guardado.CuentasBancarias.Count);
        Assert.Equal(3, await _fabrica.ContarCuentasAsync());
        Assert.All(guardado.CuentasBancarias, cuenta => Assert.Equal(guardado.Id, cuenta.ProveedorId));

        CuentaBancaria segundaCuenta = guardado.CuentasBancarias.Single(cuenta => cuenta.NumeroCuenta == "000222");
        Assert.Equal(Banco.BancoSantander, segundaCuenta.Banco);
        Assert.Equal("Segundo titular", segundaCuenta.Titular);
        Assert.Equal(Moneda.USD, segundaCuenta.Moneda);

        CuentaBancaria terceraCuenta = guardado.CuentasBancarias.Single(cuenta => cuenta.NumeroCuenta == "000333");
        Assert.Equal(Banco.ScotiabankUruguay, terceraCuenta.Banco);
        Assert.Equal("Tercer titular", terceraCuenta.Titular);
        Assert.Equal(Moneda.UYU, terceraCuenta.Moneda);
    }

    // El correo que no se informó queda en NULL, no como texto vacío.
    [Theory]
    [InlineData("contacto@ejemplo.com", "")]
    [InlineData("", "facturas@ejemplo.com")]
    public async Task Post_ConUnSoloCorreo_GuardaConservandoElInformado(string correoContacto, string correoFacturacion)
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["CorreoContacto"] = correoContacto;
        datos["CorreoFacturacion"] = correoFacturacion;

        HttpResponseMessage respuesta = await EnviarAltaAsync(CrearCliente("Administrador"), datos);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        Proveedor guardado = Assert.Single(await _fabrica.LeerProveedoresAsync());

        if (correoContacto.Length > 0)
        {
            Assert.Equal(correoContacto, guardado.CorreoContacto);
            Assert.Null(guardado.CorreoFacturacion);
        }
        else
        {
            Assert.Null(guardado.CorreoContacto);
            Assert.Equal(correoFacturacion, guardado.CorreoFacturacion);
        }
    }

    // Post/Redirect/Get: lo que recarga el usuario es el GET, así que ni se repite el alta ni el
    // mensaje de éxito vuelve a aparecer. Y el formulario queda vacío, listo para registrar otro.
    [Fact]
    public async Task Post_Exitoso_RedirigeMuestraElMensajeUnaVezYRecargarNoVuelveAGuardar()
    {
        HttpClient cliente = CrearCliente("Administrador");

        HttpResponseMessage respuesta = await EnviarAltaAsync(cliente, DatosConCuentas(1));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(Ruta, respuesta.Headers.Location?.OriginalString);

        string htmlTrasRedirigir = await LeerHtmlAsync(await cliente.GetAsync(respuesta.Headers.Location));

        Assert.Contains(MensajeDeExito, htmlTrasRedirigir);
        Assert.Single(Regex.Matches(htmlTrasRedirigir, "<fieldset class=\"cuenta-bancaria "));
        Assert.DoesNotContain("value=\"Proveedor de prueba\"", htmlTrasRedirigir);

        string htmlTrasRecargar = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        Assert.DoesNotContain(MensajeDeExito, htmlTrasRecargar);
        Assert.Single(await _fabrica.LeerProveedoresAsync());
    }

    // ---------- Disponible para otros módulos ----------

    [Fact]
    public async Task ProveedorRegistrado_ApareceEnLaConsultaParaSeleccion()
    {
        await EnviarAltaAsync(CrearCliente("Administrador"), DatosConCuentas(1));

        using IServiceScope alcance = _fabrica.Services.CreateScope();
        var consulta = alcance.ServiceProvider.GetRequiredService<ConsultarProveedoresParaSeleccion>();

        List<ProveedorOpcionDto> opciones = await consulta.EjecutarAsync();

        ProveedorOpcionDto opcion = Assert.Single(opciones);
        Assert.Equal("Proveedor de prueba", opcion.Nombre);
        Assert.Equal(Assert.Single(await _fabrica.LeerProveedoresAsync()).Id, opcion.Id);
    }

    // RF-23: fuera de Proveedores solo se selecciona. Abrir el selector no crea nada, y esa
    // pantalla tampoco ofrece un alta de proveedor.
    [Fact]
    public async Task ProveedorRegistrado_ApareceEnElSelectorDeMaterialesSinOfrecerAltaNiDuplicarlo()
    {
        HttpClient cliente = CrearCliente("Administrador");
        await EnviarAltaAsync(cliente, DatosConCuentas(1));

        string html = await LeerHtmlAsync(await cliente.GetAsync("/Materiales/Registrar"));

        // Miramos solo el contenido de la página: el menú de arriba sí tiene el enlace a Proveedores.
        string contenido = Regex.Match(html, "<main.*?</main>", RegexOptions.Singleline).Value;
        Assert.NotEmpty(contenido);
        Assert.Single(Regex.Matches(contenido, ">Proveedor de prueba</option>"));
        Assert.DoesNotContain("/Proveedores/Registrar", contenido);
        Assert.Single(await _fabrica.LeerProveedoresAsync());
    }

    // ---------- Errores: no se guarda y se explica el motivo ----------

    [Theory]
    [InlineData("Proveedor de prueba")]
    [InlineData("PROVEEDOR DE PRUEBA")]
    [InlineData("  Proveedor   de prueba ")]
    [InlineData("Proveedór de prueba")]
    public async Task Post_ConNombreDuplicado_NoGuardaYInformaElMotivo(string nombreRepetido)
    {
        await _fabrica.GuardarProveedorAsync(DatosDePruebaProveedores.CrearProveedor("Proveedor de prueba", "210000000019"));

        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["Nombre"] = nombreRepetido;

        string html = await ComprobarQueRechazaAsync(datos, proveedoresEsperados: 1, cuentasEsperadas: 1);

        Assert.Contains("Ya existe un proveedor registrado con ese nombre.", html);
    }

    [Fact]
    public async Task Post_ConRutDuplicado_NoGuardaYInformaElMotivo()
    {
        await _fabrica.GuardarProveedorAsync(DatosDePruebaProveedores.CrearProveedor("Otro proveedor", "215521750017"));

        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["Rut"] = "21.552.175-0017";

        string html = await ComprobarQueRechazaAsync(datos, proveedoresEsperados: 1, cuentasEsperadas: 1);

        Assert.Contains("Ya existe un proveedor registrado con ese RUT.", html);
    }

    [Theory]
    [InlineData("Rut", "215521750018", "El RUT ingresado no es válido.")]
    [InlineData("Rut", "000000000000", "El RUT ingresado no es válido.")]
    [InlineData("NumeroNacionalTelefono", "099123", "no es válido para Uruguay")]
    [InlineData("PaisTelefono", "ZZ", "Seleccioná un país válido")]
    [InlineData("CorreoContacto", "correo.sin.arroba", "El correo de contacto no tiene un formato válido.")]
    [InlineData("CorreoFacturacion", "correo.sin.arroba", "El correo de facturación no tiene un formato válido.")]
    public async Task Post_ConUnDatoInvalido_NoGuardaYInformaElMotivo(string campo, string valorInvalido, string mensajeEsperado)
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos[campo] = valorInvalido;

        string html = await ComprobarQueRechazaAsync(datos);

        Assert.Contains(mensajeEsperado, html);
    }

    [Fact]
    public async Task Post_ConLosDosCorreosVacios_NoGuardaYPideAlMenosUno()
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["CorreoContacto"] = "";
        datos["CorreoFacturacion"] = "   ";

        string html = await ComprobarQueRechazaAsync(datos);

        Assert.Contains("Debe informar al menos un correo electrónico.", html);
    }

    // La pantalla tiene que traer un bloque de cuenta aunque el pedido no haya mandado ninguno. El
    // script copia el primero para agregar otros: sin él, el usuario no podría corregir el error.
    [Fact]
    public async Task Post_SinCuentas_NoGuardaPideUnaYDejaUnBloqueParaCompletar()
    {
        string html = await ComprobarQueRechazaAsync(DatosValidosDelProveedor());

        Assert.Contains("Ingresá al menos una cuenta bancaria.", html);
        Assert.Single(Regex.Matches(html, "<fieldset class=\"cuenta-bancaria "));
        Assert.Contains("name=\"CuentasBancarias[0].Banco\"", html);
    }

    [Theory]
    [InlineData("Banco", "Seleccioná el banco de la cuenta.")]
    [InlineData("NumeroCuenta", "Ingresá el número de cuenta.")]
    [InlineData("Titular", "Ingresá el titular de la cuenta.")]
    [InlineData("Moneda", "Seleccioná la moneda de la cuenta.")]
    public async Task Post_ConUnaCuentaIncompleta_NoGuardaYMarcaElCampo(string campo, string mensajeEsperado)
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos[$"CuentasBancarias[0].{campo}"] = "";

        string html = await ComprobarQueRechazaAsync(datos);

        Assert.Contains(mensajeEsperado, html);
    }

    // Ausencia de registros parciales: que las otras cuentas estén bien no salva nada. No se
    // guarda el Proveedor con las cuentas buenas y descartando la mala.
    [Fact]
    public async Task Post_ConUnaCuentaInvalidaEntreOtrasValidas_NoGuardaNiElProveedorNiLasOtras()
    {
        Dictionary<string, string> datos = DatosConCuentas(3);
        datos["CuentasBancarias[1].Titular"] = "";

        string html = await ComprobarQueRechazaAsync(datos);

        Assert.Contains("Ingresá el titular de la cuenta.", html);
    }

    // ---------- Datos manipulados ----------

    // Editar el HTML del desplegable no permite meter un banco o una moneda que no están en la lista.
    [Theory]
    [InlineData("Banco", "0")]
    [InlineData("Banco", "12")]
    [InlineData("Banco", "99999999999")]
    [InlineData("Banco", "Banco inventado")]
    [InlineData("Moneda", "0")]
    [InlineData("Moneda", "3")]
    [InlineData("Moneda", "EUR")]
    public async Task Post_ConBancoOMonedaManipulados_NoGuardaNiLosCambiaPorOtroValor(string campo, string valorManipulado)
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos[$"CuentasBancarias[0].{campo}"] = valorManipulado;

        string html = await ComprobarQueRechazaAsync(datos);

        // Queda sin elegir, para que el usuario lo vea y lo corrija. No lo convertimos en un valor válido.
        Assert.DoesNotContain("selected", ObtenerSelector(html, $"CuentasBancarias[0].{campo}"));
        Assert.Contains("field-validation-error", html);
    }

    [Fact]
    public async Task Post_SinPaisEnElPedido_NoGuardaNiAsumeUruguay()
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos.Remove("PaisTelefono");

        string html = await ComprobarQueRechazaAsync(datos);

        Assert.Contains("Seleccioná el país del teléfono.", html);
    }

    // El ViewModel solo tiene los campos que se pueden cargar. Mandar otros en el pedido no sirve
    // para elegir el Id, meter un teléfono sin validar ni colgar la cuenta de otro proveedor.
    [Fact]
    public async Task Post_ConCamposQueNoSonDelFormulario_LosIgnora()
    {
        await _fabrica.GuardarProveedorAsync(DatosDePruebaProveedores.CrearProveedor("Proveedor existente", "210000000019"));
        Proveedor proveedorExistente = Assert.Single(await _fabrica.LeerProveedoresAsync());

        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["Id"] = proveedorExistente.Id.ToString();
        datos["NombreNormalizado"] = "OTRO NOMBRE";
        datos["Telefono"] = "no es un teléfono";
        datos["CuentasBancarias[0].Id"] = "999";
        datos["CuentasBancarias[0].ProveedorId"] = proveedorExistente.Id.ToString();

        HttpResponseMessage respuesta = await EnviarAltaAsync(CrearCliente("Administrador"), datos);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        List<Proveedor> proveedores = await _fabrica.LeerProveedoresAsync();
        Assert.Equal(2, proveedores.Count);

        Proveedor guardado = proveedores.Single(proveedor => proveedor.Rut == "215521750017");
        Assert.NotEqual(proveedorExistente.Id, guardado.Id);
        Assert.Equal("PROVEEDOR DE PRUEBA", guardado.NombreNormalizado);
        Assert.Equal("+59899123456", guardado.Telefono);
        Assert.Equal(guardado.Id, Assert.Single(guardado.CuentasBancarias).ProveedorId);

        // Y al que ya existía no le tocó nada.
        Assert.Single(proveedores.Single(proveedor => proveedor.Id == proveedorExistente.Id).CuentasBancarias);
    }

    // ---------- El formulario conserva lo que escribió el usuario ----------

    [Fact]
    public async Task Post_ConErrores_ConservaLosDatosYTodasLasCuentas()
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["Nombre"] = "Proveedor con error";
        datos["Rut"] = "215521750018";
        datos["PaisTelefono"] = "AR";
        datos["NumeroNacionalTelefono"] = "011 15 2345-6789";
        datos["CorreoContacto"] = "contacto@ejemplo.com";
        datos["CorreoFacturacion"] = "facturas@ejemplo.com";
        AgregarCuenta(datos, 1, "8", "000222", "Segundo titular", "2");
        AgregarCuenta(datos, 2, "11", "000333", "Tercer titular", "1");

        string html = await ComprobarQueRechazaAsync(datos);

        Assert.Contains("value=\"Proveedor con error\"", ObtenerEtiquetaDelCampo(html, "input", "Nombre"));
        Assert.Contains("value=\"215521750018\"", ObtenerEtiquetaDelCampo(html, "input", "Rut"));
        Assert.Contains("value=\"011 15 2345-6789\"", ObtenerEtiquetaDelCampo(html, "input", "NumeroNacionalTelefono"));
        Assert.Contains("value=\"contacto@ejemplo.com\"", ObtenerEtiquetaDelCampo(html, "input", "CorreoContacto"));
        Assert.Contains("value=\"facturas@ejemplo.com\"", ObtenerEtiquetaDelCampo(html, "input", "CorreoFacturacion"));

        // Las opciones no viajan en el POST: si no se recargaran, el país elegido se perdería.
        Assert.True(ObtenerOpcionesDelSelector(html, "PaisTelefono").Count > 200);
        Assert.Contains("<option selected=\"selected\" value=\"AR\">", ObtenerSelector(html, "PaisTelefono"));

        Assert.Equal(3, Regex.Matches(html, "<fieldset class=\"cuenta-bancaria ").Count);
        ComprobarCuentaConservada(html, 0, "1", "000111", "Titular 1", "1");
        ComprobarCuentaConservada(html, 1, "8", "000222", "Segundo titular", "2");
        ComprobarCuentaConservada(html, 2, "11", "000333", "Tercer titular", "1");

        // Con más de una cuenta se tiene que poder seguir quitando cualquiera de ellas.
        MatchCollection botonesQuitar = Regex.Matches(html, "<button[^>]*boton-quitar-cuenta[^>]*>");
        Assert.Equal(3, botonesQuitar.Count);
        Assert.All(botonesQuitar, boton => Assert.DoesNotContain("hidden", boton.Value));
    }

    // Lo mismo cuando el error lo detecta ASP.NET Core y el pedido no llega ni al caso de uso.
    [Fact]
    public async Task Post_ConErroresDelFormulario_TambienConservaLasCuentas()
    {
        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["Nombre"] = "";
        AgregarCuenta(datos, 1, "8", "000222", "Segundo titular", "2");

        string html = await ComprobarQueRechazaAsync(datos);

        Assert.Contains("El nombre del proveedor es obligatorio.", html);
        Assert.Equal(2, Regex.Matches(html, "<fieldset class=\"cuenta-bancaria ").Count);
        ComprobarCuentaConservada(html, 1, "8", "000222", "Segundo titular", "2");
        Assert.True(ObtenerOpcionesDelSelector(html, "PaisTelefono").Count > 200);
    }

    // ---------- Seguridad ----------

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Supervisor")]
    public async Task Post_ConRolPermitido_Registra(string rol)
    {
        HttpResponseMessage respuesta = await EnviarAltaAsync(CrearCliente(rol), DatosConCuentas(1));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Single(await _fabrica.LeerProveedoresAsync());
    }

    // Al Operador no se le muestra el formulario, pero el token lo puede sacar de cualquier
    // pantalla: lo que lo frena es el rol. El mismo caso con la sesión real está en
    // ValidacionSesionIniciadaIntegracionTests.RegistrarProveedor.cs.
    [Fact]
    public async Task Post_ComoOperador_ConTokenValido_SeRechazaSinGuardar()
    {
        HttpClient cliente = CrearCliente("Operador");
        string html = await LeerHtmlAsync(await cliente.GetAsync("/"));

        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["__RequestVerificationToken"] = ObtenerToken(html);

        HttpResponseMessage respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(datos));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(await _fabrica.LeerProveedoresAsync());
    }

    [Fact]
    public async Task Post_SinToken_SeRechazaSinGuardar()
    {
        HttpClient cliente = CrearCliente("Administrador");

        HttpResponseMessage respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(DatosConCuentas(1)));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await _fabrica.LeerProveedoresAsync());
    }

    [Fact]
    public async Task Post_ConTokenAlterado_SeRechazaSinGuardar()
    {
        HttpClient cliente = CrearCliente("Administrador");
        string html = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        Dictionary<string, string> datos = DatosConCuentas(1);
        datos["__RequestVerificationToken"] = ObtenerToken(html) + "alterado";

        HttpResponseMessage respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(datos));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await _fabrica.LeerProveedoresAsync());
    }

    // ---------- Ayudas ----------

    private static Dictionary<string, string> DatosConCuentas(int cantidad)
    {
        Dictionary<string, string> datos = DatosValidosDelProveedor();

        for (int indice = 0; indice < cantidad; indice++)
        {
            AgregarCuenta(datos, indice, "1", $"000{indice + 1}{indice + 1}{indice + 1}", $"Titular {indice + 1}", "1");
        }

        return datos;
    }

    private static void AgregarCuenta(Dictionary<string, string> datos, int indice, string banco, string numeroCuenta, string titular, string moneda)
    {
        datos[$"CuentasBancarias[{indice}].Banco"] = banco;
        datos[$"CuentasBancarias[{indice}].NumeroCuenta"] = numeroCuenta;
        datos[$"CuentasBancarias[{indice}].Titular"] = titular;
        datos[$"CuentasBancarias[{indice}].Moneda"] = moneda;
    }

    private static string ObtenerToken(string html)
    {
        string token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);

        return token;
    }

    // Primero abrimos el formulario, como haría el navegador, para conseguir el token.
    private static async Task<HttpResponseMessage> EnviarAltaAsync(HttpClient cliente, Dictionary<string, string> datos)
    {
        string html = await LeerHtmlAsync(await cliente.GetAsync(Ruta));

        var formulario = new Dictionary<string, string>(datos)
        {
            ["__RequestVerificationToken"] = ObtenerToken(html)
        };

        return await cliente.PostAsync(Ruta, new FormUrlEncodedContent(formulario));
    }

    // Lo que tiene que pasar en todo rechazo: vuelve el formulario (no una redirección), no aparece
    // el aviso de éxito y la base queda igual que antes. Devuelve el HTML para mirar el motivo.
    private async Task<string> ComprobarQueRechazaAsync(Dictionary<string, string> datos, int proveedoresEsperados = 0, int cuentasEsperadas = 0)
    {
        HttpResponseMessage respuesta = await EnviarAltaAsync(CrearCliente("Administrador"), datos);
        string html = await LeerHtmlAsync(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Datos del proveedor", html);
        Assert.DoesNotContain(MensajeDeExito, html);
        Assert.Equal(proveedoresEsperados, (await _fabrica.LeerProveedoresAsync()).Count);
        Assert.Equal(cuentasEsperadas, await _fabrica.ContarCuentasAsync());

        return html;
    }

    private static void ComprobarCuentaConservada(string html, int indice, string banco, string numeroCuenta, string titular, string moneda)
    {
        string prefijo = $"CuentasBancarias[{indice}]";

        Assert.Contains($"<option selected=\"selected\" value=\"{banco}\">", ObtenerSelector(html, $"{prefijo}.Banco"));
        Assert.Contains($"<option selected=\"selected\" value=\"{moneda}\">", ObtenerSelector(html, $"{prefijo}.Moneda"));
        Assert.Contains($"value=\"{numeroCuenta}\"", ObtenerEtiquetaDelCampo(html, "input", $"{prefijo}.NumeroCuenta"));
        Assert.Contains($"value=\"{titular}\"", ObtenerEtiquetaDelCampo(html, "input", $"{prefijo}.Titular"));
    }
}
