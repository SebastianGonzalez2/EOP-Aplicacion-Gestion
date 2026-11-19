# S3-T35 — Seguimiento interno del Sprint 3

**Fecha:** 19/11/2026  

**Proyecto:** EOP — Sistema de Gestión  

**Sprint:** 3 — Elaboración económica de presupuestos

## 1. Objetivo

Registrar los avances, dificultades, dependencias y próximos pasos del Sprint 3 para facilitar la coordinación entre los integrantes del equipo.

Este documento es un registro de seguimiento y no implica modificar el alcance ni los requerimientos aprobados.

## 2. Avances registrados

| Tarea | Descripción | Situación |
|---|---|---|
| S3-T26 | Revisar flujo de pantallas | Documentación preparada y PR creado |
| S3-T24 | Revisar modelo económico | Documentación preparada y PR creado |
| S3-T25 | Revisar reglas de cálculo y cotización | Documentación preparada y PR creado |
| S3-T35 | Realizar seguimiento interno | En curso |

Las tareas S3-T24, S3-T25 y S3-T26 se trabajaron en ramas independientes y se enviaron mediante Pull Request.

Su integración definitiva en `main` queda sujeta a la aceptación y fusión de los PR correspondientes.

## 3. Dificultades y observaciones

### 3.1. Diferencia en el tratamiento de cotizaciones no disponibles

Durante S3-T25 se identificó una diferencia entre la regla transversal de monedas de `AGENTS.md` y RF-29.

`AGENTS.md` indica que, si no existe una cotización utilizable, se rechaza el guardado.

RF-29 permite guardar y continuar editando el borrador sin cotización válida, pero impide confirmar los datos económicos.

**Seguimiento:** solicitar al equipo una aclaración de esta diferencia antes de implementar comportamientos que dependan de ella.

### 3.2. Funcionalidades en desarrollo

El Sprint 3 contempla la incorporación de conceptos, cálculos económicos, guardado de borradores y pantalla de resumen.

La revisión documental no certifica que todas estas funcionalidades estén implementadas.

Su estado deberá verificarse mediante las tareas de desarrollo, revisión de Pull Requests y pruebas correspondientes.

## 4. Dependencias

- El guardado de borradores depende de que los conceptos económicos puedan registrarse y recuperarse correctamente.
- El cálculo del margen depende de los subtotales de los tipos de concepto seleccionados.
- El cálculo de totales en USD y UYU depende de una cotización válida.
- La confirmación económica depende de los cálculos y validaciones correspondientes.
- Las pruebas QA de guardado, cálculos y resumen dependen de que las funcionalidades respectivas estén implementadas.

Estas dependencias deberán considerarse al distribuir y ejecutar las tareas del sprint.

## 5. Próximos pasos

- Comprobar el estado de revisión de los Pull Requests.
- Aclarar la diferencia identificada entre `AGENTS.md` y RF-29.
- Coordinar la implementación de las funcionalidades económicas pendientes.
- Ejecutar las pruebas correspondientes cuando estén disponibles.
- Mantener actualizadas las tarjetas de Trello con sus avances y dificultades.

## 6. Conclusión

Se registraron los avances conocidos del Sprint 3 y se identificaron las principales dependencias entre las tareas de análisis, desarrollo y pruebas.

Las tres revisiones documentales realizadas cuentan con Pull Requests creados, pendientes de confirmar su integración.

El seguimiento permite identificar qué trabajo fue realizado y qué cuestiones requieren coordinación, sin asumir que las funcionalidades pendientes ya están terminadas.

**Estado:** seguimiento documentado; requiere actualización conforme avance el sprint.