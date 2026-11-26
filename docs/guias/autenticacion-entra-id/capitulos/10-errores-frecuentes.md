# 10. Errores frecuentes y soluciones

Este capítulo es para consultar **cuando algo falla**. Primero, cómo investigar; después, cada problema con su mensaje, su causa y su solución.

> [!NOTA] De dónde salen los mensajes
> Los textos de los errores **AADSTS** son los de la tabla oficial de Microsoft (*Microsoft Entra authentication & authorization error codes*), en su idioma original. Los mensajes de EOP salen del código del repositorio. Donde no se pudo verificar un texto exacto, se describe el síntoma sin inventar el mensaje.

## 10.1 Cómo investigar un problema de login

1. **Leé el código AADSTS.** Cuando el error lo muestra Microsoft, la página incluye un código como `AADSTS50011`. Podés buscarlo en `https://login.microsoftonline.com/error?code=50011` (cambiando el número).
2. **Mirá la consola de la app.** En Visual Studio: ventana **Output**, opción *Show output from:* del servidor web. Ahí aparecen los `warn:` y `fail:` de ASP.NET Core y de `ManejadorFallosInicioSesion`.
3. **Seguí las redirecciones en el navegador.** <kbd>F12</kbd> › pestaña **Network**, marcá **Preserve log** y repetí el login. Vas a ver la cadena `/` → `login.microsoftonline.com/…/authorize` → `/signin-oidc` → destino, con el código de estado de cada paso.
4. **Revisá las cookies.** <kbd>F12</kbd> › **Application** › **Cookies**.
5. **Registros del tenant.** Si tenés permisos de administración, Entra ID tiene **registros de inicio de sesión** (*Sign-in logs*) donde cada intento aparece con su motivo de fallo.

## 10.2 Problemas de configuración del registro

### AADSTS50011 — La Redirect URI no coincide

| | |
|---|---|
| **Mensaje** | `AADSTS50011` *InvalidReplyTo - The reply address is missing, misconfigured, or doesn't match reply addresses configured for the app.* |
| **Causa** | La dirección que manda la app (`redirect_uri`) no está registrada **exactamente igual** en **Authentication**: falta, difiere en mayúsculas, en `http`/`https`, en la ruta (`/signin-oidc`), o el dominio es otro (por ejemplo, al publicar en producción) |
| **Diagnóstico** | En **Network**, abrí el pedido a `/authorize` y mirá el parámetro `redirect_uri` (viene codificado: `%3A%2F%2F` es `://`). Comparalo letra por letra con el portal |
| **Solución** | Agregar esa dirección en **Authentication** › plataforma **Web**, o corregir `CallbackPath` en `appsettings.json` |

### AADSTS700016 — La aplicación no existe en el tenant

| | |
|---|---|
| **Mensaje** | `AADSTS700016` *UnauthorizedClient_DoesNotMatchRequest - The application wasn't found in the directory/tenant.* |
| **Causa** | `ClientId` equivocado, o `TenantId` de **otro** tenant distinto del que tiene el registro |
| **Solución** | Copiar de nuevo **Application (client) ID** y **Directory (tenant) ID** desde la página **Overview** del registro |

### AADSTS90002 — No se encuentra el tenant

| | |
|---|---|
| **Mensaje** | `AADSTS90002` *InvalidTenantName - The tenant name wasn't found in the data store. Check to make sure you have the correct tenant ID.* |
| **Causa** | `TenantId` mal escrito (un carácter de más o de menos, espacios) |
| **Solución** | Copiar el **Directory (tenant) ID** desde **Overview** |

## 10.3 Problemas con el client secret

Estos errores aparecen **después** de iniciar sesión en Microsoft, cuando la app intenta canjear el código. El usuario ve que vuelve a la app, pero la app responde con error.

### AADSTS7000215 — Secreto inválido

| | |
|---|---|
| **Mensaje** | `AADSTS7000215` *Invalid client secret is provided.* |
| **Causa más común** | Se copió el **Secret ID** en lugar del **Value**. Otras: el valor quedó cortado o con espacios, o es el secreto de **otro** registro |
| **Solución** | Crear un secreto nuevo, copiar la columna **Value** y actualizar User Secrets (`AzureAd:ClientCredentials:0:ClientSecret`) |

