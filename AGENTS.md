# Sistema de Gestión EOP

Aplicación web de gestión para **EOP (Estudios, Obras y Proyectos)**, una empresa de servicios energéticos (ESCO) de Montevideo. Centraliza presupuestación, proyectos, documentación, facturación y catálogo de materiales.

Proyecto Integrador — Universidad ORT Uruguay. Equipo de 2 integrantes. 8 sprints de 2 semanas, del 26/10/2026 al 14/02/2027.

> Este archivo define las instrucciones técnicas y de trabajo del proyecto.
> **No sustituye el anteproyecto ni los requerimientos funcionales y no funcionales.**
> Antes de escribir código, leé este archivo y consultá en `docs/` los requisitos
> aplicables a la tarea. Ante contradicciones, informalas y solicitá una decisión
> antes de implementar; no inventes ni cambies reglas de negocio.
>
> El equipo trabaja con dos asistentes distintos: Codex lee este archivo
> automáticamente, y Claude Code llega hasta acá desde `CLAUDE.md`. Cualquier
> cambio en las instrucciones de los asistentes se hace **en este archivo**, no en el otro.

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

### Estructura de la solución

```
EOP.Gestion.slnx
├── src/
│   ├── EOP.Gestion.Core/              ← no referencia a ningún otro proyecto
│   │   ├── Entidades/                 ← por módulo
│   │   ├── ServiciosDominio/
│   │   ├── CasosDeUso/                ← por módulo
│   │   ├── Interfaces/
│   │   ├── DTOs/
│   │   └── Comun/
│   ├── EOP.Gestion.Infrastructure/    ← referencia a Core
│   │   ├── Persistencia/              ← DbContext, Configuraciones, Migraciones, Repositorios
│   │   ├── Almacenamiento/            ← Blob Storage
│   │   ├── Integraciones/             ← OpenAi, DatosUruguay
│   │   └── Excel/                     ← ClosedXML
│   └── EOP.Gestion.WebApp/            ← referencia a Core e Infrastructure
│       ├── Controllers/  Views/  ViewModels/  Autorizacion/  wwwroot/
└── tests/
    └── EOP.Gestion.Tests/             ← xUnit
```

**Dirección de las dependencias:** `WebApp → Core ← Infrastructure`.
`Core` no referencia a nadie. Si alguna vez hace falta agregarle una referencia,
hay código en la capa equivocada.

### Dónde va cada cosa

- **`ServiciosDominio/`** — reglas de negocio puras, que valen siempre: validar un
  RUT, calcular el margen de un presupuesto, decidir si un presupuesto está vencido.
  No saben de base de datos ni de pantallas, y se testean sin infraestructura.
- **`CasosDeUso/`** — orquestan una operación completa del usuario. «Registrar
  factura de compra» busca el proveedor, valida, pide la cotización, guarda y
  actualiza los indicadores. Usan servicios de dominio e interfaces, nunca EF Core
  directamente.
- **`Interfaces/`** — contratos de persistencia e integraciones que `Core` necesita
  y que `Infrastructure` implementa.
- **`DTOs/`** — datos que cruzan la frontera de `Core`. Los `ViewModels` de la capa
  web son otra cosa y viven en `WebApp`.
- **`Comun/`** — tipos y validaciones compartidas entre módulos.

Si una regla sirve para más de un caso de uso, es servicio de dominio. Si describe
un flujo que el usuario reconoce como una acción, es caso de uso.

### Repositorios

Se usa el patrón Repositorio para que `Core` no dependa de EF Core. Tres reglas:

1. **Uno por agregado, nunca genérico.** `IRepositorioPresupuestos`, no `IRepositorio<T>`.
2. **Los métodos se nombran según lo que necesita el caso de uso**, no según la tabla:
   `ObtenerVigentesDelCliente(id)`, no `GetAll()` para filtrar afuera.
3. **Nunca devolver `IQueryable`** desde la interfaz. Si se devuelve, EF Core se filtró
   a `Core` y se pierde el desacoplamiento.

**Excepción:** los listados con filtros combinables y los indicadores económicos no
pasan por repositorios. Van por servicios de consulta en `Infrastructure` que usan EF
Core directamente y devuelven DTOs. Meterlos en los repositorios los infla hasta
volverlos inmanejables.

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

