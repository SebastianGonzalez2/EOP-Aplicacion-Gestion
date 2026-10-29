# 9. Pruebas y verificación

La autenticación se prueba de dos maneras complementarias:

| Tipo | Qué comprueba | Limitación |
|---|---|---|
| **Manual** | El circuito real con Microsoft: tu cuenta, el MFA, el portal | Lleva tiempo y depende de una persona con cuenta |
| **Automática** | Que nuestro código y nuestra configuración hagan lo que deben, sin hablar con Microsoft | No prueba Entra ID en sí |

## 9.1 Pruebas manuales

**Preparación común**: Redirect URIs y secreto configurados (capítulos 3 y 4); app ejecutándose con el perfil **https**; navegador en **ventana privada** (InPrivate en Edge con <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd>, incógnito en Chrome con <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd>), para no mezclar sesiones anteriores.

### Prueba M1 — Entrar sin sesión

| | |
|---|---|
| **Objetivo** | Comprobar que ninguna pantalla protegida se ve sin sesión |
| **Condiciones previas** | Ventana privada nueva |
| **Pasos** | Abrir `https://localhost:<PUERTO>/` |
| **Resultado esperado** | El navegador termina en `login.microsoftonline.com` (Figura 4.1) |
| **Si falla** | Si se ve la app sin pedir login: falta la `FallbackPolicy` o la acción tiene `[AllowAnonymous]`. Si aparece un error de Microsoft: ver capítulo 10 (AADSTS50011, AADSTS700016, AADSTS90002) |

### Prueba M2 — Iniciar sesión con MFA

| | |
|---|---|
| **Objetivo** | Comprobar el login completo y la creación de la sesión |
| **Condiciones previas** | M1 superada; cuenta con MFA |
| **Pasos** | Ingresar usuario, contraseña y aprobar el segundo factor |
| **Resultado esperado** | Volvés a la app con **tu nombre** arriba a la derecha y el botón **Cerrar sesión** |
| **Si falla** | AADSTS7000215 / AADSTS7000222: secreto. *Correlation failed*: cookies o perfil http. Bucle de redirecciones: orden de middlewares |

### Prueba M3 — Cookie segura

| | |
|---|---|
| **Objetivo** | Verificar los atributos de la cookie de sesión |
| **Condiciones previas** | Sesión iniciada (M2) |
| **Pasos** | <kbd>F12</kbd> › **Application** › **Cookies** › `https://localhost:<PUERTO>` |
| **Resultado esperado** | `.AspNetCore.Cookies` (o `...C1`, `...C2`) con las columnas **HttpOnly** y **Secure** marcadas |
| **Si falla** | Revisar la configuración de `CookieAuthenticationOptions` en `Program.cs` |

### Prueba M4 — Cerrar sesión

| | |
|---|---|
| **Objetivo** | Comprobar que se cierran las dos sesiones y se vuelve a la app |
| **Condiciones previas** | Sesión iniciada |
| **Pasos** | Tocar **Cerrar sesión**; si Microsoft pregunta, elegir tu cuenta |
| **Resultado esperado** | Pantalla **"Cerraste sesión"** con estilos (Figura 7.1); la cookie `.AspNetCore.Cookies` ya no está |
| **Si falla** | Te quedás en una página de Microsoft: falta registrar `/signout-callback-oidc`. Error 400: falta el token antifalsificación en el formulario. Página sin estilos: falta `MapStaticAssets().AllowAnonymous()` |

### Prueba M5 — Después de salir, pide login de nuevo

| | |
|---|---|
| **Objetivo** | Comprobar que la sesión realmente terminó |
| **Condiciones previas** | M4 superada |
| **Pasos** | Escribir la dirección de inicio de la app y Enter |
| **Resultado esperado** | Te manda a Microsoft y **te pide la cuenta** (no entra directo) |
| **Si falla** | Si entra directo sin pedir nada, no se está cerrando la sesión de Microsoft: revisá que `SignOut` incluya el esquema `OpenIdConnect` |

### Prueba M6 — Login fallido

| | |
|---|---|
| **Objetivo** | Comprobar que un fallo en el regreso no termina en error 500 |
| **Condiciones previas** | Ventana privada nueva |
| **Pasos** | 1. Abrir la app (te lleva a Microsoft). 2. **Antes** de escribir el usuario, <kbd>F12</kbd> › **Application** › **Cookies** › `https://localhost:<PUERTO>` y borrar las que empiezan con `.AspNetCore.Correlation` y `.AspNetCore.OpenIdConnect.Nonce`. 3. Completar el login |
| **Resultado esperado** | Pantalla **"No se pudo iniciar sesión"** (Figura 8.1) y una línea `warn: …ManejadorFallosInicioSesion` en la ventana **Output** de Visual Studio |
| **Si falla** | Si aparece una página de error genérica, revisá que `OnRemoteFailure` esté configurado y que llame a `HandleResponse()` |

