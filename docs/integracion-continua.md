# Integración continua en EOP — guía de GitHub Actions (S2-T36)

**Fecha:** 16/11/2026. **Tarea:** [S2-T36](https://trello.com/c/E4LOqFwO).  
**Objetivo:** explicar qué hace la integración continua, cómo la configuramos en EOP y cómo comprobar que funciona cuando trabajamos con ramas y Pull Requests.

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
| **Workflow** | La lista de pasos automáticos que GitHub tiene que ejecutar. |
| **Job** | Un trabajo dentro del workflow; nuestro trabajo se llama `compilar-y-probar`. |
| **Runner** | La computadora temporal que GitHub prepara para ejecutar los pasos. |
| **Secret** | Un valor privado, como una contraseña, que GitHub guarda sin ponerlo dentro del código. |
| **Check** | El resultado que informa si una comprobación automática terminó bien o mal. |

## 2. ¿Qué configuramos en nuestro proyecto?

La implementación está guardada en el archivo **[`.github/workflows/ci.yml`](../.github/workflows/ci.yml)**. El formato **YAML** es simplemente una manera de escribir instrucciones de configuración; no es una pantalla de la aplicación.

La tarea [S2-T04 — Configurar integración continua](https://trello.com/c/M3ctKqUB) figura como terminada en Trello. El archivo existente define lo siguiente:

| Configuración | Qué significa |
|---|---|
| Nombre: `Integración continua EOP` | Así aparece en la pestaña Actions de GitHub. |
| Pull Request hacia `main` | Cuando abrimos o actualizamos un PR que propone cambios a la rama principal, GitHub ejecuta las comprobaciones. |
| Push a `main` | Después de que se actualiza la rama principal, GitHub vuelve a comprobar ese código. |
| `workflow_dispatch` | También podemos iniciar el workflow manualmente desde Actions. |
| `ubuntu-24.04` | GitHub prepara una computadora temporal con Ubuntu (Linux). |
| `.NET 10` | Es la versión de .NET que utiliza nuestro proyecto para compilar y hacer pruebas. |
| `contents: read` | El workflow tiene permiso para leer el repositorio, no para modificarlo directamente. |

**Esto es integración continua, no despliegue automático.** El workflow actual **no publica EOP en Azure App Service**, no actualiza la base de Azure SQL ni guarda documentos en Azure Blob Storage. Su función es **comprobar nuestro código**.

## 3. ¿Qué hace GitHub Actions paso a paso?

Cuando se inicia el workflow, GitHub prepara una computadora temporal y ejecuta estos pasos:

1. **Descarga nuestro código.** Se utiliza `actions/checkout@v7`, que permite trabajar con los archivos del repositorio.
2. **Instala .NET 10.** Se utiliza `actions/setup-dotnet@v6`, para que la computadora tenga lo necesario para compilar la aplicación.
3. **Restaura las dependencias.** Son los paquetes y bibliotecas que nuestro proyecto necesita, por ejemplo para trabajar con ASP.NET Core y EF Core.
4. **Compila la solución.** Es parecido a comprobar que el código se pueda construir en Visual Studio. Si hay un error de compilación, el trabajo falla.
5. **Prepara una base SQL Server para las pruebas.** Se usa un contenedor con SQL Server 2022. Un **contenedor** es un entorno aislado que permite ejecutar ese servicio durante las pruebas. GitHub espera a que la base responda antes de continuar.
6. **Ejecuta las pruebas automatizadas.** Usa `dotnet test`, que inicia las pruebas programadas en el proyecto. Si alguna termina mal, el resultado del workflow será de fallo.
7. **Muestra el resultado en GitHub.** Podemos abrir la ejecución y ver qué paso funcionó o falló.

En `ci.yml`, las instrucciones principales son:

```bash
dotnet restore EOP.Gestion.slnx
dotnet build EOP.Gestion.slnx --configuration Release --no-restore
dotnet test EOP.Gestion.slnx --configuration Release --no-build --no-restore --logger "console;verbosity=normal"
```

**Qué significan:** `restore` descarga las dependencias, `build` compila y `test` ejecuta las pruebas. Los parámetros adicionales evitan repetir trabajos que ya se completaron en los pasos anteriores.

## 4. ¿Por qué GitHub prepara otra base SQL?

Muchas pruebas necesitan guardar o consultar información. **No queremos que una prueba automática modifique los clientes o presupuestos de nuestra base real de Azure.**

Por eso, el workflow utiliza un **SQL Server temporal para las pruebas** que se inicia en la computadora de GitHub y se descarta al terminar el trabajo. Las pruebas reciben su dirección mediante la variable `EOP_PRUEBAS_SQLSERVER`.

Esto tiene dos ventajas: no se utilizan datos reales de EOP y las pruebas no dependen de que nuestro Azure SQL esté disponible.

En ese SQL Server de pruebas se usa una contraseña. GitHub la guarda como un **secret** llamado `EOP_CI_SQLSERVER_PASSWORD`. El archivo `ci.yml` hace referencia a ese nombre, pero **no muestra la contraseña**.

**Dónde comprobar que existe el secret:** repositorio → **Settings** → **Secrets and variables** → **Actions**. Allí se puede ver el nombre, no su contenido. Si fuera necesario configurar uno nuevo, debe tener una contraseña fuerte y cumplir las reglas de SQL Server. **No debe utilizarse la contraseña de Azure SQL real ni publicarse en Git.**

En la configuración del contenedor de pruebas aparece `TrustServerCertificate=True`: se utiliza allí para la conexión cifrada al SQL Server temporal. **No debemos copiar esta excepción a la configuración de producción de Azure SQL**, donde se debe validar el certificado.

### ¿Cómo se prepara esta configuración en el repositorio?

En nuestro caso ya está creada, pero si alguien del equipo necesita entender **cómo se dejó funcionando**, estos son sus componentes:

1. El archivo `.github/workflows/ci.yml` contiene el nombre, los eventos que lo activan y los pasos que debe ejecutar GitHub. Por eso no hace falta iniciar manualmente cada compilación.
2. En **Settings → Secrets and variables → Actions** se configura el secreto `EOP_CI_SQLSERVER_PASSWORD`, sin escribir su contraseña dentro del archivo. Desde esa pantalla se puede comprobar que existe.
3. El workflow usa ese secreto para iniciar la base SQL Server de pruebas, comprueba que esté disponible y después ejecuta `dotnet test`.
4. Al abrir un PR hacia `main`, revisamos que aparezca una ejecución en **Actions** y comprobamos el resultado.

La existencia del YAML y las ejecuciones exitosas están verificadas. **La política exacta de permisos y protección de la rama en la configuración de GitHub sigue pendiente de comprobar**; no hay que afirmar que el repositorio impide automáticamente todos los merges fallidos.

## 5. ¿Cómo sabemos que está funcionando?

Además de revisar el archivo, se comprobó una **ejecución real** en GitHub:

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

## 6. ¿Cómo lo usamos al terminar una tarea?

Nuestro procedimiento es:

1. Trabajamos en la **rama propia de la tarea**, no directamente en `main`.
2. Comprobamos localmente que el código compile y probamos la funcionalidad desarrollada.
3. Abrimos un **Pull Request** con destino `main`, indicando qué cambiamos y qué probamos.
4. GitHub Actions ejecuta automáticamente el workflow. Entramos al PR y miramos la sección **Checks** para ver el resultado.
5. Si una comprobación falla, abrimos la ejecución para saber en qué paso ocurrió y corregimos el problema en nuestra rama.
6. El otro integrante revisa el PR. **Solo después de las pruebas, revisión y autorización correspondiente** se pueden integrar los cambios.

Este proceso nos ayuda a evitar que un error de una tarea pase desapercibido al unir el código con el trabajo del compañero.

**Aclaración:** verificamos que GitHub Actions se ejecuta, pero todavía no comprobamos si el repositorio tiene reglas configuradas para **bloquear automáticamente** un merge cuando las pruebas fallan. No debemos dar por existente esa protección sin revisarla.

## 7. ¿Dónde vemos los resultados?

1. Abrimos el repositorio de GitHub.
2. Entramos a la pestaña **Actions**.
3. Seleccionamos **Integración continua EOP**.
4. Elegimos la ejecución que corresponde a nuestra rama o PR.
5. Abrimos **compilar-y-probar** para revisar cada paso.

Un resultado **verde** (`success`) indica que terminaron correctamente los pasos del workflow. Un resultado **rojo** (`failure`) indica que al menos un paso falló. Los detalles aparecen dentro del trabajo, donde podemos consultar qué salió mal.

## 8. ¿Qué hacemos si falla?

| Qué vemos | Qué revisamos |
|---|---|
| No aparece ninguna ejecución | Que el PR apunte a `main` o que el evento esté incluido en el workflow. |
| Falla la preparación de SQL Server | El contenedor de pruebas y la existencia/configuración del secret `EOP_CI_SQLSERVER_PASSWORD`. |
| Falla `restore` | Problemas al descargar las dependencias del proyecto. |
| Falla `build` | Errores del código que impiden compilar. |
| Falla `test` | La prueba concreta que no pasó y el mensaje de error asociado. |
| CI pasa, pero la app no conecta con Azure SQL | Es un problema diferente: revisar la [guía de Azure SQL](./azure-sql-database.md). |

No debemos solucionar fallos usando bases productivas para las pruebas, quitando controles de seguridad o mostrando contraseñas en los logs.

## 9. Archivos relacionados y mantenimiento

- [`.github/workflows/ci.yml`](../.github/workflows/ci.yml): las instrucciones reales que ejecuta GitHub.
- [`AGENTS.md`](../AGENTS.md): reglas del equipo sobre ramas, commits y Pull Requests.
- [S2-T04](https://trello.com/c/M3ctKqUB): tarea original de implementación de GitHub Actions.
- [Revisión de integraciones S2-T31](./revision-integracion-tecnica.md): revisión de la solución técnica.
- [Azure SQL Database](./azure-sql-database.md): documentación de la base de datos real, distinta del SQL Server temporal de pruebas.

Si cambiamos el workflow, sus pasos o las condiciones de ejecución, también tenemos que actualizar esta guía y verificar una ejecución nueva.
