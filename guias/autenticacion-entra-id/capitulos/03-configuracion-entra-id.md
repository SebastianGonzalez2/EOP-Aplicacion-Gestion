# 3. Configuración de Microsoft Entra ID desde cero (S1-T08)

En este capítulo damos de alta la aplicación en Entra ID. Al terminar vas a tener **cuatro datos** que la aplicación necesita, y vas a saber exactamente dónde se usa cada uno.

> [!RECONSTRUIDO]
> Los clics que se hicieron en el portal durante la T08 **no quedaron registrados** en el repositorio. Este procedimiento se reconstruyó a partir de la configuración que sí está en EOP (`appsettings.json`, las claves de User Secrets y las direcciones `/signin-oidc` y `/signout-callback-oidc`) y de la documentación oficial de Microsoft, consultada en octubre de 2026.

> [!ADVERTENCIA] La interfaz del portal cambia seguido
> Microsoft renombra y mueve opciones con frecuencia. Los nombres de esta guía corresponden a la documentación oficial vigente al momento de escribirla (por ejemplo, la pestaña **Redirect URI configuration** dentro de **Authentication**). Si algo no aparece con el mismo nombre, buscá la opción equivalente: el **concepto** (registrar la app, cargar la dirección de retorno, crear el secreto) no cambia. El portal puede estar en inglés o en español según tu configuración; la guía usa los nombres en inglés, que son los de la documentación.

## 3.1 Los conceptos que vas a configurar

| Concepto | Qué es | Analogía |
|---|---|---|
| **App registration** (registro de aplicación) | El alta de tu aplicación en el tenant. Sin esto, Entra ID no sabe que tu app existe y se niega a emitirle tokens | Anotar a una empresa proveedora en la recepción del edificio |
| **Application (client) ID** | Identificador **público** de tu aplicación (un GUID). Va en cada pedido de login | El número de proveedor |
| **Directory (tenant) ID** | Identificador de la organización (el directorio) | La dirección del edificio |
| **Client secret** | Una **contraseña de la aplicación** (no de un usuario). La app la usa para demostrarle a Entra ID que es ella al canjear el código | La firma autorizada del proveedor |
| **Redirect URI** | Dirección de **tu** app a la que Entra ID puede devolver al usuario. Si una dirección no está registrada, Entra ID se niega a redirigir | La lista de puertas por las que la recepción te deja salir |
| **Authentication** | La página del registro donde se configuran plataforma y Redirect URIs | — |

## 3.2 Paso 1 — Abrir el centro de administración

1. Abrí **`https://entra.microsoft.com`** (Microsoft Entra admin center). También funciona `https://portal.azure.com` buscando *Microsoft Entra ID*; los menús son equivalentes.
2. Iniciá sesión con una cuenta que tenga al menos el rol **Application Developer** (capítulo 2).
3. Si tenés varios tenants, usá el ícono **Settings** (engranaje) de la barra superior para cambiar al tenant correcto.

## 3.3 Paso 2 — Crear el registro de aplicación

1. En el menú de la izquierda: **Entra ID** › **App registrations**.
2. Arriba, elegí **New registration**.

![Entra ID › App registrations, con el botón New registration](imagenes/portal/01-app-registrations.png "Figura 3.1. Lista de registros de aplicación y botón New registration.")

3. Completá el formulario **Register an application**:

| Campo | Qué poner | Por qué |
|---|---|---|
| **Name** | Un nombre descriptivo, por ejemplo `EOP Gestion - Desarrollo` | Los usuarios pueden verlo en la pantalla de consentimiento. Se puede cambiar después |
| **Supported account types** | **Single tenant only - `<tu organización>`** | Solo pueden entrar cuentas de **tu** organización (y sus invitados). Es lo recomendado para aplicaciones internas como EOP |
| **Redirect URI** (si el formulario muestra esta sección) | Plataforma **Web** y la dirección `https://localhost:<PUERTO>/signin-oidc` | Es opcional en este paso: si no aparece, la cargás en el paso 4 |

4. Elegí **Register**.

![Formulario Register an application](imagenes/portal/02-new-registration.png "Figura 3.2. Formulario de alta: nombre y tipos de cuenta admitidos.")

