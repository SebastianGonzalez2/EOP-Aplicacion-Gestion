# Integración continua en EOP — guía de GitHub Actions (S2-T36 y S3-T01)

**Fecha original:** 16/11/2026. **Tarea:** [S2-T36](https://trello.com/c/E4LOqFwO).  
**Actualización:** 26/11/2026, tarea S3-T01 (ampliar la integración continua).  
**Objetivo:** explicar qué hace la integración continua, cómo la configuramos en EOP y cómo comprobar que funciona cuando trabajamos con ramas y Pull Requests.

## Estado actual: GitHub Actions operativo (26/11/2026)

El **26 de noviembre de 2026** comprobamos con GitHub CLI que:

- El repositorio ahora es **público**.
- El workflow `Integración continua EOP` figura **activo** y tiene ejecuciones terminadas con éxito; por ejemplo, la [ejecución del push a `main` posterior al PR #104](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/45835585013).

Según la [documentación de facturación de GitHub Actions](https://docs.github.com/en/billing/concepts/product-billing/github-actions), los **runners estándar** que ofrece GitHub no tienen costo en repositorios públicos. Por eso usamos solamente `ubuntu-24.04` y no contratamos ningún servicio. Los runners más grandes sí se cobran, así que **no hay que cambiar el tipo de runner** sin revisar antes ese punto. Si el repositorio volviera a ser privado, vuelven a aplicar los límites de uso gratuito.

### Antecedente: suspensión temporal desde el 16/11/2026

Esto ya **no está vigente**, pero lo dejamos registrado porque explica cómo trabajamos esos días.

- El 16/11/2026 el equipo informó que se había agotado el uso gratuito incluido de GitHub Actions del propietario del repositorio (en ese momento había un límite que alcanzar) y decidió **desactivar temporalmente el workflow**, para no contratar consumo adicional.
- El archivo `ci.yml` se mantuvo en el repositorio y no se eliminó ninguna prueba. Mientras duró la pausa, la compilación y las pruebas se ejecutaban en nuestras computadoras y se dejaba constancia en cada Pull Request.
- Se había previsto esperar a la renovación mensual de la cuota, a comienzos de diciembre ([ciclos de facturación](https://docs.github.com/en/billing/concepts/billing-cycles)), y [habilitar el workflow a mano](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/disable-and-enable-workflows). **Esa espera ya no corresponde:** el workflow está habilitado.
- El estado de desactivación fue comunicado por el equipo; no se verificó en su momento desde la configuración administrativa de GitHub.

## 1. ¿Qué significa «integración continua»?

Cuando desarrollamos una aplicación entre dos personas, cada uno puede modificar archivos distintos o incluso el mismo archivo. Al juntar los cambios, existe el riesgo de que algo deje de funcionar.

La **integración continua** (también llamada **CI**, por *Continuous Integration*) nos ayuda a detectar algunos de esos problemas **antes de integrar los cambios a la rama principal**.

En EOP usamos **GitHub Actions**, una herramienta de GitHub que ejecuta pasos automáticamente sobre nuestro código. Por ejemplo, comprueba si la aplicación compila y si pasan las pruebas.

**Ejemplo de EOP:** terminamos la tarea de registrar un material y abrimos un Pull Request (PR) para agregarla a `main`. GitHub Actions descarga el código de ese PR, intenta compilarlo y ejecuta las pruebas. Si alguna falla, podemos revisarla antes de integrar los cambios.

> **Importante:** que las pruebas den bien es una señal positiva, pero no garantiza que toda la aplicación funcione perfectamente. Por eso también revisamos el código y hacemos las pruebas manuales necesarias.

### Algunas palabras que vamos a encontrar

| Término | Qué significa |
|---|---|
| **Repositorio** | El lugar de GitHub donde guardamos el código y su historial. |
| **Rama (branch)** | Una versión de trabajo del código; cada tarea tiene su propia rama. |
| **`main`** | La rama principal del proyecto, donde integramos los cambios aprobados. |
| **Pull Request (PR)** | La solicitud para revisar los cambios de una rama antes de unirlos a `main`. |
| **Fork** | Una copia del repositorio en la cuenta de otra persona, desde la que puede proponernos un PR. |
| **Workflow** | La lista de pasos automáticos que GitHub tiene que ejecutar. |
| **Job** | Un trabajo dentro del workflow; nuestro trabajo se llama `compilar-y-probar`. |
| **Runner** | La computadora temporal que GitHub prepara para ejecutar los pasos. |
| **Secret** | Un valor privado, como una contraseña, que GitHub guarda sin ponerlo dentro del código. |
| **Check** | El resultado que informa si una comprobación automática terminó bien o mal. |
| **Artefacto** | Archivos que una ejecución deja guardados para descargarlos después. |
| **Informe TRX** | Archivo con el resultado de cada prueba de .NET; Visual Studio lo puede abrir. |
| **Cobertura** | Qué parte del código se ejecutó mientras corrían las pruebas. |

## 2. ¿Qué configuramos en nuestro proyecto?

La implementación está guardada en el archivo **[`.github/workflows/ci.yml`](../.github/workflows/ci.yml)**. El formato **YAML** es simplemente una manera de escribir instrucciones de configuración; no es una pantalla de la aplicación.

La tarea [S2-T04 — Configurar integración continua](https://trello.com/c/M3ctKqUB) creó el workflow y S3-T01 lo amplió. El archivo define lo siguiente:

| Configuración | Qué significa |
|---|---|
| Nombre: `Integración continua EOP` | Así aparece en la pestaña Actions de GitHub. |
| Pull Request hacia `main` | Cuando abrimos o actualizamos un PR que propone cambios a la rama principal, GitHub ejecuta las comprobaciones. |
| Push a `main` | Después de que se actualiza la rama principal, GitHub vuelve a comprobar ese código. |
| `workflow_dispatch` | También podemos iniciar el workflow manualmente desde Actions. |
| `ubuntu-24.04` | GitHub prepara una computadora temporal con Ubuntu (Linux). Es un runner estándar. |
| `.NET 10` | Es la versión de .NET que utiliza nuestro proyecto para compilar y hacer pruebas. |
| `contents: read` | El workflow tiene permiso para leer el repositorio, no para modificarlo directamente. |
| `concurrency` | Si un PR recibe un commit nuevo, se cancela la ejecución anterior de ese PR (sección 5). |
| `timeout-minutes` | Tiempos máximos para que una ejecución colgada no quede corriendo (sección 5). |

**Esto es integración continua, no despliegue automático.** El workflow **no publica EOP en Azure App Service**, no actualiza la base de Azure SQL ni guarda documentos en Azure Blob Storage. Su función es **comprobar nuestro código**.

## 3. ¿Qué hace GitHub Actions paso a paso?

Cuando el workflow se inicia, GitHub prepara una computadora temporal y ejecuta estos pasos:

| Paso | Nombre en GitHub | Qué hace |
|---|---|---|
| 1 | Descargar el código | `actions/checkout@v7` baja los archivos, sin dejar guardado el token de GitHub. |
| 2 | Iniciar SQL Server de pruebas | Genera una contraseña al azar y arranca un contenedor con SQL Server 2022. Un **contenedor** es un entorno aislado que permite ejecutar ese servicio durante las pruebas. |
| 3 | Instalar .NET 10 | `actions/setup-dotnet@v6` prepara lo necesario para compilar la aplicación. |
| 4 | Recuperar la caché de paquetes NuGet | `actions/cache@v6` trae los paquetes guardados en una ejecución anterior. |
| 5 | Restaurar dependencias | Descarga los paquetes y bibliotecas que falten, por ejemplo para ASP.NET Core y EF Core. |
| 6 | Compilar la solución | Es parecido a comprobar que el código se pueda construir en Visual Studio. Si hay un error de compilación, el trabajo falla. |
| 7 | Esperar a que SQL Server esté disponible | Consulta la base hasta que responda. SQL Server arranca mientras se restaura y compila. |
| 8 | Ejecutar pruebas automatizadas | Usa `dotnet test` y genera los informes TRX y de cobertura. Si alguna prueba termina mal, el resultado será de fallo. |
| 9 | Verificar pruebas omitidas y resumir resultados | Un script propio revisa los informes y escribe el resumen de la ejecución. |
| 10 | Quitar la contraseña temporal de los informes | Reemplaza la contraseña por `***` si apareciera dentro de algún informe. |
| 11 | Guardar informes de pruebas y cobertura | `actions/upload-artifact@v7` los deja descargables durante 7 días. |

Los pasos 9, 10 y 11 **se ejecutan aunque las pruebas fallen**, porque es justamente cuando más necesitamos los informes. Si el código no compila, no hay pruebas y esos tres pasos se saltean.

En `ci.yml`, las instrucciones principales son:

```bash
dotnet restore EOP.Gestion.slnx
dotnet build EOP.Gestion.slnx --configuration Release --no-restore
dotnet test EOP.Gestion.slnx --configuration Release --no-build --no-restore \
  --logger "console;verbosity=normal" \
  --logger "trx;LogFilePrefix=pruebas" \
  --collect "XPlat Code Coverage" \
  --settings tests/cobertura.runsettings \
  --results-directory TestResults
```

**Qué significan:** `restore` descarga las dependencias, `build` compila y `test` ejecuta las pruebas. Los parámetros `--no-restore` y `--no-build` evitan repetir trabajos que ya se completaron. Los dos `--logger` muestran los resultados en el log y los guardan en un archivo TRX; `--collect` mide la cobertura.

## 4. ¿Por qué GitHub prepara otra base SQL?

Muchas pruebas necesitan guardar o consultar información. **No queremos que una prueba automática modifique los clientes o presupuestos de nuestra base real de Azure.**

Por eso, el workflow utiliza un **SQL Server temporal para las pruebas** que se inicia en la computadora de GitHub y se descarta al terminar el trabajo. Las pruebas reciben su dirección mediante la variable `EOP_PRUEBAS_SQLSERVER`, y cada una crea su propia base con un nombre al azar y la borra al terminar.

Esto tiene dos ventajas: no se utilizan datos reales de EOP y las pruebas no dependen de que nuestro Azure SQL esté disponible.

### La contraseña de la base de pruebas

Desde S3-T01 la contraseña **se genera al azar en cada ejecución**. No está escrita en el archivo ni guardada en GitHub:

- Solo sirve para un contenedor que existe mientras dura la ejecución.
- El puerto se publica únicamente en la propia computadora del runner (`127.0.0.1`).
- GitHub la muestra como `***` en los logs (`::add-mask::`).
- Antes de subir los informes, se reemplaza si apareciera en alguno.

**El secret `EOP_CI_SQLSERVER_PASSWORD` ya no lo usa este workflow.** Sigue configurado en el repositorio; se puede borrar desde **Settings → Secrets and variables → Actions** **después de integrar a `main` el cambio de S3-T01**, porque hasta entonces la versión anterior del workflow todavía lo necesita. Nunca hay que usar en la CI la contraseña de Azure SQL real.

En la conexión de pruebas aparece `TrustServerCertificate=True`: se utiliza allí para la conexión cifrada al SQL Server temporal. **No debemos copiar esta excepción a la configuración de producción de Azure SQL**, donde se debe validar el certificado.

## 5. Controles agregados en S3-T01

### Informes de pruebas (TRX)

Cada ejecución genera un archivo `pruebas_….trx` con el resultado de cada prueba, su duración, el mensaje de error de las que fallaron y la salida que produjeron.

### Verificación de pruebas omitidas

**El problema:** las pruebas de integración que necesitan SQL Server usan `[FactConSqlServer]` o `[TheoryConSqlServer]`. Si no encuentran un servidor, se marcan como **omitidas** y `dotnet test` termina **en verde**. En una comprobación controlada del 26/11/2026 sin SQL Server quedaron omitidas 278 pruebas que lo necesitan y `dotnet test` igual informó éxito.

**El control:** el script [`.github/scripts/Verificar-ResultadosPruebas.ps1`](../.github/scripts/Verificar-ResultadosPruebas.ps1) lee los TRX y hace fallar la ejecución si:

- hay alguna prueba omitida que **no** esté en la lista de omisiones permitidas;
- no se ejecutó ninguna prueba de las clases `*SqlServerTests`;
- falta el informe TRX o el de cobertura.

Hoy la única omisión permitida es `DatosUruguayRealTests` (dos pruebas), porque llaman a la API real de datosUruguay y la CI no tiene que depender de un servicio externo. Esas omisiones igual aparecen listadas en el resumen.

Si en el futuro hace falta omitir otra prueba, hay que agregar su clase a `$clasesQuePuedenOmitirse` dentro del script. Así el cambio queda a la vista en el PR y lo revisa la otra persona.

### Cobertura de código

La mide `coverlet.collector`, que ya estaba incluido en el proyecto de pruebas, y genera un archivo `coverage.cobertura.xml`. El resumen muestra el porcentaje de **líneas** y de **ramas** (los distintos caminos de un `if`, por ejemplo) de cada proyecto.

- **No hay un porcentaje mínimo** y la ejecución no falla por la cobertura. Sirve para ver qué zonas quedaron sin probar.
- Se excluyen las **migraciones de EF Core** ([`tests/cobertura.runsettings`](../tests/cobertura.runsettings)): son código generado que las pruebas ejecutan entero y harían parecer mayor la cobertura.
- Una cobertura alta no demuestra que las pruebas verifiquen lo correcto, solo que ese código se ejecutó.

### Artefactos

Se suben **solo** los archivos TRX y de cobertura, con el nombre `informes-de-pruebas-intento-N`, y GitHub los borra a los **7 días** (por defecto serían 90). Si necesitamos conservar una evidencia por más tiempo, hay que descargarla.

### Caché de NuGet

Se guardan los paquetes descargados (`~/.nuget/packages`). La clave depende de los archivos `.csproj` de `src/` y `tests/`: si cambian las dependencias, se genera una caché nueva. `dotnet restore` se ejecuta siempre; la caché solo evita volver a descargar. Se guarda únicamente cuando el trabajo termina bien.

### Cancelación de ejecuciones obsoletas

Si subimos un commit nuevo a un PR mientras corre la ejecución anterior, esa se **cancela** porque ya no representa el código actual. Las ejecuciones de `main` y las manuales **no se cancelan** entre sí.

### Límites de tiempo

| Qué | Máximo |
|---|---:|
| Trabajo completo | 20 minutos |
| Ejecutar pruebas | 12 minutos |
| Iniciar SQL Server (incluye descargar la imagen) | 5 minutos |
| Esperar a SQL Server | 4 minutos (además de 40 intentos) |

Si una ejecución llega a un límite figura como fallida o cancelada. Antes de aumentar el número, conviene averiguar qué se colgó.

## 6. Seguridad en un repositorio público

Al ser público, **cualquier persona puede leer el código, el historial, los logs y los resúmenes de las ejecuciones**, descargar los artefactos si inició sesión en GitHub y proponernos un PR desde un fork.

| Riesgo | Cómo lo tratamos |
|---|---|
| Un PR de un fork intenta leer secretos | El workflow **no usa ningún secret**. Además usa `pull_request`, con el que GitHub no entrega secretos a los forks y les da un token de solo lectura. |
| Código del PR modifica el repositorio | Permiso `contents: read` y `persist-credentials: false` al descargar el código. |
| La contraseña de pruebas queda en un log o informe | Es temporal, se oculta en los logs y se reemplaza en los informes antes de subirlos. |
| Los informes publican datos sensibles | Las pruebas deben usar **solo datos inventados**: no copiar clientes, RUT, correos ni claves reales. Lo que imprime una prueba queda dentro del TRX. |
| Un fork altera la caché de `main` | GitHub aísla la caché creada por un PR: `main` no la usa. |
| Gasto no previsto | Solo runners estándar, límites de tiempo y retención corta. |

Reglas para quien modifique el workflow:

- **No usar `pull_request_target`** ni disparadores equivalentes para compilar o probar código de un PR: correrían código externo con acceso a secretos.
- No agregar secretos reales (Azure, Entra ID, OpenAI) a este workflow.
- Antes de aprobar la ejecución de un PR externo, revisar qué cambia, en especial dentro de `.github/`, los `.csproj` y las pruebas.

Las acciones usadas son las oficiales de GitHub (`actions/…`), referenciadas por versión mayor. Fijarlas por el identificador exacto del commit sería más estricto, pero obliga a actualizarlas a mano; por ahora no lo hacemos.

### Configuración del repositorio comprobada el 26/11/2026

Consultada con la API de GitHub, **sin modificarla**:

- Los colaboradores externos que contribuyen **por primera vez** necesitan aprobación manual para que corra el workflow.
- El token de los workflows es de **solo lectura** por defecto.
- `main` exige **una revisión aprobatoria** en los PR y no admite *force push* ni borrado. Los administradores no están obligados a cumplirlo.
- **No hay checks obligatorios:** GitHub **no bloquea automáticamente** un merge aunque `compilar-y-probar` falle. Antes de integrar hay que mirar el resultado. Hacerlo obligatorio es una decisión pendiente del equipo.

## 7. ¿Cómo sabemos que está funcionando?

Además de revisar el archivo, se comprueban **ejecuciones reales** en GitHub.

### Evidencia anterior (S2)

| Dato | Evidencia |
|---|---|
| Fecha | 15/11/2026 |
| Ejecución | [GitHub Actions — ejecución #14](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/43768608926) |
| Pull Request | #74 |
| Rama | `test/S2-T33-qa-materiales-precios` |
| Resultado | **Correcto (success)** |

En esa ejecución terminaron correctamente las etapas de instalación, descarga de dependencias, compilación, preparación de SQL Server y pruebas automatizadas.

**Qué demuestra:** el workflow pudo completar esos controles para ese cambio concreto. **Qué no demuestra:** que Azure SQL de producción funciona, que EOP ya está desplegada o que no pueda existir ningún otro error.

También se verificó una ejecución del PR de documentación S2-T36: [GitHub Actions — PR #75](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/43882013526), con resultado correcto para el commit inicial del PR. **Si hacemos nuevos commits, hay que comprobar las ejecuciones nuevas**, no dar por válido automáticamente el resultado anterior.

### Evidencia de S3-T01 (26/11/2026)

Ejecuciones del [PR #105](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/pull/105) y de dos ramas temporales creadas solo para provocar fallos. Esos cambios temporales **no forman parte de S3-T01 y no se deben integrar**.

| Qué se comprobó | Ejecución | Resultado observado |
|---|---|---|
| Ejecución completa del PR (commit `46686f9`) | [#91](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/45839580322) | **success**. 1892 pruebas: 1890 aprobadas, 0 fallidas y 2 omitidas permitidas; 403 ejecutadas de clases `*SqlServerTests`. Se subió el artefacto y se guardó la caché de NuGet, que todavía no existía. |
| Cancelación de una ejecución obsoleta | [#88](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/45839476835) | **cancelled** mientras restauraba dependencias, al subir un commit nuevo al mismo PR. |
| Una prueba que falla a propósito | [#89](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/45839548405) | **failure** en «Ejecutar pruebas automatizadas» (1 fallida de 1893). Los pasos siguientes corrieron igual y el artefacto incluye el fallo. |
| SQL Server inalcanzable para las pruebas | [#90](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/actions/runs/45839553461) | `dotnet test` terminó bien con 280 omitidas, y la ejecución quedó en **failure** en «Verificar pruebas omitidas»: 278 omisiones no permitidas y ninguna prueba `*SqlServerTests` ejecutada. |

Cobertura de la ejecución #91, sin migraciones:

| Proyecto | Líneas | Ramas |
|---|---:|---:|
| `EOP.Gestion.Core` | 95,0 % | 88,8 % |
| `EOP.Gestion.Infrastructure` | 98,4 % | 93,9 % |
| `EOP.Gestion.WebApp` | 91,5 % | 76,7 % |
| **Total** | **94,7 %** (5544 de 5854) | **82,5 %** (2384 de 2891) |

Es una foto de ese día, no un objetivo a mantener.

También se descargaron los artefactos de las ejecuciones #89, #90 y #91: contenían solamente un TRX y un informe de cobertura, sin migraciones y sin la contraseña temporal. En los logs de esas tres ejecuciones la contraseña aparece como `***`.

**Límite de esta evidencia:** no se probó un PR enviado desde un fork. Su funcionamiento sin secretos se deduce de que el workflow ya no usa ninguno.

## 8. ¿Cómo lo usamos al terminar una tarea?

1. Trabajamos en la **rama propia de la tarea**, sin modificar `main` directamente.
2. Compilamos y probamos en nuestra computadora lo que modificamos, desde la carpeta que contiene `EOP.Gestion.slnx`:

   ```powershell
   dotnet build EOP.Gestion.slnx --configuration Release
   ```

   ```powershell
   dotnet test EOP.Gestion.slnx --configuration Release
   ```

   **Hay que mirar la cantidad de omitidas:** sin SQL Server, las pruebas que lo necesitan se omiten y el resultado igual sale en verde. Se usa LocalDB o una **base de pruebas aislada** indicada en `EOP_PRUEBAS_SQLSERVER`; nunca una base de producción.

3. Probamos manualmente la funcionalidad modificada y sus criterios de aceptación.
4. Abrimos el **Pull Request hacia `main`**; GitHub Actions ejecuta automáticamente el workflow.
5. Revisamos el resultado del trabajo `compilar-y-probar` en **Checks / Actions**, incluido el resumen. Si falla, investigamos y corregimos la causa antes de integrar.
6. El otro integrante revisa el PR. **Solo integramos después de las verificaciones, la revisión y la autorización.**

### Generar los mismos informes en la computadora (opcional)

```powershell
dotnet test EOP.Gestion.slnx --configuration Release --logger "trx;LogFilePrefix=pruebas" --collect "XPlat Code Coverage" --settings tests/cobertura.runsettings --results-directory TestResults
```

```powershell
powershell -ExecutionPolicy Bypass -File .github/scripts/Verificar-ResultadosPruebas.ps1 -CarpetaResultados TestResults
```

La carpeta `TestResults` está en `.gitignore`. Conviene borrarla antes de volver a medir, porque el script lee todos los informes que encuentre. **Los informes generados en nuestra computadora pueden incluir rutas, el nombre de usuario y salida de la aplicación configurada localmente: no los publiquemos sin revisarlos.**

## 9. ¿Dónde vemos los resultados?

1. Abrimos el repositorio de GitHub.
2. Entramos a la pestaña **Actions**.
3. Seleccionamos **Integración continua EOP**.
4. Elegimos la ejecución que corresponde a nuestra rama o PR.
5. En la página **Summary** vemos:
   - la cantidad de pruebas aprobadas, fallidas y omitidas, y cuántas se ejecutaron de las clases `*SqlServerTests`;
   - los nombres de las pruebas fallidas y las omisiones con su motivo;
   - la cobertura por proyecto;
   - abajo, la sección **Artifacts**, para descargar los informes (disponibles 7 días).
6. Abrimos **compilar-y-probar** para revisar el detalle de cada paso.

El archivo `.trx` descargado se puede abrir con Visual Studio. `coverage.cobertura.xml` es un formato estándar que leen distintas herramientas de cobertura; para el día a día alcanza con la tabla del resumen.

Un resultado **verde** (`success`) indica que terminaron correctamente los pasos del workflow. Un resultado **rojo** (`failure`) indica que al menos un paso falló. **Gris** (`cancelled`) indica que se canceló, normalmente porque se subió un commit más nuevo.

## 10. ¿Qué hacemos si falla?

| Qué vemos | Qué revisamos |
|---|---|
| No aparece ninguna ejecución | Que el PR apunte a `main`, que no tenga conflictos y que el workflow siga habilitado. En un PR externo puede estar esperando aprobación. |
| Falla «Iniciar SQL Server de pruebas» | La descarga de la imagen del contenedor o el arranque de Docker. |
| Falla «Esperar a que SQL Server esté disponible» | Las últimas líneas del contenedor, que se muestran en ese mismo paso. |
| Falla `restore` | Problemas al descargar las dependencias del proyecto. |
| Falla `build` | Errores del código que impiden compilar. |
| Falla «Ejecutar pruebas automatizadas» | La prueba concreta que no pasó (resumen, log o TRX). |
| Falla «Verificar pruebas omitidas» | El mensaje del error: omisiones no permitidas, pruebas de SQL Server sin ejecutar o informes faltantes. |
| CI pasa, pero la app no conecta con Azure SQL | Es un problema diferente: revisar la [guía de Azure SQL](./azure-sql-database.md). |

No debemos solucionar fallos usando bases productivas para las pruebas, quitando controles de seguridad, agregando pruebas a la lista de omisiones sin motivo o mostrando contraseñas en los logs.

## 11. Archivos relacionados y mantenimiento

- [`.github/workflows/ci.yml`](../.github/workflows/ci.yml): las instrucciones reales que ejecuta GitHub.
- [`.github/scripts/Verificar-ResultadosPruebas.ps1`](../.github/scripts/Verificar-ResultadosPruebas.ps1): control de omisiones y resumen.
- [`tests/cobertura.runsettings`](../tests/cobertura.runsettings): configuración de la cobertura.
- [`AGENTS.md`](../AGENTS.md): reglas del equipo sobre ramas, commits y Pull Requests.
- [S2-T04](https://trello.com/c/M3ctKqUB): tarea original de implementación de GitHub Actions.
- [Revisión de integraciones S2-T31](./revision-integracion-tecnica.md): revisión de la solución técnica.
- [Azure SQL Database](./azure-sql-database.md): documentación de la base de datos real, distinta del SQL Server temporal de pruebas.

Si cambiamos el workflow, sus pasos o las condiciones de ejecución, también tenemos que actualizar esta guía y verificar una ejecución nueva.
