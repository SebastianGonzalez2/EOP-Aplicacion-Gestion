# Anexo A. Glosario

| Término | Definición breve |
|---|---|
| **AADSTS** | Prefijo de los códigos de error de Entra ID (por ejemplo `AADSTS50011`) |
| **Access token** | Token para llamar a una API en nombre del usuario. No se usa en la T08 ni en la T09 |
| **[AllowAnonymous]** | Atributo que permite entrar a una acción sin sesión |
| **App registration** | Alta de una aplicación en un tenant de Entra ID |
| **Application (client) ID** | Identificador público de la aplicación registrada |
| **[Authorize]** | Atributo que exige estar autenticado (u otra regla) para entrar a una acción |
| **Autenticación** | Comprobar quién es el usuario |
| **Autorización** | Decidir qué puede hacer el usuario |
| **Challenge** | Orden de ASP.NET Core para pedirle al usuario que se identifique; con OpenID Connect, redirige a Microsoft |
| **Claim** | Dato del usuario dentro de un token o de la sesión (nombre, `oid`, etc.) |
| **Client secret** | Contraseña de la **aplicación** para canjear el código ante Entra ID. Nunca va en el repositorio |
| **Conditional Access** | Políticas de acceso a medida del tenant (requieren Entra ID P1) |
| **Cookie de autenticación** | Cookie cifrada que guarda la sesión del usuario en la app (`.AspNetCore.Cookies`) |
| **Correlation (cookie)** | Cookie temporal que permite verificar que el login que vuelve lo inició esta app |
| **CSRF** | Ataque en el que otro sitio hace que tu navegador mande un pedido a nuestra app sin que lo decidas |
| **Directory (tenant) ID** | Identificador del tenant |
| **Endpoint** | Una dirección de la app que responde pedidos (una acción, un archivo estático…) |
| **Entra ID** | Servicio de identidades de Microsoft (antes Azure Active Directory) |
| **Esquema de autenticación** | Componente con nombre que realiza una tarea de autenticación (`Cookies`, `OpenIdConnect`) |
| **FallbackPolicy** | Política de autorización que se aplica a los endpoints que no declaran ninguna |
| **form_post** | Modo de respuesta en el que Microsoft devuelve el resultado como un formulario POST |
| **Front-channel logout** | Mecanismo para que Microsoft avise a varias apps que el usuario cerró sesión |
| **HttpOnly** | Atributo de cookie que impide leerla desde JavaScript |
| **ID token** | Token (JWT) firmado por Microsoft que dice quién inició sesión |
| **JWT** | *JSON Web Token*: formato de token de tres partes (encabezado, contenido, firma) |
| **MFA** | Autenticación multifactor: más de una prueba de identidad |
| **Microsoft.Identity.Web** | Librería oficial que integra ASP.NET Core con Entra ID |
| **Middleware** | Eslabón de la cadena por la que pasa cada petición HTTP |
| **Nonce** | Valor de un solo uso que debe volver dentro del ID token |
| **OAuth 2.0** | Estándar para delegar acceso a recursos |
| **OpenID Connect (OIDC)** | Capa sobre OAuth 2.0 para identificar usuarios |
| **Open redirect** | Vulnerabilidad que usa una redirección del sitio real para mandar al usuario a un sitio falso |
| **PKCE** | Protección del flujo de código: solo quien inició el login puede canjear el código |
| **Redirect URI** | Dirección de la app a la que Entra ID puede devolver al usuario |
| **Secure** | Atributo de cookie que exige HTTPS |
| **Security defaults** | Configuración de seguridad básica y gratuita del tenant, que incluye MFA |
| **Sesión** | Período en que la app reconoce al usuario autenticado (mientras la cookie sea válida) |
| **signin-oidc** | Ruta de la app donde vuelve el usuario después de iniciar sesión |
| **signout-callback-oidc** | Ruta de la app donde vuelve el usuario después de cerrar sesión en Microsoft |
| **Single Sign-On (SSO)** | Iniciar sesión una vez y entrar a varias aplicaciones |
| **State** | Valor de un solo uso que debe volver junto con la respuesta de Microsoft |
| **Tenant** | Directorio de una organización en Entra ID |
| **Token antifalsificación** | Valor secreto en un formulario que prueba que el pedido salió de nuestra página |
| **User Secrets** | Almacén local de configuración sensible, fuera del repositorio |
| **WebApplicationFactory** | Herramienta de pruebas que levanta la app completa en memoria |
