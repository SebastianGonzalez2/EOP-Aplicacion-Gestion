# Requerimientos no funcionales

> Generado desde el anteproyecto corregido. **El documento Word es el original**;
> este archivo es la copia de trabajo del repositorio. Si corregís el Word,
> hay que regenerar este archivo.


21 requerimientos no funcionales.

---

## RNF-001 — Confirmación de acciones destructivas


**Descripción**


El sistema deberá solicitar una confirmación explícita antes de ejecutar acciones que eliminen información o relaciones previamente registradas, o que produzcan cambios relevantes en el ciclo de vida de un registro, cuando así corresponda según los requerimientos funcionales asociados. La confirmación deberá identificar el elemento afectado y permitir al usuario cancelar la operación sin realizar modificaciones.

La eliminación de elementos de la tabla durante la elaboración de un presupuesto, definida en RF-18, no requerirá una confirmación adicional.

Tipo: Usabilidad / Seguridad.

Referencias: RF-33, RF-34, RF-35, RF-36, RF-56, RF-61, RF-64, RF-75, RF-78, RF-91, RF-103.

## RNF-002 — Protección de credenciales


**Descripción**


La aplicación no deberá almacenar ni gestionar directamente las contraseñas de los usuarios. La autenticación deberá delegarse al servicio externo de identidad definido para el sistema.

Tipo: Seguridad / Política de autenticación.

Referencias: RF-69, RF-70, RF-71.

## RNF-003 — Autenticación multifactor


**Descripción**


El acceso de los usuarios deberá estar protegido mediante autenticación multifactor administrada por el proveedor de identidad utilizado por la aplicación.

Tipo: Seguridad / Política de acceso.

Referencias: RF-69, RF-71.

## RNF-004 — Control de acceso del lado del servidor


**Descripción**


Los permisos correspondientes a los tres roles fijos y las restricciones por responsabilidad o participación deberán validarse del lado del servidor en cada operación, incluyendo accesos directos por URL o solicitudes HTTP, sin depender de ocultar opciones o botones en la interfaz. No se admitirán excepciones individuales.

Tipo: Seguridad / Control de acceso.

Referencias: RF-72, RF-73.

## RNF-005 — Protección de documentos privados


**Descripción**


Los documentos y archivos almacenados por la aplicación no deberán ser públicamente accesibles mediante el conocimiento de su URL. El acceso deberá estar restringido a usuarios autenticados y autorizados.

Tipo: Seguridad / Confidencialidad.

Referencias: RF-44, RF-57, RF-59, RF-60, RF-77, RF-78, RF-83, RF-104.

## RNF-006 — Almacenamiento de archivos fuera de la base de datos relacional


**Descripción**


Los archivos gestionados por la aplicación, incluyendo documentos de presupuestos y proyectos y los PDF originales asociados a facturas, deberán almacenarse mediante un servicio privado destinado al almacenamiento de archivos; la información estructurada de las Facturas y las referencias necesarias para relacionar los archivos con los registros correspondientes se mantendrán en la base de datos, sin duplicar el contenido completo del PDF en ella.

Tipo: Arquitectura / Mantenibilidad.

Referencias: RF-44, RF-57, RF-59, RF-77, RF-83, RF-104.

## RNF-007 — Respaldo de la información


**Descripción**


En producción, los datos estructurados de Azure SQL Database estarán protegidos mediante backups automáticos administrados por el servicio, con retención inicial de 7 días y restauración a un punto en el tiempo dentro de esa ventana según las capacidades del plan contratado; la aplicación no generará manualmente backups SQL periódicos. Para archivos en Azure Blob Storage se habilitarán Blob Soft Delete y Container Soft Delete, ambos con retención de 14 días, así como versionado de Blobs con una política de ciclo de vida inicial para conservar versiones anteriores durante 14 días y eliminarlas posteriormente según las capacidades del servicio. Estas protecciones de archivos no constituyen una copia de seguridad independiente de toda la cuenta de almacenamiento ni incorporan Azure Backup o Backup Vault.

Tipo: Disponibilidad / Recuperación ante fallos.

Referencias: Base de datos; almacenamiento documental.

## RNF-008 — Recuperación ante pérdida o eliminación accidental


**Descripción**


