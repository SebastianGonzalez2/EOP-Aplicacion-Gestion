# S3-T28 [QA] — Pruebas de materiales y precios

**Sprint:** 3 — Elaboración económica de presupuestos

**Requerimientos:** US-007 · US-010A · RF-19, RF-20 · DT-11 · RNF-4, RNF-10, RNF-14

**Base probada:** `main` en `ac0b87d` · **Fecha:** 27/11/2026

## 1. Resultado

No se encontraron defectos funcionales en el alcance de la tarea. **No se modificó código de producción**:
solo se agregaron pruebas y este documento. No se tocaron cálculos económicos ni nada de S3-T20.

Quedan observaciones menores (sección 7) y verificaciones pendientes (sección 8).

## 2. Criterios de aceptación verificados

«Existente» = ya había una prueba antes de esta tarea. «Nueva» = agregada en S3-T28. «Manual» = revisada en el
navegador (sección 6).

| Origen | Criterio | Estado | Evidencia |
|---|---|---|---|
| US-007 | Material identificado sin asociación automática | Cumple, con límite: el análisis con IA (US-006) no existe todavía y la fila detectada se simuló | Existente, manual |
| US-007 | Búsqueda y selección manual | Cumple | Existente, manual |
| US-007 | Sustituir la selección | Cumple | Existente, nueva (fila sin guardar) |
| US-007 | Sin material en el catálogo: no se crea | Cumple | Existente, manual |
| US-007 | Sin permisos o presupuesto no editable: el servidor rechaza | Cumple | Existente, nueva (Vencido y estados editables), manual |
| US-010A / RF-19 | Referencias con proveedor, precio, moneda y fecha | Cumple | Existente, manual |
| US-010A / RF-20 | Selección manual: se copian nombre, unidad, precio y moneda | Cumple | Existente, nueva (momento de la copia) |
| US-010A / RF-20 | Los valores no se editan desde la tabla ni se modifica el catálogo | Cumple | Existente, manual |
| US-010A / RF-20 / DT-11 | Conservar la historia económica y el subtotal | Cumple en la tabla de conceptos | Existente, nueva (edición real del catálogo), manual |
| US-010A | Referencia única sin preselección | Cumple | Existente |
| US-010A / RF-20 | Rechazo de la combinación duplicada; mismo material con otro proveedor permitido | Cumple (ver observación 1) | Existente, nueva (filas pendientes), manual |
| US-010A | El catálogo se modifica por US-071, no desde el presupuesto | Cumple | Existente, nueva, manual |
| RNF-4 / RNF-10 | Control de acceso y validación en el servidor, antifalsificación | Cumple | Existente, nueva, manual |
| RNF-14 | Adaptación a distintos tamaños de pantalla | **No verificado** salvo escritorio a 1024×768 | Manual parcial |

## 3. Escenarios que ya estaban cubiertos

| Escenario | Pruebas existentes (principales) |
|---|---|
| Búsqueda y selección | `Busqueda_*`, `SinTextoDeBusqueda_*`, `ListadoDelSelector_*`, `Seleccion_Vincula*`, `Sustitucion_*`, `ConsultarMaterialesTests`, `SeleccionarMaterialDeConceptoTests` |
| Referencias por proveedor | `FilaConMaterial_*`, `PanelDeReferencias_*`, `ObtenerConReferencias_*` |
| Valores históricos (catálogo cambiado directamente en la base) | `ElCatalogoCambiaDespuesDeSeleccionar_*`, `ElCatalogoCambiaAntesDeCambiarProveedor_*`, `GuardarBorradorNuevamente_TrasCambiosEnElCatalogo_*`, `ConceptoDeMaterial_SeGuardaConSusCuatro*`, `AltaManual_MaterialConservaLaReferencia*` |
| Mismo material, distintos proveedores | `MismoMaterial_ConDistintosProveedores_*`, `IntercambioDeProveedores*` |
| Duplicados, también concurrentes | `ReferenciaYaUsada*`, `CambioDeProveedor_ALaCombinacionDeOtraFila*`, `AltaManual_DosSolicitudes*`, `DosFilasQueSeleccionan*`, `ElMismoMaterialProveedor_NoSePuedeRepetir*` |
| Cambio de proveedor | `CambioDeProveedor_*`, `CambiarProveedorDeConceptoTests` |
| Actualización confirmada y ausencia de actualización automática | `ActualizacionConfirmada_*`, `SinConfirmacionExplicita_*`, `ElegirLaMismaReferenciaOtraVez_*`, `ActualizarReferenciaDeConceptoTests` |
| Catálogo sin cambios desde el presupuesto | Comparaciones con `FotoDelCatalogoAsync` |
| Permisos, estados y antifalsificación | `*SinPermisoOPresupuestoNoEditable*`, `AltaManual_SinPermiso*`, `PermisosConceptosPresupuestoTests`, `MatrizDeAutorizacionTests`, `SiElPresupuestoDejaDeSerEditableAntesDeGuardar*` |
| Persistencia al guardar y volver a consultar | `GuardarBorrador_PersisteTodoYSeRecupera*` |

## 4. Pruebas nuevas

12 casos de integración contra SQL Server. Los diez primeros están en
`SeleccionarMaterialSqlServerTests.QaMaterialesPrecios.cs` y reutilizan los datos y utilidades de la clase
existente.

