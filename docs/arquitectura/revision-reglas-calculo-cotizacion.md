# S3-T25 — Revisión de reglas de cálculo y cotización

## 1. Objetivo

Revisar las reglas económicas del sistema EOP relacionadas con subtotales, margen, conversiones entre monedas, redondeos y disponibilidad de cotizaciones.

Esta tarea corresponde al Sprint 3 y es de análisis. No implica implementar nuevas funcionalidades ni modificar las reglas de negocio aprobadas.

## 2. Documentación relacionada

Se revisaron las siguientes fuentes:

- `AGENTS.md`: reglas generales de arquitectura y negocio.
- `docs/requerimientos-funcionales.md`: RF-27, RF-28, RF-29, RF-30 y RF-31.
- `docs/user-stories.md`: US-012 y US-013.
- `docs/decisiones-tecnicas.md`: decisiones de persistencia y conservación de valores económicos.

## 3. Cálculo de subtotales

### 3.1. Materiales

Para los conceptos de tipo Material, el subtotal se calcula multiplicando la cantidad por el precio unitario seleccionado.

**Subtotal = Cantidad × Precio unitario**

Ejemplo ilustrativo:

- Cantidad: 3 unidades.
- Precio unitario: USD 100.
- Subtotal: USD 300.

El subtotal conserva la moneda original del precio seleccionado.

Cuando se modifica la cantidad o el precio utilizado, debe recalcularse el subtotal.

### 3.2. Conceptos de importe manual

Los conceptos de importe directo, como Mano de obra, Alquiler, Traslado y Costos indirectos, utilizan el importe ingresado por el usuario como subtotal.

Ejemplo:

- Mano de obra: UYU 2.000.
- Subtotal: UYU 2.000.

No se aplica una multiplicación por cantidad cuando el tipo de concepto utiliza directamente un importe.

## 4. Cálculo del margen

El sistema permite registrar como máximo un concepto de tipo Margen por presupuesto.

El usuario debe ingresar un porcentaje y seleccionar uno o más tipos de conceptos que participarán en su cálculo.

La base del margen se expresa siempre en UYU.

**Base del margen en UYU = Subtotales UYU seleccionados + (Subtotales USD seleccionados × Cotización)**

**Subtotal Margen = Base del margen × Porcentaje / 100**

El resultado se expresa en UYU.

### Ejemplo ilustrativo

Supongamos:

- Materiales: USD 300.
- Mano de obra: UYU 2.000.
- Cotización hipotética: 40 UYU/USD.
- Porcentaje de margen: 10 %.
- Base seleccionada: Materiales y Mano de obra.

Primero convertimos los USD a UYU:

300 × 40 = UYU 12.000.

Calculamos la base:

12.000 + 2.000 = UYU 14.000.

Aplicamos el margen:

14.000 × 10 / 100 = UYU 1.400.

**Resultado: Margen = UYU 1.400.**

La cotización de 40 UYU/USD es únicamente un valor ficticio utilizado para explicar el cálculo.

El concepto Margen no puede formar parte de su propia base.

Si cambia un concepto perteneciente a uno de los tipos seleccionados, debe recalcularse el margen.

Las modificaciones de tipos no seleccionados no deben modificar el margen.

## 5. Conversión de monedas

El sistema trabaja con:

- UYU: pesos uruguayos.
- USD: dólares estadounidenses.

Los importes originales conservan su moneda.

Los subtotales de distintas monedas no se suman directamente.

Para calcular los totales, el sistema utiliza una cotización expresada en pesos uruguayos por dólar estadounidense.

Las fórmulas aprobadas en RF-29 son:

**Total UYU = Subtotal UYU + (Subtotal USD × Cotización)**

**Total USD = Subtotal USD + (Subtotal UYU ÷ Cotización)**

El subtotal UYU incluye el concepto Margen, que siempre se expresa en esa moneda.

### Ejemplo ilustrativo

Continuando con el ejemplo anterior:

- Subtotal USD: USD 300.
- Mano de obra: UYU 2.000.
- Margen: UYU 1.400.
- Subtotal UYU: UYU 3.400.
- Cotización hipotética: 40 UYU/USD.

**Total UYU:**

3.400 + (300 × 40) = UYU 15.400.

**Total USD:**

300 + (3.400 / 40) = USD 385.

Ambos totales representan el mismo presupuesto expresado en monedas diferentes.