**Las otras opciones de "Supported account types"** (no las usamos, pero conviene saber qué hacen):

| Opción | Quién puede iniciar sesión | Cuándo se usa |
|---|---|---|
| **Multiple Entra ID tenants** | Cuentas de cualquier organización con Entra ID | Productos SaaS para muchas empresas |
| **Any Entra ID Tenant + Personal Microsoft accounts** | Lo anterior más cuentas personales (Outlook, Xbox…) | Aplicaciones abiertas al público |
| **Personal accounts only** | Solo cuentas personales | Aplicaciones de consumo |

> [!EOP]
> EOP es una aplicación interna de una sola empresa, por eso usa un tenant concreto: en `appsettings.json` el campo `TenantId` tiene el GUID del tenant (no `common` ni `organizations`). Eso es coherente con **Single tenant**.

## 3.4 Paso 3 — Copiar los identificadores

Al registrar, el portal abre la página **Overview** (Información general) de la aplicación. Copiá estos dos valores:

| Dato en el portal | Dónde lo vas a usar | ¿Es secreto? |
|---|---|---|
| **Application (client) ID** | `appsettings.json` › `AzureAd:ClientId` | No |
| **Directory (tenant) ID** | `appsettings.json` › `AzureAd:TenantId` | No |

![Página Overview del registro](imagenes/portal/03-overview.png "Figura 3.3. Overview: Application (client) ID y Directory (tenant) ID.")

> [!NOTA]
> El Client ID y el Tenant ID **no son secretos**: viajan en la dirección del navegador en cada login (lo vas a ver en el capítulo 4). Por eso EOP los tiene en `appsettings.json`, que está versionado. Lo único secreto es el **client secret**.

> [!CONSEJO]
> En la misma página, el botón **Endpoints** muestra las direcciones de Entra ID para tu tenant. La que dice **OpenID Connect metadata document** es la que la librería consulta automáticamente para descubrir todo lo demás.

## 3.5 Paso 4 — Configurar la plataforma Web y las Redirect URIs

Una **Redirect URI** es una dirección de tu aplicación a la que Microsoft puede devolver al usuario. Necesitamos **dos**:

| Dirección | Para qué | ¿Quién la atiende? |
|---|---|---|
| `https://localhost:<PUERTO>/signin-oidc` | Regreso **después de iniciar sesión** | Microsoft.Identity.Web (no escribís un Controller) |
| `https://localhost:<PUERTO>/signout-callback-oidc` | Regreso **después de cerrar sesión** | Microsoft.Identity.Web |

Pasos (interfaz actual según la documentación de Microsoft):

1. En el registro de la aplicación, en el menú **Manage**, elegí **Authentication**.
2. En la pestaña **Redirect URI configuration**, elegí **Add Redirect URI**.
3. En el panel **Select a platform to add redirect URI**, elegí el mosaico **Web**.
   - **Web** es para aplicaciones donde el código corre en el servidor (ASP.NET Core MVC).
   - **No** elijas *Single-page application*: es para aplicaciones JavaScript que corren en el navegador y usa otro flujo.
4. En **Redirect URI**, escribí `https://localhost:<PUERTO>/signin-oidc`.
5. Dejá **Front-channel logout URL** vacío (se explica en el capítulo 8 como mejora opcional).
6. Elegí **Configure**.
7. Repetí el procedimiento, o usá la opción para agregar otra dirección a la plataforma Web que ya creaste, con `https://localhost:<PUERTO>/signout-callback-oidc`.
8. Guardá los cambios si el portal muestra un botón **Save**.

> [!NOTA] Si ves una interfaz distinta
> En versiones anteriores del portal (y en algunas páginas de documentación) el mismo lugar se llama **Platform configurations** › **Add a platform** › **Web**. Es la misma configuración con otro diseño.

![Authentication: plataforma Web con las dos Redirect URIs](imagenes/portal/04-authentication-web.png "Figura 3.4. Plataforma Web con /signin-oidc y /signout-callback-oidc.")