La recuperación de Azure SQL Database permitirá restaurar los datos estructurados a un punto en el tiempo dentro de la retención inicial de 7 días, según el servicio contratado. Blob Soft Delete y Container Soft Delete permitirán recuperar, según las capacidades del servicio, archivos y contenedores eliminados accidentalmente durante 14 días; el versionado, con conservación inicial de versiones anteriores durante 14 días mediante política de ciclo de vida, ofrecerá protección frente a determinadas sobrescrituras accidentales, sin constituir un historial funcional de documentos en la aplicación. Antes de la puesta en producción se comprobará de forma controlada la recuperación de una base de datos y de archivos de prueba; se repetirá una comprobación de recuperación al menos cada seis meses, sin sobrescribir datos productivos ni desarrollar un módulo propio. Los tiempos efectivos se verificarán con el plan contratado y esas pruebas, sin establecer RTO o RPO garantizados ni afirmar recuperación ante cualquier desastre, eliminación definitiva de la cuenta de almacenamiento o pérdida de toda la infraestructura.

Tipo: Disponibilidad / Continuidad.

Referencias: Base de datos; almacenamiento documental.

## RNF-009 — Comunicación segura


**Descripción**


Las comunicaciones entre los usuarios y la aplicación deberán realizarse utilizando conexiones cifradas mediante HTTPS.

Tipo: Seguridad / Estándar técnico.

Referencias: Aplicación completa.

## RNF-010 — Validación del lado del servidor


**Descripción**


Los datos recibidos por la aplicación deberán validarse en el servidor independientemente de las validaciones realizadas en la interfaz de usuario.

Tipo: Seguridad / Integridad de datos.

Referencias: Formularios y operaciones de alta/modificación del sistema.

## RNF-011 — Protección de secretos y credenciales de servicios externos


**Descripción**


Las claves de acceso, tokens, cadenas de conexión y demás secretos utilizados para acceder a servicios externos y recursos de infraestructura no deberán almacenarse directamente en el código fuente ni exponerse al usuario.

Tipo: Seguridad / Configuración.

Referencias: OpenAI API (transcripción y análisis de relevamientos); datosUruguay API; Azure SQL Database; Azure Blob Storage; Microsoft Entra ID y los secretos de configuración realmente utilizados por las integraciones incluidas.

## RNF-012 — Registro de errores e integraciones


**Descripción**


El sistema deberá registrar información suficiente sobre errores ocurridos durante procesos automáticos e integraciones externas para permitir su diagnóstico, sin almacenar en los logs contraseñas, tokens u otros secretos, ni el contenido íntegro de Facturas o PDFs ni RUT u otros datos personales sensibles.

Tipo: Mantenibilidad / Observabilidad.

Referencias: datosUruguay API (consultas de cotizaciones históricas); OpenAI API (transcripción y análisis de relevamientos); mecanismos de autenticación e integraciones externas incluidas. No se consideran OCR de facturas, correo automático, actualización Fivisa ni procesos programados exclusivos de vencimiento.

## RNF-013 — Tolerancia a fallos de servicios externos


**Descripción**


Un error o indisponibilidad temporal de un servicio externo no deberá provocar pérdida de información previamente registrada ni impedir el uso de funcionalidades que no dependen de ese servicio. Ante fallos de OpenAI API se conservará el relevamiento y podrán realizarse tareas manuales que no requieran IA. Para nuevas operaciones económicas que requieran cotización histórica USD/UYU, si no existe una válida disponible y datosUruguay API no la proporciona, el sistema impedirá completar el registro e informará el motivo; no almacenará conversiones pendientes. Esto no exime de la disponibilidad de Azure SQL Database y Azure Blob Storage para las operaciones que los requieran.

Tipo: Confiabilidad / Disponibilidad.

Referencias: OpenAI API (transcripción y análisis de relevamientos); datosUruguay API; Azure SQL Database; Azure Blob Storage; Microsoft Entra ID; WhatsApp Click to Chat (apertura de conversaciones).

## RNF-014 — Adaptabilidad de la interfaz


**Descripción**


La interfaz web deberá poder utilizarse desde navegadores modernos tanto en equipos de escritorio como en dispositivos móviles, adaptándose al tamaño de pantalla sin impedir el acceso a las funcionalidades principales.

Tipo: Usabilidad / Compatibilidad.

