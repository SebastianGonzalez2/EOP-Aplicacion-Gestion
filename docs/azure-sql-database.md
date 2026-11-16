# Azure SQL Database en EOP — guía de configuración y uso (S2-T36)

**Fecha:** 16/11/2026. **Tarea:** [S2-T36](https://trello.com/c/E4LOqFwO).  
**Objetivo:** explicar para qué usamos Azure SQL Database, cómo se conecta nuestra aplicación y qué revisar si necesitamos configurar, comprobar o recuperar la base de datos.

**Importante:** esta guía mezcla dos tipos de información, que vamos a distinguir siempre:
- **Comprobado:** se puede verificar en el código, en los documentos anteriores o en los registros de trabajo.
- **Pendiente de comprobar:** todavía tenemos que mirar el recurso real en Azure Portal. No vamos a inventar valores de configuración.

Para redactar esta versión no se modificó Azure ni se hizo una conexión nueva al portal. **Nunca agregamos al repositorio contraseñas, cadenas de conexión completas, tokens ni direcciones IP personales.**

## 1. ¿Qué es Azure SQL Database y para qué lo usamos?

Una **base de datos** es el lugar donde guardamos información organizada. En EOP, por ejemplo, necesitamos guardar los datos de clientes, proveedores, usuarios, materiales y presupuestos.

**SQL Server** es el sistema que permite guardar y consultar esos datos. **Azure SQL Database** es un servicio de Microsoft que ofrece esa base de datos desde la nube, sin que tengamos que mantener instalado un servidor SQL en una de nuestras computadoras.

Esto nos sirve para que la información no dependa de la computadora de un único integrante y para que varias instalaciones autorizadas de nuestra aplicación puedan consultar la misma base de datos. **No significa que cualquier persona pueda entrar:** sigue siendo necesario tener una conexión y permisos válidos.

Hay tres elementos diferentes que conviene no confundir:

| Elemento | Qué significa en nuestro proyecto |
|---|---|
| **Azure SQL Database** | La base de datos de la aplicación, donde se guardan los registros. |
| **Servidor SQL de Azure** | El recurso de Azure que administra la conexión a esa base y sus reglas de acceso. No es una computadora física que tengamos que administrar nosotros. |
| **Azure Portal** | La página web de Microsoft donde podemos ver y configurar esos recursos. |

Los documentos PDF, imágenes y otros archivos **no** van dentro de SQL: para eso el proyecto prevé [Azure Blob Storage](./azure-blob-storage.md). La base SQL guarda los datos organizados y, cuando corresponda, la referencia al archivo.

## 2. ¿Qué se hizo hasta ahora?

La tarea [S2-T01 — Configurar Azure SQL Database](https://trello.com/c/GvRlOZix) figura como terminada en Trello. Su objetivo era preparar la base y verificar una conexión segura.

En el repositorio comprobamos que:

- La aplicación usa **Entity Framework Core (EF Core)** para comunicarse con SQL Server. EF Core es una herramienta que nos permite trabajar con datos desde C#, sin escribir manualmente todas las consultas SQL.
- La clase **`EopDbContext`** representa el acceso a las tablas de la aplicación. Está en `src/EOP.Gestion.Infrastructure/Persistencia/EopDbContext.cs`.
- El archivo `src/EOP.Gestion.Infrastructure/InyeccionDeDependencias.cs` configura EF Core mediante `UseSqlServer` y obtiene la cadena de conexión desde una configuración externa.
- Los cambios de estructura de la base se guardan como **migraciones** en `src/EOP.Gestion.Infrastructure/Persistencia/Migraciones/`.

**Lo que todavía no verificamos en esta tarea:** el nombre y las características actuales del recurso en Azure, su método de conexión real, las reglas de red y la política de recuperación. Tampoco está resuelta la causa de los fallos intermitentes de conexión observados durante el desarrollo.

## 3. ¿Cómo está organizada nuestra configuración de Azure SQL?

Una **suscripción** es la cuenta de facturación y administración que agrupa recursos de Azure. Dentro de una suscripción hay **grupos de recursos**, que funcionan como carpetas para organizar servicios. En uno de esos grupos están el servidor SQL y la base.

Vamos a completar esta tabla con los datos reales del portal:

| Dato que necesitamos | Estado actual |
|---|---|
| Suscripción donde está Azure SQL | **Por comprobar** |
| Grupo de recursos | **Por comprobar** |
| Nombre del servidor SQL | **Por comprobar** |
| Nombre de la base de datos | **Por comprobar** |
| Región (ubicación del servicio) | **Por comprobar** |
| Plan o capacidad contratada | **Por comprobar** |
| Tipo de autenticación usado por la conexión | **Por comprobar** |
| Redes autorizadas para conectarse | **Por comprobar**; sin publicar IP personales |
| Copias de seguridad y días de retención configurados | **Por comprobar** |

**Cómo encontrarlo:** entrar a [Azure Portal](https://portal.azure.com), buscar **SQL databases**, seleccionar la base de EOP y abrir **Overview / Información general**. Desde allí se puede identificar el servidor asociado y consultar otras opciones.

No asumimos que Azure SQL está en la misma suscripción que Blob Storage. En [la guía de Blob](./azure-blob-storage.md) ya se documentó esa infraestructura por separado.

## 4. ¿Cómo se conecta nuestra aplicación a la base?

Cuando iniciamos la aplicación, ocurren estas cosas:

1. **`Program.cs` prepara los servicios.** Llama a `AgregarInfraestructura` para dejar configurado el acceso a la base de datos.
2. **La aplicación busca una cadena de conexión.** Es un texto privado que indica a qué servidor y base conectarse y cómo autenticarse. La configuración se llama `ConnectionStrings:EopDb`.
3. **EF Core prepara el acceso mediante `EopDbContext`.** Cuando una funcionalidad consulta o guarda datos, EF Core utiliza esa configuración para intentar comunicarse con SQL.
4. **SQL Server responde** si la conexión, la red y los permisos son correctos. Que la aplicación haya iniciado no significa necesariamente que ya pudo consultar la base.

Por ejemplo, cuando registramos un cliente, nuestro código trabaja con el repositorio y EF Core para guardar el registro en SQL Server.

### ¿Dónde guardamos la cadena de conexión?

**En desarrollo:** usamos el mecanismo **User Secrets** de .NET, que permite guardar datos privados fuera del repositorio. El proyecto espera la configuración `ConnectionStrings:EopDb`. Un ejemplo de cómo establecerla (con una cadena propia, no para copiar un valor real al documento) es:

```bash
dotnet user-secrets set "ConnectionStrings:EopDb" "<cadena-privada>" --project src/EOP.Gestion.WebApp
```

Este comando es una **instrucción de configuración**, no una prueba de que la conexión funciona. Antes de ejecutarlo hay que confirmar el entorno al que apuntamos.

**En producción, cuando utilicemos Azure App Service:** la cadena se configurará en las opciones privadas del servicio que ejecute la WebApp, usando el nombre `EopDb` o la clave `ConnectionStrings__EopDb`, según el mecanismo elegido. **Todavía no tenemos verificada esa conexión.**

La conexión debe cifrar la comunicación y validar el certificado del servidor. No corresponde quitar controles de seguridad para intentar que una conexión fallida funcione.

### ¿Qué relación tiene Microsoft Entra ID?

**Microsoft Entra ID** se encarga de verificar quién intenta iniciar sesión. **Azure SQL Database**, en cambio, guarda el usuario de EOP, su estado y otros datos de la aplicación.

Son pasos distintos: Microsoft puede haber autenticado correctamente a alguien y, aun así, la aplicación no permitirle entrar porque no pudo consultar SQL para comprobar si ese usuario está habilitado.

## 5. ¿Qué son las migraciones?

Una **migración** es un cambio controlado en la estructura de la base de datos. Por ejemplo, si agregamos una propiedad nueva a la entidad Cliente, es posible que necesitemos crear una columna nueva en la tabla correspondiente.

En vez de cambiar las tablas manualmente cada vez, EF Core permite registrar esos cambios en archivos de migración, que guardamos en Git junto con el código.

Los comandos relevantes son:

| Comando | Para qué sirve |
|---|---|
| `dotnet ef migrations list` | Consultar las migraciones reconocidas por EF Core. |
| `dotnet ef database update` | **Aplicar cambios** a la base configurada. No usarlo sin revisar a qué base apunta. |
| `dotnet ef migrations script --idempotent` | Preparar un script SQL para revisarlo antes de un cambio de esquema. |

Para ejecutarlos en esta solución hay que indicar el proyecto de Infrastructure, el proyecto de inicio WebApp y el contexto `EopDbContext`. Por ejemplo, este comando **solo lista** migraciones y no aplica cambios:

```bash
dotnet ef migrations list --project src/EOP.Gestion.Infrastructure --startup-project src/EOP.Gestion.WebApp --context EopDbContext
```

El comando `database update` sí modifica la base configurada: antes de usarlo, verificar el destino, revisar los cambios y contar con la autorización y protección de datos necesarias. **No debemos ejecutar migraciones de producción automáticamente por abrir un PR.** El repositorio también contiene pruebas que comprueban el comportamiento de las migraciones en SQL Server.

## 6. ¿Cómo comprobamos que funciona?

Para una verificación real, sin cambiar datos:

1. En Azure Portal, abrir la base de datos y comprobar que el recurso esté disponible.
2. Abrir el **servidor SQL** asociado y revisar la sección **Networking / Redes**, donde se controlan los accesos permitidos. Una red o dirección IP no autorizada puede impedir la conexión. **No habilitar acceso general** como solución rápida.
3. Verificar qué método de autenticación usa la conexión y si la identidad tiene los permisos necesarios. No copiar contraseñas al informe.
4. Desde un equipo autorizado, comprobar la conexión a SQL mediante una consulta sencilla de lectura, como `SELECT 1`. Si devuelve `1`, esa consulta llegó a ejecutarse; **no demuestra por sí sola que todas las funciones de EOP estén bien**.
5. Probar una operación de la aplicación y comprobar el resultado. Registrar la fecha, el entorno utilizado y si la prueba funcionó o falló.
6. Si se revisan las migraciones aplicadas, consultar el registro `__EFMigrationsHistory`, sin modificarlo manualmente.

**Resultado por ahora:** S2-T01 figura como completada, pero la revisión actual de S2-T36 todavía no incluye una prueba nueva contra Azure SQL. La conexión desde el futuro App Service tampoco se ha verificado.

## 7. ¿Qué hacemos si aparece el error de inicio de sesión?

En las pruebas del equipo se observaron fallas en las que el primer inicio de sesión no siempre funcionaba, aunque después pudiera entrar. **Todavía no sabemos la causa exacta.**

Para investigarlo sin adivinar:

1. Anotar **cuándo** ocurrió y si la página mostró un error antes o después del inicio de sesión con Microsoft.
2. Revisar los registros de la aplicación. El código de `Program.cs` puede rechazar el inicio de sesión cuando falla la consulta que comprueba al usuario en SQL.
3. Comprobar el estado de la base, los errores de conexión y las reglas de red de Azure.
4. Verificar si el error se repite al conectar directamente desde una herramienta autorizada, sin pasar por la pantalla de inicio de sesión de la aplicación.
5. Registrar solo el tipo de error y datos técnicos que no revelen contraseñas, identificadores personales ni información de clientes.

Una interrupción temporal, una configuración de red o una pausa/reanudación del servicio **son posibilidades para investigar, no causas confirmadas**.

## 8. ¿Cómo protegemos los datos y cómo se recuperan?

Azure SQL Database ofrece mecanismos administrados de copias de seguridad y restauración. Esto sirve si una base necesita recuperarse después de un problema.

Según **RNF-007**, para producción se prevé una retención inicial de **7 días** con copias automáticas administradas por Azure SQL, según el plan contratado. **Todavía tenemos que comprobar que esa retención esté configurada realmente.**

Según **RNF-008**, antes de usar el sistema en producción debemos hacer una restauración de prueba **sobre una base separada**, sin sobrescribir los datos originales. La comprobación deberá repetirse al menos cada seis meses. Esa prueba **no consta como realizada**.

La aplicación también deberá usar conexiones seguras, mantener las credenciales fuera del código y controlar los permisos. No confundir las copias de seguridad de SQL con la protección de archivos de Azure Blob Storage, que tiene su propia guía.

## 9. Qué nos falta completar con Azure Portal

- Identificar y registrar los **valores reales** de la tabla de la sección 3.
- Comprobar los ajustes de **red, autenticación y conexión cifrada**.
- Verificar el **nivel de servicio**, porque es relevante para investigar eventuales pausas o problemas de disponibilidad.
- Registrar el resultado de una **prueba nueva de conexión y consulta**, sin incluir datos sensibles.
- Comprobar el **respaldo** configurado y, cuando corresponda antes de producción, registrar la prueba de restauración.
- Cuando se despliegue Azure App Service, comprobar su acceso real a la base. El acceso desde nuestra computadora no alcanza para darlo por validado.

**Documentos relacionados:** [reglas técnicas del proyecto](../AGENTS.md), [requerimientos no funcionales](./requerimientos-no-funcionales.md), [revisión de integraciones S2-T31](./revision-integracion-tecnica.md), [Azure Blob Storage](./azure-blob-storage.md) e [integración continua](./integracion-continua.md).

**Mantenimiento:** si se cambia la configuración o se realiza una nueva prueba, actualizar esta guía con lo comprobado mediante una rama y un Pull Request.
