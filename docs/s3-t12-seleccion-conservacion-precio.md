# S3-T12 — Selección y conservación del precio (US-010A)

**Sprint:** 3 — Elaboración económica de presupuestos

**Requerimientos:** RF-19, RF-20 · DT-11 · RNF-4, RNF-10, RNF-14

Esta tarea parte de S3-T09 (US-007) y S3-T11 y **no las rehace**. Las validaciones de duplicidad y de
selección que corresponden a S3-T13 tampoco se adelantan: la regla de «misma combinación material-proveedor
en otra fila» ya existía desde S3-T09 y simplemente se reutiliza.

## 1. Qué agrega S3-T12

Desde el panel de referencias de S3-T11 (pantalla `SeleccionarMaterial`, fila que ya tiene material), cada
referencia del material, **menos la que la fila ya tiene**, ofrece un botón «Seleccionar». Al usarlo:

1. La fila conserva **el mismo material**; solo cambia la referencia de proveedor.
2. Se vuelven a copiar a la fila los cuatro datos históricos de DT-11: nombre del material, unidad, precio
   unitario y moneda, tomados del catálogo en ese momento.
3. `ConceptoDetectado` y `Cantidad` no se tocan. El catálogo no se modifica.
4. Queda guardado en la base de datos y se vuelve a la tabla de conceptos con un mensaje de confirmación.

Además, la **referencia que la fila ya tiene** puede actualizarse a pedido (sección 6): si el catálogo cambió el
nombre, la unidad, el precio o la moneda, el panel ofrece «Actualizar valores», que pide confirmación y vuelve a
copiar los cuatro valores vigentes de esa misma referencia.

## 2. Piezas

| Pieza | Qué hace |
|---|---|
| `ConceptosPresupuestoController.CambiarProveedor` (POST) | Valida sesión, token antifalsificación, rol y que el presupuesto sea editable para ese usuario (RNF-4); delega en el caso de uso. |
| `CambiarProveedorDeConcepto` (Core, caso de uso) | Exige selección expresa (`referenciaId > 0`) y traduce el resultado del repositorio a un mensaje. |
| `IRepositorioConceptosPresupuesto.CambiarProveedorSiPuedeEditarAsync` | Contrato nuevo. |
| `RepositorioConceptosPresupuesto` | Los dos métodos de selección (US-007 y T12) comparten `CopiarReferenciaALaFilaAsync`: misma transacción, mismo bloqueo `UPDLOCK`, mismo control de duplicados. T12 agrega dos condiciones: la fila debe tener material y la referencia debe ser de ese mismo material. |
| `ResultadoSeleccionarMaterial` | Dos valores nuevos: `ConceptoSinMaterial` y `ReferenciaDeOtroMaterial`. |
| `SeleccionarMaterial.cshtml` | Columna de acción en el panel de T11. |

## 3. Cómo se conservan los valores históricos

- `ConceptoPresupuesto` guarda sus propias columnas (`Concepto`, `Unidad`, `PrecioUnitario`, `Moneda`); nunca
  lee el catálogo para mostrarse ni para calcular (DT-11).
- Solo `CopiarReferencia` las escribe, y solo se llama cuando el usuario elige expresamente una referencia.
  No hay ningún formulario ni acción que reciba esos valores desde la pantalla: el formulario de T12 envía
  únicamente `referenciaId`, y si la solicitud trae otros campos se ignoran.
- Cambiar el catálogo después (nombre o unidad del material, precio o moneda de la referencia) no toca las
  filas ya guardadas.
- Una nueva selección expresa vuelve a copiar los valores **vigentes** de la referencia elegida (RF-020,
  último párrafo de la descripción). Para la referencia que la fila ya tiene, esa nueva selección es la
  actualización manual con confirmación de la sección 6.

## 4. Validaciones del servidor

| Caso | Resultado |
|---|---|
| Sin sesión activa, rol sin acceso, Operador que no es responsable, o presupuesto no editable | Acceso denegado; no cambia nada (se controla en el controlador y de nuevo dentro de la transacción). |
| Sin token antifalsificación | 400. |
| `referenciaId` vacío, 0 o negativo | Mensaje «Seleccioná la referencia de proveedor…». |
| Referencia inexistente | Mensaje y la fila queda igual. |
| Referencia de **otro material** | Se rechaza: el material de la fila se conserva. |
| Fila sin material | Se rechaza: primero hay que elegir el material (US-007). |
| Fila que no es de tipo Material, o de otro presupuesto | Se rechaza. |
| La misma referencia que ya tiene la fila | «Ya es la seleccionada»; no se vuelve a copiar nada. |
| Material y proveedor ya usados en otra fila del presupuesto | Se rechaza (RF-20); el índice único de la base cubre las solicitudes simultáneas. |

