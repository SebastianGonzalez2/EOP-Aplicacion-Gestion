# Azure SQL Database — configuración y operación de EOP (S2-T36)

**Fecha de documentación:** 16/11/2026.  
**Tarea:** [S2-T36 — Documentar Azure e integración continua](https://trello.com/c/E4LOqFwO).  
**Alcance:** registrar la configuración conocida, la conexión desde la aplicación y los procedimientos de verificación, migración y diagnóstico. **No se modificó ningún recurso de Azure ni se ejecutaron conexiones nuevas al portal para redactar este documento.**

> No guardar contraseñas, cadenas de conexión completas, tokens, direcciones IP personales, identificadores de usuario ni datos empresariales reales en este repositorio. La configuración efectiva de los recursos debe volver a comprobarse en Azure Portal antes de una operación de infraestructura o despliegue.

## 1. Estado y fuentes de evidencia

- La tarjeta [S2-T01 — Configurar Azure SQL Database](https://trello.com/c/GvRlOZix) figura como terminada y describe la preparación de la base y la verificación de la conexión segura.
- En el código, `EopDbContext` reside en `src/EOP.Gestion.Infrastructure/Persistencia/EopDbContext.cs`; `InyeccionDeDependencias.AgregarInfraestructura` obtiene `ConnectionStrings:EopDb` y registra `UseSqlServer`.
- Las migraciones de Entity Framework Core están en `src/EOP.Gestion.Infrastructure/Persistencia/Migraciones/`.
- La aplicación usa Microsoft Entra ID para **identificar al usuario**; posteriormente consulta Azure SQL para encontrar o vincular al usuario de EOP y comprobar que esté habilitado. La autenticación de Entra ID y la conectividad de SQL son procesos diferentes.
- El equipo informó fallas **intermitentes** de inicio de sesión al utilizar Azure SQL. No hay diagnóstico confirmado de la causa. No debe interpretarse el cierre de S2-T01 como garantía de conectividad estable.

**Estado de verificación de esta documentación:** configuración del código y tarjeta de trabajo comprobadas; nombre, región, nivel de servicio, red, modo de autenticación de SQL y retención efectiva **pendientes de contrastar con Azure Portal**.

## 2. Inventario de la infraestructura

| Elemento | Configuración conocida / pendiente |
|---|---|
| Proveedor de base de datos | Azure SQL Database, mediante EF Core y proveedor SQL Server |
| Suscripción de Azure SQL | **Por confirmar en Azure Portal** (no inferirla de la suscripción utilizada para Blob) |
| Grupo de recursos de Azure SQL | **Por confirmar** |
| Servidor lógico y nombre de la base | **Por confirmar**; no escribir usuario o contraseña en este registro |
| Región y modalidad de cómputo | **Por confirmar**; verificar si hay pausa automática y el estado de la base |
| Método de autenticación de la conexión SQL | **Por confirmar** con la configuración efectiva; no confundirlo con el inicio de sesión Entra ID de usuarios |
| Reglas de firewall / redes autorizadas | **Por confirmar**, sin publicar IP particulares |
| Cifrado del transporte y validación de certificado | Verificar en la configuración efectiva de la conexión; no deshabilitar la validación de certificado para resolver fallas |
| Retención de backups automática | RNF-007 exige **7 días inicialmente en producción**, según plan contratado; **no consta aquí su configuración real** |
| Azure App Service | Tecnología prevista para producción; conectividad con Azure SQL desde App Service **no verificada** |

El almacenamiento documental es otro recurso: `eopstorage245`, contenedor privado `documentos-eop`, documentado y verificado parcialmente en [Azure Blob Storage](./azure-blob-storage.md). **No debe suponerse** que los permisos o la red de Blob habilitan SQL ni viceversa.

## 3. Cómo se conecta la aplicación

1. `Program.cs` registra Infrastructure con `builder.Services.AgregarInfraestructura(builder.Configuration)`.
2. `InyeccionDeDependencias` obtiene la clave `ConnectionStrings:EopDb` usando `GetConnectionString("EopDb")`. Si falta, la aplicación falla al inicializar con un error explícito.
3. `AddDbContext<EopDbContext>(...UseSqlServer(cadenaConexion))` configura EF Core. Las consultas reales abren la conexión cuando la necesitan; la mera inicialización no confirma conectividad.
4. Los repositorios de Infrastructure utilizan `EopDbContext`. El código de Core no maneja credenciales de Azure ni establece conexiones directamente.

**Desarrollo:** la cadena se guarda fuera de Git, mediante User Secrets de `src/EOP.Gestion.WebApp`. Comando de ejemplo desde la raíz del repositorio (ingresar la cadena solo en el equipo de desarrollo y evitar que quede en capturas o logs):

```bash
dotnet user-secrets set "ConnectionStrings:EopDb" "<cadena-de-conexion-privada>" --project src/EOP.Gestion.WebApp
```

Antes de ejecutar el comando, comprobar que el proyecto WebApp tiene habilitado User Secrets. Una cadena de SQL Server para Azure debe usar cifrado y validar correctamente el certificado del servidor; comprobar sus parámetros efectivos sin copiarlos a este documento.

**App Service (cuando se despliegue):** configurar la cadena como **Connection string** llamada `EopDb` en la configuración de la aplicación, o como variable de configuración `ConnectionStrings__EopDb`. **No duplicar la cadena en ambos lugares.** Configurarla en el entorno que ejecutará la WebApp, nunca en un archivo versionado. Verificar red, credenciales y permisos de SQL desde ese entorno; que funcione en el equipo local no demuestra que funcione desde App Service.

El archivo `src/EOP.Gestion.WebApp/appsettings.json` contiene opciones no secretas del proveedor de identidad (`AzureAd`) y de Blob. **No contiene la cadena SQL**. El registro de Entra ID utilizado para los usuarios y los recursos de la suscripción institucional son administraciones distintas; no asumir que compartir credenciales entre ellas sea necesario o correcto.

## 4. Comprobación de Azure SQL sin divulgar secretos

Realizar y registrar la fecha, el resultado y el responsable de cada paso, sin capturas con credenciales:

1. Entrar a Azure Portal con la identidad que tenga permisos para **ver** el recurso SQL. Identificar suscripción, grupo, servidor lógico, base, región y modalidad de cómputo. Registrar solo metadatos no sensibles aprobados para documentación.
2. En la base, confirmar el estado de servicio, métricas y eventos; revisar si se encuentra pausada o reanudándose, si aplica a su modalidad.
3. En el servidor SQL, revisar **Networking** (redes autorizadas, firewall y acceso público/privado según la decisión adoptada). Permitir únicamente el acceso necesario. **No abrir el servidor a todas las IP** para solucionar una falla de prueba.
4. Revisar el método de autenticación realmente utilizado por la cadena `EopDb` y los permisos mínimos sobre la base; no registrar usuario, contraseña o tokens.
5. Desde un cliente autorizado, probar la apertura de conexión cifrada y una consulta de solo lectura (p. ej., `SELECT 1`). Distinguir la conexión del inicio de sesión web con Entra ID.
6. Verificar el historial de migraciones aplicadas (`__EFMigrationsHistory`) y el estado de los esquemas esperados. No ejecutar cambios de esquema durante una verificación de solo lectura.
7. Guardar **evidencia no sensible**: fecha, entorno probado, éxito/fallo y código o tipo de error general. Si falla, registrar el incidente para diagnóstico en vez de afirmar que el entorno está validado.

**Registro pendiente de completar tras verificación real:**

| Comprobación | Evidencia actual |
|---|---|
| Identificación y configuración efectiva de Azure SQL | Pendiente de comprobación en portal |
| Conexión desde equipo de desarrollo | S2-T01 se marcó terminada; nueva prueba estable no realizada en S2-T36 |
| Conexión y permisos desde App Service | Pendiente; no afirmar que hay despliegue |
| Diagnóstico de acceso intermitente | Pendiente; sin causa demostrada |
| Configuración real y restauración de backup SQL | Pendiente; ver RNF-007 y RNF-008 |

## 5. Migraciones y cambios de esquema

EF Core utiliza migraciones versionadas junto al código. Desde la raíz del repositorio, con `dotnet-ef` disponible y una configuración de arranque válida:

```bash
# Inspeccionar las migraciones disponibles (sin aplicar cambios).
dotnet ef migrations list --project src/EOP.Gestion.Infrastructure --startup-project src/EOP.Gestion.WebApp --context EopDbContext

# Solo para el entorno de desarrollo autorizado, previa revisión y respaldo adecuado.
dotnet ef database update --project src/EOP.Gestion.Infrastructure --startup-project src/EOP.Gestion.WebApp --context EopDbContext

# Generar un script idempotente para revisión antes de una futura aplicación controlada.
dotnet ef migrations script --idempotent --project src/EOP.Gestion.Infrastructure --startup-project src/EOP.Gestion.WebApp --context EopDbContext --output migraciones-eop.sql
```

**Precaución:** `database update` modifica la base apuntada por `EopDb`; no usarlo a ciegas contra producción. Revisar el script, el destino, los datos existentes y las pruebas SQL Server antes de autorizar una migración productiva. No versionar scripts temporales si contienen información sensible. La generación de un script **no** prueba que pueda aplicarse correctamente: el repositorio incluye pruebas específicas de migraciones sobre SQL Server real (`ScriptsDeMigracionSqlServerTests`).

## 6. Diagnóstico de fallas de inicio de sesión

El flujo tiene dos verificaciones separadas:

1. **Entra ID / OpenID Connect:** Microsoft autentica la identidad. Errores aquí incluyen problemas de configuración del registro, redirect URI, sesión o MFA.
2. **Consulta de usuario EOP en SQL:** en `Program.cs`, el evento `OnTicketReceived` llama a `IdentificarOVincularUsuario`. Si SQL no responde, la sesión se rechaza y el usuario ve la pantalla de error; esto **no demuestra** que Entra ID haya fallado.

Si el primer ingreso falla y luego funciona, revisar los registros del servidor por **tipo de error** e **identificador de solicitud**, junto con el estado/latencia de Azure SQL y las condiciones de red. Comparar momento exacto de fallas y reintentos. No publicar mensajes que contengan datos sensibles ni modificar el firewall a ciegas. Una hipótesis como pausa automática, interrupción transitoria o firewall **requiere comprobación**: no hay causa identificada en esta documentación.

## 7. Respaldo, recuperación y despliegue

- **RNF-007:** la política aprobada prevé backups automáticos administrados por Azure SQL Database con una retención inicial de 7 días en producción, condicionada al servicio contratado. No se programa backup SQL propio en EOP.
- **RNF-008:** antes de producción se debe comprobar mediante **restauración controlada** a una base de prueba, sin sobrescribir la productiva, y repetir la verificación al menos cada seis meses. No registrar como realizada una prueba que todavía no consta.
- Para desplegar en App Service faltan confirmar la configuración de la WebApp, sus variables de entorno, la conectividad de red con SQL, los permisos y la autenticación desde el entorno desplegado. **La integración continua actual no despliega la aplicación.**

## 8. Referencias y mantenimiento

- [AGENTS.md](../AGENTS.md): arquitectura, seguridad, secretos y estrategia de ramas.
- [RNF-007, RNF-008, RNF-009 y RNF-011](./requerimientos-no-funcionales.md): disponibilidad, recuperación y protección de credenciales.
- [Revisión de integración técnica S2-T31](./revision-integracion-tecnica.md): evaluación arquitectónica y pendientes.
- [Azure Blob Storage](./azure-blob-storage.md): recurso documental separado.
- [Integración continua](./integracion-continua.md): compilación y pruebas en GitHub Actions.

Cualquier cambio efectivo de recursos, permisos, red, cadena de conexión, retención o procedimiento debe motivar una actualización de este documento mediante rama y Pull Request, previa verificación y sin exponer secretos.
