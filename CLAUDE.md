# Sistema de Gestión EOP

Aplicación web de gestión para **EOP (Estudios, Obras y Proyectos)**, una empresa de servicios energéticos (ESCO) de Montevideo. Centraliza presupuestación, proyectos, documentación, facturación y catálogo de materiales.

Proyecto Integrador — Universidad ORT Uruguay. Equipo de 2 integrantes. 8 sprints de 2 semanas, del 26/10/2026 al 14/02/2027.

> Este archivo define las reglas del proyecto. Leelo antes de escribir código.
> Si algo acá contradice lo que te piden en el chat, preguntá antes de avanzar.

---

## Stack

| Capa | Tecnología |
|---|---|
| Lenguaje | C# |
| Framework | ASP.NET Core MVC sobre .NET 10 LTS |
| Vistas | Razor Views, HTML, CSS, Bootstrap, JavaScript |
| ORM | Entity Framework Core (code first, con migraciones) |
| Base de datos | SQL Server en desarrollo · Azure SQL Database en producción |
| Identidad | Microsoft Entra ID + Microsoft.Identity.Web + MFA |
| Archivos | Azure Blob Storage (privado) |
| IA | OpenAI API — transcripción de audios y análisis de relevamientos |
| Audio | MediaRecorder API del navegador |
| Excel | ClosedXML |
| Gantt | Frappe Gantt |
| Gráficos | Chart.js |
| Cotizaciones | datosUruguay API (USD/UYU, datos del BCU) |
| Hosting | Azure App Service · Azure WebJobs |
| Email | Azure Communication Services Email |

**Fuera del alcance de la primera versión** (no implementar, aunque aparezca mencionado en documentación vieja):

- Azure AI Document Intelligence / OCR de facturas — las facturas se registran **manualmente**
- Generación del Word de propuesta comercial mediante IA — EOP la prepara afuera y sube el PDF
- Sincronización automática de precios con Fivisa
- Pantallas de administración de tipos de proyecto, tipos de documento y unidades (son conjuntos fijos)

---

## Arquitectura

**Monolito modular.** Una sola aplicación web, con responsabilidades internas separadas por área funcional: presupuestación, proyectos, gestión documental, clientes, proveedores, materiales, facturas y administración de usuarios.

Separar siempre presentación, lógica de negocio, persistencia e integraciones externas:

- Los **Controllers** coordinan la solicitud, no contienen reglas de negocio
- Las **reglas de negocio** van en servicios reutilizables
- El **acceso a datos** pasa por EF Core
- Cada **integración externa** se encapsula en su propio servicio, nunca se llama desde un Controller

---

## Reglas que todo código debe cumplir

Vienen de los requerimientos no funcionales. No son sugerencias.

**Seguridad y acceso**

- **RNF-4** — Todo control de acceso se verifica **del lado del servidor**. Ocultar un botón en la vista no es control de acceso.
- **RNF-3** — Autenticación mediante Entra ID con MFA. La app no gestiona contraseñas.
- **RNF-2 / RNF-11** — Ningún secreto, cadena de conexión ni credencial de servicio externo va en el código ni en archivos versionados.
- **RNF-5** — Los documentos son privados. No hay URLs públicas a archivos.
- **RNF-9** — Toda comunicación por HTTPS.

**Datos**

- **RNF-10 / RNF-18** — Toda validación se hace del lado del servidor. La validación en el navegador es comodidad, no control.
- **RNF-6** — Los archivos van a Blob Storage. En la base quedan los datos estructurados y la referencia. Nunca el binario.
- **RNF-1** — Toda acción destructiva pide confirmación explícita.

**Integraciones**

- **RNF-13** — Si un servicio externo falla, la aplicación sigue funcionando y lo informa. Nunca se rompe la operación entera.
- **RNF-12** — Los errores y las llamadas a servicios externos se registran.

**Interfaz**

- **RNF-14** — Adaptable a distintos tamaños de pantalla (Bootstrap).
- **RNF-19 / RNF-20** — Toda operación que demore muestra avance o retroalimentación visual.
- **RNF-17** — El sistema debe poder extenderse sin reescribir lo existente.

---

