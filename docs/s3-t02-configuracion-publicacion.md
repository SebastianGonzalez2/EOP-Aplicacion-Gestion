# S3-T02 — Preparar configuración de publicación

**Fecha de verificación:** 26/11/2026.  
**Tarea:** [S3-T02 en Trello](https://trello.com/c/yYPlhmix).  
**Rama:** `feat/S3-T02-configuracion-publicacion`.

## Objetivo

Comprobar que la aplicación EOP puede compilarse, publicarse y arrancar con su configuración de producción, antes de contratar o crear el servicio de alojamiento en Azure. No se requiere un despliegue efectivo en esta tarea.

## Verificaciones realizadas

| Comprobación | Resultado |
|---|---|
| Compilación de la solución en Release | Correcta: cuatro proyectos compilados, 0 errores y 12 advertencias. |
| Publicación de la WebApp | Correcta: se generaron los archivos en `artifacts/publish`, incluidos los ensamblados, dependencias, configuración y recursos web. |
| Arranque de la publicación local | Correcto: entorno `Production`, desde la carpeta publicada, sin iniciar la aplicación mediante Visual Studio. |
| Solicitud HTTPS sin sesión | HTTP 302, sin seguir la redirección. Compatible con el flujo de autenticación esperado; no equivale a un inicio de sesión completo. |
| Pruebas de configuración y autenticación seleccionadas | Todas aprobadas, según el resultado informado por el integrante que las ejecutó localmente. No se registró el número exacto. |
| Estado de Git después de las pruebas | Rama de la tarea confirmada; `git status --short` sin cambios. |

Se utilizó el SDK 11.0.100 y el runtime de .NET 10.0.15 instalado localmente. Los proyectos mantienen el destino `net10.0`. Para reproducir el entorno de integración continua se recomienda utilizar el SDK de .NET 10. No se modificó el framework objetivo.

Las advertencias de compilación fueron 10 CS8629 en casos de uso de materiales y 2 xUnit2031 en pruebas. No impidieron compilar ni publicar. Se recomienda revisar las advertencias de nulabilidad por separado; no se determinó si representan defectos efectivos.

### Comandos ejecutados

```powershell
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes
dotnet build EOP.Gestion.slnx -c Release
dotnet publish src/EOP.Gestion.WebApp/EOP.Gestion.WebApp.csproj -c Release -o artifacts/publish
Get-ChildItem artifacts/publish
```

Para la prueba de arranque, se configuraron temporalmente las variables `ASPNETCORE_ENVIRONMENT=Production`, `DOTNET_ENVIRONMENT=Production`, `ASPNETCORE_URLS=https://localhost:7125` y `ConnectionStrings__EopDb` con una cadena ficticia que apunta a un puerto local sin servicio SQL previsto. No se usaron credenciales reales para esta prueba.

```powershell
cd artifacts/publish
dotnet EOP.Gestion.WebApp.dll
```

Desde otra terminal:

```powershell
curl.exe -sS -o NUL -w "%{http_code}" https://localhost:7125/
```

Resultado: `302`. El servidor informó `Application started` y `Hosting environment: Production`.

Desde la raíz del repositorio, con la sesión temporal de producción cerrada:

```powershell
dotnet test EOP.Gestion.slnx -c Release --no-build --filter "FullyQualifiedName~InfraestructuraTests|FullyQualifiedName~ConfiguracionAlmacenamientoTests|FullyQualifiedName~FlujoOpenIdConnectRealTests"
git branch --show-current
git status --short
```

Las pruebas seleccionadas verifican la configuración de persistencia y almacenamiento sin conectar a Azure, y el flujo OpenID Connect con un proveedor de identidad simulado y SQLite en memoria. No se ejecutó la suite completa en esta tarea porque no hubo cambios funcionales.

## Configuración prevista para el despliegue

El alojamiento se realizará en la suscripción institucional, junto con Azure SQL Database y Azure Blob Storage. El registro de Microsoft Entra ID permanecerá en el tenant personal. Al momento de la revisión no existía un App Service.

| Parámetro | Configuración que deberá comprobarse al desplegar |
|---|---|
| Alojamiento | Azure App Service con runtime compatible con .NET 10, entorno Production y HTTPS obligatorio. |
| SQL | Proporcionar externamente `ConnectionStrings__EopDb` (o su equivalente en App Service), verificar cifrado y conectividad desde el servidor. |
| Entra ID | Mantener el registro del tenant de identidad; configurar las credenciales externas y las URI de retorno necesarias para la URL real, incluida `/signin-oidc`. Comprobar el inicio y cierre de sesión y MFA. |
| Blob Storage | Mantener el almacenamiento privado. Habilitar identidad administrada del App Service, asignar los permisos de datos necesarios y verificar el acceso de red y la pertenencia al tenant adecuado. |
| Secretos | Mantenerlos fuera del código y de los archivos versionados; revisar los artefactos definitivos antes de desplegar. |

La aplicación ya cuenta con configuración de desarrollo, lectura de secretos externos, HTTPS, manejo de errores para producción y configuración de identidad administrada para almacenamiento. No se identificaron cambios de código necesarios para la preparación local.

Estas comprobaciones se relacionan con RNF-003 (autenticación y MFA), RNF-005 (privacidad de documentos), RNF-009 (HTTPS) y RNF-011 (protección de secretos). No constituyen una certificación de cumplimiento en el entorno real.

## Resultado y límites

La preparación local de la publicación se verificó satisfactoriamente. No se creó infraestructura, no se incurrió en consumo adicional por App Service, no se ejecutaron migraciones sobre la base real ni se publicó la aplicación en Internet. No se modificó código de producción; este archivo registra la evidencia de la tarea.

Queda pendiente, en el despliegue efectivo, comprobar el arranque en Azure, la autenticación completa, la conectividad y permisos reales de SQL y Blob, y la ausencia de secretos en el paquete definitivo. La prueba local con una cadena ficticia no valida dichas integraciones.

## Referencias internas

- [AGENTS.md](../AGENTS.md)
- [Requerimientos no funcionales](./requerimientos-no-funcionales.md)
- [Azure SQL Database](./azure-sql-database.md)
- [Azure Blob Storage](./azure-blob-storage.md)
- [Integración continua](./integracion-continua.md)
