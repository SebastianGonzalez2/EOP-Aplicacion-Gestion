---
name: auditar-evidencia-testing
description: "Auditar y corregir exhaustivamente la tabla de testing y la matriz de trazabilidad de un sprint de EOP, contrastando cada caso con requisitos, pruebas y ejecuciones. Usar después de la elaboración inicial para verificar los documentos y preparar un PR sin hacer merge."
---

# Auditoría independiente de evidencia de testing de EOP (Codex)

## Propósito

Actuá como **revisor independiente** de los documentos de testing preparados por otro agente (habitualmente Claude Code). No te limites a comentar: comprobá cada caso, corregí los errores demostrables, validá los dos archivos y prepará el PR para revisión humana.

No trates la salida del autor anterior ni los totales de un PR como fuentes de verdad. Tu tarea es contrastarlos con requisitos, código histórico y registros reales. Si no podés acceder a una fuente necesaria, declaralo como límite; **nunca certifiques una auditoría exhaustiva sin respaldo**.

Entrada: número de sprint, fecha de cierre/corte y, cuando corresponda, informe consolidado o fuentes adicionales. Si el sprint no se puede identificar, solicitá el dato. No infieras que un documento de otro agente está disponible solo porque el usuario lo adjuntó a su conversación.

Leé **primero** el `AGENTS.md` vigente. Aplicá los requisitos y la política de ramas/PR. Ignorá completamente las horas estimadas.

## 1. Preflight y alcance histórico

1. Comprobá `git status`, rama, cambios locales y archivos existentes. No borres ni pises trabajo ajeno.
2. Localizá `docs/testing/evidencia-testing-sprint-N.md` y `docs/testing/matriz-trazabilidad-sprint-N.md` (N = sprint). Si faltan, documentá la anomalía; no atribuyas su contenido al otro agente.
3. Consultá el informe consolidado del sprint (si existe y es accesible), tarjetas e historial Trello, requisitos pertinentes en `docs/`, PR, merges y código histórico. Títulos mal numerados del informe se resuelven por fechas, secuencia y tareas, señalando la discrepancia.
4. Reconstruí independientemente tareas **efectivamente** completadas, adelantadas, trasladadas, absorbidas, no terminadas y criterios aplicables. Conservá el ID original de Trello y contrastá con documentación de los sprints anteriores.
5. Identificá fecha de cierre/corte y commit de `main` integrado en ese momento; no confundas commits de rama, merges o integraciones provisionales de Actions.
6. Para un sprint abierto, exigí una petición explícita de revisión parcial, fechá el corte y **no** lo declares finalizado.

Si una fuente esencial no está disponible, comprobá alternativas legítimas y explicá la limitación. Las fuentes de negocio no se sustituyen por suposiciones del código.

## 2. Auditoría OBLIGATORIA, caso por caso

Para **cada identificador** de la tabla:

1. **Alcance:** verificá la tarea real del sprint; identificador original, fecha de realización y posibles duplicaciones con otro sprint.
2. **Criterio:** rastreá US, RF, RNF y criterio de aceptación en `docs/user-stories.md`, `docs/requerimientos-funcionales.md`, `docs/requerimientos-no-funcionales.md`, `docs/alcance.md` y decisiones pertinentes.
3. **Código de prueba:** abrí **cada archivo** enlazado; confirmá clase y método, código de configuración, datos, mocks/fixtures, condiciones y aserciones.
4. **Datos de entrada:** compará literalmente valores, combinaciones y precondiciones con el caso documentado.
5. **Esperado:** evaluá lo que obliga el requisito y lo que realmente comprueba el test; señalá si hay brecha.
6. **Obtenido:** comprobá salidas y efectos observados. No extrapoles, por ejemplo, del ViewModel a un recorrido visual, de SQLite a SQL Server, de la presencia de un atributo a una autorización HTTP ni de un mock al servicio real.
7. **Ejecución:** rastreá el log/TRX individual o el resultado agregado de PR, cotejando fecha, commit y árbol de pruebas usado por el runner.
8. **Estado:** asigná la etiqueta adecuada según respaldo y fecha; no uses Done, merge o mera existencia del método como prueba de aprobación.
9. **Trazabilidad:** comprobá vínculo uno a uno a la matriz, enlaces reales y límites de la evidencia.
10. **Redundancia y separación:** dividí reglas realmente diferentes; agrupá solo comportamientos homogéneos; no generes casos para inflar métricas.

Comprobá la **cobertura inversa**: para cada criterio importante completado, identificá caso que lo demuestra o falta explícita. No es necesario copiar todos los métodos individuales del repositorio al anexo. Distinguí tareas de análisis/documentación de escenarios ejecutables: no inventes tests para ellas.

### Cómo interpretar resultados históricos

- **Aprobado — ejecución informada:** hay prueba individual aprobada dentro del sprint o suite satisfactoria del PR cuyo árbol contiene el método. Aclarar en la matriz si es resultado individual o global; una cifra agregada no prueba por sí sola cada caso.
- **Aprobado — verificación posterior:** aprobación identificable posterior al cierre, contrastada con versión de test compatible. No presentarla como ejecución en sprint.
- **Aprobado — prueba manual informada:** recorrido manual y resultado explícitos informados por el equipo; respetar el alcance exacto descrito.
- **Sin evidencia de ejecución:** no se encuentra un resultado documentado. No implica falla ni justifica inventar la prueba.
- **Fallido:** ejecución real fallida; especificar contexto si hubo corrección posterior. Fallos de facturación, runner, cuota o job no iniciado **no** son tests fallidos.

