# S3-T11 — Consulta de precios por proveedor (US-010A)

**Sprint:** 3 — Elaboración económica de presupuestos

**Requerimientos:** RF-19, RF-20 (solo la parte de mostrar antes de seleccionar) · RNF-4, RNF-10, RNF-14

Esta tarea parte de S3-T09 (US-007) y **no duplica** lo que ya hace. La selección y la conservación del
precio como valor histórico son de S3-T12; las validaciones de selección y duplicidad, de S3-T13.

## 1. Qué ya cubría US-007 (S3-T09)

| Criterio de US-010A / RF-19 | Estado | Dónde |
|---|---|---|
| Se muestran proveedor, precio, moneda y fecha de última actualización de cada referencia | Cubierto para un material **buscado por nombre** | `SeleccionarMaterial.cshtml` (resultados de la búsqueda), que reutiliza `ConsultarMateriales` |
| Todas las referencias del material, ordenadas por proveedor | Cubierto | `ConsultarMateriales.EjecutarAsync` |
| Fecha guardada en UTC y mostrada en hora de Uruguay | Cubierto | `ZonaHorariaUruguay.ConvertirUtcALocal` |
| Material sin referencias | Cubierto en la vista («Sin proveedores con precio registrado»), **sin prueba** | Vista |
| La consulta no modifica el catálogo | Cubierto (`AsNoTracking` y «foto» del catálogo) | Pruebas de US-007 |
| Permisos y estado del presupuesto, validados en el servidor | Cubierto | `PermisosConceptosPresupuesto.PuedeEditar` en el GET |
| Selección expresa, incluso con un solo proveedor | Cubierto | US-007 (conservado, no se amplía acá) |

## 2. Qué agrega S3-T11

**Consultar las referencias de un material ya elegido, sin buscarlo de nuevo.** Cuando la fila ya tiene
material, la pantalla `SeleccionarMaterial` muestra arriba un panel de solo lectura con todas las referencias
actuales del catálogo de ese material: Proveedor, Precio unitario, Moneda y Última actualización. La
referencia que la fila tiene hoy se marca como «Referencia de esta fila». El panel aclara que son los valores
de hoy del catálogo y que los ya copiados al presupuesto no cambian (RF-19).

- `FilaConceptoDto` incorpora `MaterialId` (ya existía en `ConceptoPresupuesto`) para saber qué material consultar.
- `ConsultarMateriales.ObtenerConReferenciasAsync(materialId)` trae el material con sus referencias; reutiliza
  `ObtenerPorIdAsync` y `ReferenciaPrecioMaterialDto`. Solo lee (`AsNoTracking`).
- El panel no tiene botones ni formularios: elegir una referencia a partir de él es S3-T12.
- Va en la misma pantalla, así que hereda su control de acceso (Operador solo responsable y en estado editable;
  Supervisor y Administrador según RF-73).

## 3. Pruebas

- `ConsultarMaterialesTests` (SQLite en memoria): referencias con proveedor, precio, moneda y fecha; solo las
  del material pedido; material sin referencias; material inexistente o Id inválido; la consulta no sigue
  entidades ni cambia el catálogo.
- `SeleccionarMaterialSqlServerTests` (SQL Server real): panel con varios proveedores, monedas UYU y USD
  distinguidas, fecha en hora de Uruguay (una referencia guardada a las 01:30 UTC del 2/11 se ve como 01/11),
  sin panel en filas sin material, búsqueda con precios y fechas, material sin referencias en la búsqueda,
  acceso denegado sin permiso, y catálogo idéntico antes y después de consultar.

## 4. Observaciones para decidir (no se cambió nada)

- **Formato de fecha (resuelto).** Las pantallas de presupuestos (US-007 y US-011) mostraban `dd/MM/yyyy` y el
  catálogo de materiales `dd/MM/yyyy HH:mm`. La documentación no fija el formato; el equipo decidió unificar con
  hora. Ahora la búsqueda de `SeleccionarMaterial`, el panel de esta tarea y el selector de referencias de
  US-011 muestran `dd/MM/yyyy HH:mm` en hora de Uruguay, igual que el catálogo.
- **Material seleccionado vs. referencia (ya señalado en S3-T09).** US-007 habla de elegir un *material* y
  US-010A de elegir después la *referencia*, pero el modelo (DT-11) no permite una fila con material y sin
  referencia. El panel de esta tarea consulta las referencias del material de la fila; cómo se cambia solo el
  proveedor sin volver a buscar el material queda para S3-T12.