No deben sumarse entre sí.

## 6. Obtención de cotizaciones

Según RF-29, la cotización utilizada debe:

- Corresponder al dólar publicado por el Banco Central del Uruguay.
- Obtenerse mediante datosUruguay API.
- Expresarse en UYU/USD.
- Ser estrictamente mayor que cero.

Cuando no exista publicación para la fecha actual, por ejemplo en fines de semana o feriados, deberá utilizarse la última cotización válida anterior.

No se permiten cotizaciones futuras, valores estimados ni promedios.

Una falla temporal del servicio no debe interpretarse automáticamente como ausencia de publicación.

## 7. Ausencia de cotización válida

RF-29 establece que, cuando no se consigue una cotización válida, el sistema debe:

- Informar al usuario sobre el problema.
- Permitir continuar editando el presupuesto.
- Permitir guardar el borrador.
- Evitar mostrar conversiones monetarias como definitivas.
- Impedir confirmar los datos económicos hasta obtener una cotización válida.

### Diferencia detectada entre documentos

Durante la revisión se identificó una diferencia que requiere aclaración.

En `AGENTS.md`, la regla transversal de monedas indica que, cuando no existe una cotización utilizable, se rechaza el guardado.

Sin embargo, RF-29 y US-013 distinguen entre guardar un borrador y confirmar los datos económicos, permitiendo la primera operación sin cotización válida y rechazando la segunda.

Esta diferencia debe elevarse al equipo para aclarar la redacción y mantener consistentes las instrucciones del repositorio.

La presente revisión no introduce ninguna modificación en las reglas de negocio.

## 8. Redondeos

Los cálculos económicos deben conservar la precisión de:

- Cantidades.
- Precios unitarios.
- Porcentajes.
- Cotización.

No deben aplicarse redondeos intermedios que alteren el resultado.

Los totales finales se expresan con dos decimales.

Se utiliza el redondeo matemático especificado en RF-29: cuando el tercer decimal es 5 o superior, se incrementa el segundo decimal.

Ejemplo:

- 125,344 se muestra como 125,34.
- 125,345 se muestra como 125,35.

El redondeo de presentación no modifica los valores originales de los conceptos.

## 9. Recálculo automático

El sistema debe actualizar los resultados cuando se modifican valores que intervienen en los cálculos.

Entre ellos:

- Cantidad de materiales.
- Precios utilizados.
- Importes manuales.
- Porcentaje de margen.
- Tipos seleccionados como base del margen.
- Cotización actual utilizada durante la elaboración.

Los subtotales, el margen y los totales deben reflejar la información económica vigente durante la elaboración.

## 10. Conservación de datos confirmados

Al confirmar los datos económicos deben conservarse:

- Total en UYU.
- Total en USD.
- Cotización aplicada.
- Fecha de publicación de la cotización.
- Fecha de confirmación.

Las variaciones posteriores de la cotización no deben modificar automáticamente esos valores confirmados.

Si posteriormente se modifican y vuelven a confirmar los datos económicos, deben conservarse los nuevos valores.

Cuando exista un PDF comercial previamente cargado, debe revisarse su vigencia conforme a RF-44.

## 11. Situación de implementación y verificaciones pendientes

Esta tarea revisa las reglas documentadas, pero no certifica su implementación funcional.

Quedan para las tareas correspondientes:

- Implementar y verificar los cálculos de subtotales.
- Implementar el cálculo del margen.
- Verificar la integración con datosUruguay.
- Implementar conversiones y redondeos.
- Validar las respuestas ante ausencia de cotización.
- Comprobar la conservación de valores económicos confirmados.
- Ejecutar las pruebas automatizadas y de integración pertinentes.

## 12. Conclusión

Se revisaron las fórmulas económicas, el uso de monedas, los criterios de redondeo y las reglas de selección de cotizaciones.

Se confirmó documentalmente que el sistema debe preservar los importes originales y utilizar una cotización válida para las conversiones, sin sumar directamente importes en monedas diferentes.

Se identificó una diferencia de redacción entre `AGENTS.md` y RF-29 respecto del guardado de borradores cuando no existe una cotización disponible.

La revisión deja documentadas las reglas aprobadas y los escenarios que deberán comprobarse durante la implementación y las pruebas del Sprint 3.

**Resultado:** revisión documental completada, con una discrepancia pendiente de aclaración por el equipo.