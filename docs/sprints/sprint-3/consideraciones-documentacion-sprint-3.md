# Consideraciones para la documentación del Sprint 3

**Proyecto:** EOP — Sistema de Gestión  
**Sprint:** 3 — Elaboración económica de presupuestos  
**Fecha de apertura del registro:** 25/11/2026

## 1. Propósito del registro

Conservar las decisiones, modificaciones de requerimientos y observaciones surgidas durante el desarrollo del sprint para incorporarlas posteriormente al apartado «Nuevos requerimientos funcionales surgidos y modificaciones de requerimientos» de la documentación del Sprint 3.

Este archivo es un registro de trabajo acumulativo. No sustituye el anteproyecto en Word ni modifica por sí mismo los requerimientos aprobados. Para cada cambio se deberá verificar su aprobación por el procedimiento de gestión de cambios del proyecto, su implementación real y sus evidencias antes de presentar la documentación final. Se podrán incorporar nuevas entradas conforme avance el sprint.

## 2. Cambio funcional — Navegación libre durante la elaboración de presupuestos

**Fecha:** 25/11/2026  
**Tareas:** S3-T14 (guardado de borradores, antecedente), S3-T15 (gestión de cambios sin guardar), S3-T16 (restricciones de edición, antecedente).  
**Estado:** cambio aprobado (ver 2.8); copias Markdown de RF-005, RF-069, US-003, US-042 y del flujo de pantallas actualizadas en la rama `feat/S3-T15-gestion-cambios-pendientes`; anteproyecto en Word pendiente de actualizar; implementación y pruebas de S3-T15 en curso.

### 2.1. Comportamiento previsto originalmente

RF-005 establecía que, al intentar cerrar la aplicación o acceder a otra sección con cambios sin guardar, se debía mostrar una advertencia con tres opciones: Guardar, Descartar cambios y Continuar editando.

US-003 reflejaba esa regla en su descripción, reglas de negocio y criterios de aceptación. El guardado definitivo se realizaba mediante la acción explícita Guardar borrador.

### 2.2. Motivo del cambio

Durante el desarrollo de S3-T14 y el análisis de S3-T15 se observó que el flujo original obligaría a interrumpir la elaboración del presupuesto cada vez que el usuario necesitara consultar o registrar información en otro módulo (por ejemplo, un material o un proveedor).

La S3-T14 ya utiliza almacenamiento temporal en sesión, separado de la persistencia definitiva en SQL Server. Aprovechar este mecanismo permite reducir las interrupciones y evitar guardados innecesarios, sin convertir el avance temporal en información definitivamente guardada.

### 2.3. Nuevo comportamiento acordado en la conversación

- Permitir la navegación libre entre módulos de EOP sin obligar a guardar o descartar los cambios pendientes del presupuesto.
- Al regresar al mismo presupuesto, recuperar el avance temporal si la sesión y el almacenamiento siguen disponibles y el usuario mantiene autorización para editarlo.
- Conservar los borradores de distintos presupuestos de forma independiente y aislada por usuario.
- Mantener el guardado definitivo exclusivamente mediante una acción explícita de guardado. No introducir guardado automático ni modificar el estado del presupuesto por guardar el borrador.
- Mantener un indicador visible mientras existan cambios pendientes. Texto propuesto: «Tenés cambios sin guardar. Tus cambios se conservan temporalmente. Guardá el borrador para conservarlos definitivamente».
- Permitir el descarte voluntario con confirmación explícita, sin modificar los datos previamente persistidos ni afectar a otros usuarios o presupuestos.
- Al intentar cerrar sesión desde EOP con cambios pendientes, advertir al usuario y ofrecer Guardar, Descartar cambios y Continuar editando. Si el guardado falla, no cerrar sesión y conservar los cambios que continúen disponibles. La advertencia deberá contemplar también el cierre de sesión desde otros módulos.
- Ante el cierre de pestaña o navegador, recarga o salida externa, evaluar la advertencia nativa del navegador mediante beforeunload, sin interrumpir la navegación interna permitida. No se puede garantizar su aparición ni personalizar sus botones. No guardar ni descartar automáticamente al cerrar el navegador.

### 2.4. Decisión técnica sobre la sesión

Se mantiene la configuración existente de ocho horas de inactividad (IdleTimeout de ASP.NET Core Session). Se consideró ampliarla a 24 horas, pero se descartó por ahora. No se incorpora Redis ni otra infraestructura de persistencia temporal.

Actualmente la aplicación utiliza AddDistributedMemoryCache: el almacenamiento reside en memoria del proceso, no constituye un respaldo durable y puede perderse al expirar la sesión, reiniciarse el proceso o desplegarse una nueva versión. Ocho horas de inactividad no equivalen a una garantía de conservación durante ocho horas. Se mantienen los mecanismos de identificación, versionado, sincronización y control de conflictos de S3-T14.