### AADSTS7000222 — Secreto vencido

| | |
|---|---|
| **Mensaje** | `AADSTS7000222` *InvalidClientSecretExpiredKeysProvided - The provided client secret keys are expired.* |
| **Causa** | Pasó la fecha de vencimiento elegida al crear el secreto |
| **Solución** | **Certificates & secrets** › **New client secret**, actualizar User Secrets en cada máquina y borrar el secreto viejo |

### AADSTS7000218 — No se mandó ningún secreto

| | |
|---|---|
| **Mensaje** | `AADSTS7000218` *The request body must contain the following parameter: 'client_assertion' or 'client_secret'.* |
| **Causa** | La app no encontró el secreto. Por ejemplo: User Secrets cargados en **otro** proyecto, claves mal escritas (`ClientCredential` sin `s`, falta el `:0:`), o el entorno no es `Development` (User Secrets solo se cargan en Development) |
| **Diagnóstico** | `dotnet user-secrets list --project <ruta del proyecto web>` y revisar `ASPNETCORE_ENVIRONMENT` en `launchSettings.json` |
| **Solución** | Volver a cargar las dos claves del capítulo 4.4 en el proyecto web correcto |

## 10.4 Problemas de HTTPS y cookies

### El navegador dice que la conexión no es privada

| | |
|---|---|
| **Síntoma** | Advertencia de certificado al abrir `https://localhost:<PUERTO>` |
| **Causa** | El certificado de desarrollo no existe o no es de confianza |
| **Solución** | `dotnet dev-certs https --trust` y reiniciar el navegador |

### "Correlation failed."

| | |
|---|---|
| **Síntoma** | Después de autenticarse en Microsoft, al volver a `/signin-oidc` la app da error. En EOP se ve la pantalla **"No se pudo iniciar sesión"** y en el log aparece `AuthenticationFailureException` |
| **Causa** | Al volver, la app no encontró la cookie `.AspNetCore.Correlation.*` que dejó al empezar. Pasa si: se ejecuta con el perfil **http** (la cookie exige HTTPS); se usó el botón **Atrás** o se reenvió un login viejo; se tardó demasiado en la pantalla de Microsoft; se empezó el login en una pestaña y se terminó en otra ventana privada; o el navegador bloquea cookies de `localhost` |
| **Solución** | Usar el perfil **https**, empezar de nuevo desde la app (no desde el historial) y no bloquear cookies para `localhost` |

### Entré directo a /signin-oidc y da error

Si abrís `/signin-oidc` a mano, la app recibe una respuesta que no pidió (sin `state`) y la rechaza. En EOP termina en **"No se pudo iniciar sesión"**, con `Código: sin código` en el log. **Es el comportamiento correcto.**

### Bucle infinito de redirecciones

| | |
|---|---|
| **Síntoma** | Después del login, la app vuelve a mandar a Microsoft una y otra vez |
| **Causas** | `UseAuthorization()` antes que `UseAuthentication()`; o la cookie de sesión no se guarda (perfil **http** con `SecurePolicy = Always`) |
| **Solución** | Orden correcto de middlewares (capítulo 4.6) y perfil **https** |

### La aplicación no arranca: falta la cadena de conexión (propio de EOP)

| | |
|---|---|
| **Mensaje** | `Falta la cadena de conexión 'EopDb'. En desarrollo, configurala con: dotnet user-secrets set "ConnectionStrings:EopDb" "<cadena>" --project src/EOP.Gestion.WebApp` |
| **Causa** | User Secrets incompletos en una máquina nueva. No es de Entra ID, pero suele aparecer al configurar los secretos |
| **Solución** | Cargar la cadena de conexión con ese mismo comando |

## 10.5 Problemas del cierre de sesión