**Casillas de "Implicit grant and hybrid flows"**: si el portal las muestra (*Access tokens* e *ID tokens*), **dejalas sin marcar**. Esas casillas habilitan un flujo antiguo en el que el token viaja directamente por el navegador. Nosotros usamos el **flujo de código**, en el que el token se obtiene servidor a servidor, y no las necesita.

> [!EOP]
> En EOP las direcciones son `https://localhost:7125/signin-oidc` (registrada en la T08) y `https://localhost:7125/signout-callback-oidc` (registrada en la T09, como se indicó en el PR #13). La parte `/signin-oidc` sale de `"CallbackPath": "/signin-oidc"` en `appsettings.json`; `/signout-callback-oidc` es el valor por defecto de la librería.

> [!ADVERTENCIA] Reglas de las Redirect URIs
> - Tienen que empezar con `https` (con `localhost` se admite `http`, pero en EOP no lo usamos).
> - Distinguen **mayúsculas y minúsculas**: `/signin-oidc` no es lo mismo que `/Signin-OIDC`.
> - Máximo 256 caracteres cada una.
> - Para producción vas a agregar las direcciones del dominio real (por ejemplo `https://eop.azurewebsites.net/signin-oidc`). Microsoft recomienda usar **registros separados** para desarrollo y producción, así las direcciones `localhost` no quedan habilitadas en producción.

## 3.6 Paso 5 — Crear el client secret

El **client secret** es la contraseña de la aplicación. Se usa en un único momento: cuando la app canjea el código de un solo uso por el ID token, hablando directamente con Microsoft (servidor a servidor).

1. En el registro de la aplicación: **Certificates & secrets** › pestaña **Client secrets** › **New client secret**.
2. En **Description**, escribí algo que identifique para qué y para quién es, por ejemplo `desarrollo-local-sebastian`.
3. En **Expires**, elegí la duración. Microsoft recomienda **menos de 12 meses**; el máximo permitido es **24 meses**.
4. Elegí **Add**.
5. **Copiá inmediatamente la columna Value** y guardala en un lugar seguro (en el capítulo 4 la cargamos en User Secrets).

![Certificates & secrets con el secreto recién creado](imagenes/portal/05-client-secret.png "Figura 3.5. Client secrets: copiá la columna Value (difuminá el valor si sacás una captura).")

> [!ADVERTENCIA] Value no es Secret ID
> - La tabla muestra dos columnas: **Value** y **Secret ID**. El que necesitás es **Value**. El *Secret ID* es solo un identificador del secreto y **no sirve** para autenticar.
> - El *Value* **se muestra una sola vez**. Si salís de la página sin copiarlo, tenés que crear un secreto nuevo.
> - **Nunca** lo pegues en `appsettings.json`, en el código, en un commit, en un chat ni en una captura sin difuminar.

> [!CONSEJO] Anotá la fecha de vencimiento
> Cuando el secreto vence, el login deja de funcionar con el error **AADSTS7000222** (capítulo 10). Anotá la fecha y creá uno nuevo **antes** de que venza.

> [!GENERAL] Secretos para producción
> Microsoft recomienda **no usar client secrets en producción** y preferir un **certificado** (idealmente guardado en **Azure Key Vault**) o *managed identities* / credenciales federadas. Para desarrollo local, un secreto en User Secrets es aceptable. EOP todavía no tiene despliegue en producción, así que esto queda como decisión futura.

## 3.7 Paso 6 — Permisos de API (dejar como están)

En **API permissions** vas a ver que el registro ya trae el permiso **Microsoft Graph › User.Read** (tipo *Delegated*). Para iniciar sesión **no hace falta agregar nada**: la librería pide los permisos estándar `openid` y `profile`, que son los que permiten obtener el ID token con el nombre del usuario.

> [!NOTA]
> Algunos tenants no permiten que los usuarios acepten permisos por su cuenta. En ese caso, la primera vez que alguien inicia sesión puede aparecer un mensaje de que se necesita aprobación de un administrador (error relacionado: **AADSTS65001**). Un administrador lo resuelve desde **API permissions** › **Grant admin consent for `<organización>`**.

## 3.8 Paso 7 — Asegurarte de que el tenant exige MFA

Recordá: **el MFA no se configura en la aplicación ni en el registro**, sino en las políticas del tenant. Hay dos mecanismos:

| Mecanismo | Licencia | Cómo funciona |
|---|---|---|
| **Security defaults** | Gratis | Todos los usuarios deben registrar MFA; Microsoft decide cuándo pedirlo (siempre a los administradores) |
| **Conditional Access** | Entra ID P1 o superior | Políticas a medida, por ejemplo "exigir MFA siempre para la app EOP" |

Los dos no se usan a la vez: para usar Conditional Access hay que desactivar los Security defaults.

**Cómo ver si los Security defaults están activos** (requiere al menos el rol *Conditional Access Administrator* para cambiarlos):

1. **Entra ID** › **Overview** › **Properties**.
2. **Manage security defaults**.
3. Fijate si **Security defaults** está en **Enabled**.

![Manage security defaults](imagenes/portal/06-security-defaults.png "Figura 3.6. Propiedades del tenant: Security defaults.")

> [!ADVERTENCIA]
> Con Security defaults, **Microsoft decide cuándo pedir el segundo factor** según el riesgo (ubicación, dispositivo, rol). Puede que un usuario común no vea el MFA en todos los inicios de sesión. Si el requerimiento es "MFA siempre", hace falta **Conditional Access**.

> [!RECONSTRUIDO]
> No consta en el repositorio **qué mecanismo** usa el tenant de EOP. Sí consta que en las pruebas manuales de la T09 el inicio de sesión **pidió MFA**. No modificamos ninguna configuración del tenant.

> [!PENDIENTE]
> Que la aplicación **verifique por su cuenta** que hubo MFA (y no solo confíe en la política del tenant) corresponde a la **T11**.

## 3.9 Opcional — Restringir qué usuarios pueden entrar

Por defecto, **cualquier usuario del tenant** puede iniciar sesión en la aplicación. Si querés limitarlo a un grupo de personas, desde el lado de Microsoft:

1. **Entra ID** › **Enterprise apps** › tu aplicación › **Properties**: poné **Assignment required?** en **Yes**.
2. En **Users and groups** asigná a las personas o grupos habilitados.

Si alguien no asignado intenta entrar, Microsoft lo rechaza (error **AADSTS50105**).

> [!EOP]
> Esto **no** forma parte de lo documentado para EOP. En EOP la decisión de quién puede usar el sistema la va a tomar la propia aplicación, según los usuarios registrados y su estado Activo/Inactivo.

> [!PENDIENTE]
> Rechazar identidades que no están registradas en EOP o que están Inactivas corresponde a la **T11**.

## 3.10 Resumen: qué te llevás del portal

| Dato | Ejemplo (ficticio) | Destino | ¿Versionado en Git? |
|---|---|---|---|
| Instance | `https://login.microsoftonline.com/` | `appsettings.json` › `AzureAd:Instance` | Sí |
| Directory (tenant) ID | `00000000-0000-0000-0000-00000000aaaa` | `appsettings.json` › `AzureAd:TenantId` | Sí |
| Application (client) ID | `00000000-0000-0000-0000-00000000c11e` | `appsettings.json` › `AzureAd:ClientId` | Sí |
| Ruta de regreso del login | `/signin-oidc` | `appsettings.json` › `AzureAd:CallbackPath` | Sí |
| Client secret (**Value**) | `<CLIENT-SECRET>` | **User Secrets** › `AzureAd:ClientCredentials:0:ClientSecret` | **No, nunca** |
| Redirect URIs | `https://localhost:<PUERTO>/signin-oidc` y `/signout-callback-oidc` | Ya quedaron cargadas en el portal | — |

> [!PRUEBA]
> Antes de pasar al capítulo 4, revisá en el portal que:
> 1. **Overview** muestra el Client ID y el Tenant ID que copiaste.
> 2. **Authentication** muestra la plataforma **Web** con las dos Redirect URIs, escritas exactamente igual (mayúsculas, puerto, `https`).
> 3. **Certificates & secrets** muestra un secreto con fecha de vencimiento futura, y tenés guardado su **Value**.
