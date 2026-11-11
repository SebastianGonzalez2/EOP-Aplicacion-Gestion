# Azure Blob Storage — configuración e integración de EOP (S2-T02 y S2-T03)

**Estado:** infraestructura creada; servicio compartido implementado en S2-T03 y prueba real de guardar/recuperar desde C# satisfactoria (sección 8). Permanecen pendientes las pruebas negativas de acceso y la configuración del futuro App Service.  
**Fecha del registro:** 11/11/2026.  
**Tareas:** [S2-T02 — Configurar Azure Blob Storage](https://trello.com/c/FR4B8FrA) y [S2-T03 — Preparar servicio compartido de almacenamiento](https://trello.com/c/wOSb5Ld6).  
**Alcance:** registrar cómo se preparó en Azure el almacenamiento de archivos privados. Este documento no implementa los casos de uso de carga/descarga en la aplicación.

> Esta es una descripción de la configuración realizada manualmente en el portal de Azure, no una definición de infraestructura como código ni una garantía de que toda la configuración futura permanecerá idéntica. Antes de desplegar o recrear el entorno, comprobar los valores vigentes en el portal. No registrar aquí secretos, tokens, claves, identificadores de usuario ni direcciones IP personales.

## 1. Recursos y organización

| Propiedad | Configuración |
|---|---|
| Suscripción | Azure for Students (cuenta institucional de la facultad) |
| Grupo de recursos | `eop-dev` |
| Cuenta de almacenamiento | `eopstorage245` |
| Tipo | StorageV2, uso general v2 |
| Región | Brazil South |
| Rendimiento | Estándar |
| Redundancia | ZRS (redundancia de zona) |
| Nivel de acceso predeterminado | Hot / Frecuente |
| Contenedor | `documentos-eop` |
| Acceso anónimo al contenedor | Privado; no se permite acceso anónimo |

**Ubicación en el portal:** Cuentas de almacenamiento → `eopstorage245` → Almacenamiento de datos → Contenedores → `documentos-eop`.

La cuenta se creó en la suscripción **institucional**, donde está disponible el crédito de Azure for Students. El registro de la aplicación de Microsoft Entra ID utilizado para iniciar sesión en EOP está en un entorno **personal distinto**. La autenticación de usuarios en la aplicación y la identidad que accede al almacenamiento son cuestiones separadas; la conexión entre tenants y la autorización del futuro App Service siguen pendientes de definición y prueba.

El nombre `eopstorage245` sustituye al nombre inicialmente considerado `eopstorage`, porque Azure informó que ya estaba ocupado.

### Motivos principales

- **Blob Storage y SQL separados:** los archivos físicos permanecen fuera de SQL Server/Azure SQL. La base relacional guardará los datos estructurados y las referencias a los blobs, conforme a RNF-006.
- **ZRS:** se eligió para mejorar la resistencia frente a fallos de una zona de disponibilidad respecto de LRS. ZRS no es un backup independiente ni cubre todos los incidentes posibles.
- **Estándar y Hot:** configuración adecuada como punto de partida para documentos empresariales que se consultarán desde la aplicación. Se deberá revisar el consumo y los patrones reales de acceso.

## 2. Configuración avanzada

- Espacio de nombres jerárquico: **deshabilitado**.
- NFS v3: **deshabilitado**.
- SFTP: **deshabilitado**.
- Replicación entre inquilinos: **deshabilitada**.
- Servicios Azure Files: no se utilizarán para el almacenamiento documental de EOP.

No se necesita un sistema de archivos compartido ni acceso SFTP para los casos de uso acordados. El futuro servicio de almacenamiento en ASP.NET Core utilizará la integración de Blob Storage desde Infrastructure, no acceso anónimo directo de los usuarios.

## 3. Redes, seguridad y cifrado

| Control | Estado configurado |
|---|---|
| Acceso de red pública | Habilitado, **limitado a redes virtuales e IP seleccionadas** |
| IP de desarrollo | Una dirección pública de cliente autorizada; **no se registra su valor** |
| Red virtual asociada | Ninguna, por ahora |
| Punto de conexión privado | Ninguno, por ahora |
| Preferencia de enrutamiento | Red de Microsoft |
| Acceso anónimo a blobs | Deshabilitado en la cuenta |
| Acceso mediante claves compartidas de la cuenta | Deshabilitado |
| Autorización predeterminada con Entra ID en Azure Portal | Habilitada |
| Transferencia segura | Obligatoria |
| TLS mínimo | 1.2 |
| Operaciones de copia entre cuentas | Limitadas a cuentas del mismo tenant de Entra (opción preliminar) |
| Cifrado de datos en reposo | Claves administradas por Microsoft |
| Compatibilidad para claves administradas por cliente | Solo blobs y archivos |
| Cifrado adicional de infraestructura | Deshabilitado |
| Microsoft Defender para Storage | No habilitado en esta tarea |

**Consideración de red:** el acceso permitido desde la IP del equipo de desarrollo **no autoriza automáticamente** al futuro Azure App Service. Antes de integrar y desplegar la aplicación habrá que definir y probar una vía de red permitida para su identidad y su ejecución (por ejemplo, conectividad mediante red virtual y las opciones compatibles con el plan de hosting), manteniendo los documentos privados. Una IP doméstica puede cambiar y no debe ser una dependencia del funcionamiento de producción.

**Consideración de permisos:** el bloqueo del acceso anónimo no sustituye la autorización del lado del servidor en EOP. Cuando se implementen las acciones de documentos, la aplicación deberá comprobar roles y restricciones por responsabilidad/participación antes de entregar cualquier archivo.

## 4. Protección de datos y ciclo de vida

| Mecanismo | Configuración |
|---|---|
| Blob Soft Delete | Habilitado; 14 días |
| Container Soft Delete | Habilitado; 14 días |
| Versionado de blobs | Habilitado |
| Restauración a un momento dado | Deshabilitada |
| Fuente de cambios (change feed) | Deshabilitada |
| Inmutabilidad a nivel de versión | Deshabilitada |

**Regla de ciclo de vida creada:** `EliminarVersionesAnteriores14Dias`.

- Ámbito: todos los blobs de la cuenta.
- Tipo: blobs en bloques.
- Subtipo: **versiones anteriores**, sin seleccionar blobs base ni instantáneas.
- Condición: la versión fue **creada hace más de 14 días**.
- Acción: **eliminar la versión de blob**.

La regla no está destinada a eliminar los documentos actuales. Los días se cuentan desde la **creación de cada versión**, no desde el momento en que dejó de ser la actual. El procesamiento de las reglas se realiza de forma asincrónica; no hay que interpretar el umbral como una hora exacta de eliminación.

Los mecanismos anteriores **no** son un backup independiente de toda la cuenta, ni un historial de documentos gestionado por la aplicación. Antes de producción se deben realizar pruebas controladas de recuperación de archivos y contenedores según RNF-008.

**Pendiente de comprobación adicional:** verificar en el portal que la regla se mantiene habilitada y realizar pruebas específicas de sobrescritura, versiones anteriores, eliminación y recuperación. En esta tarea se configuró la protección, pero esas recuperaciones todavía no se probaron.

## 5. Identidad y permisos (RBAC)

- En el contenedor `documentos-eop` se asignó el rol **Storage Blob Data Contributor** (Colaborador de datos de Storage Blob) a la **cuenta institucional de desarrollo**.
- **Ámbito:** únicamente el contenedor `documentos-eop`, no toda la suscripción.
- Propósito: comprobar desde el portal la lectura y escritura de blobs mediante Microsoft Entra ID, sin habilitar las claves compartidas.
- El rol permite también modificaciones y eliminaciones; debe restringirse a quienes lo necesiten y revisarse antes de producción.

Este permiso no se otorgó a los usuarios finales de EOP ni al App Registration personal. En una tarea posterior se configurará la identidad de ejecución de la aplicación y sus permisos mínimos. El nombre o identificador concreto de la cuenta institucional no se documenta aquí.

## 6. Verificaciones manuales realizadas

**Comprobaciones efectuadas el 11/11/2026 por el equipo a través del portal:**

| Prueba | Resultado observado |
|---|---|
| Creación de la cuenta de almacenamiento | Azure mostró aprovisionamiento correcto |
| Creación del contenedor `documentos-eop` | Contenedor disponible |
| Subida de un PDF de comprobación | Correcta |
| Descarga y apertura de ese PDF | Correcta |
| Autenticación de datos desde el portal | Cuenta de usuario de Microsoft Entra |
| Apertura de la URL directa del blob en ventana de incógnito, sin token | Azure devolvió XML con `PublicAccessNotPermitted` y mensaje de acceso público no permitido |

Durante la primera navegación apareció un rechazo de autorización al enumerar archivos; después de actualizar el portal el aviso desapareció y las operaciones de carga/descarga funcionaron. No se determinó la causa exacta; si se reproduce, revisar propagación de RBAC, sesión e IP permitida antes de modificar las políticas de seguridad.

La prueba en incógnito confirma que **esa solicitud anónima fue rechazada**. No sustituye una auditoría general de permisos ni las pruebas desde la aplicación desplegada.

**No ejecutado durante S2-T02:** pruebas automatizadas de código, pruebas de restauración de archivos eliminados, recuperación de versiones antiguas, validación del App Service ni verificación de disponibilidad ante fallos de zona. No se modificó código C# durante S2-T02; la integración se desarrolló después, en S2-T03 (sección 8).

## 7. Pendientes y dependencias

1. **S2-T03 — Servicio compartido de almacenamiento:** implementado en código y comprobado desde desarrollo con una subida y descarga reales el 11/11/2026 (sección 8). Permanecen pendientes las pruebas negativas de permisos/red y la verificación del App Service, correspondientes a la preparación y operación de esos entornos.
2. **Acceso del App Service:** configurar su identidad administrada, permisos de datos mínimos, conectividad de red y compatibilidad con los distintos tenants. No suponer que el permiso del usuario de desarrollo se transfiere al servidor.
3. **Integraciones funcionales posteriores:** aplicar controles de acceso del servidor a operaciones de documentos, propuestas y PDF de facturas. Guardar en SQL referencias, nunca binarios. La eliminación lógica de documentos y facturas no debe borrar automáticamente su archivo físico.
4. **Recuperación:** verificar la regla de ciclo de vida y ejecutar pruebas controladas de eliminación, sobrescritura y recuperación antes de producción; documentar el resultado y repetir las comprobaciones según RNF-008.
5. **Operación y costos:** supervisar almacenamiento, operaciones, versiones retenidas y gasto de la suscripción. Revisar la suscripción y el entorno adecuados antes del uso empresarial definitivo; el recurso actual pertenece a `eop-dev` y utiliza créditos educativos.

## 8. Integración con la aplicación (S2-T03)

Se agregó el servicio técnico compartido que usarán los futuros casos de uso (documentos de proyecto, propuestas, PDF de facturas). No incluye Controllers, pantallas ni reglas de negocio.

| Pieza | Ubicación |
|---|---|
| Contrato `IServicioAlmacenamiento` (`GuardarAsync`, `ObtenerAsync`) | `Core/Interfaces/Almacenamiento/` |
| Modelos `ArchivoParaGuardar`, `ArchivoDescargado` | `Core/DTOs/Almacenamiento/` |
| `AlmacenamientoNoDisponibleException` (falla técnica sin tipos de Azure) | `Core/Comun/` |
| `ServicioAlmacenamientoBlob` (SDK `Azure.Storage.Blobs`) | `Infrastructure/Almacenamiento/` |
| `AlmacenamientoBlobOpciones` y `FabricaContenedorBlob` (configuración y credencial) | `Infrastructure/Almacenamiento/` |
| Registro en inyección de dependencias | `AgregarInfraestructura` (singleton) |

**Funcionamiento.** `GuardarAsync` genera la referencia `<carpeta>/<guid>.<extensión>`, sube el archivo con la condición «solo si no existe» (nunca sobrescribe) y devuelve la referencia, que es lo único que se guarda en la base. `ObtenerAsync` devuelve el contenido en streaming o `null` si el blob no existe. El servicio no genera URLs ni SAS; los archivos siempre se entregan a través de la aplicación. Tampoco borra: la eliminación lógica de documentos y facturas conserva el archivo (ver `AGENTS.md`).

**Validaciones técnicas del servicio.** Carpeta (minúsculas, números y guiones), nombre y tipo de contenido presentes y con forma válida, archivo no vacío y legible, y referencia con el formato exacto que genera el propio servicio. La validación de negocio —formatos admitidos, límite de 100 MB, permisos del usuario, tipo de documento— corresponde a cada caso de uso y no se duplica aquí.

**Autenticación.** La aplicación se identifica con Microsoft Entra ID mediante una identidad técnica; no hay claves, cadenas de conexión ni SAS. Es independiente del usuario que inicia sesión en EOP (su App Registration está en otro tenant).

| Entorno | Credencial | Dónde se configura |
|---|---|---|
| Producción (App Service) | `ManagedIdentityCredential` (asignada por el sistema, o por el usuario si se indica `ClientIdIdentidadAdministrada`) | `appsettings.json`: `AlmacenamientoBlob:UsarIdentidadAdministrada = true` |
| Desarrollo | `DefaultAzureCredential` (Visual Studio, Azure CLI, etc.) | `appsettings.Development.json`: `UsarIdentidadAdministrada = false` |

`appsettings.json` solo guarda valores no secretos (`NombreCuenta`, `NombreContenedor`). El tenant institucional para desarrollo es opcional y no se versiona:

```bash
dotnet user-secrets set "AlmacenamientoBlob:TenantId" "<tenant-institucional>" --project src/EOP.Gestion.WebApp
```

**Fallas.** Cualquier error de Azure (sin permiso, red, servicio no disponible) o de obtención del token se registra (tipo de error, estado HTTP y código de Azure; nunca el contenido, tokens ni el mensaje de la excepción) y se convierte en `AlmacenamientoNoDisponibleException`, que los casos de uso deben atrapar para informar al usuario sin romper el resto de la aplicación (RNF-13). La configuración se valida al pedir el servicio por primera vez, no al arrancar, para que la aplicación inicie aunque falte.

**Pruebas automatizadas** (`ServicioAlmacenamientoBlobTests`, `ConfiguracionAlmacenamientoTests`, `ArquitecturaTests`): usan el SDK real contra un Azure simulado (transporte HTTP falso), sin red ni credenciales. **No prueban la conexión real.**

**Prueba manual de integración contra Azure real — satisfactoria (11/11/2026).** Se ejecutó desde el equipo de desarrollo, autenticado con la cuenta institucional de Visual Studio y desde la red autorizada, utilizando el servicio C# de S2-T03 (no el simulador). El destino fue la cuenta `eopstorage245` y el contenedor privado `documentos-eop`.

- Se generó un archivo de texto de prueba con contenido único y se llamó a `GuardarAsync` en la carpeta `pruebas-integracion`.
- Se utilizó la referencia devuelta por `GuardarAsync` para ejecutar `ObtenerAsync`.
- Se verificó que el archivo se recuperara, que el tipo MIME fuera `text/plain` y que el texto recuperado coincidiera exactamente con el original.
- **Resultado observado:** 1 prueba ejecutada, **1 correcta, 0 fallidas**, con compilación satisfactoria de los proyectos involucrados.
- **Autenticación:** el primer intento falló porque `DefaultAzureCredential` intentó obtener una identidad administrada no disponible en el equipo local (IMDS). Al restringir el mecanismo a `VisualStudioCredential` mediante la variable temporal de PowerShell `AZURE_TOKEN_CREDENTIALS=VisualStudioCredential`, la prueba pasó. No fue necesario modificar los permisos RBAC ni las reglas de red de Azure.
- La clase `AzureBlobRealManualTests.cs` fue una prueba temporal y se eliminó después de su ejecución; **no se incorpora a la suite automática**, porque depende de una cuenta de Azure y de la red autorizada. El archivo de texto subido puede eliminarse manualmente desde el portal cuando ya no se necesite.

**Comprobaciones pendientes** (no cubiertas por esa prueba de subida/descarga):

1. Probar el rechazo con una identidad sin el rol correspondiente (esperado: falla controlada sin datos sensibles en el registro).
2. Probar desde una IP no autorizada (esperado: acceso denegado y falla controlada).
3. En App Service: configurar la identidad administrada, el rol sobre el contenedor y una vía de red permitida (ver sección 3), y repetir la prueba de integración. **Sin esto, producción no podrá acceder al almacenamiento.**
4. Si el App Service queda en un tenant distinto del de la cuenta de almacenamiento, definir y verificar la autenticación entre tenants.

## 9. Trazabilidad

- **RNF-005:** los documentos no pueden ser consultados públicamente por conocer su URL.
- **RNF-006:** archivos en un almacenamiento privado, separados de la base de datos relacional.
- **RNF-007 / RNF-008:** eliminación temporal, versionado y recuperación (configurados; pruebas de recuperación pendientes).
- **RNF-009:** comunicación segura.
- **RNF-011:** no almacenar ni publicar secretos de infraestructura.
- **RNF-004:** autorización del lado del servidor, a integrar en los casos de uso posteriores.
- **US-016, US-033, US-034, US-035 y carga de PDF de facturas:** futuros consumidores del almacenamiento, sin implementar sus funcionalidades en S2-T02.

Fuentes internas: [AGENTS.md](../AGENTS.md), [requerimientos no funcionales](./requerimientos-no-funcionales.md), [alcance](./alcance.md), [historias de usuario](./user-stories.md).

---

**Mantenimiento:** si cambia la cuenta, contenedor, seguridad, red, retención o permisos, actualizar este registro en una rama y revisarlo por Pull Request. Nunca agregar claves de acceso, cadenas de conexión, tokens, direcciones IP personales ni documentos empresariales reales al repositorio.