> [!NOTA] ¿Por qué borrar cookies y no "cancelar el MFA"?
> Si rechazás el MFA, Microsoft muchas veces muestra **su propia** página de error y no vuelve a la app. Eso también es un resultado correcto (no entraste), pero no ejercita nuestro manejo. Borrar las cookies temporales **simula un ataque**: un login que la app no inició. La app lo detecta y lo rechaza.

> [!EOP]
> Las pruebas M1 a M6 se hicieron manualmente al cerrar la T09, con una cuenta real del tenant y MFA, y todas dieron el resultado esperado.

## 9.2 Pruebas automáticas: cómo ejecutarlas

### Desde Visual Studio

1. Menú **Test** › **Test Explorer**.
2. **Run All Tests** (o <kbd>Ctrl</kbd>+<kbd>R</kbd>, <kbd>A</kbd>).
3. Para correr solo las de sesión, escribí `Cuenta` o `Sesion` en el cuadro de búsqueda del Test Explorer.

### Desde la terminal

```bash
dotnet test EOP.Gestion.slnx
```

Solo las pruebas de autenticación:

```bash
dotnet test EOP.Gestion.slnx --filter "FullyQualifiedName~CuentaControllerTests|FullyQualifiedName~ManejadorFallosInicioSesionTests|FullyQualifiedName~SesionIntegracionTests"
```

Resultado esperado (EOP al cerrar la T09):

```text
Passed!  - Failed: 0, Passed: 33, Skipped: 0, Total: 33
```

## 9.3 Probar sin llamar a Microsoft: la idea

Las pruebas automáticas **nunca** hablan con Entra ID. ¿Cómo se prueba entonces un login? Separando responsabilidades:

- **Lo que hace nuestro código** (qué le pide a ASP.NET Core) se prueba con **pruebas unitarias**: llamamos al método y miramos el resultado que devuelve, sin ejecutarlo de verdad.
- **Cómo se arma la aplicación** (políticas, middlewares, rutas) se prueba con **pruebas de integración**: levantamos la app entera en memoria y **reemplazamos solo el login de Microsoft** por un esquema de prueba.

## 9.4 Pruebas unitarias del Controller

Un `ChallengeResult` o un `SignOutResult` no hace nada hasta que ASP.NET Core lo ejecuta. En una prueba unitaria simplemente lo **inspeccionamos**:

```csharp
[Fact]
public void IniciarSesion_SinSesion_PideAutenticacionConEntraId()
{
    var controller = CrearController(UsuarioAnonimo());

    var resultado = Assert.IsType<ChallengeResult>(controller.IniciarSesion("/Home/Privacy"));

    Assert.Equal([OpenIdConnectDefaults.AuthenticationScheme], resultado.AuthenticationSchemes);
    Assert.Equal("/Home/Privacy", resultado.Properties?.RedirectUri);
}
```

Para crear el Controller hace falta "simular" la petición:

```csharp
private static CuentaController CrearController(ClaimsPrincipal usuario)
{
    var contexto = new DefaultHttpContext { User = usuario };
    var contextoAccion = new ActionContext(contexto, new RouteData(), new ControllerActionDescriptor());

    return new CuentaController
    {
        ControllerContext = new ControllerContext(contextoAccion),
        Url = new UrlHelper(contextoAccion)
    };
}

private static ClaimsPrincipal UsuarioAnonimo() => new(new ClaimsIdentity());

// Una identidad con "tipo de autenticación" cuenta como autenticada.
private static ClaimsPrincipal UsuarioAutenticado() =>
    new(new ClaimsIdentity([new Claim("name", "Usuario de Prueba")], "Prueba"));
```

| Pieza | Para qué |
|---|---|
| `DefaultHttpContext` | Una petición HTTP "de mentira" |
| `ClaimsIdentity()` sin tipo | Usuario **anónimo** |
| `ClaimsIdentity(claims, "Prueba")` | Usuario **autenticado**: lo que define que está autenticado es el segundo parámetro |
| `ControllerActionDescriptor` | Lo exige `ControllerContext`: con un `ActionDescriptor` genérico lanza una excepción |
| `UrlHelper` real | Para que `Url.IsLocalUrl` y `Url.Content` funcionen de verdad |

> [!CONSEJO]
> `Url.Action(...)` (usado en `CerrarSesion`) necesita las rutas de la app completa y falla en una prueba unitaria. Para esa prueba, EOP usa un `IUrlHelper` falso (`UrlHelperDePrueba`) que arma `/Controller/Accion`.

**Pruebas de atributos por reflexión.** Si alguien borra un `[AllowAnonymous]` o un `[ValidateAntiForgeryToken]` por error, ninguna otra prueba lo notaría. Por eso se verifica su presencia:

```csharp
[Fact]
public void CerrarSesion_SoloAceptaPostConTokenAntifalsificacion()
{
    var metodo = typeof(CuentaController).GetMethod(nameof(CuentaController.CerrarSesion))!;

    Assert.NotNull(metodo.GetCustomAttribute<HttpPostAttribute>());
    Assert.NotNull(metodo.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    Assert.Null(metodo.GetCustomAttribute<HttpGetAttribute>());
}
```

**Prueba del manejador de fallos.** Se construye un `RemoteFailureContext` a mano y se usa un logger falso que guarda lo que se registró:

```csharp
var fallo = new InvalidOperationException("mensaje con dato sensible");
fallo.Data["error"] = "access_denied";
fallo.Data["error_description"] = "descripcion con dato sensible";
// ...
Assert.Contains("access_denied", logger.Mensaje);
Assert.DoesNotContain("dato sensible", logger.Mensaje);
```

## 9.5 Pruebas de integración con WebApplicationFactory

`WebApplicationFactory<Program>` (paquete **`Microsoft.AspNetCore.Mvc.Testing`**) arranca **la aplicación real** en memoria, con su `Program.cs`, y entrega un `HttpClient` para hacerle pedidos.

```bash
dotnet add tests/EOP.Gestion.Tests package Microsoft.AspNetCore.Mvc.Testing --version 10.0.10
```

> [!NOTA]
> Con *top-level statements* (un `Program.cs` sin clase), en versiones anteriores había que agregar `public partial class Program;` al final para que las pruebas pudieran usar `Program`. En EOP, con .NET 10, **no hizo falta**: compiló sin agregarlo.

La fábrica de EOP cambia solo dos cosas respecto de la app real:

```csharp
public sealed class FabricaDePrueba : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Cadena de conexión ficticia: la app no se conecta a la base al arrancar.
        builder.UseSetting(
            "ConnectionStrings:EopDb",
            "Server=(localdb)\\pruebas;Database=NoSeUsa;Trusted_Connection=True");

        builder.ConfigureTestServices(servicios =>
        {
            // 2. Un esquema de login "de mentira" que reemplaza a Microsoft.
            servicios.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, EsquemaDePrueba>(EsquemaDePrueba.Nombre, null);

            servicios.PostConfigure<AuthenticationOptions>(opciones =>
            {
                opciones.DefaultScheme = EsquemaDePrueba.Nombre;
                opciones.DefaultAuthenticateScheme = EsquemaDePrueba.Nombre;
                opciones.DefaultChallengeScheme = EsquemaDePrueba.Nombre;
            });
        });
    }
}
```

El esquema de prueba considera autenticado a quien manda un encabezado especial y, cuando hay que "pedir login", responde **401** en lugar de redirigir a Microsoft:

```csharp
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    if (!Request.Headers.ContainsKey(Encabezado))
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    var identidad = new ClaimsIdentity([new Claim("name", NombreUsuario)], Nombre);
    var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Nombre);

    return Task.FromResult(AuthenticateResult.Success(ticket));
}
```

Ejemplo de prueba:

```csharp
[Fact]
public async Task CerrarSesion_SinTokenAntifalsificacion_SeRechaza()
{
    var respuesta = await CrearCliente(conSesion: true)
        .PostAsync("/Cuenta/CerrarSesion", new FormUrlEncodedContent([]));

    Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
}
```

> [!CONSEJO] Algo que aprendimos
> Un **GET** a una acción que solo tiene `[HttpPost]` devuelve **404**, no 405: con rutas convencionales de MVC, para un GET esa acción "no existe". En la prueba de integración la primera suposición fue 405 y hubo que corregirla. Lo importante se cumple igual: por GET no se cierra la sesión.

## 9.6 Las 24 pruebas que agregó la T09

| Archivo | Pruebas | Qué verifican |
|---|---|---|
| `Unitarias/CuentaControllerTests.cs` | 11 | Challenge a Entra ID; URL de retorno externa o vacía (3 casos); redirección si ya hay sesión; `SignOut` de los dos esquemas; POST + token en `CerrarSesion`; `[AllowAnonymous]` en 4 acciones |
| `Unitarias/ManejadorFallosInicioSesionTests.cs` | 2 | Redirige y evita la excepción; registra el código sin datos sensibles |
| `Integracion/SesionIntegracionTests.cs` | 11 | FallbackPolicy activa; cookie HttpOnly + Secure; `/` y `/Home/Privacy` bloqueadas sin sesión; 4 recursos públicos; nombre y formulario con token con sesión; GET a `CerrarSesion` no cierra; POST sin token rechazado |

Con las 9 que ya existían (arquitectura, infraestructura, clientes y manejo de errores), el total es **33**.
