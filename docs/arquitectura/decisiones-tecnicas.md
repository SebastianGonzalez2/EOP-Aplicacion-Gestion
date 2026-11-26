# Decisiones técnicas

Registro de las decisiones de diseño tomadas durante el desarrollo, con su motivo y las alternativas descartadas.

Alimenta la sección «Diseño final de la solución» del documento y la tarea de documentación de decisiones técnicas de cada sprint. Cada decisión debe poder explicarse en la defensa.

Las reglas vigentes que se derivan de estas decisiones están en [`AGENTS.md`](../../AGENTS.md); este archivo guarda el **porqué**.

---

## DT-01 — Organización del proyecto en capas

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión:** Dividimos la aplicación en tres proyectos principales: `Core`, `Infrastructure` y `WebApp`, además de un proyecto separado para las pruebas.

**Justificación:** Elegimos esta organización para separar las responsabilidades del sistema. En `Core` tenemos las reglas de negocio, en `Infrastructure` el acceso a datos y las integraciones, y en `WebApp` los controladores y las pantallas.

De esta manera, evitamos mezclar todo el código en un mismo lugar. También podemos probar la lógica de negocio sin depender directamente de la base de datos o de la interfaz.

La desventaja es que necesitamos más archivos y algunas configuraciones adicionales, pero consideramos que vale la pena para mantener el proyecto ordenado a medida que crece.

---

## DT-02 — Separación entre servicios de dominio y casos de uso

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión:** Dentro de `Core`, separamos las reglas de negocio reutilizables de las operaciones completas que realiza el usuario.

**Justificación:** Nos pareció importante distinguir entre una regla que puede utilizarse en diferentes funcionalidades y una operación concreta del sistema.

Por ejemplo, validar un RUT es una regla que pueden necesitar tanto Clientes como Proveedores. En cambio, registrar un cliente es una operación que reúne varias validaciones y finalmente guarda la información.

Esta separación evita repetir código y facilita las pruebas. También nos ayuda a mantener un criterio común cuando trabajamos los dos integrantes del equipo.

---

## DT-03 — Uso del patrón Repositorio para acceder a los datos

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión:** Los casos de uso acceden a los datos mediante interfaces de repositorio. Las implementaciones que utilizan Entity Framework Core quedan en `Infrastructure`.

Para los listados con filtros combinables utilizamos servicios de consulta específicos, también ubicados en `Infrastructure`.

**Justificación:** Queríamos que las reglas de negocio no dependieran directamente de cómo guardamos los datos.

Esto nos permite, por ejemplo, probar un caso de uso utilizando un repositorio de prueba, sin tener que conectarnos a SQL Server.

Para los listados elegimos un enfoque más directo porque suelen necesitar consultas y filtros particulares. Si agregáramos todos esos métodos a los repositorios, terminarían teniendo demasiadas responsabilidades.

Sabemos que esta organización requiere escribir algo más de código, pero nos permite mantener mejor separadas las capas.

---

## DT-04 — Reglas de validación del RUT uruguayo

**Fecha:** 04/11/2026 · **Sprint:** 1

**Decisión.** `ValidadorRut` exige 12 dígitos, prefijo entre 01 y 22, número de serie distinto de 000000, sufijo 001 y dígito verificador correcto. La misma clase valida el RUT de Clientes y de Proveedores.

**Motivo.** Los requerimientos piden validar la estructura uruguaya del RUT (RF-23, RF-86, RF-89, US-053, US-054), pero no explican cuáles son sus reglas. El validador anterior solo controlaba el largo y el dígito verificador, y por eso aceptaba números como `000000000000`.

**De dónde salen las reglas.**

| Regla | Fuente |
|---|---|
| 12 dígitos: prefijo de 2, serie de 6, sufijo de 3 y dígito verificador | DGI, División Informática, expediente 2024-5-1-0002222 |
| El prefijo es la Unidad Operativa de inscripción, la serie es secuencial y el sufijo es una constante | DGI, el mismo expediente |
| Prefijo entre 01 y 22, serie distinta de 000000 y sufijo 001 | Biblioteca pública `python-stdnum`, no es una fuente oficial |
| Factores 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 del dígito verificador | Biblioteca pública `python-stdnum`, no es una fuente oficial |

La DGI no publica los valores válidos de cada parte ni el algoritmo del dígito verificador: el Ministerio de Economía y Finanzas negó ese acceso en el mismo expediente por tratarse de información reservada, para que nadie pueda fabricar números en apariencia válidos. Por eso esos valores no se pueden confirmar con una fuente oficial.

Las reglas se probaron contra 203 RUT de la lista y los ejemplos de esa biblioteca, y los aceptó a todos. Esos RUT no se guardaron en el repositorio.

**Límites.**

- Que un RUT pase esta validación no prueba que exista ni que esté activo. Eso solo lo puede responder la DGI.
- Si la DGI empezara a usar un prefijo mayor que 22, habría que ampliar el rango, porque hoy esos RUT se rechazarían.

**Alternativas descartadas.**

- *Controlar solo el largo y el dígito verificador.* No cumple lo que piden los requerimientos sobre la estructura.
- *Limitar el prefijo a 01–21.* Rechazaría RUT que figuran como válidos en esa lista, como los que empiezan con 22.
- *Consultar el servicio de la DGI.* Es la única forma de saber si un RUT existe, pero es una integración externa que los requerimientos no piden.

**Fuentes.**

