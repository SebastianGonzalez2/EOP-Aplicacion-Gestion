# Documentación del proyecto

Copia de trabajo de la documentación del Sistema de Gestión EOP, para consulta durante
el desarrollo. **El anteproyecto en Word es el documento original**; estos archivos se
regeneran a partir de él cuando cambia.

| Archivo | Contenido |
|---|---|
| [`necesidades-y-objetivos.md`](./necesidades-y-objetivos.md) | Necesidades del cliente (N-xx) y objetivos académicos y de producto |
| [`requerimientos-funcionales.md`](./requerimientos-funcionales.md) | Los 109 RF con sus reglas de negocio, dependencias y prioridad |
| [`requerimientos-no-funcionales.md`](./requerimientos-no-funcionales.md) | Los 21 RNF |
| [`user-stories.md`](./user-stories.md) | Las 76 US con criterios de aceptación; las excluidas llevan aviso |
| [`alcance.md`](./alcance.md) | Funcionalidades incluidas y excluidas de la primera versión |
| [`decisiones-tecnicas.md`](./decisiones-tecnicas.md) | Decisiones de diseño con su motivo y alternativas descartadas |
| [`azure-blob-storage.md`](./azure-blob-storage.md) | Configuración de almacenamiento privado Azure Blob Storage, verificaciones, servicio de la aplicación y pendientes (S2-T02, S2-T03) |
| [`uso-de-ia.md`](./uso-de-ia.md) | Registro de uso de IA generativa |

## Cómo usarlos

Al implementar una tarea, consultá el requerimiento real antes de asumir el
comportamiento. Las reglas transversales están en [`AGENTS.md`](../AGENTS.md).

Ejemplo de referencia en un prompt:

    Implementá S1-T19 según el RF-085 de docs/requerimientos-funcionales.md

## Trazabilidad

    Necesidad (N-xx) → Objetivo (OA/OP-xx) → RF-xxx → US-xxx → tarea de sprint → prueba

## Pendiente

Las 18 user stories excluidas están marcadas en `user-stories.md`, pero en el anexo del
documento Word todavía no llevan esa aclaración. Conviene incorporarla antes de la
próxima entrega.
