# Decisiones técnicas

Registro de las decisiones de diseño tomadas durante el desarrollo, con su motivo y las alternativas descartadas.

Alimenta la sección «Diseño final de la solución» del documento y la tarea de documentación de decisiones técnicas de cada sprint. Cada decisión debe poder explicarse en la defensa.

Las reglas vigentes que se derivan de estas decisiones están en [`AGENTS.md`](../AGENTS.md); este archivo guarda el **porqué**.

---

## DT-01 — Organización del proyecto en capas

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión:** Dividimos la aplicación en tres proyectos principales: `Core`, `Infrastructure` y `WebApp`, además de un proyecto separado para las pruebas.

**Justificación:** Elegimos esta organización para separar las responsabilidades del sistema. En `Core` tenemos las reglas de negocio, en `Infrastructure` el acceso a datos y las integraciones, y en `WebApp` los controladores y las pantallas.

De esta manera, evitamos mezclar todo el código en un mismo lugar. También podemos probar la lógica de negocio sin depender directamente de la base de datos o de la interfaz.

La desventaja es que necesitamos más archivos y algunas configuraciones adicionales, pero consideramos que vale la pena para mantener el proyecto ordenado a medida que crece.

---

## DT-02 — Separación entre servicios de dominio y casos de uso

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión:** Dentro de `Core`, separamos las reglas de negocio reutilizables de las operaciones completas que realiza el usuario.

**Justificación:** Nos pareció importante distinguir entre una regla que puede utilizarse en diferentes funcionalidades y una operación concreta del sistema.

Por ejemplo, validar un RUT es una regla que pueden necesitar tanto Clientes como Proveedores. En cambio, registrar un cliente es una operación que reúne varias validaciones y finalmente guarda la información.

Esta separación evita repetir código y facilita las pruebas. También nos ayuda a mantener un criterio común cuando trabajamos los dos integrantes del equipo.

---

## DT-03 — Uso del patrón Repositorio para acceder a los datos

**Fecha:** 26/10/2026 · **Sprint:** 1

**Decisión:** Los casos de uso acceden a los datos mediante interfaces de repositorio. Las implementaciones que utilizan Entity Framework Core quedan en `Infrastructure`.

Para los listados con filtros combinables utilizamos servicios de consulta específicos, también ubicados en `Infrastructure`.

**Justificación:** Queríamos que las reglas de negocio no dependieran directamente de cómo guardamos los datos.

Esto nos permite, por ejemplo, probar un caso de uso utilizando un repositorio de prueba, sin tener que conectarnos a SQL Server.

Para los listados elegimos un enfoque más directo porque suelen necesitar consultas y filtros particulares. Si agregáramos todos esos métodos a los repositorios, terminarían teniendo demasiadas responsabilidades.

Sabemos que esta organización requiere escribir algo más de código, pero nos permite mantener mejor separadas las capas.

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

## DT-05 — Largo máximo de 200 caracteres para los nombres

**Fecha:** 06/11/2026 · **Sprint:** 1

**Decisión.** El nombre de Cliente y el de Material admiten como máximo 200 caracteres, igual que el de Proveedor, que ya tenía ese largo desde S2-T10. El límite se aplica en tres lugares que usan el mismo valor:

- la columna de la base (`HasMaxLength(200)`, migraciones `S1_T27_*`);
- el caso de uso (`ValidarDatosCliente.LargoMaximoNombre` y `RegistrarMaterial.LargoMaximoNombre`), que es el control definitivo del servidor;
- el `ViewModel` (`[StringLength]`), que solo sirve para avisar en el formulario.

Los espacios del principio y del final se quitan antes de medir y no se guardan.

**Motivo.** Los requerimientos (RF-89, RF-108 y US-041) piden que el nombre sea obligatorio, pero no fijan un largo: esto es una decisión técnica, no una regla de negocio. La revisión del modelo de S1-T27 encontró dos problemas con las columnas sin límite (`nvarchar(max)`):

- En Material, el nombre no tenía límite, pero `NombreNormalizado` sí (450). Un nombre muy largo pasaba la validación y SQL Server lo rechazaba con un error de truncado, que el usuario veía como una página de error genérica.
- Una columna `nvarchar(max)` acepta textos de cualquier tamaño y no se puede indexar, lo que complicaría una búsqueda por nombre más eficiente en el futuro.

Se eligió 200 para que los tres módulos que registran organizaciones o materiales se comporten igual. Alcanza de sobra para razones sociales y denominaciones de materiales reales.

**Alternativas descartadas.**

- *Dejar `nvarchar(max)`.* Mantiene el error de truncado en Material y no protege la base de textos desmedidos.
- *Usar 450 en Material para igualarlo con `NombreNormalizado`.* Arregla el error, pero deja tres módulos con largos distintos sin ningún motivo de negocio.
- *Validar el largo solo en el formulario.* No es control: un POST armado a mano lo saltea (RNF-10 y RNF-18).

---

## DT-06 — Autenticación mediante Microsoft Entra ID

**Sprint:** 1

**Decisión:** Utilizamos Microsoft Entra ID para autenticar a los usuarios, integrándolo mediante `Microsoft.Identity.Web` y OpenID Connect. Microsoft se encarga de la autenticación y del segundo factor, mientras que EOP administra los usuarios habilitados y sus roles.