Referencias: Aplicación completa.

## RNF-015 — Tipografía de la interfaz


**Descripción**


PENDIENTE DE DEFINIR/VALIDAR: tipografía principal que deberá utilizar la interfaz de la aplicación. Su uso deberá mantenerse de forma consistente en las distintas secciones de la aplicación, incluyendo títulos, textos, formularios, botones y demás elementos visuales.

Tipo: Usabilidad / Diseño visual.

Referencias: Aplicación completa.

## RNF-016 — Colores de la interfaz


**Descripción**


La interfaz de la aplicación deberá utilizar verde y blanco como colores principales, de acuerdo con la identidad visual solicitada por el cliente. Estos colores deberán aplicarse de forma consistente en las distintas pantallas y componentes del sistema, manteniendo una adecuada legibilidad y diferenciación de los elementos de la interfaz.

Tipo: Usabilidad / Diseño visual.

Referencias: Aplicación completa.

## RNF-017 — Extensibilidad del sistema


**Descripción**


La aplicación deberá diseñarse y desarrollarse de forma modular, permitiendo incorporar nuevas funcionalidades y realizar modificaciones futuras sin requerir cambios significativos en los componentes existentes ni afectar el correcto funcionamiento del sistema.

Tipo: Mantenibilidad / Extensibilidad.

Referencias: Arquitectura general de la aplicación.

## RNF-018 — Validación de datos


**Descripción**


La aplicación deberá validar los datos ingresados por el usuario antes de procesarlos, mostrando mensajes claros cuando existan campos incorrectos o incompletos.

Tipo: Usabilidad / Integridad de datos.

Referencias: Formularios y operaciones de alta y modificación del sistema.

## RNF-019 — Indicador visual de avance del proceso de presupuestación


**Descripción**


Durante las distintas etapas de elaboración de un presupuesto, la interfaz deberá mostrar de forma visible un indicador de progreso que permita al usuario identificar la etapa actual del proceso y las etapas restantes.

El indicador deberá contemplar las siguientes etapas generales:

Elaboración → Resumen → Documentos → Revisión interna

La etapa actual deberá encontrarse visualmente diferenciada de las etapas completadas y de las etapas pendientes.

El indicador tendrá carácter informativo y no deberá permitir avanzar directamente hacia una etapa posterior omitiendo las validaciones o acciones requeridas por el flujo de presupuestación.

Tipo: Usabilidad / Navegación.

Referencias: Flujo de presupuestación; RF-05; RF-31; RF-32; RF-33.

## RNF-020 — Retroalimentación visual durante operaciones de procesamiento


**Descripción**


El sistema deberá mostrar una indicación visual de progreso o procesamiento durante aquellas operaciones que requieran espera por parte del usuario, como generación de archivos, transcripción, análisis o consultas externas. Mientras la operación esté en curso, la acción correspondiente no deberá poder ejecutarse nuevamente.

Tipo: Usabilidad / Diseño visual.

Referencias: Operaciones que requieran espera por parte del usuario.

## RNF-021 — Validación completa del RUT uruguayo


**Descripción**


El sistema deberá validar en el servidor todo RUT que se registre o modifique, comprobando su estructura uruguaya y su dígito verificador antes de guardar los datos. Se utilizará la misma validación en todos los módulos que reciban un RUT.

**Reglas de validación**

1. Antes de validar, se quitarán los separadores habituales: puntos, guiones y espacios.
2. El RUT deberá tener exactamente 12 dígitos.
3. Los dos primeros dígitos (prefijo) deberán estar entre 01 y 22.
4. Los seis dígitos siguientes (número de serie) no podrán ser todos cero.
5. Los tres dígitos siguientes (sufijo) deberán ser 001.
6. El último dígito deberá coincidir con el dígito verificador calculado a partir de los 11 anteriores.
7. Si el RUT no cumple alguna de estas reglas, el sistema deberá informar el error y no guardar los datos.

Esta validación comprueba el formato, pero no confirma que el RUT exista o esté activo ante la DGI. Los criterios técnicos y sus fuentes están documentados en DT-04 de `decisiones-tecnicas.md`.

Tipo: Integridad de datos.

Referencias: RF-23, RF-86, RF-89, RF-109, US-009, US-053, US-054, US-072, RNF-10, RNF-18, DT-04.
