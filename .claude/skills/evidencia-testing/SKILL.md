---
name: evidencia-testing
description: "Elaborar la tabla de evidencias de testing y su matriz de trazabilidad para un sprint de EOP. Usar cuando se solicite documentar pruebas de un sprint cerrado o un avance parcial explícitamente autorizado, antes de una auditoría independiente."
---

# Elaborar evidencia de testing de EOP (Claude Code)

## Objetivo y límites

Generá dos documentos de testing rigurosos, trazables y legibles para el anteproyecto del sprint solicitado. Este es el **primer paso** del flujo de dos agentes: Claude prepara y Codex audita independientemente y abre el PR. **No marques como comprobado nada que las fuentes no acrediten.**

Parámetros que aporta el usuario: número del sprint y, cuando sea relevante, informe consolidado, fecha de cierre/corte y alcance excepcional. Si el número del sprint no se puede determinar, preguntalo; no lo adivines.

- **Sprint cerrado:** documentá el alcance realmente ejecutado hasta su fecha de cierre, no el backlog planificado.
- **Sprint en curso:** no generes documentación por defecto. Hacelo solo si el usuario solicita expresamente un corte parcial; registrá la fecha y llamalo *avance parcial*, no cierre definitivo.
- **Sin fuente esencial:** identificá el impedimento y solicitá el archivo o acceso faltante. No afirmes cobertura completa ni inventes la fuente.

Leé primero `AGENTS.md` vigente y seguí sus reglas de arquitectura, seguridad, trazabilidad, Git y revisión. `CLAUDE.md` remite a `AGENTS.md`; no dupliques ni modifiques esas reglas aquí. Ignorá por completo las horas estimadas.

## 1. Reconstrucción del sprint