**Justificación:** El uso de Entra ID ya estaba establecido en los requerimientos. Nuestra decisión fue cómo integrarlo con la aplicación y cómo relacionar las cuentas de Microsoft con los usuarios de EOP.

No desarrollamos un sistema propio de contraseñas, porque Microsoft ya proporciona los mecanismos necesarios para comprobar la identidad.

Cuando una persona ingresa por primera vez, EOP vincula su cuenta de Microsoft con el usuario que el Administrador registró previamente. Después, utiliza el identificador de Microsoft para reconocerlo, en lugar de depender de un correo electrónico que podría cambiar.

Así mantenemos separadas dos responsabilidades: Microsoft comprueba quién es la persona y EOP determina si puede acceder al sistema.

---

## DT-07 — Control de permisos desde el servidor

**Sprint:** 1

**Decisión:** Los permisos se controlan desde el servidor utilizando los tres roles fijos del sistema: Administrador, Supervisor y Operador.

Además, el sistema vuelve a comprobar el estado y el rol del usuario en cada solicitud autenticada.

**Justificación:** No alcanza con ocultar botones o restringir opciones en la interfaz, porque un usuario podría intentar acceder directamente a una dirección o enviar una solicitud al servidor.

Por eso, las acciones protegidas comprueban los permisos antes de ejecutarse.

También decidimos consultar el rol actual del usuario en cada solicitud. De esta manera, si un Administrador cambia el rol de alguien o desactiva su cuenta, el cambio se aplica sin depender de que esa persona cierre sesión y vuelva a entrar.

Esto implica realizar consultas adicionales a la base de datos, pero lo consideramos adecuado para mantener actualizados los permisos.

---

## DT-08 — Persistencia con Entity Framework Core, SQL Server y migraciones

**Sprint:** 1

**Decisión:** Utilizamos Entity Framework Core con el enfoque *Code First*, SQL Server como base de datos y migraciones para administrar los cambios del esquema.

**Justificación:** Estas tecnologías forman parte del stack definido para el proyecto. Optamos por organizar las entidades y sus configuraciones en código, y utilizar migraciones para mantener actualizada la estructura de la base de datos.

Esto permite que los cambios de las tablas queden registrados junto con el código y puedan revisarse antes de incorporarlos.

También facilita que los dos integrantes trabajemos sobre una misma estructura y podamos identificar qué cambios se hicieron.

La principal precaución es revisar correctamente las migraciones, especialmente cuando modifican tablas que ya contienen información.

---

## DT-09 — Validaciones en la aplicación y restricciones en la base de datos

**Sprint:** 1

**Decisión:** Además de validar los datos desde la aplicación, utilizamos restricciones de SQL Server para proteger la integridad de la información.

**Justificación:** Aunque los casos de uso validen los datos antes de guardarlos, también queremos evitar que la base acepte información incorrecta si alguna validación se omite o aparece un error en el código.

Por eso utilizamos restricciones para cuestiones importantes, como evitar valores duplicados, impedir roles inválidos y exigir determinados datos obligatorios.

Esto también ayuda cuando dos solicitudes intentan registrar el mismo dato simultáneamente: la base de datos es la que garantiza que no se guarden duplicados.

La desventaja es que algunas reglas deben mantenerse tanto en el código como en la base, por lo que tenemos que asegurarnos de que sean coherentes.

---

## DT-10 — Protección del último Administrador activo ante operaciones simultáneas

**Sprint:** 1

**Decisión:** Utilizamos transacciones y mecanismos de bloqueo de SQL Server para evitar que el sistema quede sin administradores activos, incluso cuando se intentan realizar varios cambios de rol simultáneamente.

**Justificación:** No alcanza con comprobar que exista más de un Administrador antes de modificar un rol.

Por ejemplo, si existen dos administradores y ambos intentan cambiar su rol al mismo tiempo, las dos solicitudes podrían comprobar que hay otro Administrador disponible y permitir los cambios. El resultado sería un sistema sin administradores.

Para evitarlo, hacemos que estas operaciones se coordinen desde la base de datos. Así, una solicitud espera a que termine la otra y puede comprobar la situación actual antes de continuar.

Es una solución más compleja que hacer una consulta normal, pero protege una regla importante del sistema.

---

## DT-11 — Pruebas con bases de datos de prueba y SQL Server real

**Sprint:** 1

**Decisión:** Para probar la persistencia utilizamos SQLite en memoria en muchas de las pruebas de integración, y SQL Server real en las pruebas que dependen de comportamientos específicos de ese motor.

**Justificación:** No todas las pruebas necesitan levantar una base de datos SQL Server completa. Para comprobar operaciones comunes, una base SQLite en memoria nos permite ejecutar pruebas de forma más sencilla y aislada.

Sin embargo, SQLite no se comporta exactamente igual que SQL Server. Por ejemplo, existen diferencias en los bloqueos, ciertas restricciones y las migraciones.

Por eso, cuando necesitamos comprobar esos comportamientos, ejecutamos pruebas contra SQL Server.

De esta manera combinamos pruebas prácticas para el desarrollo cotidiano con otras que verifican cómo funcionará realmente la persistencia del sistema.

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