### 2.5. Impacto en requerimientos e historias de usuario

| Artefacto | Acción a incorporar en la documentación |
|---|---|
| RF-005 — Continuar la elaboración o modificación de un presupuesto editable | Modificar la regla de advertencia obligatoria al navegar hacia otra sección. Diferenciar conservación temporal de guardado definitivo, incorporar recuperación condicionada del avance, indicador y descarte voluntario, y precisar el comportamiento de cierre de sesión y salida del navegador. Mantener estados editables, permisos y conservación histórica. |
| US-003 — Guardar avances del presupuesto | Actualizar descripción, reglas de negocio y criterios de aceptación. Sustituir la advertencia obligatoria al abandonar la pantalla por navegación libre; incluir recuperación temporal, indicador, descarte voluntario, aislamiento y advertencia de cierre de sesión. Mantener guardado explícito, estado y referencias históricas. |
| RF-069 — Iniciar sesión en el sistema | Complementar la regla de cierre de sesión con una referencia a RF-005 cuando existan cambios pendientes. No cambiar autenticación ni vinculación de identidad. |
| US-042 — Iniciar y cerrar sesión | Actualizar el escenario de cierre de sesión: distinguir el cierre normal del cierre con cambios pendientes; contemplar cancelación, guardado y descarte. |
| S3-T15 (Trello) | Ajustar la descripción y los criterios de la tarea una vez formalizado el cambio. |
| RNF | No se propone modificar su redacción ni agregar nuevos RNF. Verificar el cumplimiento de RNF-001, RNF-004, RNF-010, RNF-014, RNF-019 y RNF-020; revisar la conveniencia de agregar una referencia a RF-005 en RNF-001 por trazabilidad. RNF-019 corresponde al indicador de etapas, que debe mantenerse independiente. |
| Diseño y pruebas | Actualizar los mockups, flujo de navegación, casos de prueba y evidencias que resulten afectados; registrar la decisión en el diseño final y mantener la trazabilidad. |

No se propone incorporar un nuevo RF numerado: se trata de una modificación del comportamiento de requerimientos existentes, con impacto en usabilidad y cierre de sesión. No cambia el objetivo del producto, la arquitectura general, los roles, los estados editables ni la política de guardado definitivo.

### 2.6. Criterios de aceptación a revisar o incorporar

- Navegar a otro módulo y volver conserva los avances temporales disponibles sin escribirlos en SQL Server.
- Abrir otro presupuesto no elimina ni mezcla el avance del anterior; los usuarios no acceden a borradores ajenos.
- El indicador representa el estado real y desaparece tras un guardado exitoso o descarte confirmado.
- Guardar conserva los datos, los precios históricos y el estado conforme a las reglas vigentes. Si falla, informar sin provocar pérdida silenciosa ni cerrar sesión.
- Descartar voluntariamente elimina solo cambios pendientes del presupuesto correspondiente; cancelar conserva el avance.
- Cerrar sesión sin cambios sigue el flujo normal; con cambios pendientes se ofrece la decisión indicada y se respetan las autorizaciones.
- Verificar recarga, expiración o pérdida de sesión, conflictos entre pestañas, navegaciones internas permitidas y limitaciones de la advertencia nativa.
- Revisar valores de formularios que aún no fueron enviados al servidor: no confundirlos con los cambios que ya se incorporaron al borrador en sesión ni asumir que están conservados.

Estos son escenarios a especificar y verificar; no constituyen resultados de pruebas ejecutadas.

### 2.7. Decisiones pendientes antes de cerrar la documentación

- ~~Formalizar el cambio conforme al proceso de gestión de cambios y registrar la validación correspondiente.~~ Resuelto el 25/11/2026 (ver 2.8).
- ~~Definir y aprobar el comportamiento al cerrar sesión con varios presupuestos pendientes.~~ Resuelto el 25/11/2026 (ver 2.8).
- ~~Definir el tratamiento de datos de formularios aún no enviados.~~ Resuelto el 25/11/2026 (ver 2.8). La advertencia ante la pérdida del almacenamiento temporal se conserva tal como la implementó S3-T14.
- Verificar el comportamiento real de S3-T15 y sus pruebas, incluyendo regresiones de S3-T14 y S3-T16; incorporar enlaces a los PR y evidencias cuando estén disponibles.
- Actualizar el anteproyecto original en Word y sincronizar las copias Markdown afectadas. No declarar actualizado el original si solo se modificaron las copias.