### Estilo de código: claridad y facilidad de comprensión

Estas reglas son obligatorias para toda implementación futura, tanto de Claude Code como de Codex.

**1. Priorizar claridad sobre brevedad**

El código debe estar pensado para un estudiante con conocimientos básicos e intermedios de programación que necesita entender su funcionamiento.

No busques escribir la menor cantidad posible de líneas. Si una solución requiere algunas líneas adicionales pero resulta significativamente más fácil de entender, preferila.

Esto no significa duplicar código ni introducir complejidad innecesaria. Queremos código sencillo, correcto y mantenible.

**2. Utilizar estructuras de control explícitas**

- Preferir `if`, `else if` y `else` claramente escritos.
- Utilizar llaves `{ }`, incluso cuando un bloque tenga una sola instrucción.
- Evitar operadores ternarios (`condicion ? valor1 : valor2`) cuando un `if/else` sea más claro.
- Evitar expresiones `switch` compactas si un `switch` tradicional se entiende mejor.
- Evitar combinar múltiples condiciones complejas en una sola expresión difícil de leer.
- Evitar operadores de acceso condicional (`?.`) y coalescencia nula (`??`) cuando oculten decisiones relevantes o hagan menos claro el flujo. Se permiten cuando su significado es inmediato.
- Mantener los tipos anulables (`Usuario?`, `string?`, etc.) cuando correspondan. Son parte del sistema de tipos de C# y no deben eliminarse por preferencias de estilo.

**3. Nombres descriptivos**

Utilizar nombres de clases, métodos, propiedades y variables claros y descriptivos, preferentemente en español siguiendo las convenciones del proyecto.

Evitar abreviaturas poco evidentes. Preferir `cantidadAdministradoresActivos` antes que `cantAdmin`, y `usuarioEncontrado` antes que nombres genéricos.

**4. Métodos fáciles de seguir**

- Organizar el código en pasos lógicos.
- Separar validaciones, operaciones y resultados cuando mejore la comprensión.
- Evitar métodos excesivamente largos que mezclen responsabilidades.
- Evitar expresiones lambda complejas cuando existan alternativas más legibles.
- Evitar encadenamientos largos de métodos cuando dificulten comprender el resultado.
- No introducir patrones, abstracciones, clases auxiliares o librerías que la funcionalidad no necesite.
- Utilizar características normales de C#, EF Core y ASP.NET Core cuando sean apropiadas. No sacrificar seguridad, eficiencia o integridad de los datos por evitar una sintaxis que sea necesaria.

**5. Comentarios explicativos en español**

Los comentarios deben tener un lenguaje natural, claro y relativamente informal, parecido a una explicación entre compañeros de facultad.

Comentar principalmente:

- Por qué existe una validación importante.
- Qué regla de negocio se está aplicando.
- Qué hace una operación que no resulta evidente a primera vista.
- Por qué se eligió una solución técnica que requiere explicación.

No agregar comentarios por obligación a cada línea. Evitar comentarios excesivamente técnicos, ceremoniosos o que simplemente repitan el código.

No eliminar, resumir ni reescribir los comentarios explicativos existentes del estudiante por considerarlos largos o redundantes. Si un comentario contiene un error conceptual real, señalarlo y explicar la corrección antes de modificarlo.

**6. Código didáctico sin sacrificar calidad**

La simplicidad de escritura no debe comprometer:

- Seguridad y autorización.
- Validaciones del servidor.
- Integridad y consistencia de los datos.
- Manejo correcto de errores.
- Seguridad ante operaciones concurrentes.
- Separación de responsabilidades.
- Capacidad de realizar pruebas.
- Rendimiento razonable.

Cuando una solución técnicamente correcta requiera código menos sencillo, implementarla correctamente y agregar una explicación clara de cómo funciona.

**7. Explicar las implementaciones**

Al finalizar una tarea, explicar en términos sencillos:

- Qué se implementó.
- Qué archivos se modificaron y por qué.
- Cómo se relacionan las clases principales.
- Cuál es el recorrido de una operación.
- Qué validaciones se realizan.
- Qué pruebas se ejecutaron.