1. Comprobá la rama actual, `git status` y archivos locales sin versionar. Nunca descartes cambios ajenos.
2. Consultá el informe consolidado del sprint **si está disponible**, la lista Done y el historial de las tarjetas en Trello, los PR y sus merges. El nombre de la tarjeta puede indicar otro sprint: conservá su ID original y clasificá por **ejecución efectiva**.
3. Identificá fechas reales del sprint, corte histórico y último commit de `main` integrado al cierre; no confundas un commit de rama de PR con el merge, ni una ejecución posterior con una del sprint.
4. Comprobá tareas adelantadas, replanificadas, incompletas y absorbidas por otras. Contrastá con las matrices de sprints anteriores; no dupliques sus casos.
5. Si el título del informe está mal numerado (por ejemplo, «Sprint #»), identificá la sección por orden, fechas y tareas y dejá constancia de la discrepancia, sin descartarla ni inventar su alcance.
6. Las tareas de análisis, planificación o documentación solo generan casos de **testing** cuando existe una verificación pertinente; no fabriques un test por cada tarjeta Done.

Fuentes prioritarias: anteproyecto e informe proporcionados; `docs/user-stories.md`, `docs/requerimientos-funcionales.md`, `docs/requerimientos-no-funcionales.md`, `docs/alcance.md` y decisiones técnicas aplicables; Trello; historial Git; tests y resultados disponibles. Leé solo las secciones pertinentes. Si las fuentes contradicen una regla de negocio, señalalo: no la resuelvas por cuenta propia.

## 2. Verificar la relación requisito → prueba → ejecución

Para cada criterio de aceptación relevante de una tarea efectivamente realizada:

1. Identificá US, RF, RNF, regla y escenario comprobado.
2. Localizá tests reales dentro de `tests/EOP.Gestion.Tests/` u otros registros legítimos de QA. Leé el **cuerpo del método, datos, preparación, dependencias y aserciones**; no te bases solo en el nombre.
3. Registrá datos de entrada concretos, resultado esperado por el requisito y lo que el test **realmente observa**. No atribuyas una pantalla revisada al navegador si solo hay una inspección de atributos, un ViewModel, un repositorio simulado o HTML estático.
4. Distinguí pruebas unitarias, integración HTTP, SQL Server/LocalDB, bases simuladas y manuales; no atribuyas a SQL Server una ejecución con SQLite o mocks.
5. Localizá el respaldo de ejecución: método aprobado en log/TRX, suite completa informada en PR, resultado manual explícito o ausencia de resultado.
6. Cuando sea posible, cotejá el archivo del árbol **ejecutado** con la versión histórica de cierre; los tests que evolucionaron después no acreditan sin más el estado anterior.
7. Incluí casos representativos para todos los criterios importantes, sin transcribir cada test individual. Agrupá únicamente comprobaciones del mismo comportamiento; separá entradas, reglas o resultados materialmente diferentes.
8. Comprobá también la dirección inversa: criterio completado → casos que lo cubren o brecha explícita.

**Existencia de test ≠ ejecución satisfactoria.** Que Trello diga Done, que un PR esté mergeado o que el total global pase tampoco demuestra por sí solo una verificación manual específica.

### Estados autorizados

- **Aprobado — ejecución informada:** hay ejecución satisfactoria del sprint en CI por método, o suite satisfactoria informada y se comprobó que el test estaba en ese árbol. Indicá en la matriz si el respaldo es individual o global y sus límites.
- **Aprobado — verificación posterior:** ejecución satisfactoria identificable después del cierre, vinculada al método y a una versión compatible. Señalá que no ocurrió en el sprint.
- **Aprobado — prueba manual informada:** el equipo describió explícitamente una comprobación manual satisfactoria, con el alcance informado. No la presentes como test automatizado.
- **Sin evidencia de ejecución:** no se localizó el resultado del escenario; no equivale a fallo. No sustituyas el resultado por «se implementó».
- **Fallido:** resultado de prueba realmente fallido y verificado. Si después se corrigió, distinguí las fechas y revisiones; no confundas un fallo de infraestructura o facturación de Actions con un test fallido.

No inventes entradas, resultados, fechas ni logs. Si la evidencia es parcial, acotá la afirmación.

## 3. Archivo principal: SOLO tabla para el anteproyecto

Creá o actualizá `docs/testing/evidencia-testing-sprint-N.md`, reemplazando N por el sprint real.

**Una sola tabla Markdown, sin título, introducción, notas, conclusiones ni texto fuera de ella.** Encabezados exactos y en este orden:

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
| --- | --- | --- | --- | --- | --- |

- ID con formato `CP-SN-001`, `CP-SN-002`, etc., donde N es el número real del sprint. No renumeres IDs existentes durante una ampliación; agregá nuevos de forma estable.
- **Tarea:** conservar su ID de Trello original, incluso si empieza con otro sprint.
- **Datos:** valores/escenarios reales; resultado esperado según especificación; obtenido según una observación respaldada.
- **Estado:** exactamente una de las cinco etiquetas indicadas.
- Una fila puede combinar métodos que prueben el mismo comportamiento; separá casos que oculten condiciones distintas.
- No agregues encabezados de sección adicionales ni columnas de trazabilidad en este archivo.
- Mantené texto comprensible para una defensa universitaria, sin exageraciones.

## 4. Matriz de trazabilidad

Creá o actualizá `docs/testing/matriz-trazabilidad-sprint-N.md` con:

| Caso del anexo | Tarea | Requisito / criterio de aceptación | Archivo y método de test, o registro manual | Evidencia de ejecución |
| --- | --- | --- | --- | --- |

Cada caso de la tabla principal debe aparecer **exactamente una vez** en la matriz. Usá enlaces reales a archivos y commits históricos, nombre exacto de clase/método y enlace a PR, job, log/TRX o evidencia manual. Si no existe, escribí explícitamente qué falta. No inventes enlaces.

Podés incluir **solo en la matriz**: fechas de corte, reconstrucción de alcance, tareas trasladadas, explicación de estados, diferencias entre resultados de rama/merge, criterios parcialmente cubiertos, limitaciones y decisiones documentales. Distinguí pruebas del sprint, verificaciones posteriores y el método de comprobación. Los ejemplos de Sprint 1 y 2 orientan el formato, **no** sustituyen la investigación del sprint actual.

## 5. Rama y restricciones

Usá `docs/SN-evidencia-testing` como rama de trabajo (N = sprint), salvo instrucción específica del usuario.

- Si existe, reutilizala sin pisar cambios; si no, creala desde `main` actualizado, nunca desde la rama de documentación de otro sprint.
- No edites código de producción, tests, requisitos, anteproyecto ni documentos de otros sprints.
- No implementes funcionalidades ni alteres el alcance.
- No ejecutes operaciones en Azure o bases compartidas. Reutilizá evidencias existentes; si falta alguna prueba, anotá la brecha en vez de simularla.
- **En esta fase no hagas commit, push ni PR.** La auditoría independiente de Codex prepara la integración.

## 6. Comprobaciones y entrega

Antes de terminar, verificá:
- Exactamente seis columnas por fila en la tabla principal, IDs únicos y sin filas de prosa.
- Correspondencia uno a uno de IDs entre tabla y matriz.
- Tareas incluidas según el corte; tareas excluidas por traslado o por no estar terminadas.
- Requisitos, entradas y aserciones coherentes; métodos existentes y enlaces comprobables.
- Estados coherentes con ejecuciones, fechas y versiones; ausencia de duplicación entre sprints.
- Diff limitado a los dos archivos solicitados.

Informá al usuario: número y fecha de corte, cantidad de casos y conteo por estado, tareas incluidas/excluidas, tareas Done sin caso (si corresponde), evidencias faltantes y limitaciones. Si falta alguna fuente esencial, no afirmes que la documentación está completa. **Dejá los archivos preparados para la auditoría independiente.**
