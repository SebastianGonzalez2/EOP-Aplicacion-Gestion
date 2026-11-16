# Integración continua — GitHub Actions (S2-T36)

**Fecha de documentación:** 16/11/2026.  
**Tarea:** [S2-T36 — Documentar Azure e integración continua](https://trello.com/c/E4LOqFwO).  
**Implementación previa:** [S2-T04 — Configurar integración continua](https://trello.com/c/M3ctKqUB) (tarjeta terminada).  
**Fuente de verdad:** [`.github/workflows/ci.yml`](../.github/workflows/ci.yml). Si el workflow cambia, revisar este documento.

## 1. Qué hace y qué no hace

El workflow **Integración continua EOP** valida automáticamente el código de la aplicación `.NET 10` cuando se propone o integra un cambio. Ejecuta la compilación y las pruebas automatizadas usando un **SQL Server temporal** que corre dentro de GitHub Actions, para no depender de una base de desarrollo o de producción.

**No hace despliegue continuo:** no publica la WebApp en Azure App Service, no actualiza una Azure SQL Database real, no se conecta a Blob Storage y no ejecuta automáticamente las migraciones de producción. No confundir CI (compilar y probar) con CD (desplegar).

La revisión de CI sirve como control técnico previo al merge, pero no sustituye la revisión del otro integrante del equipo ni las pruebas manuales que correspondan a cada historia de usuario.

## 2. Activación, permisos y entorno

| Propiedad | Valor declarado en `ci.yml` |
|---|---|
| Nombre | `Integración continua EOP` |
| Push | Cuando se actualiza `main` |
| Pull request | Cuando el PR tiene como destino `main` |
| Ejecución manual | Disponible mediante `workflow_dispatch` en la pestaña Actions |
| Trabajo (job) | `compilar-y-probar` |
| Ejecutador | `ubuntu-24.04` |
| Permiso del workflow | `contents: read` |
| SDK | `.NET 10` (`10.0.x`) |
| Base de pruebas | Contenedor `mcr.microsoft.com/mssql/server:2022-latest`, puerto 1433, edición Developer |

Un pull request a `main` se valida en su versión propuesta. El push a `main` vuelve a ejecutar el flujo sobre el código integrado.

## 3. Configuración del secreto

En la configuración de **GitHub Actions Secrets and variables** del repositorio se espera el secreto de repositorio:

- `EOP_CI_SQLSERVER_PASSWORD`: contraseña que necesita el usuario `sa` del SQL Server **temporal de la CI**. El workflow la proporciona al servicio `sqlserver`, al chequeo de disponibilidad y a las pruebas.

La contraseña no aparece en el YAML; GitHub la inyecta durante el trabajo. Debe cumplir los requisitos de complejidad de SQL Server. **No pegar su valor en commits, conversaciones, capturas, documentación ni logs.** La CI no necesita ni debe reutilizar la contraseña de Azure SQL Database real.

No se puede consultar el valor del secreto desde esta documentación. Que una ejecución haya funcionado demuestra que la configuración necesaria estuvo disponible **para esa ejecución**, pero no certifica que el secreto mantenga el mismo estado en el futuro.

Para revisar su **existencia sin mostrar el valor**: repositorio en GitHub → **Settings** → **Secrets and variables** → **Actions**. Si no existe, crearlo con el nombre exacto y un valor fuerte. Evitar intentar diagnosticar errores imprimiendo el secreto.

## 4. Etapas del workflow

1. **Descargar código:** `actions/checkout@v7`.
2. **Instalar .NET 10:** `actions/setup-dotnet@v6`, versión `10.0.x`.
3. **Restaurar:** `dotnet restore EOP.Gestion.slnx`.
4. **Compilar:** `dotnet build EOP.Gestion.slnx --configuration Release --no-restore`.
5. **Esperar SQL Server:** el job consulta `SELECT 1` usando `sqlcmd` dentro del contenedor; espera hasta 40 intentos con intervalos de tres segundos y falla si la base no responde.
6. **Probar:** `dotnet test EOP.Gestion.slnx --configuration Release --no-build --no-restore --logger "console;verbosity=normal"` con `EOP_PRUEBAS_SQLSERVER` apuntando al SQL Server local del job.

La cadena temporal de pruebas declara cifrado `Encrypt=True` y `TrustServerCertificate=True`. Este último valor es **solo para el contenedor de pruebas dentro de GitHub Actions**; no tomarlo como ejemplo de configuración de Azure SQL de producción.

Las pruebas que dependen de SQL Server real pueden ejecutarse en CI sin abrir Azure SQL a Internet. Otras pruebas usan bases aisladas/simuladas según su diseño. El resultado final depende de que todas las pruebas incluidas concluyan correctamente.

## 5. Evidencia de ejecución verificada

Se consultó el registro real de GitHub Actions:

| Dato | Resultado |
|---|---|
| Workflow | Integración continua EOP |
| Fecha (UTC) | 15/11/2026 |
| Ejecución | [Run #14 — PR #74](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/43768608926) |
| Rama | `test/S2-T33-qa-materiales-precios` |
| Commit | `fb1aeab516c5b88760f070a9bf9f2b08c45647ef` |
| Evento | `pull_request` a `main` |
| Resultado global | **success** |
| Trabajo `compilar-y-probar` | **success** |

El registro de jobs muestra como **success** la instalación de .NET, la restauración, la compilación, la espera de SQL Server y la ejecución de pruebas. Esto es una evidencia puntual y verificable de CI; **no demuestra ningún despliegue de producción ni estabilidad de Azure SQL real**.

## 6. Cómo verificar una tarea y su PR

1. Crear o reutilizar la **rama de la tarea**, actualizada desde `main`, según [AGENTS.md](../AGENTS.md).
2. Antes de abrir el PR, ejecutar localmente las comprobaciones pertinentes. La compilación general se puede reproducir con:
   ```bash
   dotnet restore EOP.Gestion.slnx
   dotnet build EOP.Gestion.slnx --configuration Release --no-restore
   ```
   Para pruebas SQL Server locales, disponer de una instancia de pruebas y configurar **fuera del código** `EOP_PRUEBAS_SQLSERVER`, según el entorno utilizado. No apuntar la suite a una base productiva.
3. Abrir un PR con destino `main`, explicando el cambio, la tarea, las pruebas realizadas y los riesgos o pendientes.
4. En la página del PR abrir **Checks** o el enlace de **Actions** y comprobar que `compilar-y-probar` finaliza correctamente. Si falla, abrir el paso concreto y corregir la causa en la rama.
5. Solicitar la revisión del otro integrante. **Integrar únicamente tras revisión, autorización y controles pertinentes**. El simple hecho de que exista un archivo YAML no equivale a un control completado.
6. Para un historial: GitHub → **Actions** → **Integración continua EOP** → seleccionar la ejecución y consultar su evento, commit, logs y resultado. Usar este enlace en la evidencia del sprint.

**No se verificó aquí la configuración efectiva de protección de ramas o checks obligatorios.** El workflow existe y corre, pero no se debe afirmar que GitHub bloquea por sí mismo todo merge fallido sin revisar las reglas del repositorio.

## 7. Problemas habituales y diagnóstico

| Síntoma | Qué revisar primero |
|---|---|
| El job no inicia | Eventos `push`, `pull_request` y rama destino; estado de GitHub Actions y permisos |
| SQL Server no inicia o no queda listo | Que exista el secreto `EOP_CI_SQLSERVER_PASSWORD`, complejidad de contraseña, servicio del contenedor y paso de espera |
| Falla `restore` | SDK, paquetes y conectividad del ejecutador |
| Falla `build` | Error de compilación, referencias a proyectos y cambios recientes |
| Falla `test` | Nombre y traza de la prueba, conexión a SQL Server temporal, migraciones, fallos de lógica o pruebas no deterministas |
| PR con checks correctos, pero Azure falla | Son sistemas distintos: consultar [Azure SQL](./azure-sql-database.md) y [Blob Storage](./azure-blob-storage.md) |

No corregir un problema de CI conectando las pruebas a recursos productivos ni publicando contraseñas en los logs. El job se puede volver a ejecutar desde la interfaz de Actions cuando esté permitido; cada ejecución debe quedar asociada a un commit identificable.

## 8. Trazabilidad y mantenimiento

- [S2-T04](https://trello.com/c/M3ctKqUB): implementación de la integración continua.
- [S2-T31](./revision-integracion-tecnica.md): revisión del diseño de la integración.
- [S2-T36](https://trello.com/c/E4LOqFwO): estos procedimientos y evidencias.
- [Plan y convenciones de SCM en AGENTS.md](../AGENTS.md): ramas, commits, revisión de PR y etiquetas de entrega.
- [Azure SQL Database](./azure-sql-database.md): configuración y operación de la base real, separada de la instancia de pruebas de CI.

Si cambia el workflow, su secreto, los eventos, el SDK, la base de pruebas o el mecanismo de validación de PR, actualizar esta guía en la misma revisión de código y adjuntar una ejecución verificable.