### 2.8. Aprobación y decisiones aprobadas (25/11/2026)

**Aprobación del cambio.** El 25/11/2026 Sebastián confirmó que el cambio de navegación libre quedó aprobado por el equipo y autorizó actualizar la documentación e implementar S3-T15. La aprobación del cliente, si corresponde, no consta en este registro y deberá agregarse cuando exista.

**Decisiones de diseño aprobadas junto con el cambio:**

1. **Cierre de sesión con varios presupuestos pendientes.** La advertencia lista cada presupuesto con cambios sin guardar (código y nombre), con enlace para abrirlo, y ofrece:
   - *Guardar todo y cerrar sesión:* guarda los presupuestos uno por uno con el mismo caso de uso de «Guardar borrador». Cada guardado es una transacción independiente (no es todo o nada). Si alguno falla, la sesión no se cierra y se informa el resultado de cada presupuesto: los guardados quedan guardados y los que fallaron conservan sus cambios temporales.
   - *Descartar todo y cerrar sesión:* exige una segunda confirmación que nombra los presupuestos afectados.
   - *Continuar editando:* cierra la advertencia sin cambiar nada.
   - Para evitar conflictos entre pestañas, la advertencia envía la versión y el identificador de cada borrador que mostró. Si alguno ya no coincide, no se guarda, no se descarta y no se cierra la sesión.
   - El servidor controla los pendientes al cerrar sesión: un pedido sin decisión explícita, por ejemplo sin JavaScript, se redirige a una pantalla de confirmación equivalente.
2. **Limpieza de la sesión al cerrar sesión.** Después de un cierre de sesión correcto se vacía la sesión temporal (`Session.Clear()`), para que ningún cambio temporal quede almacenado para otra identidad en el mismo navegador.
3. **Datos de formularios escritos y no enviados.** No forman parte del borrador ni se conservan. En la pantalla de elaboración, si hay datos escritos y no enviados y el usuario navega dentro de EOP, se muestra un modal de Bootstrap («Tenés datos escritos que todavía no agregaste») con *Volver al formulario* y *Seguir sin agregarlos*. No se usa `sessionStorage` ni otro almacenamiento temporal nuevo.
4. **Advertencia nativa (`beforeunload`).** Se activa desde la plantilla general solo si el usuario tiene borradores pendientes, porque cerrar el navegador elimina la cookie de sesión y con ella los cambios temporales. No se activa en los enlaces ni formularios internos de EOP. Su texto, botones y aparición dependen del navegador.

### 2.9. Referencias

- [RF-005 y RF-069](../../requerimientos/requerimientos-funcionales.md)
- [US-003 y US-042](../../historias-de-usuario/user-stories.md)
- [Requerimientos no funcionales](../../requerimientos/requerimientos-no-funcionales.md)
- [Decisiones técnicas](../../arquitectura/decisiones-tecnicas.md)
- [Decisiones y dependencias del Sprint 3](decisiones-dependencias-sprint-3.md)
- [Configuración de sesión en Program.cs](../../../src/EOP.Gestion.WebApp/Program.cs)
- [PR #99 — S3-T14](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/pull/99)
- [Tarjeta S3-T15](https://trello.com/c/n159gvnC)

## 3. Incidencia de dedicación — viernes 27/11/2026

**Integrante:** Sebastián Gonzalez  
**Fecha:** viernes 27/11/2026  
**Dedicación prevista para la jornada:** 5 horas  
**Dedicación real:** 1 hora y 30 minutos  
**Desvío:** 3 horas y 30 minutos menos de dedicación.

Por motivos de salud, Sebastián se encontraba enfermo y no pudo cumplir con la dedicación prevista para esa jornada. Únicamente pudo completar el trabajo correspondiente a S3-T28 [QA] — Probar materiales y precios, entregado mediante el [PR #112](https://github.com/SebastianGonzalez2/EOP-Aplicacion-Gestion/pull/112).

Al documentar el Sprint 3, registrar esta incidencia en el seguimiento del esfuerzo real y explicar su impacto en el avance de la jornada. La finalización del trabajo y la integración del PR deben registrarse por separado.

## 4. Control para la documentación de cierre del Sprint 3

- [ ] Revisar todas las entradas de este registro y confirmar su estado.
- [ ] Incorporar los cambios aprobados en el apartado de modificaciones de requerimientos funcionales del Sprint 3.
- [ ] Justificar cada cambio mediante el problema detectado, decisión tomada y efecto en el sistema.
- [ ] Actualizar los artefactos afectados y comprobar su consistencia con la versión efectivamente implementada.
- [ ] Incorporar evidencia verificable y dejar expresamente identificados los puntos pendientes o no aprobados.