| Prueba | Casos | Qué faltaba |
|---|---|---|
| `CatalogoEditadoDesdeSuPantalla_...` | 1 | El catálogo solo se cambiaba directamente en la base. Ahora se edita por la pantalla real (US-071) y se comprueban los cuatro valores **y el subtotal** (DT-11). |
| `CatalogoCambiaEntreLaSeleccionYElGuardado_...` | 1 | Con el borrador (S3-T14/T15) hay un intervalo entre seleccionar y guardar: se guarda lo copiado al seleccionar, y otra sesión lo vuelve a ver. |
| `DuplicadoContraFilasPendientes_...` | 1 | Duplicado contra filas sin guardar en agregar, asignar material y cambiar proveedor, sin alterar el borrador. |
| `FilaSinGuardar_AdmiteCambiar...` | 1 | Cambiar proveedor, material y valores en una fila que todavía no tiene Id definitivo. |
| `CombinacionLiberadaEnElBorrador_...` | 1 | Reutilizar en una fila nueva una combinación liberada antes de guardar. |
| `EstadosEditables_Permiten...` | 4 | Las operaciones en «Para revisión interna» y «Solicitud de modificación del cliente» con los distintos roles, sin cambiar el estado. |
| `NombreYPrecioEnElLimite_...` | 1 | Nombre de 200 caracteres y precio `decimal(28,10)` a través del borrador. |
| Caso `Vencido` en dos pruebas existentes | 2 | Faltaba ese estado no editable en la selección y en el cambio de proveedor. |

**Comprobación de que detectan fallas** (cambios temporales en `src/`, revertidos):

- Se hizo que el guardado volviera a leer el catálogo. De los 93 casos que tenía en ese momento la clase
  `SeleccionarMaterialSqlServerTests`, fallaron 5, todos nuevos: las pruebas anteriores de esa clase no lo
  detectaban.
- Se dejó de registrar las filas nuevas en las referencias finales del guardado: los 115 casos de selección y
  guardado del borrador siguieron pasando. La prueba de combinación liberada verifica el comportamiento, pero
  no protege esa parte del código en particular (posiblemente porque EF Core ya ordena las operaciones; no se
  investigó más). No se observó ningún comportamiento incorrecto.

## 5. Compilación y pruebas ejecutadas

SQL Server LocalDB, con bases descartables propias de las pruebas.

| Ejecución | Compilación | Pruebas |
|---|---|---|
| Línea base (`ac0b87d`, Debug) | 0 errores, 12 advertencias | 1892: 1890 correctas, 0 fallidas, 2 omitidas |
| Final (Release, `--no-incremental`) | 0 errores, 12 advertencias, ninguna en archivos de S3-T28 | 1904: 1902 correctas, 0 fallidas, 2 omitidas |
| Subconjunto de la funcionalidad (final) | — | 371 de 371 correctas |

Las 2 omitidas son `DatosUruguayRealTests`: consultan el servicio externo real de cotizaciones y no son parte de
esta tarea. **No se consideran aprobadas.**

## 6. Verificación manual realizada

Aplicación real en `localhost`, en el navegador integrado a 1024×768, con una base descartable con datos
inventados y una **sesión simulada** (Operador responsable y Supervisor). El arranque usado quedó fuera del
repositorio y la base se eliminó al terminar.

- Modal «Agregar concepto»: búsqueda sin distinguir mayúsculas, mensaje sin resultados, referencias sin
  preselección y unidad del material junto a la cantidad.
- Mismo material con dos proveedores; la combinación repetida se rechazó al agregar, al asignar material y al
  cambiar proveedor, conservando las filas.
- Material sin proveedores: se informa y no permite asignarlo. Aviso antes de cerrar el modal con datos.
- Asignar material a la fila detectada conservó el texto detectado y la cantidad.
- Aviso de cambios sin guardar, «Guardar borrador» y nueva consulta con los mismos datos (también como
  Supervisor).
- Después de cambiar nombre, unidad y precios desde «Editar material» (incluido un precio con decimales), la
  fila conservó sus valores. «Actualizar valores»: «Cancelar» no cambió nada; confirmar actualizó solo esa fila.
- Cambio válido de proveedor, guardado.
- Presupuesto aprobado: solo lectura, y la URL de selección respondió 403 a un Supervisor.

## 7. Defectos y observaciones

**Defectos funcionales:** ninguno. **Correcciones en producción:** ninguna.

Observaciones para decidir en el equipo (no se cambió nada):

1. **Texto del rechazo por duplicado.** RF-20 dice que se informe que hay que modificar la cantidad de la fila o
   elegir otra referencia. Al agregar y al asignar material el mensaje dice «Conservá la fila existente o
   seleccioná otra referencia» (coincide con US-010A: usar la fila existente). Al cambiar proveedor dice
   «Modificá la cantidad de esa fila…», pero la interfaz todavía no permite editar cantidades.
2. **Nombre de la unidad.** La elaboración del presupuesto muestra «Metro» o «Centimetro» (sin tilde) y el
   catálogo muestra «m» o «cm».

## 8. Limitaciones y pendientes

- US-006 (análisis con IA) no está implementado: las filas detectadas se insertan como datos de prueba.
- El resumen previo, la planilla Excel y los indicadores (US-013, US-014, RF-103) no están implementados: las
  comprobaciones de DT-11 sobre ellos quedan pendientes.
- El JavaScript del selector de materiales no tiene pruebas automatizadas; solo se revisó a mano.
- Pendiente manual: inicio de sesión real con Entra ID, tamaños móvil y tableta (RNF-14), otros navegadores y
  uso con teclado o lector de pantalla.
