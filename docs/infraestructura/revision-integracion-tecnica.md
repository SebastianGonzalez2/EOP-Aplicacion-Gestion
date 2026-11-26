# S2-T31 — Revisión del diseño de integración técnica

**Fecha de revisión:** 14/11/2026  
**Tipo de trabajo:** análisis y diseño (sin cambios de infraestructura ni de código de producción).  
**Tarea:** [S2-T31 — Revisar diseño de integración técnica](https://trello.com/c/Tjg7n6pi).

## Objetivo y alcance

Revisar si las integraciones de **Azure SQL Database**, **Azure Blob Storage** y la **integración continua con GitHub Actions** son coherentes con la arquitectura definida en `AGENTS.md` y con los requerimientos no funcionales. Se revisaron los archivos del repositorio y las evidencias ya documentadas. **No se realizaron conexiones nuevas al portal de Azure ni se ejecutaron pruebas de infraestructura durante esta revisión.**

Esta revisión no reemplaza [S2-T36 — Documentar Azure e integración continua](https://trello.com/c/E4LOqFwO), donde corresponde completar los procedimientos y las configuraciones efectivas. La documentación detallada de Blob Storage ya existe y se reutiliza; no se reproduce aquí.

## Integraciones revisadas

| Integración | Diseño encontrado y evidencia | Evaluación |
|---|---|---|
| **Azure SQL Database** | La aplicación utiliza `EopDbContext` y EF Core (`UseSqlServer`) en `src/EOP.Gestion.Infrastructure/InyeccionDeDependencias.cs`. La cadena `ConnectionStrings:EopDb` se obtiene de configuración externa (User Secrets en desarrollo y configuración del futuro App Service), sin incluir credenciales en el código versionado. El cambio de esquema se administra mediante migraciones en `Infrastructure/Persistencia/Migraciones`. La tarjeta [S2-T01](https://trello.com/c/GvRlOZix) registra la preparación de Azure SQL y la verificación de conexión como terminadas. | **Diseño consistente.** La configuración y conectividad real del entorno futuro de App Service no están verificadas en esta revisión. |
| **Azure Blob Storage** | Se usa `IServicioAlmacenamiento` en Core, implementado por `ServicioAlmacenamientoBlob` en Infrastructure. El almacenamiento es privado; SQL conserva referencias, no el contenido de los archivos. La configuración y sus controles están en [`azure-blob-storage.md`](azure-blob-storage.md), que registra una prueba real satisfactoria de subida y descarga desde desarrollo el 11/11/2026. | **Diseño consistente.** La conexión desde App Service y determinadas pruebas negativas y de recuperación siguen pendientes, como ya señala ese documento. |
| **Integración continua** | El workflow [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) se activa con push a `main`, pull requests dirigidos a `main` o ejecución manual. Restaura dependencias, compila `.NET 10` y ejecuta pruebas con un SQL Server temporal dentro del entorno de GitHub Actions. La contraseña se obtiene del secreto `EOP_CI_SQLSERVER_PASSWORD`; no se requiere conexión a Azure SQL ni Blob Storage para ese flujo. La tarjeta [S2-T04](https://trello.com/c/M3ctKqUB) figura como terminada. | **Workflow definido y versionado.** No se comprobó aquí el resultado de una ejecución reciente ni la existencia/configuración efectiva del secreto en GitHub. |

## Compatibilidad con las reglas del proyecto

- **Separación de responsabilidades:** la configuración SQL y la implementación del almacenamiento están en Infrastructure; Core utiliza contratos y no depende directamente de servicios Azure (arquitectura de `AGENTS.md`).
- **RNF-002 / RNF-011:** la cadena SQL y las credenciales de los servicios no deben versionarse. Se observó el uso de configuración externa en el código; la revisión de secretos y permisos reales del entorno debe hacerse sin registrarlos en Git.
- **RNF-005 / RNF-006:** los documentos deben ser privados y almacenarse fuera de SQL; la implementación y el diseño de Blob siguen ese criterio.
- **RNF-007 / RNF-008:** las políticas de protección de datos están especificadas. Las pruebas de recuperación pendientes deben conservarse como tales, sin afirmar que ya se realizaron.
- **RNF-012 / RNF-013:** los errores de las integraciones deben registrarse sin exponer datos sensibles y no deben inutilizar las funcionalidades independientes; el servicio compartido de Blob documenta un tratamiento de errores acorde a esta separación.

## Pendientes identificados y destino

1. **S2-T36 (documentación):** reunir la configuración y los procedimientos efectivos de Azure SQL (recursos, autenticación, conectividad segura, migraciones y configuración externa sin secretos) y explicar la operación/verificación del workflow. Enlazar, no duplicar, `docs/infraestructura/azure-blob-storage.md`.
2. **Antes del despliegue de producción:** configurar y probar la cadena de conexión SQL desde App Service, sus permisos y su conectividad de red. La conexión desde el equipo de desarrollo no demuestra la del servidor desplegado.
3. **Antes de consumir Blob desde App Service:** definir y probar la identidad administrada, RBAC mínimo, red permitida y compatibilidad entre el tenant de la autenticación de usuarios y el tenant del almacenamiento, según los pendientes del documento de Blob.
4. **Verificación de CI:** comprobar en la pestaña Actions una ejecución exitosa del workflow para un commit o PR concreto; si falla, registrar la causa y resolverla en su tarea correspondiente. No inferir éxito de la mera presencia del YAML.
5. **Antes de producción, conforme a RNF-008:** ejecutar y registrar pruebas controladas de recuperación de Azure SQL y de archivos de Blob, sin sobrescribir información productiva.

## Resultado de la revisión

**No se identificó, en la revisión estática de estos tres diseños, una necesidad justificada de rehacer las integraciones existentes.** El enfoque actual es coherente con la arquitectura modular y con la separación de datos estructurados y archivos. Los puntos abiertos son principalmente **verificaciones de configuración real, conectividad y recuperación** para el entorno desplegado; no deben confundirse con funcionalidades ya probadas en producción.

El alcance de S2-T31 se limita a este diagnóstico. Los procedimientos operativos y configuraciones detalladas corresponden a S2-T36, y cualquier eventual corrección técnica deberá tratarse en la tarea pertinente, con su rama y pruebas.
