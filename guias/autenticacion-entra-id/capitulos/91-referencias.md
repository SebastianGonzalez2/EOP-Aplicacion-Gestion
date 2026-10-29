# Anexo B. Referencias

## Documentación oficial consultada (octubre de 2026)

| Tema | Fuente |
|---|---|
| Registrar una aplicación | Microsoft Learn — *How to Register an App in Microsoft Entra ID* · `learn.microsoft.com/entra/identity-platform/quickstart-register-app` |
| Redirect URIs | Microsoft Learn — *How to add a redirect URI to your application* · `learn.microsoft.com/entra/identity-platform/how-to-add-redirect-uri` |
| Restricciones de Redirect URIs | Microsoft Learn — *Redirect URI (reply URL) best practices and limitations* · `learn.microsoft.com/entra/identity-platform/reply-url` |
| Client secrets y certificados | Microsoft Learn — *Add and manage app credentials in Microsoft Entra ID* · `learn.microsoft.com/entra/identity-platform/how-to-add-credentials` |
| Protocolo OpenID Connect y cierre de sesión | Microsoft Learn — *OpenID Connect (OIDC) on the Microsoft identity platform* · `learn.microsoft.com/entra/identity-platform/v2-protocols-oidc` |
| Códigos de error AADSTS | Microsoft Learn — *Microsoft Entra authentication & authorization error codes* · `learn.microsoft.com/entra/identity-platform/reference-error-codes` |
| Security defaults (MFA) | Microsoft Learn — *Configure Security Defaults for Microsoft Entra ID* · `learn.microsoft.com/entra/fundamentals/security-defaults` |
| FallbackPolicy | Microsoft Learn — *Create an ASP.NET Core web app with user data protected by authorization* (sección *Require authenticated users*) · `learn.microsoft.com/aspnet/core/security/authorization/secure-data` |
| Archivos estáticos y autorización | Microsoft Learn — *Serve static files in ASP.NET Core apps* (sección *Static file authorization*) · `learn.microsoft.com/aspnet/core/fundamentals/static-files` |

## Fuentes del repositorio EOP

| Fuente | Qué aporta |
|---|---|
| PR #10 — *S1-T08: Configurar autenticación con Microsoft Entra ID* (commits `a805cba`, `602289a`) | Paquete, `appsettings.json`, `Program.cs` inicial, uso de User Secrets |
| PR #13 — *S1-T09: implementar inicio y cierre de sesión* (commit `f7a0ec3`, merge `fdc9e78`) | Controller, vistas, política de respaldo, cookie, manejo de fallos, pruebas |
| `docs/user-stories.md` — US-042 | Escenarios de aceptación |
| `docs/requerimientos-funcionales.md` — RF-069, RF-071 | Reglas de inicio y cierre de sesión y MFA |
| `docs/requerimientos-no-funcionales.md` — RNF-2, 3, 4, 9, 11, 12, 14 | Restricciones de seguridad e interfaz |
| `AGENTS.md` | Arquitectura y reglas de seguridad del proyecto |
