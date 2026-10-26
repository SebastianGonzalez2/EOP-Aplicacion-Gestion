# Decisiones técnicas

Registro de las decisiones de diseño tomadas durante el desarrollo, con su motivo y las alternativas descartadas.

Alimenta la sección «Diseño final de la solución» del documento y la tarea de documentación de decisiones técnicas de cada sprint. Cada decisión debe poder explicarse en la defensa.

Las reglas vigentes que se derivan de estas decisiones están en [`AGENTS.md`](../AGENTS.md); este archivo guarda el **porqué**.

---

## DT-01 — Tres proyectos en la solución

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión.** La solución se divide en `Core`, `Infrastructure` y `WebApp`, más un proyecto de pruebas. `Core` no referencia a ningún otro proyecto.

**Motivo.** La arquitectura documentada define cuatro responsabilidades separadas: presentación, lógica de negocio, persistencia e integraciones externas. Un proyecto por responsabilidad convierte esa separación en una restricción del compilador en lugar de una convención que se respeta por disciplina.

**Alternativas descartadas.**

- *Un único proyecto con carpetas.* Más simple al principio, pero nada impide que un Controller consulte la base directamente, y con 58 historias de usuario eso termina pasando.
- *Clean Architecture completa con capa de aplicación separada, CQRS y mediador.* Resuelve problemas de escala que este sistema no tiene, contradice la arquitectura documentada como monolito modular y no entra en las horas estimadas.

---

## DT-02 — Separación entre servicios de dominio y casos de uso

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión.** Dentro de `Core`, las reglas de negocio puras viven en `ServiciosDominio/` y la orquestación de operaciones completas del usuario en `CasosDeUso/`.

**Motivo.** Sin una regla escrita, con dos personas trabajando en paralelo y con dos asistentes de IA distintos, la misma validación termina implementada en varios lugares. La regla práctica: si una regla sirve para más de un caso de uso, es servicio de dominio; si describe un flujo que el usuario reconoce como una acción, es caso de uso.

**Alternativas descartadas.**

- *Un único `Servicios/`.* Menos carpetas, pero mezcla reglas reutilizables con orquestación, y las primeras dejan de ser testeables de forma aislada.

---

## DT-03 — Patrón Repositorio sobre Entity Framework Core

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión.** El acceso a datos desde `Core` pasa por interfaces de repositorio definidas en `Core/Interfaces` e implementadas en `Infrastructure/Persistencia/Repositorios`. Un repositorio por agregado, nunca genérico, sin exponer `IQueryable`.

**Motivo.** Mantiene `Core` sin ninguna dependencia de Entity Framework Core, lo que hace que la regla de dependencias sea imposible de romper y permite probar los casos de uso sin base de datos.

**Costo asumido.** `DbContext` ya cumple el rol de Unit of Work y `DbSet<T>` el de repositorio, así que esta capa agrega archivos que en parte delegan. Se acepta a cambio del desacoplamiento y de la testeabilidad.

**Excepción.** Los listados con filtros combinables y los indicadores económicos no pasan por repositorios: van por servicios de consulta en `Infrastructure` que usan EF Core directamente y devuelven DTOs. Incluirlos haría crecer los repositorios con métodos de un solo uso.

**Alternativas descartadas.**

- *Usar `DbContext` directamente desde los casos de uso.* Menos código, pero ata `Core` a EF Core y obliga a una base real para probar.
- *Repositorio genérico `IRepositorio<T>`.* Termina necesitando métodos específicos igual, o devolviendo `IQueryable`, que filtra EF Core al dominio.

---

<!--
Formato para las próximas:

## DT-NN — Título

**Fecha:** dd/mm/aaaa · **Sprint:** N

**Decisión.**

**Motivo.**

**Alternativas descartadas.**
-->
