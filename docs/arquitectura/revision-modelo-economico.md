# S3-T24 — Revisión del modelo económico

## 1. Objetivo

Revisar la estructura del modelo económico del sistema EOP, verificando las entidades, sus relaciones y las reglas de negocio relacionadas con conceptos, importes, monedas y presupuestos.

Esta tarea corresponde al Sprint 3 y es de análisis. No implica implementar nuevas funcionalidades ni modificar las reglas económicas aprobadas.

## 2. Documentación relacionada

Se consideran los siguientes documentos:

- `AGENTS.md`: reglas de arquitectura y desarrollo.
- `docs/historias-de-usuario/user-stories.md`: historias US-011, US-012 y US-013.
- `docs/requerimientos/requerimientos-funcionales.md`: requisitos económicos, especialmente RF-27, RF-28, RF-29 y RF-30.
- `docs/arquitectura/decisiones-tecnicas.md`: decisiones sobre persistencia y conservación de precios históricos.

## 3. Entidades del modelo económico

### 3.1. Presupuesto

La entidad `Presupuesto` representa el trabajo que se desea presupuestar.

Contiene información como:

- Identificador.
- Código interno.
- Nombre del proyecto.
- Cliente asociado.
- Tipo de proyecto.
- Responsable.
- Estado.
- Fecha de creación.
- Plazo de vigencia.

Un presupuesto puede contener múltiples conceptos económicos.

### 3.2. ConceptoPresupuesto

La entidad `ConceptoPresupuesto` representa una fila de la tabla económica del presupuesto.

Contiene:

- Identificador del presupuesto.
- Tipo de concepto.
- Descripción del concepto.
- Concepto detectado mediante IA, cuando corresponde.
- Material y referencia de proveedor, cuando corresponde.
- Cantidad.
- Unidad.
- Precio unitario o importe.
- Moneda.
- Porcentaje de margen, cuando corresponde.

Los datos aplicables dependen del tipo de concepto seleccionado.

### 3.3. TipoConcepto

Los tipos de concepto definidos en el modelo son:

1. Material.
2. Mano de obra.
3. Alquiler.
4. Traslado.
5. Costos indirectos.
6. Margen.

Los tipos están definidos como opciones fijas del sistema.

### 3.4. MaterialProveedor

La entidad `MaterialProveedor` representa la relación entre un material y un proveedor.

Almacena:

- MaterialId.
- ProveedorId.
- PrecioUnitario.
- Moneda.
- FechaUltimaActualizacion.

Cuando se selecciona una referencia del catálogo para un presupuesto, se deben conservar sus datos históricos.

El método `CopiarReferencia()` de `ConceptoPresupuesto` copia el nombre del material, la unidad, el precio unitario y la moneda.

Esto permite que modificaciones posteriores en el catálogo no alteren los valores históricos del presupuesto.

### 3.5. MargenBaseTipo

La entidad `MargenBaseTipo` representa los tipos de concepto elegidos por el usuario como base de cálculo del margen.

Permite identificar qué tipos intervienen en el cálculo del porcentaje.

Un presupuesto puede contener como máximo un concepto de tipo Margen.

## 4. Relaciones del modelo

Las relaciones principales son:

- Un presupuesto puede contener múltiples conceptos.
- Cada concepto pertenece a un presupuesto.
- Cada concepto tiene un tipo.
- Un concepto de tipo Material puede asociarse a una referencia material-proveedor.
- El margen utiliza los tipos seleccionados mediante `MargenBaseTipo`.

La relación entre un concepto y el catálogo no implica que los precios del presupuesto se actualicen automáticamente cuando cambie el catálogo.

## 5. Reglas de cálculo

### 5.1. Materiales

El subtotal de un material se obtiene mediante:

**Subtotal = Cantidad × Precio unitario**

### 5.2. Conceptos de importe manual

Para conceptos como mano de obra, alquiler, traslado y costos indirectos, el importe ingresado por el usuario se utiliza directamente como subtotal.

La moneda puede ser UYU o USD.

### 5.3. Margen

El usuario selecciona uno o más tipos de concepto como base para calcular el margen.

La base debe expresarse en UYU.

Los importes USD correspondientes a los tipos seleccionados se convierten a UYU utilizando la cotización aplicable.

**Subtotal del margen = Base en UYU × Porcentaje / 100**

El margen se expresa siempre en UYU.

El propio margen no puede utilizarse como parte de su base.

## 6. Monedas y cotizaciones

El sistema utiliza dos monedas:

- UYU: pesos uruguayos.
- USD: dólares estadounidenses.

Los importes originales conservan su moneda.

No deben sumarse directamente importes de monedas diferentes.

Para mostrar ambos totales, se utiliza una única cotización UYU/USD válida, obtenida mediante datosUruguay conforme a RF-29.

Las fórmulas aprobadas son:

**Total UYU = Subtotal UYU + (Subtotal USD × Cotización)**

**Total USD = Subtotal USD + (Subtotal UYU ÷ Cotización)**

Los totales finales deben mostrarse con dos decimales.

No deben redondearse anticipadamente los valores intermedios utilizados en los cálculos.

## 7. Conservación de valores históricos

Al seleccionar un material del catálogo, el presupuesto conserva:

- Nombre del material.
- Unidad de medida.
- Precio unitario.
- Moneda.

Una modificación posterior de los precios del catálogo no debe alterar automáticamente los datos económicos de presupuestos anteriores.

Al confirmar los datos económicos, deben conservarse los totales en ambas monedas, la cotización aplicada y su fecha de publicación.

## 8. Reglas de validación

El modelo económico debe respetar las siguientes reglas:

- Cada concepto debe tener un tipo válido.
- Los campos obligatorios dependen del tipo seleccionado.
- Un presupuesto puede contener como máximo un margen.
- El margen debe utilizar los tipos seleccionados como base.
- No se permite repetir una misma combinación material-proveedor dentro del presupuesto.
- Los cálculos deben respetar las monedas originales.
- La confirmación económica requiere una cotización válida.
- Los cambios del catálogo no deben modificar los precios históricos del presupuesto.

## 9. Situación actual y tareas pendientes

Durante la revisión del código se identificaron las siguientes entidades:

- `Presupuesto`
- `ConceptoPresupuesto`
- `TipoConcepto`
- `MaterialProveedor`
- `MargenBaseTipo`

También se identificó su registro correspondiente en `EopDbContext`.

La existencia de estas clases no significa que todos los casos de uso y cálculos económicos estén implementados.

Quedan pendientes de implementación o verificación en las tareas correspondientes:

- Incorporación y validación de conceptos.
- Cálculo de subtotales.
- Cálculo del margen.
- Conversión de monedas mediante datosUruguay.
- Totales y redondeos.
- Conservación de datos económicos confirmados.
- Pruebas de integración y de cálculos.

## 10. Conclusión

La estructura revisada contempla las entidades necesarias para representar conceptos económicos, referencias de materiales y proveedores y bases de cálculo del margen.

El modelo permite conservar información histórica del catálogo y distinguir los diferentes tipos de concepto.

Las reglas aprobadas establecen cómo calcular subtotales, márgenes y totales en USD y UYU, sin mezclar directamente monedas.

La revisión documental no sustituye las pruebas funcionales ni confirma que todos los cálculos estén implementados.

La implementación y validación de las operaciones económicas quedan dentro de las tareas correspondientes del Sprint 3.

**Resultado:** revisión estructural y documental del modelo económico realizada, con las reglas y dependencias identificadas.