## 5. Pruebas

- `CambiarProveedorDeConceptoTests` (unitarias): selección expresa y traducción de cada resultado a su mensaje.
- `SeleccionarMaterialSqlServerTests` (SQL Server real), sección S3-T12: botones solo en las referencias
  distintas de la actual; el formulario envía solo `referenciaId`; el cambio actualiza proveedor, precio y
  moneda y conserva material, concepto detectado y cantidad; valores escritos a mano se ignoran; cambios del
  catálogo posteriores y anteriores a la selección; otras filas intactas; todos los rechazos de la sección 4;
  permisos por rol y estado; dos cambios simultáneos sobre la misma fila.
- `MatrizDeAutorizacionTests`: la acción `CambiarProveedor` quedó declarada en la matriz de RF-73.
- Se ajustaron dos expresiones regulares de S3-T11 porque la fila del panel ahora termina con la celda de acción.

## 6. Actualización manual de la referencia actual (RF-020)

RF-020 dice que, para aplicar una referencia **actualizada** del catálogo, «el usuario deberá realizar una nueva
selección». Como la referencia que la fila ya tiene no se ofrece como «otra» (y S3-T09 rechaza volver a elegirla
para no pisar los valores históricos), el caso quedaba sin camino. Se completó así:

- **Cuándo se ofrece.** En la fila de la referencia actual del panel, si alguno de los cuatro valores que la fila
  copió (nombre, unidad, precio, moneda) ya no coincide con el catálogo, aparece **«Actualizar valores»**. Si
  coinciden, el panel indica «Valores al día con el catálogo» y no ofrece nada. Que el catálogo cambie nunca
  actualiza la fila por sí solo.
- **Confirmación explícita.** El botón solo abre un diálogo (patrón de `docs/diseno-visual.md` §18.1) que muestra
  los valores de la fila junto a los del catálogo y avisa que los de la fila no se pueden recuperar después.
  Recién «Actualizar valores», dentro del diálogo, envía el POST con `confirmar=true`. El servidor exige ese
  campo (`ActualizarReferenciaDeConcepto`): una solicitud sin confirmar se rechaza y no cambia nada.
- **Qué hace.** `POST ConceptosPresupuesto/ActualizarReferencia` → `ActualizarReferenciaDeConcepto` →
  `RepositorioConceptosPresupuesto.ActualizarReferenciaActualSiPuedeEditarAsync`. No recibe ninguna referencia:
  usa la que la fila ya guarda, así no se puede usar para cambiar de proveedor ni de material. Vuelve a copiar los
  cuatro valores con `CopiarReferencia` y conserva material, proveedor, `ConceptoDetectado` y cantidad. El
  catálogo se lee sin seguimiento y no se modifica.
- **Mismas protecciones que el resto.** Mismo control de rol y de estado editable (controlador y dentro de la
  transacción, con `UPDLOCK`): un presupuesto aprobado, vencido, cancelado o en cualquier estado no editable nunca
  se modifica, y el Operador solo si es el responsable. Si la fila no tiene material, no es de tipo Material o no
  es del presupuesto de la URL, se rechaza.
- **Sin cambios.** Si al confirmar los cuatro valores ya coinciden (por ejemplo, otra persona acaba de
  actualizarlos), se informa que no hay nada que actualizar y no se guarda.
- Los helpers comunes (`UsuarioPuedeEditarAsync`, `ObtenerFilaBloqueadaAsync`, `ObtenerReferenciaConMaterialAsync`)
  se extrajeron del flujo de selección para que los tres flujos compartan el mismo control sin duplicarlo.

No se elimina ninguna fila ni se amplía el alcance a otras tareas.

## 7. Pruebas de la actualización manual

- `ActualizarReferenciaDeConceptoTests` (unitarias): sin confirmación no se consulta al repositorio; cada
  resultado se traduce a su mensaje; los resultados que esta acción no debe producir no se ignoran.
- `SeleccionarMaterialSqlServerTests` (SQL Server real): el botón solo aparece si el catálogo cambió y consultar el
  panel no actualiza nada; la actualización confirmada copia los cuatro valores y conserva material, proveedor,
  concepto detectado y cantidad sin tocar el catálogo; sin confirmación (campo ausente, `false` o vacío) la fila
  conserva su historia aunque el catálogo haya cambiado; fila ya al día; una referencia mandada en la solicitud se
  ignora; permisos por rol y estado (Borrador sin ser responsable, Aprobado, Rechazado, Enviado, Vencido,
  Cancelado) sin ningún cambio; fila sin material, que no es Material o de otro presupuesto; token
  antifalsificación; dos actualizaciones simultáneas.
- `MatrizDeAutorizacionTests`: la acción `ActualizarReferencia` quedó declarada en la matriz de RF-73.