**8. Escribir directamente los roles en los atributos de autorización**

Cuando un Controller o una acción necesite restringir el acceso por rol, escribir los nombres
de los roles permitidos directamente en el atributo `[Authorize]`, sin crear una
constante auxiliar para agruparlos ni armar la cadena con `nameof`.

Ejemplo para una acción disponible para Administrador o Supervisor:

```csharp
[Authorize(Roles = "Administrador,Supervisor")]
```

- Usar exactamente los nombres definidos en `RolUsuario`: `Administrador`, `Supervisor`
  y `Operador`. Si se cambia el nombre de un rol, revisar también los atributos
  que lo mencionen, porque las cadenas de texto no se actualizan automáticamente.
- Separar los roles con comas significa que alcanza con tener **uno** de ellos.
- Para acciones que solo requieren una sesión iniciada, utilizar `[Authorize]`
  sin especificar `Roles`.
- Aplicar las restricciones correspondientes **en cada acción**, incluidos los métodos
  `GET` que muestran formularios restringidos y los métodos `POST` que procesan datos.
  Restringir la pantalla no sustituye la autorización de la operación en el servidor.
- Esta regla es una convención de legibilidad: nunca debe ampliar los permisos
  establecidos por los requerimientos ni sustituir las verificaciones de seguridad.

---

## Fuentes de verdad y trazabilidad

`AGENTS.md` establece las convenciones técnicas, la arquitectura y las instrucciones de trabajo para los asistentes. La especificación funcional proviene del anteproyecto corregido en Word. Los archivos Markdown de `docs/` son copias de trabajo de ese documento y deben mantenerse sincronizados con él.

La documentación ya está cargada en `docs/`. Para implementar una tarea, seguí este procedimiento:

1. Identificá la User Story (US), los requerimientos funcionales (RF), los no funcionales (RNF) y las restricciones de alcance relacionados.
2. Leé las **secciones aplicables** de `docs/user-stories.md`, `docs/requerimientos-funcionales.md`, `docs/requerimientos-no-funcionales.md` y `docs/alcance.md`. No es necesario cargar completos todos los documentos en cada tarea.
3. Inspeccioná el código y la configuración existentes para reutilizar lo ya implementado y evitar duplicaciones.
4. Respetá el alcance concreto de la tarea y sus dependencias. No adelantes tareas posteriores ni inventes requisitos o reglas.
5. Si detectás contradicciones entre documentación, instrucciones, pedido actual o código, **informalas antes de modificar el comportamiento**; no resuelvas diferencias de negocio por tu cuenta.
6. Después de implementar, explicá los archivos modificados, las pruebas realizadas y la trazabilidad entre tarea, US y RF/RNF.

Si cambia el anteproyecto original, actualizá también las copias Markdown correspondientes de `docs/` antes de tratarlas como vigentes.

El tablero de Trello `EOP - Sistema de Gestion` tiene cinco listas: Product Backlog, Sprint Backlog, In Progress, Testing, Done. Las tareas se nombran `S<sprint>-T<nn> [categoría] descripción (<horas>h)`.

---

## Horas estimadas — regla obligatoria para asistentes de IA

Las horas estimadas de las tareas son estimaciones internas del equipo y **deben ignorarse por completo**. No las busques, consultes, analices, interpretes ni menciones, salvo que el usuario lo solicite expresamente. Si aparecen incidentalmente en el título, la descripción o la documentación de una tarea, ignorá ese dato.

**Nunca utilices las horas estimadas** para planificar, dividir tareas, asignar duraciones a etapas, dimensionar, limitar, simplificar, recortar o ampliar el trabajo. Tampoco ajustes la implementación para que coincida con una estimación, ni repartas horas entre pasos por iniciativa propia.

La planificación y la implementación deben basarse en los requerimientos (RF/RNF), historias de usuario, criterios de aceptación, dependencias, estado real del código, complejidad técnica y buenas prácticas. **El alcance y la calidad determinan el trabajo necesario; las horas estimadas no intervienen en ninguna decisión.**

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

- Nombres definitivos de entidades y DbContext
- Convención de nombres de las pruebas
- Configuración de GitHub Actions