| Síntoma | Causa | Solución |
|---|---|---|
| Después de **Cerrar sesión** te quedás en una página de Microsoft y no volvés a la app | `https://localhost:<PUERTO>/signout-callback-oidc` no está registrada como Redirect URI | Agregarla en **Authentication** (capítulo 7.5) |
| Después de salir, **Iniciar sesión** entra directo sin pedir la cuenta | Solo se cerró la cookie local: falta el esquema `OpenIdConnect` en `SignOut` | `SignOut(propiedades, Cookies, OpenIdConnect)` |
| **400 Bad Request** al tocar Cerrar sesión | El formulario no manda el token antifalsificación (no usa `asp-action` o `method="post"`, o se armó el HTML a mano) | Usar el *tag helper* `<form asp-controller="Cuenta" asp-action="CerrarSesion" method="post">` |
| **404** al intentar cerrar sesión con un enlace | `CerrarSesion` solo acepta POST | Usar el formulario, no un `<a href>` |
| "¿De qué cuenta querés salir?" | Comportamiento normal de Microsoft cuando no recibe una pista de la cuenta | Nada que corregir (mejora opcional `logout_hint`, capítulo 8.6) |
| Al apretar **Atrás** se ve la página anterior | El navegador muestra una copia guardada | Recargar: pide login. Mejora opcional: `Cache-Control: no-store` |

## 10.6 Problemas relacionados con MFA

| Código | Mensaje oficial (resumido) | Qué significa | Qué hacer |
|---|---|---|---|
| `AADSTS50076` | *…the user must use multifactor authentication to access the resource.* | El tenant exige MFA para este acceso | Completar el segundo factor; es parte del flujo normal |
| `AADSTS50079` / `AADSTS50072` | *…the user is required to use multifactor authentication.* / *User needs to enroll for second factor authentication (interactive).* | El usuario **no tiene registrado** un método de MFA | Registrarlo en `https://mysignins.microsoft.com/security-info` |
| `AADSTS50074` | *Strong authentication is required and the user did not pass the MFA challenge.* | No se completó el MFA | Reintentar; revisar el celular y la app Authenticator |
| `AADSTS53003` | *BlockedByConditionalAccess - Access has been blocked by Conditional Access policies.* | Una política de acceso condicional bloqueó el acceso | Consultar al administrador del tenant |

**"No me pide MFA nunca."** Con *Security defaults*, Microsoft decide **cuándo** pedir el segundo factor según el riesgo; puede no pedirlo en cada login. Si el requerimiento es "siempre", hace falta *Conditional Access* (capítulo 3.8). La aplicación **no** lo controla.

## 10.7 Problemas de permisos y usuarios

| Código | Mensaje oficial (resumido) | Causa | Solución |
|---|---|---|---|
| `AADSTS65001` | *DelegationDoesNotExist - The user or administrator hasn't consented to use the application…* | El tenant no deja que los usuarios acepten permisos | Un administrador: **API permissions** › **Grant admin consent for…** |
| `AADSTS50105` | *EntitlementGrantsNotFound - The signed in user isn't assigned to a role for the signed in app.* | **Assignment required?** está en **Yes** y el usuario no está asignado | Asignarlo en **Enterprise apps** › la app › **Users and groups** |
| `AADSTS50020` | *UserUnauthorized - … does not exist in tenant … This account needs to be added as an external user in the tenant first.* | La cuenta es de **otra** organización y el registro es *single tenant* | Invitar al usuario como invitado, o usar una cuenta del tenant |
| `AADSTS50034` | *UserAccountNotFound - To sign into this application, the account must be added to the directory.* | La cuenta no existe en el tenant (o se escribió mal) | Revisar el usuario; que un administrador lo cree |
| `AADSTS50057` | *UserDisabled - The user account is disabled.* | Cuenta deshabilitada en Entra ID | Que un administrador la habilite |

## 10.8 Problemas visuales o de navegación

| Síntoma | Causa | Solución |
|---|---|---|
| "Sesión cerrada" o "No se pudo iniciar sesión" **sin estilos** | La FallbackPolicy bloquea los archivos de `wwwroot` | `app.MapStaticAssets().AllowAnonymous();` |
| Un error durante el login lleva **otra vez** al login | La página de error exige sesión | `[AllowAnonymous]` en `HomeController.Error` |
| El menú muestra el **correo** en lugar del nombre | El token no trae el claim `name` | Normal: es el respaldo (`User.Identity.Name`). Revisar el perfil del usuario en Entra ID |