Cuando Actions pruebe un merge provisional, verificá qué SHA descargó el runner (checkout), no solo la SHA del PR. Si el árbol al cierre difiere del ejecutado, no extrapoles sin comprobarlo. Un test que pasó en una versión posterior puede no acreditar el mismo comportamiento al cierre.

## 3. Corregir ambos documentos (solo estos)

**Tabla:** `docs/testing/evidencia-testing-sprint-N.md`.

Debe contener una **única tabla Markdown**, sin texto adicional, exactamente con estas columnas:

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
| --- | --- | --- | --- | --- | --- |

Mantené IDs `CP-SN-###` ya asignados; agregá nuevos IDs correlativos solo cuando se justifique, separá escenarios mezclados, corregí entradas, esperados, obtenidos y estados sin maquillar vacíos. Conservar el ID original de la tarea aunque provenga de otro sprint. Escribir para el anteproyecto, sin perder rigor.

**Matriz:** `docs/testing/matriz-trazabilidad-sprint-N.md`.

Usá estas columnas para sus casos:

| Caso del anexo | Tarea | Requisito / criterio de aceptación | Archivo y método de test, o registro manual | Evidencia de ejecución |
| --- | --- | --- | --- | --- |

Referenciá requisitos, métodos, archivos en **commits históricos adecuados**, PR, jobs, logs o QA manual; no inventes enlaces. Podés agregar fuera de la tabla de la matriz: reconstrucción del sprint, fechas, evidencia limitada, diferencias de CI, criterios parciales y pendientes. Debe haber **exactamente una entrada por ID del anexo**.

No borres un caso sin respaldo únicamente para convertir todas las filas en «Aprobado». Si un caso no fue ejecutado, mantené su vacío identificado o justificá documentalmente por qué no corresponde incluirlo.

## 4. Restricciones de modificación y pruebas

- Modificá únicamente esos dos archivos Markdown. No cambies `src/`, `tests/`, requerimientos, anteproyecto, otros sprints ni comentarios existentes.
- No implementes funcionalidades, no inventes datos y no cambies reglas de negocio.
- No ejecutes operaciones en Azure SQL, Blob Storage o recursos compartidos. Reutilizá logs existentes. Si hiciera falta ejecutar una comprobación, usá un entorno local aislado y explicá por qué; no repitas una suite enorme sin motivo.
- Verificá el diff completo: ningún cambio incidental o no relacionado.
- Si encontrás una contradicción funcional sin resolver, dejá constancia y pedí decisión; no la corrijas como si fuera un error de redacción.

## 5. Validación antes del PR

Comprobá objetivamente:

1. Las seis columnas y una sola tabla en el archivo principal; sin secciones adicionales.
2. Unicidad y formato de IDs; correspondencia uno a uno con la matriz, sin faltantes ni huérfanos.
3. Métodos y enlaces existentes, nombre de clase correcto, contenido de las aserciones y datos de entrada.
4. Evidencia de ejecución vinculada al árbol, commit, fecha y estado.
5. Alcance y fecha del sprint, traslados y duplicaciones respecto de otros sprints.
6. Criterios importantes cubiertos o limitaciones identificadas.
7. Cambios limitados a dos Markdown.
8. Las verificaciones de CI relevantes; si el PR ejecuta CI, comunicar su resultado real, sin declarar éxito antes de observarlo.

Elaborá un resumen de hallazgos: contá **casos distintos afectados** (no equipares número de cambios con fallos del sistema), correcciones realizadas, nuevos casos, pendientes y limitaciones. No generes un tercer archivo de auditoría salvo pedido explícito.

## 6. Git y Pull Request

Trabajá en `docs/SN-evidencia-testing` (N = sprint), o rama específica ya aprobada por el usuario.

- Reutilizá la rama si existe; no sobrescribas cambios locales ni incorpores accidentalmente cambios de otra tarea.
- Si no existe, creala desde `main` actualizado, nunca desde el trabajo sin integrar de otro sprint.
- Tras validar, hacé commit descriptivo que incluya la referencia del sprint; push a la rama y abrí PR hacia `main`. Si ya existe PR, actualizalo.
- En el PR: número y corte del sprint, cantidad de casos por estado, alcance, fuentes/commits, hallazgos corregidos, pruebas y límites, y tareas trasladadas.
- Ante fuentes esenciales ausentes o hallazgos críticos no resueltos: **no declararlo listo para entrega**; explicar bloqueos y dejar PR en borrador si corresponde.
- **Nunca aprobar ni integrar el PR**: otro integrante debe revisarlo y autorizar el merge.

## 7. Entrega al usuario

Informá: total de casos y desglose por estado, cantidad de casos corregidos y agregados, pendientes concretos, alcance y versión histórica revisados, verificaciones realizadas, commit, enlace al PR y si está listo **para revisión** o requiere resolver bloqueos. No digas «todo perfecto» ni «100 % validado» sin evidencia exhaustiva. Indicá expresamente si la CI del PR no se pudo ejecutar.
