# 11. Checklist de reproducción

Esta es la receta corta, en orden cronológico, para repetir todo en **un proyecto nuevo**. La teoría está en los capítulos anteriores: el número entre paréntesis indica dónde.

## Fase A — Preparación (cap. 2)

- [ ] Tengo acceso a `https://entra.microsoft.com` en el tenant correcto.
- [ ] Puedo usar **New registration** (rol Application Developer o superior).
- [ ] Tengo una cuenta de prueba con MFA registrado (`mysignins.microsoft.com/security-info`).
- [ ] `dotnet --list-sdks` muestra la versión de .NET del proyecto.
- [ ] `dotnet dev-certs https --check --trust` confirma un certificado confiable.
- [ ] Anoté el puerto HTTPS de `Properties/launchSettings.json` (perfil `https`).

## Fase B — Registro en Entra ID (cap. 3)

- [ ] **Entra ID** › **App registrations** › **New registration**.
- [ ] Name descriptivo; **Supported account types**: *Single tenant only*; **Register**.
- [ ] Copié **Application (client) ID** y **Directory (tenant) ID** desde **Overview**.
- [ ] **Authentication** › **Add Redirect URI** › **Web** › `https://localhost:<PUERTO>/signin-oidc` › **Configure**.
- [ ] Agregué también `https://localhost:<PUERTO>/signout-callback-oidc`.
- [ ] Casillas de *implicit grant* (Access tokens / ID tokens) **sin marcar**.
- [ ] **Certificates & secrets** › **New client secret** › copié el **Value** (no el Secret ID) y anoté el vencimiento.
- [ ] Confirmé con el administrador cómo se exige MFA (Security defaults o Conditional Access).

## Fase C — Conexión del proyecto (cap. 4, T08)

- [ ] `dotnet add package Microsoft.Identity.Web` (en EOP: `--version 4.16.1`).
- [ ] `appsettings.json` con la sección `AzureAd`: `Instance`, `TenantId`, `ClientId`, `CallbackPath = /signin-oidc`. **Sin secreto**.
- [ ] `dotnet user-secrets init` y:
  - [ ] `dotnet user-secrets set "AzureAd:ClientCredentials:0:SourceType" "ClientSecret"`
  - [ ] `dotnet user-secrets set "AzureAd:ClientCredentials:0:ClientSecret" "<CLIENT-SECRET>"`
- [ ] En `Program.cs`:

```csharp
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.PostConfigure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options => { options.ResponseType = "code"; options.UsePkce = true; });
```

- [ ] Orden: `UseHttpsRedirection()` → `UseRouting()` → `UseAuthentication()` → `UseAuthorization()`.
- [ ] Prueba con un `[Authorize]` temporal: me manda a Microsoft, inicio sesión y vuelvo. **Saqué el `[Authorize]` temporal.**

## Fase D — Sesión segura y todo cerrado (cap. 6 y 8, T09)

- [ ] Cookie de sesión:

```csharp
builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
```

- [ ] Política de respaldo:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

- [ ] `app.MapStaticAssets().AllowAnonymous();`
- [ ] `[AllowAnonymous]` en la acción `Error` del `HomeController`.

## Fase E — Inicio de sesión (cap. 5, T09)

- [ ] `Controllers/CuentaController.cs` con `IniciarSesion(string? urlRetorno)`:
  - [ ] `[HttpGet]` y `[AllowAnonymous]`.
  - [ ] `Url.IsLocalUrl(urlRetorno) ? urlRetorno : Url.Content("~/")`.
  - [ ] Si ya está autenticado: `LocalRedirect(destino)`.
  - [ ] Si no: `Challenge(new AuthenticationProperties { RedirectUri = destino }, OpenIdConnectDefaults.AuthenticationScheme)`.
- [ ] `Views/Shared/_EstadoSesionPartial.cshtml`: nombre (`User.FindFirst("name")`) + formulario de cierre, o enlace "Iniciar sesión".
- [ ] `<partial name="_EstadoSesionPartial" />` en `_Layout.cshtml`.

## Fase F — Cierre de sesión (cap. 7, T09)

- [ ] `CerrarSesion()` con `[HttpPost]` y `[ValidateAntiForgeryToken]`.
- [ ] `SignOut(new AuthenticationProperties { RedirectUri = Url.Action(nameof(SesionCerrada), "Cuenta") }, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme)`.
- [ ] Formulario `<form asp-controller="Cuenta" asp-action="CerrarSesion" method="post">`.
- [ ] `SesionCerrada()` con `[AllowAnonymous]` y su vista.
- [ ] `/signout-callback-oidc` registrada en Entra ID (fase B).

## Fase G — Fallos del login (cap. 8, T09)

- [ ] Clase `ManejadorFallosInicioSesion.ManejarAsync(RemoteFailureContext, ILogger)`: log con tipo + código + id de solicitud, sin descripción; `Response.Redirect(PathBase + "/Cuenta/ErrorInicioSesion")`; `HandleResponse()`.
- [ ] En el `PostConfigure` de OpenIdConnect: encadenar `options.Events.OnRemoteFailure` llamando primero al evento anterior.
- [ ] `ErrorInicioSesion()` con `[AllowAnonymous]` y su vista.

## Fase H — Verificación (cap. 9)

- [ ] Pruebas manuales M1 a M6 superadas.
- [ ] Pruebas unitarias del Controller y del manejador de fallos.
- [ ] Pruebas de integración con `WebApplicationFactory<Program>` y esquema de prueba.
- [ ] `dotnet test` en verde.
- [ ] `git status` **no** muestra `secrets.json` ni secretos en ningún archivo.
- [ ] La descripción del PR incluye la configuración de Azure que tiene que hacer quien revise.

## Si algo falla

Buscá el código AADSTS o el síntoma en el **capítulo 10**.
