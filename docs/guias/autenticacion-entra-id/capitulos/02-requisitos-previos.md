# 2. Requisitos previos

Antes de empezar, verificá que tenés todo lo de esta lista. Cada requisito incluye **cómo comprobarlo** y **qué hacer si te falta**.

## 2.1 Resumen

| # | Requisito | Para qué | Cómo comprobarlo |
|---|---|---|---|
| 1 | Acceso a un **tenant de Microsoft Entra ID** | Registrar la aplicación y tener usuarios para iniciar sesión | Entrar a `https://entra.microsoft.com` |
| 2 | Permiso para **registrar aplicaciones** | Crear el registro de aplicación | Ver la sección 2.3 |
| 3 | Una **cuenta de usuario** del tenant con **MFA configurado** | Probar el login real | `https://mysignins.microsoft.com/security-info` |
| 4 | **.NET SDK** (en EOP: .NET 10) | Compilar y ejecutar | `dotnet --list-sdks` |
| 5 | **Visual Studio** con la carga de trabajo web | Programar y depurar | Visual Studio Installer |
| 6 | **Certificado HTTPS de desarrollo** confiable | Ejecutar en `https://localhost` | `dotnet dev-certs https --check --trust` |
| 7 | **Git** y acceso al repositorio | Trabajar en ramas y PRs | `git --version` |
| 8 | Un **navegador** (Edge o Chrome) | Probar el flujo | — |

> [!EOP]
> Esta guía se verificó en este entorno: **.NET SDK 10.0.401**, **Visual Studio Community 2026 (18.10.3)**, Windows 11, proyecto `net10.0` con **Microsoft.Identity.Web 4.16.1**. Con otras versiones los menús o los nombres pueden variar levemente.

## 2.2 Tenant de Microsoft Entra ID

Un **tenant** es el directorio de una organización dentro de Entra ID: ahí viven sus usuarios, grupos y aplicaciones registradas. Lo identifica un GUID llamado **Directory (tenant) ID**.

**Cómo comprobar que tenés acceso**

1. Abrí `https://entra.microsoft.com` e iniciá sesión con tu cuenta de trabajo o estudio.
2. En el menú de la izquierda elegí **Entra ID** › **Overview** (Información general).
3. Tendrías que ver el nombre de la organización y el campo **Tenant ID**.
4. Si tenés acceso a varios tenants, el ícono de **Settings** (engranaje) de la barra superior permite cambiar de directorio. Asegurate de estar en el correcto **antes** de registrar nada.

**Si no tenés un tenant propio**

- Al crear una **cuenta gratuita de Azure** se crea automáticamente un tenant (normalmente llamado *Default Directory*), y vos quedás como administrador.
- En un proyecto académico o de empresa, lo habitual es usar el **tenant de la organización**: pedile a quien lo administra que te dé el rol necesario (sección 2.3) o que registre la aplicación por vos y te pase los valores.

> [!RECONSTRUIDO]
> En el repositorio de EOP no quedó registrado **qué cuenta ni qué rol** se usaron para registrar la aplicación durante la T08. Solo consta el resultado: un `TenantId` y un `ClientId` en `appsettings.json` y un secreto guardado en User Secrets.

## 2.3 Permiso para registrar aplicaciones

Según la documentación de Microsoft, para registrar una aplicación necesitás como mínimo el rol **Application Developer**. Los administradores (por ejemplo **Global Administrator**) también pueden hacerlo.

**Cómo comprobarlo**: en **Entra ID** › **App registrations**, si el botón **New registration** está habilitado y al usarlo te deja completar el formulario, tenés permiso. Si está deshabilitado o da error, necesitás que un administrador te asigne el rol.

> [!NOTA]
> Hay tenants configurados para que cualquier usuario pueda registrar aplicaciones y otros que lo restringen. Por eso puede que funcione sin tener el rol asignado explícitamente.

## 2.4 Una cuenta de prueba con MFA

Para probar el login real necesitás una cuenta **del mismo tenant** (o invitada en él) que tenga configurado un segundo factor.

1. Entrá a `https://mysignins.microsoft.com/security-info` con esa cuenta.
2. Tendrías que ver al menos un método, por ejemplo **Microsoft Authenticator**.
3. Si no hay ninguno, usá **Add sign-in method** y seguí las instrucciones (instalar Microsoft Authenticator en el celular y escanear el código QR).

Si el tenant tiene activados los *Security defaults*, Microsoft te va a pedir que registres el método la primera vez que inicies sesión.

## 2.5 .NET SDK

```bash
dotnet --list-sdks
```

Tendrías que ver una línea que empiece con la versión que usa tu proyecto. En EOP:

```text
10.0.401 [C:\Program Files\dotnet\sdk]
```

**Si falta**: descargalo de `https://dotnet.microsoft.com/download` (el **SDK**, no solo el *Runtime*) o instalalo desde Visual Studio Installer.

## 2.6 Visual Studio

Necesitás la carga de trabajo **ASP.NET and web development** (Desarrollo de ASP.NET y web).

1. Abrí **Visual Studio Installer**.
2. En tu instalación de Visual Studio elegí **Modify**.
3. Verificá que esté marcada **ASP.NET and web development**.

**Alternativa**: todo lo de esta guía también se puede hacer con **Visual Studio Code** o cualquier editor más la terminal (`dotnet new`, `dotnet add package`, `dotnet run`, `dotnet test`). Cuando un paso tiene versión por terminal, la guía la muestra.

## 2.7 HTTPS en desarrollo

Entra ID exige `https` en las direcciones de retorno (salvo algunas excepciones para `localhost`), y EOP configura la cookie de sesión para que **solo viaje por HTTPS**. Por eso necesitás que tu máquina confíe en el certificado de desarrollo de ASP.NET Core.

```bash
dotnet dev-certs https --check --trust
```

Si dice que no hay un certificado válido y confiable, crealo y confialo:

```bash
dotnet dev-certs https --trust
```

Windows va a mostrar un aviso de seguridad preguntando si confiás en el certificado: aceptalo. Es un certificado **solo para tu máquina**.

> [!PRUEBA]
> Ejecutá tu aplicación con el perfil `https` y abrí la dirección `https://localhost:<PUERTO>`. El navegador **no** tiene que mostrar "La conexión no es privada".

## 2.8 El puerto HTTPS de tu proyecto

La dirección de retorno que vas a registrar en Entra ID incluye el puerto. Lo encontrás en `Properties/launchSettings.json`, en el perfil `https`:

```json
"https": {
  "commandName": "Project",
  "applicationUrl": "https://localhost:7125;http://localhost:5090",
  "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
}
```

> [!EOP]
> En EOP el puerto HTTPS es **7125**, así que las direcciones de retorno son `https://localhost:7125/signin-oidc` y `https://localhost:7125/signout-callback-oidc`.

> [!NOTA] ¿Y si cambio el puerto?
> Para direcciones `localhost`, la documentación de Microsoft indica que Entra ID **ignora el puerto** al comparar la Redirect URI (`https://localhost:7125/signin-oidc` y `https://localhost:5001/signin-oidc` se consideran iguales). Igual conviene registrar el puerto real, para que la configuración sea fácil de leer, y **no registrar dos veces la misma dirección cambiando solo el puerto**. Para un dominio real (producción) el puerto y el resto de la dirección **sí** tienen que coincidir exactamente; si no, el login falla con **AADSTS50011** (capítulo 10).
