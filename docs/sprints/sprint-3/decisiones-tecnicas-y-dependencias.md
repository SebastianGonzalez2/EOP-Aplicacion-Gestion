# S3-T32 — Decisiones técnicas y dependencias del Sprint 3

**Fecha:** 19/11/2026

**Proyecto:** EOP — Sistema de Gestión

**Sprint:** 3 — Elaboración económica de presupuestos

## 1. Objetivo

Registrar las decisiones técnicas aprobadas que afectan la elaboración económica de presupuestos, sus dependencias y las funcionalidades pendientes de implementación o verificación.

Este documento no introduce decisiones nuevas ni modifica los requerimientos aprobados.

## 2. Documentación de referencia

- `AGENTS.md`: arquitectura y reglas transversales.
- `docs/arquitectura/decisiones-tecnicas.md`: decisiones técnicas previamente registradas.
- `docs/requerimientos/requerimientos-funcionales.md`: RF-20, RF-27, RF-28, RF-29, RF-30 y RF-31.
- `docs/historias-de-usuario/user-stories.md`: US-010A, US-011, US-012 y US-013.
- `docs/infraestructura/integracion-continua.md`: configuración y estado de GitHub Actions.

## 3. Decisiones sobre persistencia

### 3.1. Separación de responsabilidades

La aplicación utiliza la arquitectura definida en `AGENTS.md`:

- Core: entidades y reglas de negocio.
- Infrastructure: persistencia mediante Entity Framework Core.
- WebApp: controladores y vistas.

Las reglas económicas no deben implementarse directamente en los controladores.

### 3.2. Persistencia de conceptos

La entidad `ConceptoPresupuesto` representa las filas económicas del presupuesto.

Cada concepto pertenece a un presupuesto y posee un tipo.

El modelo permite representar materiales, importes manuales y márgenes mediante los campos correspondientes.

### 3.3. Conservación de precios históricos

Según DT-11, cuando el usuario selecciona una referencia de material y proveedor, el concepto conserva:

- Nombre del material.
- Unidad.
- Precio unitario.
- Moneda.

Los cambios posteriores en el catálogo no deben modificar los valores históricos conservados en el presupuesto.

### 3.4. Base de cálculo del margen

La entidad `MargenBaseTipo` permite representar los tipos de concepto seleccionados para calcular el margen.

Un presupuesto admite como máximo un concepto de tipo Margen.

El margen se expresa siempre en UYU.

## 4. Decisiones sobre integración con datosUruguay

### 4.1. Fuente de cotización

RF-29 establece que las conversiones USD/UYU utilizan la cotización diaria publicada por el Banco Central del Uruguay, obtenida mediante datosUruguay API.

La cotización se expresa en UYU/USD y debe ser mayor que cero.

### 4.2. Cotizaciones anteriores

Cuando no existe publicación para la fecha actual, se utiliza la última cotización válida publicada anteriormente.

No deben utilizarse cotizaciones futuras, promedios ni valores estimados.

### 4.3. Conservación de cotización confirmada

Al confirmar los datos económicos deben conservarse:

- Total UYU.
- Total USD.
- Cotización utilizada.
- Fecha de publicación.
- Fecha de confirmación.

Los cambios posteriores de la cotización no modifican automáticamente esos valores confirmados.

### 4.4. Ausencia de cotización válida

RF-29 permite continuar editando y guardar un borrador cuando no existe una cotización válida, pero impide confirmar los datos económicos.

Se identificó una diferencia respecto a la redacción de la regla transversal de monedas de `AGENTS.md`, que indica rechazar el guardado sin cotización utilizable.

Esta diferencia queda pendiente de aclaración por el equipo. No se modifica ninguna regla en esta tarea.

## 5. Dependencias funcionales

| Funcionalidad | Dependencia |
|---|---|
| Guardar conceptos económicos | Modelo de conceptos y persistencia |
| Calcular subtotal de materiales | Cantidad y precio unitario seleccionado |
| Calcular margen | Subtotales y tipos seleccionados como base |
| Calcular totales USD/UYU | Subtotales y cotización válida |
| Confirmar datos económicos | Validaciones, cálculos y cotización válida |
| Consultar resumen | Datos económicos y totales calculados |
| Ejecutar QA económico | Funcionalidades implementadas y disponibles |

## 6. Integración continua y pruebas

El proyecto utiliza xUnit para las pruebas automatizadas.

La documentación `docs/infraestructura/integracion-continua.md` registra que GitHub Actions fue desactivado temporalmente el 16/11/2026 por una limitación del uso gratuito disponible.

Mientras esa situación continúe, las verificaciones locales y la revisión de Pull Requests son especialmente importantes.

La reactivación y las verificaciones adicionales de integración continua corresponden a las tareas técnicas previstas.

El estado actual de GitHub Actions deberá comprobarse antes de afirmar que la integración continua está nuevamente operativa.

## 7. Funcionalidades pendientes de implementación o verificación

Las siguientes funcionalidades corresponden a tareas del Sprint 3 y deben verificarse individualmente:

- Registro y validación de conceptos económicos.
- Búsqueda y selección de materiales.
- Selección de referencias de precios por proveedor.
- Conservación de valores históricos.
- Guardado y recuperación de borradores.
- Gestión de cambios sin guardar con navegación libre (S3-T15; cambio aprobado el 25/11/2026, ver [consideraciones-documentacion-sprint-3.md](consideraciones-documentacion.md)).
- Cálculo de subtotales y margen.
- Conversión monetaria y redondeos.
- Integración con datosUruguay.
- Consulta y confirmación del resumen.
- Restricciones de edición y permisos.
- Pruebas QA correspondientes.

Su inclusión en esta lista no implica que todas estén pendientes actualmente; su estado efectivo depende de los avances y Pull Requests del equipo.

## 8. Riesgos y observaciones

### 8.1. Dependencias entre tareas

Las pruebas funcionales necesitan que las operaciones correspondientes estén implementadas.

Se recomienda comprobar las dependencias antes de comenzar una tarea para evitar bloqueos.

### 8.2. Cotizaciones

La disponibilidad de datosUruguay puede afectar la confirmación de datos económicos.

El sistema debe distinguir la ausencia de una cotización publicada de una falla temporal de comunicación.

### 8.3. Valores históricos

Los precios del catálogo pueden modificarse posteriormente.

Es necesario comprobar que dichas modificaciones no alteren los valores históricos previamente conservados en los presupuestos.

### 8.4. Coordinación del equipo

Cada tarea debe trabajarse en una rama independiente y fusionarse mediante Pull Request.

La documentación deberá mantenerse coherente con las decisiones aprobadas y las funcionalidades realmente integradas.

## 9. Conclusión

Se registraron las decisiones técnicas existentes sobre persistencia de conceptos, conservación de precios históricos e integración económica mediante datosUruguay.

También se identificaron las dependencias funcionales y los aspectos que requieren seguimiento durante el Sprint 3.

No se introducen nuevas reglas de negocio ni modificaciones al código de producción.

**Resultado:** documentación de decisiones técnicas y dependencias preparada para revisión del equipo.