## Reglas de negocio transversales

**Roles.** Tres, fijos: `Administrador`, `Supervisor`, `Operador`.

No existen permisos individuales, ni excepciones por usuario, ni creación de roles nuevos. Además del rol, hay restricciones por responsabilidad o participación en un proyecto. Siempre verificadas en el servidor.

Nunca se puede dejar al sistema sin administradores activos.

**Estados de presupuesto.** Exactamente estos ocho, escritos así:

```
Borrador
Para revisión interna
Enviado / pendiente de respuesta del cliente
Aprobado
Rechazado
Solicitud de modificación del cliente
Vencido
Cancelado
```

Editables: Borrador, Para revisión interna, Solicitud de modificación del cliente.

**Códigos internos.** Los genera el sistema, nunca el usuario:

- Presupuesto: `PRES-AAAAMMDD-<NOMBRE DEL PROYECTO>-####` (correlativo global de 4 cifras)
- Factura de venta: `FV-####`
- Factura de compra: `FC-####`

**Bajas.** Clientes y usuarios no se eliminan: se inactivan. Los documentos de proyecto y las facturas se eliminan de forma **lógica**; el archivo original se conserva en Blob Storage.

**Proyecto y presupuesto.** Un proyecto tiene como máximo un presupuesto, y un presupuesto pertenece como máximo a un proyecto.

**Facturas.** Toda factura activa pertenece a exactamente un proyecto. No se editan los datos de una factura ya guardada ni se reasigna a otro proyecto. El PDF original es obligatorio.

**Monedas.** Importes y monedas originales se conservan siempre. Las conversiones usan cotización histórica válida de datosUruguay. Si no hay cotización utilizable, se rechaza el guardado y se informa el motivo — no se guardan conversiones pendientes.

**Precios del catálogo.** Al tomar un precio para un presupuesto, se copia como valor histórico de ese presupuesto. Actualizar el catálogo después no modifica presupuestos anteriores.

---

## Convenciones

**Código** — Convenciones estándar de C# y .NET. Nombres descriptivos en clases, métodos y variables. Comentarios solo donde el código no se explica solo.

**Idioma** — Entidades, propiedades y textos de interfaz en español, siguiendo el vocabulario del documento (Presupuesto, Proveedor, Relevamiento, Concepto). Palabras clave y tipos del framework en inglés, como corresponde.

**Ramas** — Nunca se trabaja directo sobre `main`. Una rama por tarea:

```
feat/US-053-validaciones-cliente
fix/US-042-restricciones-acceso
```

**Commits** — Mensaje descriptivo que empieza con la referencia:

```
US-053: validar RUT y correos obligatorios en alta de cliente
S1-T03: configurar DbContext y migración inicial
```

**Pull requests** — Todo cambio entra por PR, revisada por el otro integrante. Antes de aprobar se verifica que compile, funcione y no rompa lo existente. Las versiones de entregas se marcan con tags.

---

## Trazabilidad

Cada tarea de desarrollo corresponde a una user story, y cada user story a uno o más requerimientos funcionales. Al implementar algo, verificá el requerimiento real antes de asumir el comportamiento.

Los requerimientos van a vivir en `docs/` (pendiente de cargar). Mientras tanto, el anteproyecto es la fuente.

El tablero de Trello `EOP - Sistema de Gestion` tiene cinco listas: Product Backlog, Sprint Backlog, In Progress, Testing, Done. Las tareas se nombran `S<sprint>-T<nn> [categoría] descripción (<horas>h)`.

---

## Qué NO hacer

- No implementar funcionalidad que esté fuera del alcance de la primera versión
- No inventar reglas de negocio: si el requerimiento no lo dice, preguntá
- No poner secretos en el código ni en `appsettings.json` versionado
- No hacer validación solo en el navegador
- No guardar archivos en la base de datos
- No eliminar físicamente registros que el documento define como baja lógica
- No hacer push directo a `main`

---

## Pendiente de definir

Se completa a medida que avance el sprint 1:

- Estructura de carpetas de la solución
- Nombres definitivos de entidades y DbContext
- Estrategia de tests y su ubicación
- Configuración de GitHub Actions