- [Expediente 2024-5-1-0002222, Ministerio de Economía y Finanzas](https://www.gub.uy/ministerio-economia-finanzas/sites/ministerio-economia-finanzas/files/2024-04/Expediente-2024-5-1-0002222%20Francisco%20Xavier.pdf)
- [Validación del RUT en `python-stdnum`](https://github.com/arthurdejong/python-stdnum/blob/master/stdnum/uy/rut.py)

---

## DT-05 — Autenticación mediante Microsoft Entra ID

**Sprint:** 1

**Decisión:** Utilizamos Microsoft Entra ID para autenticar a los usuarios, integrándolo mediante `Microsoft.Identity.Web` y OpenID Connect. Microsoft se encarga de la autenticación y del segundo factor, mientras que EOP administra los usuarios habilitados y sus roles.

**Justificación:** El uso de Entra ID ya estaba establecido en los requerimientos. Nuestra decisión fue cómo integrarlo con la aplicación y cómo relacionar las cuentas de Microsoft con los usuarios de EOP.

No desarrollamos un sistema propio de contraseñas, porque Microsoft ya proporciona los mecanismos necesarios para comprobar la identidad.

Cuando una persona ingresa por primera vez, EOP vincula su cuenta de Microsoft con el usuario que el Administrador registró previamente. Después, utiliza el identificador de Microsoft para reconocerlo, en lugar de depender de un correo electrónico que podría cambiar.

Así mantenemos separadas dos responsabilidades: Microsoft comprueba quién es la persona y EOP determina si puede acceder al sistema.

---

## DT-06 — Control de permisos desde el servidor

**Sprint:** 1

**Decisión:** Los permisos se controlan desde el servidor utilizando los tres roles fijos del sistema: Administrador, Supervisor y Operador.

Además, el sistema vuelve a comprobar el estado y el rol del usuario en cada solicitud autenticada.

**Justificación:** No alcanza con ocultar botones o restringir opciones en la interfaz, porque un usuario podría intentar acceder directamente a una dirección o enviar una solicitud al servidor.

Por eso, las acciones protegidas comprueban los permisos antes de ejecutarse.

También decidimos consultar el rol actual del usuario en cada solicitud. De esta manera, si un Administrador cambia el rol de alguien o desactiva su cuenta, el cambio se aplica sin depender de que esa persona cierre sesión y vuelva a entrar.

Esto implica realizar consultas adicionales a la base de datos, pero lo consideramos adecuado para mantener actualizados los permisos.

---

## DT-07 — Persistencia con Entity Framework Core, SQL Server y migraciones

**Sprint:** 1

**Decisión:** Utilizamos Entity Framework Core con el enfoque *Code First*, SQL Server como base de datos y migraciones para administrar los cambios del esquema.

**Justificación:** Estas tecnologías forman parte del stack definido para el proyecto. Optamos por organizar las entidades y sus configuraciones en código, y utilizar migraciones para mantener actualizada la estructura de la base de datos.

Esto permite que los cambios de las tablas queden registrados junto con el código y puedan revisarse antes de incorporarlos.

También facilita que los dos integrantes trabajemos sobre una misma estructura y podamos identificar qué cambios se hicieron.

La principal precaución es revisar correctamente las migraciones, especialmente cuando modifican tablas que ya contienen información.

---

## DT-08 — Validaciones en la aplicación y restricciones en la base de datos

**Sprint:** 1

**Decisión:** Además de validar los datos desde la aplicación, utilizamos restricciones de SQL Server para proteger la integridad de la información.

**Justificación:** Aunque los casos de uso validen los datos antes de guardarlos, también queremos evitar que la base acepte información incorrecta si alguna validación se omite o aparece un error en el código.

Por eso utilizamos restricciones para cuestiones importantes, como evitar valores duplicados, impedir roles inválidos y exigir determinados datos obligatorios.

Esto también ayuda cuando dos solicitudes intentan registrar el mismo dato simultáneamente: la base de datos es la que garantiza que no se guarden duplicados.

La desventaja es que algunas reglas deben mantenerse tanto en el código como en la base, por lo que tenemos que asegurarnos de que sean coherentes.

---

## DT-09 — Protección del último Administrador activo ante operaciones simultáneas

**Sprint:** 1

**Decisión:** Utilizamos transacciones y mecanismos de bloqueo de SQL Server para evitar que el sistema quede sin administradores activos, incluso cuando se intentan realizar varios cambios de rol simultáneamente.

**Justificación:** No alcanza con comprobar que exista más de un Administrador antes de modificar un rol.

Por ejemplo, si existen dos administradores y ambos intentan cambiar su rol al mismo tiempo, las dos solicitudes podrían comprobar que hay otro Administrador disponible y permitir los cambios. El resultado sería un sistema sin administradores.

Para evitarlo, hacemos que estas operaciones se coordinen desde la base de datos. Así, una solicitud espera a que termine la otra y puede comprobar la situación actual antes de continuar.

Es una solución más compleja que hacer una consulta normal, pero protege una regla importante del sistema.

---

## DT-10 — Pruebas con bases de datos de prueba y SQL Server real

**Sprint:** 1

**Decisión:** Para probar la persistencia utilizamos SQLite en memoria en muchas de las pruebas de integración, y SQL Server real en las pruebas que dependen de comportamientos específicos de ese motor.

**Justificación:** No todas las pruebas necesitan levantar una base de datos SQL Server completa. Para comprobar operaciones comunes, una base SQLite en memoria nos permite ejecutar pruebas de forma más sencilla y aislada.

Sin embargo, SQLite no se comporta exactamente igual que SQL Server. Por ejemplo, existen diferencias en los bloqueos, ciertas restricciones y las migraciones.

Por eso, cuando necesitamos comprobar esos comportamientos, ejecutamos pruebas contra SQL Server.

De esta manera combinamos pruebas prácticas para el desarrollo cotidiano con otras que verifican cómo funcionará realmente la persistencia del sistema.

---

## DT-11 — Precios actuales en el catálogo y valores históricos en el presupuesto

**Fecha:** 14/11/2026 · **Sprint:** 2

**Decisión:** El precio, la moneda y la fecha de actualización no se guardan en `Material`, sino en `MaterialProveedor`, que tiene un registro por cada combinación de material y proveedor. Ese registro guarda solamente el precio **actual**. Cuando un concepto de presupuesto use una referencia, va a **copiar** el precio y la moneda a sus propias columnas, en lugar de leerlos del catálogo cada vez.

**Ampliación aprobada por EOP (14/11/2026):** el concepto no copia solo el precio y la moneda. Cuando el usuario selecciona expresamente una referencia de material y proveedor, el concepto conserva **cuatro datos históricos**: el nombre del material, la unidad de medida, el precio unitario y la moneda (ver «Decisión aprobada con EOP: datos históricos completos del concepto»).

En la revisión del modelo de datos de S2-T29 comprobamos que el esquema actual cumple estas reglas, por lo que lo mantuvimos sin cambios.

**Justificación:** Un mismo material se le puede comprar a varios proveedores, y cada uno tiene su propio precio, moneda y fecha de actualización (US-040, RF-19). Si el precio estuviera en `Material`, solo podríamos guardar uno. Por eso es una relación muchos a muchos, resuelta con una tabla intermedia que además tiene datos propios.

El índice único sobre `(MaterialId, ProveedorId)` impide registrar dos veces la misma combinación (RF-22, US-010B), incluso cuando dos usuarios lo intentan al mismo tiempo. Las restricciones CHECK exigen un precio mayor que cero y la moneda UYU o USD.

El catálogo no guarda un historial de precios, porque RF-21 no lo exige. La historia queda en cada presupuesto: al seleccionar una referencia se copian su precio y su moneda (RF-20, US-010A). Si el concepto leyera el precio del catálogo, actualizar una referencia cambiaría presupuestos ya enviados o aprobados, que es justamente lo que prohíben RF-20, RF-21 y US-071.

Todas las claves foráneas de estas tablas usan `Restrict`. Ningún requerimiento permite eliminar materiales, proveedores ni sus relaciones, y con `Cascade`, borrar un proveedor se llevaría sin aviso sus cuentas bancarias y sus referencias de precio.

Hay dos reglas que la base no puede controlar por su cuenta, porque una restricción CHECK solo mira su propia fila: que todo proveedor tenga al menos una cuenta bancaria y que todo material tenga al menos una referencia de precio. Las controlan los casos de uso, y el material se guarda junto con su primera referencia en una única transacción.

**Alternativas descartadas:**

- Guardar precio y moneda en `Material`: no permite que un mismo material tenga distintos proveedores.
- Que el concepto del presupuesto lea el precio desde `MaterialProveedor`: los presupuestos cambiarían cada vez que se actualiza el catálogo.
- Guardar un historial de precios en el catálogo: RF-21 no lo pide y, aun así, cada presupuesto necesitaría saber qué precio usó.
- Conservar en el concepto solamente el precio y la moneda (la opción B de la consulta a EOP): US-071 permite cambiar el nombre y la unidad de un material, así que un presupuesto ya enviado o aprobado mostraría el nombre y la unidad nuevos junto a un precio calculado con los anteriores. EOP eligió la opción A.

**Decisión aprobada con EOP: datos históricos completos del concepto**

Durante S2-T29 quedó pendiente consultar con EOP si el concepto debía conservar solo el precio y la moneda, o también el nombre y la unidad del material. EOP ya respondió y aprobó la **opción A**. No requiere otra consulta.

Cuando el usuario seleccione **expresamente** una referencia de material y proveedor para un concepto del presupuesto, el sistema copia al concepto:

1. el nombre del material en ese momento;
2. la unidad de medida utilizada en ese momento;
3. el precio unitario de la referencia seleccionada;
4. la moneda de ese precio.

Estos valores pertenecen al concepto. Cambiar después el nombre, la unidad, el precio o la moneda en el catálogo (US-071, RF-21) **no actualiza** los conceptos ya guardados. Ejemplo: un presupuesto con «Cable eléctrico 2,5 mm²», unidad Metro, cantidad 10 y precio unitario USD 3 (subtotal USD 30) conserva exactamente esos datos aunque el catálogo cambie el nombre, la unidad o el precio del material.

Las reglas de RF-20 y US-010A se mantienen sin cambios: la selección es siempre manual y expresa (incluso cuando hay un solo proveedor), los valores copiados no se editan desde la tabla del presupuesto y una misma combinación material-proveedor no puede repetirse dentro del mismo presupuesto. RF-20 y US-010A nombraban solo el precio y la moneda: esta decisión los amplió, y el anteproyecto en Word y las copias de `docs/` ya se ajustaron para que coincidan (S2-T30).

**Estado de la implementación.** Al 18/11/2026, S3-T05 implementó el modelo y la persistencia de `ConceptoPresupuesto`, `TipoConcepto` y `MargenBaseTipo` (US-011): entidades, configuraciones de EF Core, la migración `S3_T05_CrearConceptosPresupuesto`, sus índices y sus restricciones CHECK. Lo que todavía no existe es el caso de uso que copia los cuatro datos al seleccionar la referencia ni las vistas del presupuesto, que corresponden a US-010A, US-011 y US-012.

**Qué comprobar cuando se implemente `ConceptoPresupuesto`:**

- La unidad copiada usa el mismo tipo y la misma restricción que `Material` (`UnidadMedida`, las 14 opciones fijas), y el nombre copiado admite como mínimo los 200 caracteres del nombre del material.
- El precio copiado usa `decimal(28,10)` y la moneda tiene el mismo CHECK (UYU o USD) que el catálogo.
- Al seleccionar la referencia se copian los cuatro datos en la misma operación, y después se leen siempre desde el concepto, nunca desde `Material` ni `MaterialProveedor`.
- Prueba de regresión con SQL Server: guardar un concepto, modificar en el catálogo el nombre, la unidad, el precio y la moneda, y comprobar que el concepto conserva los cuatro valores originales y su subtotal.
- Prueba de que los listados y vistas del presupuesto muestran los valores del concepto y no los actuales del catálogo.
- Lo mismo para todo lo que se arma a partir del concepto: el cálculo del subtotal (RF-28), el resumen previo (RF-31, US-013), la planilla Excel (RF-37, RF-38, US-014) y los indicadores del proyecto que salen del presupuesto vinculado (RF-103). Ninguno puede leer nombre, unidad, precio ni moneda de `Material` o `MaterialProveedor`. Se agregó en S2-T30: la lista anterior solo nombraba «los listados y vistas».

Con S3-T05 ya quedaron cubiertos por `ConfiguracionConceptosPresupuestoTests` y `ConceptosPresupuestoSqlServerTests` el tipo y la restricción de la unidad, los 200 caracteres del nombre, el precio `decimal(28,10)`, el CHECK de moneda y el índice único filtrado `(PresupuestoId, MaterialProveedorId)`. Siguen pendientes para US-010A, US-011 y US-012 la copia de los cuatro datos en una misma operación y las pruebas de que los cálculos, listados y vistas leen del concepto y no del catálogo.

**Para tener en cuenta en tareas posteriores:**

- El precio copiado al concepto tiene que usar la misma precisión que el catálogo (`decimal(28,10)`) para no perder decimales.
- La regla de que una combinación material-proveedor no se repita dentro de un presupuesto (RF-20) se puede garantizar con un índice único **filtrado**. Si el concepto apunta a `MaterialProveedorId`, como plantea el MER, alcanza con `(PresupuestoId, MaterialProveedorId) WHERE MaterialProveedorId IS NOT NULL`, porque cada `MaterialProveedor` ya es una combinación única. El filtro hace falta porque, en un índice único, SQL Server trata los NULL como valores iguales: sin él no podría haber dos filas de mano de obra en el mismo presupuesto. La regla de un único Margen por presupuesto se resuelve de la misma forma.
- La consulta a EOP sobre conservar también la unidad y el nombre del material ya está resuelta: se conservan (ver «Decisión aprobada con EOP» más arriba). El MER ya prevé una unidad propia en `ConceptoPresupuesto`, y ahora queda aclarado que se copia del material al seleccionar la referencia.
- En la revisión también detectamos que las fechas se guardaban en UTC y se mostraban y comparaban sin pasarlas a la hora de Uruguay (UTC−3). Por ejemplo, un presupuesto creado a las 22:00 recibía en su código la fecha del día siguiente. No era un problema del esquema y se corrigió en una tarea aparte (FIX-UTC-FECHAS): las fechas siguen guardándose en UTC, y `ZonaHorariaUruguay` (en `Core/Comun`) las convierte para el código `PRES-AAAAMMDD-...`, para mostrarlas y para interpretar el día elegido en los filtros. Cualquier fecha nueva que se muestre o se filtre por día debe pasar por esa clase.

**Comparación con el MER del anteproyecto (sección 2.9.2):**

1. **Qué verificamos.** Comparamos las entidades, las claves y las relaciones del MER con lo implementado en Proveedor, CuentaBancaria, Material, MaterialProveedor, Presupuesto, TipoProyecto, Cliente y Usuario. Coinciden las relaciones principales:
   - Proveedor 1 → N CuentaBancaria;
   - Material N ↔ N Proveedor a través de `MaterialProveedor`, con precio, moneda y fecha de actualización;
   - Presupuesto con claves foráneas a Cliente, TipoProyecto y Usuario Responsable.

   El MER también confirma esta decisión: `ConceptoPresupuesto` tiene sus propios `PrecioUnitario` y `Moneda`, además de las referencias a `MaterialId` y `MaterialProveedorId`.
2. **Diferencias y su justificación.** Son una evolución del diseño preliminar, no errores:
   - Las unidades de medida son el enum `UnidadMedida` y no la tabla `Unidad`, porque son 14 opciones fijas validadas con EOP (RF-68) y su administración quedó fuera del alcance (US-063 a US-065).
   - Moneda, Banco, Estado y Rol se guardan como números con restricciones CHECK, en vez de texto libre, para que la base rechace valores inválidos (DT-08).
   - Agregamos `NombreNormalizado` en Material y Proveedor, y `NumeroCorrelativo` en Presupuesto, para que los índices únicos garanticen los nombres sin duplicados (RF-68, US-009) y el correlativo global del código (RF-01).
   - Los nombres de texto tienen 200 caracteres y no 256, por el criterio común de S1-T27.
   - Hay diferencias de nombres sin efecto en el modelo: `NombreProyecto` por `NombreTrabajo`, `ResponsableId` por `ResponsableUsuarioId`, `CuentasBancarias` por `CuentaBancariaProveedor` y `Id` por `<Entidad>Id`.
3. **Qué sigue pendiente.** Corresponde a funcionalidades de otros sprints:
   - `ConceptoPresupuesto`, `TipoConcepto` y `MargenBaseTipo`: el modelo y la persistencia ya están (S3-T05); faltan los casos de uso y las vistas de US-010A, US-011 y US-012;
   - `Relevamiento`;
   - los datos de envío, vencimiento, rechazo y confirmación económica del Presupuesto;
   - `Proyecto`, `ProyectoParticipante`, `Tarea` y `DocumentoProyecto` con `TipoDocumento`;
   - `CostoAdicional`, `FacturaCompra` y `FacturaVenta`.

   Cuando se implemente la relación Presupuesto–Proyecto, que en el MER es la clave foránea `ProyectoId` dentro de Presupuesto, la regla de "como máximo un presupuesto por proyecto y viceversa" va a necesitar un índice único filtrado sobre `ProyectoId`.
4. **Problemas detectados.** No encontramos errores en el esquema ni en lo ya implementado. El único problema detectado fue el manejo de fechas en UTC mencionado arriba, que se corrigió en FIX-UTC-FECHAS.

**Revisión de reglas y dependencias (S2-T30, 14/11/2026)**

S2-T30 volvió sobre lo anterior con otra pregunta: ¿las reglas compartidas se cumplen igual en todos lados, se controlan en el servidor y concuerdan con los requerimientos? Fue un análisis; no se modificó código. Se corrió toda la suite contra SQL Server real: 1434 pruebas, 0 fallidas y 0 omitidas.

*Qué se verificó y dónde está la evidencia:*

| Regla | Dónde se cumple | Prueba que lo respalda |
|---|---|---|
| El catálogo guarda solo el precio actual por material-proveedor | `MaterialProveedor` (sin historial, ninguna otra tabla de precios) | `RelacionMaterialProveedorSqlServerTests` |
| Una combinación material-proveedor no se repite | Consulta previa en `AgregarProveedorAMaterial` + índice único `(MaterialId, ProveedorId)` + `IndicesUnicos` | `AgregarProveedorAMaterialTests`, `Base_DebeRechazarDosRelaciones…` |
| Precio > 0, con máximo de 18 enteros y 10 decimales, y moneda UYU o USD | `ValidadorPrecioUnitario` y `Enum.IsDefined` en los tres casos de uso, repetidos en los tres ViewModels; CHECK y `decimal(28,10)` en la base | S2-T22 y `RelacionMaterialProveedorSqlServerTests` |
| Catálogo y presupuestos nunca se borran físicamente | Todas las claves foráneas son `Restrict` | `Base_NoDebePermitirBorrar…` |
| Código `PRES-AAAAMMDD-NOMBRE-####` generado por el sistema, único, global y que no cambia | `GeneradorCodigoPresupuesto` + `SecuenciaPresupuestos` (1 a 9999, sin ciclo) + índices únicos de `Codigo` y `NumeroCorrelativo` | `PresupuestosSqlServerTests` (correlativo global, altas simultáneas, código estable, agotamiento en 9999) |
| El día del código y de los filtros es el de Uruguay | `ZonaHorariaUruguay` en el generador, en `ConsultarPresupuestos` y en las tres vistas que muestran fechas | `ZonaHorariaUruguayTests`, `FiltrarPresupuestosTests`, `CercaDeLaMedianoche_…` |
| Permisos de Presupuestos, Materiales y Proveedores controlados en el servidor | `[Authorize(Roles = …)]` en cada acción GET y POST, con token antifalsificación en los POST | `MatrizDeAutorizacionTests` (coincide con RF-73) |
| Nombre de material único y obligatorio, unidad de la lista fija | `RegistrarMaterial` y `EditarMaterial` + índice único + CHECK | `UnidadesMaterialSqlServerTests`, `DuplicadosMaterialesSqlServerTests` |
| Proveedor: nombre y RUT únicos, RUT válido, teléfono, un correo como mínimo y cuenta bancaria completa | `ValidarDatosProveedor` + índices únicos + CHECK | `RegistroValidarDatosProveedorTests`, `RegistrarProveedorSqlServerTests` |

*Sobre FIX-UTC-FECHAS:* no quedó ningún lugar sin convertir. Los únicos tipos de fecha del sistema son `Presupuesto.FechaCreacion` y `MaterialProveedor.FechaUltimaActualizacion`; las dos se guardan en UTC, y todo lo que las muestra, filtra o usa para el código pasa por `ZonaHorariaUruguay`. No se duplicó nada.

*Dependencias con sprints posteriores:*

- **US-007, US-010A y US-011** crean `ConceptoPresupuesto` y copian los cuatro datos. Necesitan lo que ya existe: `UnidadMedida`, `Moneda`, el precio `decimal(28,10)` y el índice único del catálogo.
- **RF-28, RF-31, RF-37 y RF-103** (subtotal, resumen, Excel e indicadores del proyecto) dependen de que el concepto sea la única fuente de esos valores. Se agregaron a la lista de comprobaciones de arriba.
- **RF-34 y RF-36** (envío y vencimiento) usan el plazo de vigencia que hoy guarda el presupuesto. Ver el segundo punto de «Aspectos sin definir».
- **RF-102 y RF-109** (agregar cuenta bancaria y editar proveedor) van a reutilizar `ValidadorCuentaBancaria` y las reglas de `ValidarDatosProveedor`. La comprobación de duplicados de hoy no excluye al propio proveedor, así que para la edición hará falta una variante «excepto este Id», como la que ya usa `EditarMaterial` con los materiales.
- **RF-53** (crear presupuesto desde un proyecto) y la regla de un presupuesto por proyecto necesitan el índice único filtrado sobre `ProyectoId` ya descrito arriba.

*Inconsistencias en la documentación:*

1. **RF-020 y US-010A nombraban solo el precio y la moneda**, mientras que DT-11 y la decisión aprobada por EOP son cuatro datos. Era la única contradicción de fondo. EOP ya corrigió el anteproyecto en Word, y en S2-T30 se llevó el mismo cambio a las copias de `docs/`: el título, la descripción y las reglas de conservación y de no edición de RF-020 (`requerimientos-funcionales.md`); el PARA, la descripción, las reglas y tres escenarios de US-010A (`user-stories.md`); y la fila de US-010A en `alcance.md`.
2. **RF-021, RF-108, RF-019, US-071 y RF-31 no se contradicen.** Hablan de «precios y monedas» o de «valores históricos» en general, y RF-108 ya dice «ni otros valores históricos». Quedan completos con lo de arriba, aunque conviene que RF-021 («conservarán precios y monedas históricos») se lea también como nombre y unidad.
3. **RF-020 y RF-28 se leen distinto sin chocar.** RF-28 habla de «una modificación del precio utilizado», pero RF-020 prohíbe editarlo desde la tabla. Es consistente porque la única forma de cambiarlo es una nueva selección expresa de la referencia (RF-020, último párrafo de la descripción). Esa nueva selección vuelve a copiar los cuatro datos; hoy lo dice DT-11 de forma implícita («al seleccionar… se copian»).

*Aspectos sin definir en los requerimientos.* Se anotan para consultarlos cuando se implementen; no se inventó ninguna regla:

1. Qué pasa con los cuatro valores copiados cuando el usuario cambia el material de una fila que ya tenía una referencia seleccionada (RF-013 permite sustituir la selección mientras el presupuesto sea editable). Lo razonable sería descartarlos y exigir una nueva selección, pero ningún requerimiento lo dice. También falta definir si una fila con material y sin referencia seleccionada puede guardarse y qué subtotal tendría.
2. El plazo de vigencia no tiene máximo (hoy acepta hasta 2.147.483.647 días). Al implementar RF-34 y RF-36, «fecha de envío más plazo» daría una fecha imposible y una excepción si el plazo es enorme. Hace falta un tope técnico, parecido al que S2-T22 puso a los precios, y conviene confirmarlo con EOP.
3. Qué debe pasar cuando se agoten los correlativos del 0001 al 9999 (RF-01 exige cuatro cifras). Hoy SQL Server rechaza el siguiente número con un error y la aplicación lo muestra con la pantalla de error general; hay una prueba que lo documenta.
4. Dos personas que editan a la vez el mismo material: gana el último que guarda y los campos que el otro cambió se pisan sin aviso. Ningún requerimiento pide control de concurrencia para el catálogo (solo DT-09 lo pide, para los administradores).

*Observaciones menores, sin cambios:*

- `ValidarDatosProveedor` no controla los largos máximos (nombre 200, cuenta 50, titular 200, correos 254). Los controla el ViewModel del lado del servidor y, en último lugar, la base, así que hoy no se puede guardar un dato mal. Pero Materiales y Presupuestos sí lo hacen dentro del caso de uso. Cuando RF-102 y RF-109 reutilicen estas reglas desde otra pantalla, conviene mover esos largos al caso de uso para que valgan para cualquier entrada.
- El nombre del proveedor se guarda tal como se escribió, con los espacios de los extremos, mientras que Material y Presupuesto lo guardan sin ellos (criterio de S1-T27). La comparación de duplicados no se ve afectada porque usa el nombre normalizado.
- `RegistrarMaterial`, `AgregarProveedorAMaterial` y `EditarMaterial` toman la hora con `DateTime.UtcNow`, mientras `RegistrarPresupuesto` usa el reloj inyectable (`TimeProvider`). Es correcto, porque en ambos casos queda en UTC, pero esas fechas no se pueden fijar en las pruebas. Los casos de uso nuevos con fechas deberían usar `TimeProvider`.

*Conclusión.* El diseño y lo implementado cumplen DT-11, los requerimientos de catálogo y presupuesto y las reglas transversales de `AGENTS.md`. No hay nada que corregir en el código para cerrar S2-T30. Las copias de RF-020, US-010A y `alcance.md` ya quedaron alineadas con el Word. Lo que queda es llevar los cuatro puntos sin definir a las tareas que implementen `ConceptoPresupuesto` y el envío del presupuesto.

---

## DT-12 — Consulta bajo demanda y caché en memoria de las cotizaciones USD/UYU

**Fecha:** 25/11/2026 · **Sprint:** 3 · **Tarea:** S3-T18 (US-012)

**Qué ya estaba decidido y no se decide acá.** Las reglas de negocio de la cotización vienen de los requerimientos y no cambian con esta decisión: la fuente es el BCU a través de datosUruguay, en UYU/USD y mayor que cero; si un día no hay publicación se usa la última válida anterior, nunca una posterior, un promedio ni una estimación; y una falla temporal del servicio no equivale a ausencia de publicación (RF-029, RF-077, RF-103, RF-104, RNF-013). Qué se guarda al confirmar un presupuesto o registrar una factura o un costo está en RF-029, RF-031 y RF-103 y en el MER (`CotizacionConfirmada`, `FechaCotizacionConfirmada`, `CotizacionAplicada`, `FechaCotizacionAplicada`), sin tabla independiente de cotizaciones. La viabilidad de la API se comprobó en la [prueba de concepto S3-T03](../pruebas-concepto/S3-T03-datosuruguay.md), y las reglas de cálculo se revisaron en [S3-T25](revision-reglas-calculo-cotizacion.md). DT-12 solo decide **cómo y cuándo** se consulta el proveedor y cuánto tiempo se reutiliza una respuesta.

**Decisión.**

1. **Consulta bajo demanda.** El servicio (`ServicioCotizacionesDatosUruguay`) llama a la API solamente cuando un módulo pide una cotización que no está en una entrada vigente de la caché. No hay procesos programados, tareas en segundo plano ni consultas periódicas.
2. **Caché en memoria con `IMemoryCache`.** Las cotizaciones obtenidas se guardan en la caché en memoria de ASP.NET Core. El servicio se registra como Singleton y la caché es única por instancia de la aplicación, así que la comparten todos los usuarios: si un usuario ya pidió la cotización de hoy, el siguiente la recibe sin otra consulta.
3. **Expiración absoluta.** 15 minutos para la cotización vigente (la fecha pedida es hoy en Uruguay) y 24 horas para las históricas (la fecha pedida es anterior a hoy). Es absoluta y no deslizante: una entrada vence a ese plazo desde que se guardó, aunque se siga usando.
4. **Clave por fecha solicitada.** La entrada se identifica por la fecha pedida (`cotizacion-usd:AAAA-MM-DD`). El valor guardado es la `CotizacionUsd` completa, que conserva por separado la fecha efectiva de publicación; así, el sábado y el domingo tienen entradas propias aunque los dos usen la publicación del viernes, y quien consume el servicio siempre sabe qué fecha tiene el valor que usa.
5. **Una sola consulta por fecha ante pedidos simultáneos.** Cada fecha tiene un turno (`SemaphoreSlim`). Si dos pedidos de la misma fecha llegan sin la entrada en caché, el primero consulta y el segundo espera; al entrar vuelve a mirar la caché y la encuentra. Pedidos de fechas distintas no se esperan entre sí. La espera respeta la cancelación del pedido.
6. **Solo se guardan cotizaciones válidas.** Una falla del proveedor (`CotizacionNoDisponibleException`), una respuesta inválida o la ausencia de publicación (`null`) no se guardan: la siguiente consulta vuelve a preguntarle al proveedor.

**Comportamiento en situaciones particulares.**

- *Cuando una entrada vence* no pasa nada: la caché deja de considerarla, y recién la próxima vez que un módulo necesite esa fecha se consulta la API.
- *Cuando la aplicación se reinicia* (despliegue, reciclado del App Service) la caché arranca vacía y las primeras consultas de cada fecha van a la API. No se pierde nada, porque la caché nunca es la fuente de los valores aplicados (ver más abajo).
- *Cuando el proveedor falla*: si hay una entrada vigente se sigue usando hasta que vence, porque es una cotización válida obtenida hace menos de su plazo; si no la hay o ya venció, el servicio informa la falla. **Nunca se usa una entrada vencida para ocultar una falla del proveedor**: `MemoryCache` no devuelve entradas vencidas y el servicio no guarda copias aparte.

**Caché temporal frente a conservación permanente.** La caché es solamente una forma de no repetir consultas; puede vaciarse en cualquier momento y no es un registro. La cotización que se aplica a una operación económica se conserva en la base, en la propia entidad, cuando la tarea responsable la confirma o la registra: al confirmar los datos económicos del presupuesto (RF-029, RF-031) y al guardar una factura o un costo adicional (RF-077, RF-103, RF-104). Desde ese momento manda el valor guardado, aunque datosUruguay esté caído o la caché tenga otro valor. S3-T18 no implementa esa persistencia: deja el servicio listo y devuelve el valor y la fecha efectiva que esas tareas deberán guardar.

**Motivo.**

- *Bajo demanda:* la cotización solo hace falta cuando alguien trabaja en un presupuesto, una factura o un costo. Un proceso periódico consultaría aunque nadie la use, sumaría una pieza más para mantener y probar, y AGENTS.md ya excluye los procesos programados para otros casos parecidos (vencimiento de presupuestos). Además, el proveedor limita las consultas por IP (20 cada 10 segundos y 300 por hora).
- *`IMemoryCache`:* viene con ASP.NET Core, no necesita servidores ni configuración, es segura para usar desde varios hilos y maneja sola la expiración. Los datos que guarda son pocos (un valor y dos fechas por día consultado), públicos y fáciles de volver a obtener.
- *15 minutos para la vigente:* durante el día el BCU todavía puede publicar el valor de hoy (en la prueba real del 25/11/2026, la publicación del 24/11 apareció ese mismo día a las 17:11), y RF-030 pide recalcular cuando cambia la cotización actual. Un plazo corto permite tomar la nueva publicación sin consultar en cada recálculo. Coincide además con los 15 minutos que el propio proveedor indica en `Cache-Control`.
- *24 horas para las históricas:* la publicación de un día que ya pasó no debería cambiar, así que puede reutilizarse más tiempo; aun así vence, por si el proveedor corrigiera un dato.
- *Turno por fecha:* sin él, varios usuarios que abren presupuestos al mismo tiempo dispararían la misma consulta varias veces, gastando el límite del proveedor. `GetOrCreate` de `IMemoryCache` no lo evita, porque puede ejecutar la función de creación más de una vez en paralelo.
- *Sin Redis ni otra infraestructura:* EOP es un monolito que corre en un único App Service. Una caché distribuida solo aporta cuando hay varias instancias que deban compartirla, y agregaría un servicio de Azure más para configurar, pagar, proteger y mantener disponible, que además podría fallar. Para datos públicos que se vuelven a obtener en una consulta, no se justifica.

**Limitaciones.**

- La caché es por instancia: si algún día se escalara a varias instancias, cada una tendría la suya y podría consultar al proveedor por separado. Las reglas de negocio no se ven afectadas, solo la cantidad de consultas.
- Si se pide la cotización de un día anterior antes de que el proveedor la haya publicado, se devuelve la publicación previa y queda hasta 24 horas en caché aunque mientras tanto se publique la de ese día. Está detallado como decisión pendiente en [`s3-t18-servicio-cotizaciones.md`](../sprints/sprint-3/s3-t18-servicio-cotizaciones.md), sección 9.
- El diccionario de turnos conserva un `SemaphoreSlim` por cada fecha consultada mientras la aplicación está en marcha. Para el volumen de EOP es despreciable.
- La búsqueda hacia atrás tiene un tope de 10 páginas por consulta; si se alcanza sin encontrar una cotización válida, se informa como falla (`BusquedaIncompleta`) y no como ausencia de publicación.

**Alternativas descartadas.**

- *Actualización programada* (por ejemplo, un servicio en segundo plano que consulte cada X minutos): consulta aunque nadie la necesite y agrega una pieza más, sin resolver las cotizaciones históricas de fechas arbitrarias.
- *Sin caché:* cada recálculo del presupuesto consultaría la API, sumando demora y acercándose al límite de solicitudes; ante una caída momentánea fallarían todos los recálculos.
- *Guardar las cotizaciones consultadas en una tabla de la base:* el MER no prevé una tabla de cotizaciones (RF-103 lo excluye expresamente) y mezclaría datos temporales con los valores aplicados, que pertenecen a cada operación.
- *Caché distribuida (Redis, `IDistributedCache`):* ver el motivo; no aporta con una sola instancia.
- *Expiración deslizante:* una cotización consultada seguido no vencería nunca y la vigente no tomaría la nueva publicación del día.
- *Usar la última entrada conocida cuando el proveedor falla:* ocultaría la falla y podría aplicar una cotización de otra fecha, que RF-103 prohíbe.

**Dónde está.** `src/EOP.Gestion.Infrastructure/Integraciones/DatosUruguay/` (`ServicioCotizacionesDatosUruguay`, `BuscadorCotizacionesDatosUruguay`, `ClienteDatosUruguay`), el registro en `InyeccionDeDependencias` y las pruebas `ServicioCotizacionesDatosUruguayTests` (caché, expiración, aislamiento entre fechas y concurrencia).

---

<!--
Formato para las próximas:

## DT-NN — Título

**Fecha:** dd/mm/aaaa · **Sprint:** N

**Decisión.**

**Motivo.**

**Alternativas descartadas.**
-->

---

# Nuevo requerimiento no funcional incorporado

Registro del requerimiento surgido durante el desarrollo del Sprint 1. Ya se incorporó al anteproyecto y a [`requerimientos-no-funcionales.md`](../requerimientos/requerimientos-no-funcionales.md) como RNF-021. Se conserva aquí el contexto de su origen y su relación con DT-04.

## RNF-021 — Validación completa del RUT uruguayo (Sprint 1)

**Descripción**

El sistema deberá validar en el servidor todo RUT que se registre o modifique, comprobando su estructura uruguaya y su dígito verificador antes de guardarlo. La validación será la misma en todos los módulos que reciban un RUT.

**Reglas**

1. Antes de validar se quitan los separadores habituales: puntos, guiones y espacios.
2. El RUT debe tener exactamente 12 dígitos.
3. Los dos primeros dígitos (prefijo) deben estar entre 01 y 22.
4. Los seis dígitos siguientes (número de serie) no pueden ser todos cero.
5. Los tres dígitos siguientes (sufijo) deben ser 001.
6. El último dígito debe coincidir con el dígito verificador calculado a partir de los 11 anteriores.
7. Un RUT que no cumpla alguna de estas reglas se rechaza, con un mensaje claro y sin guardar datos.

Tipo: Integridad de datos.

Referencias: RF-23, RF-86, RF-89, RF-109, US-009, US-053, US-054, US-072, RNF-10, RNF-18, DT-04.
