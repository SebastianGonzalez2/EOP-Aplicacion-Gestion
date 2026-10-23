# Registro de uso de IA generativa

Este archivo alimenta la sección **«Uso de sistemas de Inteligencia Artificial Generativa durante el proyecto»** del documento del Proyecto Integrador, que exige describir todas las herramientas de IA utilizadas e incluir evidencias de su uso.

## Cómo se completa

**Una línea por semana**, los viernes. Dos minutos. No hace falta anotar cada consulta: alcanza con qué herramienta se usó, para qué y qué quedó como evidencia.

No hace falta registrar el trabajo de programación en detalle: el historial de commits del repositorio ya es evidencia con fecha y autor.

Las skills `consistencia-requerimientos-eop` y `cierre-de-sprint-eop` agregan su propia línea cada vez que se ejecutan.

## IA como herramienta de trabajo

| Fecha | Herramienta | Para qué | Resultado | Evidencia |
|---|---|---|---|---|
| 23/10/2026 | Claude | Extraer el cronograma del anteproyecto y generar las cards del tablero | 58 user stories y 38 tareas del sprint 1 creadas en Trello | Tablero `EOP - Sistema de Gestion` |
| 23/10/2026 | Claude | Chequeo de consistencia entre la tabla de alcance y los anexos detallados | 14 RF y 18 US declaradas fuera de alcance sin marcar en el anexo | Informe de consistencia |
| 23/10/2026 | Claude | Creación de dos skills de apoyo al proyecto | `consistencia-requerimientos-eop` y `cierre-de-sprint-eop` | Definición de cada skill |

## IA integrada en el producto

El sistema desarrollado incorpora IA generativa como parte de su funcionalidad. Se documenta aparte porque es objeto del proyecto, no herramienta de construcción.

| Funcionalidad | User stories | Servicio | Estado |
|---|---|---|---|
| Transcripción de relevamientos en audio | US-005 | OpenAI API | Previsto — sprint 6 |
| Análisis del relevamiento y generación de la tabla de conceptos | US-006 | OpenAI API | Previsto — sprint 6 |

Quedó **fuera del alcance** de la primera versión:

- Generación del Word de propuesta comercial mediante IA (US-015). EOP elabora la propuesta externamente y sube el PDF final.
- Extracción automática de datos de facturas mediante Azure AI Document Intelligence. Las facturas se registran manualmente.

## Registro semanal

Se agrega una línea por semana a partir del inicio del sprint 1 (26/10/2026).

<!--
Formato:
Semana N (dd-dd/mm) — <herramienta> para <qué>. <herramienta> para <qué>.
-->
