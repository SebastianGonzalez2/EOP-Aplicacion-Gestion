# Documentación del proyecto

Acá está la documentación de trabajo de EOP, organizada por tema. **El anteproyecto en Word es la fuente original de la especificación**; sus copias de trabajo deben mantenerse sincronizadas.

| Carpeta | Qué contiene |
|---|---|
| [`requerimientos/`](./requerimientos/) | Necesidades, objetivos, alcance y RF/RNF. |
| [`historias-de-usuario/`](./historias-de-usuario/) | Historias y criterios de aceptación. |
| [`arquitectura/`](./arquitectura/) | Decisiones técnicas y revisiones del modelo y cálculo. |
| [`infraestructura/`](./infraestructura/) | Azure SQL, Blob Storage e integración continua. |
| [`diseno-visual/`](./diseno-visual/) | Diseño de pantallas, flujos y referencias visuales. |
| [`guias/`](./guias/) | Manuales de capacitación. |
| [`testing/`](./testing/) | Evidencias y matrices de trazabilidad por sprint. |
| [`pruebas-concepto/`](./pruebas-concepto/) | Resultados y evaluaciones de experimentos; el código experimental queda fuera de docs. |
| [`sprints/`](./sprints/) | Decisiones, cambios de documentación e implementaciones por sprint; el seguimiento actual se consulta en Trello. |
| [`gestion-proyecto/`](./gestion-proyecto/) | Registros y documentación de gestión. |

## Archivos principales

### requerimientos

- [alcance.md](./requerimientos/alcance.md)
- [necesidades-y-objetivos.md](./requerimientos/necesidades-y-objetivos.md)
- [requerimientos-funcionales.md](./requerimientos/requerimientos-funcionales.md)
- [requerimientos-no-funcionales.md](./requerimientos/requerimientos-no-funcionales.md)

### historias-de-usuario

- [user-stories.md](./historias-de-usuario/user-stories.md)

### arquitectura

- [decisiones-tecnicas.md](./arquitectura/decisiones-tecnicas.md)
- [revision-modelo-economico.md](./arquitectura/revision-modelo-economico.md)
- [revision-reglas-calculo-cotizacion.md](./arquitectura/revision-reglas-calculo-cotizacion.md)

### infraestructura

- [azure-blob-storage.md](./infraestructura/azure-blob-storage.md)
- [azure-sql-database.md](./infraestructura/azure-sql-database.md)
- [integracion-continua.md](./infraestructura/integracion-continua.md)
- [revision-integracion-tecnica.md](./infraestructura/revision-integracion-tecnica.md)

### diseno-visual

- [diseno-visual.md](./diseno-visual/diseno-visual.md)
- [flujo-pantallas-presupuesto.md](./diseno-visual/flujo-pantallas-presupuesto.md)

### guias

- [Guía de autenticación con Microsoft Entra ID](./guias/autenticacion-entra-id/README.md) · [PDF](./guias/autenticacion-entra-id/salida/Guia-Autenticacion-Entra-ID.pdf)

### testing

- [evidencia-testing-sprint-1.md](./testing/evidencia-testing-sprint-1.md)
- [evidencia-testing-sprint-2.md](./testing/evidencia-testing-sprint-2.md)
- [matriz-trazabilidad-sprint-1.md](./testing/matriz-trazabilidad-sprint-1.md)
- [matriz-trazabilidad-sprint-2.md](./testing/matriz-trazabilidad-sprint-2.md)

### pruebas-concepto

- [evaluacion-ia-s3-t04.md](./pruebas-concepto/evaluacion-ia-s3-t04.md)
- [S3-T03-datosuruguay.md](./pruebas-concepto/S3-T03-datosuruguay.md)

### sprints

- [Índice del Sprint 3](./sprints/sprint-3/README.md)
- [consideraciones-documentacion-sprint-3.md](./sprints/sprint-3/consideraciones-documentacion.md)
- [decisiones-dependencias-sprint-3.md](./sprints/sprint-3/decisiones-tecnicas-y-dependencias.md)
- [s3-t09-buscador-materiales.md](./sprints/sprint-3/implementaciones/s3-t09-buscador-materiales.md)
- [s3-t11-consulta-precios.md](./sprints/sprint-3/implementaciones/s3-t11-consulta-precios.md)
- [s3-t12-seleccion-conservacion-precio.md](./sprints/sprint-3/implementaciones/s3-t12-seleccion-conservacion-precio.md)
- [s3-t18-servicio-cotizaciones.md](./sprints/sprint-3/implementaciones/s3-t18-servicio-cotizaciones.md)

### gestion-proyecto

- [uso-de-ia.md](./gestion-proyecto/uso-de-ia.md)

## Cómo usar esta documentación

Antes de implementar una tarea, leé [AGENTS.md](../AGENTS.md) y las secciones de requerimientos, alcance e historias de usuario que correspondan. La trazabilidad se mantiene con los identificadores existentes:

    Necesidad (N-xx) → Objetivo (OA/OP-xx) → RF-xxx → US-xxx → tarea de sprint → prueba

## Pendiente

Las 18 historias excluidas están marcadas en el archivo de historias de usuario, pero en el anexo del documento Word todavía no llevan esa aclaración. Conviene incorporarla antes de la próxima entrega.
