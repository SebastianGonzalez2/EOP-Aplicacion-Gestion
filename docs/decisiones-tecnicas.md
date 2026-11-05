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

## DT-04 — Reglas de validación del RUT uruguayo

**Fecha:** 04/11/2026 · **Sprint:** 2

**Decisión.** `ValidadorRut` exige 12 dígitos, prefijo entre 01 y 22, número de serie distinto de 000000, sufijo 001 y dígito verificador correcto. La misma clase valida el RUT de Clientes y de Proveedores.

**Motivo.** Los requerimientos piden validar la estructura uruguaya del RUT (RF-23, RF-86, RF-89, US-053, US-054), pero no explican cuáles son sus reglas. El validador anterior solo controlaba el largo y el dígito verificador, y por eso aceptaba números como `000000000000`.

**De dónde salen las reglas.**

| Regla | Fuente |
|---|---|
| 12 dígitos: prefijo de 2, serie de 6, sufijo de 3 y dígito verificador | DGI, División Informática, expediente 2024-5-1-0002222 |
| El prefijo es la Unidad Operativa de inscripción, la serie es secuencial y el sufijo es una constante | DGI, el mismo expediente |
| Prefijo entre 01 y 22, serie distinta de 000000 y sufijo 001 | Biblioteca pública `python-stdnum`, no es una fuente oficial |
| Factores 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 del dígito verificador | Biblioteca pública `python-stdnum`, no es una fuente oficial |

La DGI no publica los valores válidos de cada parte ni el algoritmo del dígito verificador: el Ministerio de Economía y Finanzas negó ese acceso en el mismo expediente por tratarse de información reservada, para que nadie pueda fabricar números en apariencia válidos. Por eso esos valores no se pueden confirmar con una fuente oficial.

Las reglas se probaron contra 203 RUT de la lista y los ejemplos de esa biblioteca, y los aceptó a todos. Esos RUT no se guardaron en el repositorio.

**Límites.**

- Que un RUT pase esta validación no prueba que exista ni que esté activo. Eso solo lo puede responder la DGI.
- Si la DGI empezara a usar un prefijo mayor que 22, habría que ampliar el rango, porque hoy esos RUT se rechazarían.

**Alternativas descartadas.**

- *Controlar solo el largo y el dígito verificador.* No cumple lo que piden los requerimientos sobre la estructura.
- *Limitar el prefijo a 01–21.* Rechazaría RUT que figuran como válidos en esa lista, como los que empiezan con 22.
- *Consultar el servicio de la DGI.* Es la única forma de saber si un RUT existe, pero es una integración externa que los requerimientos no piden.

**Fuentes.**

- [Expediente 2024-5-1-0002222, Ministerio de Economía y Finanzas](https://www.gub.uy/ministerio-economia-finanzas/sites/ministerio-economia-finanzas/files/2024-04/Expediente-2024-5-1-0002222%20Francisco%20Xavier.pdf)
- [Validación del RUT en `python-stdnum`](https://github.com/arthurdejong/python-stdnum/blob/master/stdnum/uy/rut.py)

---

<!--
Formato para las próximas:

## DT-NN — Título

**Fecha:** dd/mm/aaaa · **Sprint:** N

**Decisión.**

**Motivo.**

**Alternativas descartadas.**
-->

---

# Nuevos requerimientos no funcionales

Requerimientos surgidos durante el desarrollo, anotados acá para pasarlos al apartado «Nuevos requerimientos surgidos» de la documentación de cada sprint. No modifican los requerimientos vigentes en `requerimientos-no-funcionales.md`.

## Sprint 2 — Validación completa del RUT uruguayo

**Descripción**

El sistema deberá validar en el servidor todo RUT que se registre o modifique, comprobando su estructura uruguaya y su dígito verificador antes de guardarlo. La validación será la misma en todos los módulos que reciban un RUT.

**Reglas**

1. Antes de validar se quitan los separadores habituales: puntos, guiones y espacios.
2. El RUT debe tener exactamente 12 dígitos.
3. Los dos primeros dígitos (prefijo) deben estar entre 01 y 22.
4. Los seis dígitos siguientes (número de serie) no pueden ser todos cero.
5. Los tres dígitos siguientes (sufijo) deben ser 001.
6. El último dígito debe coincidir con el dígito verificador calculado a partir de los 11 anteriores.
7. Un RUT que no cumpla alguna de estas reglas se rechaza, con un mensaje claro y sin guardar datos.

Tipo: Integridad de datos.

Referencias: RF-23, RF-86, RF-89, RF-109, US-009, US-053, US-054, US-072, RNF-10, RNF-18, DT-04.
