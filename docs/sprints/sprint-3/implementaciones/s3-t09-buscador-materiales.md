# S3-T09 — Buscador y selección manual de materiales (US-007)

**Sprint:** 3 — Elaboración económica de presupuestos

**Requerimientos:** RF-11, RF-12, RF-13, RF-20, RF-67, RF-68, RF-73 · RNF-4, RNF-10, RNF-14

## 1. Qué hace

En la tabla de conceptos de un presupuesto (`/ConceptosPresupuesto/Index/{id}`), cada fila de tipo
Material muestra un botón **Seleccionar material** (si todavía no tiene material) o **Cambiar material**
(si ya lo tiene). Ese botón abre `SeleccionarMaterial`, donde el usuario:

1. Ve la fila: Concepto detectado, material actual y cantidad.
2. Busca por nombre en el catálogo (la búsqueda solo lee: reutiliza `ConsultarMateriales`).
3. Elige expresamente una referencia (material + proveedor + precio) con **Seleccionar** o **Sustituir**.

`AsignarMaterial` (POST) guarda la elección mediante el caso de uso `SeleccionarMaterialDeConcepto`.

## 2. Reglas aplicadas

| Regla | Dónde se aplica |
|---|---|
| Nunca se elige un material automáticamente; sin texto de búsqueda no se lista nada | `ConceptosPresupuestoController.PrepararSeleccionAsync` |
| Concepto detectado y cantidad no cambian al seleccionar o sustituir | `ConceptoPresupuesto.CopiarReferencia` (no los toca) |
| Se copian los cuatro datos históricos de la nueva referencia (DT-11) | `RepositorioConceptosPresupuesto.SeleccionarMaterialSiPuedeEditarAsync` |
| Elegir la referencia que la fila ya tiene no vuelve a copiar valores | Resultado `SinCambios` |
| Seleccionar no crea ni modifica materiales, proveedores ni referencias | El catálogo se lee con `AsNoTracking`; probado con una «foto» del catálogo antes y después |
| Si el material no existe, se registra antes desde el catálogo (US-041) | Mensaje sin resultados; el botón «Registrar material» solo lo ven Supervisor y Administrador |
| Operador solo sobre presupuestos de los que es responsable; el presupuesto debe estar en estado editable | `PermisosConceptosPresupuesto.PuedeEditar`, en el GET, en el POST y otra vez dentro de la transacción |
| Misma combinación material-proveedor no repetida entre filas del presupuesto (RF-20) | Consulta previa + índice único de la base como respuesta definitiva |
| La fila se busca por Id **y** por presupuesto | Evita modificar filas de otro presupuesto cambiando la URL |
| Solo filas de tipo Material | Resultado `ConceptoNoEsMaterial` |
| Dos sustituciones simultáneas de la misma fila no se bloquean | La fila se lee con `UPDLOCK` dentro de la transacción |

## 3. Pendiente de otras tareas

- **US-006 (análisis del relevamiento con IA).** Todavía no existe quien cree filas de tipo Material con
  `ConceptoDetectado` y sin material. La pantalla ya funciona con ese tipo de fila (las pruebas la crean
  directamente en la base, igual que lo haría el análisis), pero no hay forma de generarlas desde la
  aplicación hasta que US-006 esté implementada. No se simuló ningún resultado de IA.
- **US-010A (referencias de precios).** Se usa lo que ya existe (copia de los cuatro datos históricos al
  elegir la referencia). Queda para US-010A lo que no corresponde a esta tarea: elegir otra referencia
  *sin buscar de nuevo el material* y los demás criterios de esa historia.
- **Modificación de cantidad y eliminación de filas (US-011/RF-18):** fuera de esta tarea.

## 4. Diferencia detectada entre documentos (no se resolvió)

US-007 habla de seleccionar un *material* para la fila y US-010A de elegir después la *referencia de proveedor*
de ese material. El modelo vigente (`CK_ConceptosPresupuesto_ReferenciaCompleta`, DT-11) no permite guardar
una fila con material sin su referencia: `MaterialId` y `MaterialProveedorId` van juntos. Por eso, en esta
tarea, **seleccionar el material equivale a elegir expresamente una referencia material-proveedor**, que es lo
mismo que ya hace el alta manual de conceptos (US-011). Si el equipo quiere separar las dos selecciones en
pasos distintos, habría que decidir primero cómo se representa una fila con material y sin referencia.

## 5. Verificación

Pruebas nuevas: `SeleccionarMaterialDeConceptoTests` (unitarias) y `SeleccionarMaterialSqlServerTests`
(integración contra SQL Server). La matriz de autorización de RF-73 incluye las dos acciones nuevas.
