### Configuración técnica del proyecto

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-001 | S1-T01 / S1-T02 — Configurar GitHub y crear proyecto | Dependencias entre capas: Core → Infrastructure, Core → WebApp, Infrastructure → WebApp. | Ninguna referencia prohibida (WebApp → Core ← Infrastructure). | Sin referencias prohibidas. PR #6: 7/7 aprobadas. | Aprobado |
| CP-S1-002 | S1-T03 — Configurar EF Core con SQL Server | Registrar la infraestructura con cadena de conexión de prueba y sin cadena (nula, «» y «   »). | Con cadena: EopDbContext registrado con SQL Server. Sin cadena: excepción que impide arrancar. | Registro correcto y excepción en los 3 casos sin cadena. PR #6: 7/7 aprobadas. | Aprobado |
| CP-S1-003 | S1-T04 — Manejo básico de errores | Excepción InvalidOperationException con mensaje «dato sensible de prueba» en la solicitud «solicitud-123». | Vista de error genérica con la referencia «solicitud-123»; el log guarda el tipo de excepción y la referencia, sin el mensaje. | Vista y log según lo esperado, sin el mensaje sensible. PR #7: 8/8 aprobadas. | Aprobado |
| CP-S1-004 | S1-T07 — Administrador inicial y entidad Usuario | Inicialización del primer administrador con el script «inicializar-administrador.sql». | Usuario Administrador activo creado en la base local. | No se documentaron pruebas ni resultados (PR #9 sin descripción). | Sin evidencia verificable |

### Autenticación y sesión (US-042)

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-005 | S1-T08 — Autenticación con Microsoft Entra ID | Prueba manual: inicio de sesión con una cuenta real de Entra ID. | Autenticación en Entra ID y retorno a la aplicación; Client Secret fuera del repositorio. | Inicio de sesión y redirección correctos (prueba manual informada en PR #10, sin registro adjunto). | Aprobado |
| CP-S1-006 | S1-T09 — Inicio y cierre de sesión | Acceso sin sesión a «/» y «/Home/Privacy». | La pantalla protegida no se muestra; se exige autenticación. | Acceso denegado sin sesión. PR #13: 33/33 aprobadas. | Aprobado |
| CP-S1-007 | S1-T09 — Inicio y cierre de sesión | Cierre de sesión por GET, por POST sin token antifalsificación y por POST válido. | GET no cierra (404); POST sin token rechazado (400); POST válido cierra la sesión local y la de Entra ID. | Según lo esperado. PR #13: 33/33 aprobadas. | Aprobado |
| CP-S1-008 | S1-T09 — Inicio y cierre de sesión | Retorno externo tras el login: «https://sitio-falso.com», «//sitio-falso.com», «/\sitio-falso.com», «https:sitio-falso.com» y nulo. | Redirección al inicio de la aplicación (sin redirección abierta). | Redirige al inicio en los 5 casos. PR #13: 33/33 aprobadas. | Aprobado |
| CP-S1-009 | S1-T09 — Inicio y cierre de sesión | Prueba manual con cuenta real y MFA: login, nombre en el menú, cierre, nuevo login y fallo de login. | Acceso tras MFA, cierre efectivo, nuevo pedido de login y pantalla amigable ante el fallo. | Comprobado manualmente (informado en PR #13, sin registro adjunto). | Aprobado |
| CP-S1-010 | S1-T10 — Vinculación con Entra ID | Usuario Activo ya vinculado (Object ID 11111111-1111-1111-1111-111111111111) y primer acceso de «juan.perez@eop.com.uy», Activo y sin vincular. | El vinculado accede; el del primer acceso queda vinculado por su correo y obtiene sesión. | Según lo esperado. PR #16: 120 aprobadas. | Aprobado |
| CP-S1-011 | S1-T10 — Vinculación con Entra ID | Identidad no registrada en EOP, usuario Inactivo (vinculado o no) y token sin claim «oid». | No se crea sesión, no se vincula y se redirige a la pantalla de error. | Sin sesión y con redirección a la pantalla de error. PR #16: 120 aprobadas. | Aprobado |
| CP-S1-012 | S1-T10 — Vinculación con Entra ID | Usuario vinculado cuyo correo cambió en EOP; intento de vincular un usuario ya vinculado a otro identificador. | Se identifica por Object ID; no se sobrescribe la vinculación existente. | Según lo esperado. PR #16: 120 aprobadas. | Aprobado |
| CP-S1-013 | S1-T11 — Restricciones de acceso y sesiones | Usuario con sesión iniciada que se inactiva en la base; nuevo pedido a «/» y «/Home/Privacy». | Pedido rechazado, cookie eliminada y nuevo login sin sesión. | Rechazado y cookie borrada. PR #18: 176 aprobadas. | Aprobado |
| CP-S1-014 | S1-T11 — Restricciones de acceso y sesiones | Cookie falsificada («valor-inventado») o alterada a partir de una real; falla de la base durante la validación. | Sin acceso; la falla se registra sin datos sensibles. | Sin acceso en todos los casos. PR #18: 176 aprobadas. | Aprobado |
| CP-S1-015 | S1-T11 — Restricciones de acceso y sesiones | Prueba manual: usuario con sesión abierta pasado a Inactivo en SQL Server. | Al actualizar, el acceso se rechaza. | Acceso rechazado (informado en PR #18, sin registro adjunto). | Aprobado |
| CP-S1-016 | S1-T30 — Pruebas de autenticación y permisos | Flujo OpenID Connect con Entra ID simulado: pedido por HTTP; tokens firmados con otra clave, de otra audiencia, de otro tenant, vencidos o con nonce ajeno. | Redirección a Entra ID con PKCE y a HTTPS; los tokens inválidos no crean sesión ni vinculan. | Según lo esperado. Sin cifra en PR #43; aprobado en la reejecución del cierre del Sprint 1 (1169/1169). | Aprobado |
| CP-S1-017 | S1-T30 — Pruebas de autenticación y permisos | MFA no completado: Entra ID responde «interaction_required», «access_denied» o «consent_required». | No se crea sesión; el log solo guarda el código de error. | Según lo esperado. Aprobado en la reejecución del cierre del Sprint 1 (1169/1169). | Aprobado |

### Roles y permisos (US-044)

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-018 | S1-T12 — Roles fijos | Enumeración de roles y configuración de Usuario.Rol en EF Core. | Exactamente 3 roles (Administrador = 1, Supervisor = 2, Operador = 3); rol obligatorio y restringido por CHECK. | Según lo esperado. PR #19: suite satisfactoria (sin cifra). | Aprobado |
| CP-S1-019 | S1-T13 — Autorización por rol | Cada rol pide la acción de su rol; Operador pide por URL directa una acción de Administrador o Supervisor. | Cada rol accede solo a lo permitido; el Operador recibe Acceso denegado (403). | Según lo esperado. PR #22: 189 aprobadas; prueba manual con Operador y Supervisor. | Aprobado |
| CP-S1-020 | S1-T13 — Autorización por rol | Rol cambiado en la base de Operador a Supervisor con la sesión abierta. | Los pedidos siguientes usan el rol nuevo sin cerrar sesión. | Permisos actualizados. PR #22: 189 aprobadas; prueba manual. | Aprobado |
| CP-S1-021 | S1-T14 — Modificación de roles | Administrador cambia a Juan de Operador a Supervisor (POST /Usuarios/ModificarRol). | Rol guardado; obtiene los permisos de Supervisor y pierde los de Operador. | Según lo esperado. PR #25: 338/338 aprobadas. | Aprobado |
| CP-S1-022 | S1-T14 — Modificación de roles | Rol enviado: «Gerente», «99», «0», «administrador», «Operador », «Administrador,Supervisor», «1,2». | Rechazo; el rol no cambia. | Rechazados sin cambios. PR #25: 338/338 aprobadas. | Aprobado |
| CP-S1-023 | S1-T14 — Modificación de roles | Supervisor u Operador con token válido; pedido sin sesión; Administrador sin token antifalsificación. | Operación denegada sin modificar datos. | Según lo esperado. PR #25: 338/338 aprobadas. | Aprobado |
| CP-S1-024 | S1-T14 — Modificación de roles | Único Administrador activo intenta cambiar su rol a Operador. | Cambio rechazado; conserva el rol y sus permisos. | Rechazado. PR #25: 338/338 aprobadas. | Aprobado |
| CP-S1-025 | S1-T14 — Modificación de roles | SQL Server (LocalDB): solicitudes simultáneas que degradan a los últimos administradores, 25 repeticiones, con y sin READ_COMMITTED_SNAPSHOT. | Siempre queda exactamente un Administrador activo. | Según lo esperado. PR #25: 12 pruebas ejecutadas contra LocalDB. | Aprobado |
| CP-S1-026 | S1-T30 — Pruebas de autenticación y permisos | Matriz de RF-73 aplicada a todas las acciones publicadas y a todos los POST. | Cada acción tiene exactamente el acceso de la matriz; todo POST exige token antifalsificación. | Según lo esperado. Aprobado en la reejecución del cierre del Sprint 1 (1169/1169). | Aprobado |

### Registro de usuarios (US-043)

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-027 | S1-T17 — Registro administrativo de usuarios | Alta de «Ana Pérez», «ana.perez@eop.com.uy», con cada rol fijo. | Usuario guardado Activo, con el rol elegido y pendiente de vinculación. | Según lo esperado. Sin cifra en PR #39; PR #42: 931 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-028 | S1-T17 — Registro administrativo de usuarios | Correo ya registrado para otro usuario activo o inactivo; variantes «ANA.PEREZ@EOP.COM.UY» y «  Ana.Perez@eop.com.uy  » en SQL Server. | Alta impedida con «El correo corporativo ya se encuentra registrado.» | Según lo esperado. PR #42: 931 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-029 | S1-T17 — Registro administrativo de usuarios | Nombre vacío; correo «sin-arroba» o «ana perez@eop.com.uy»; rol vacío; nombre de 151 y correo de 255 caracteres. | Alta rechazada con el motivo de cada campo; no se guarda nada. | Según lo esperado. PR #42: 931 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-030 | S1-T17 — Registro administrativo de usuarios | Supervisor u Operador abre el formulario por URL o envía el POST con token válido; pedido sin sesión. | Operación denegada; no se guarda nada. | Según lo esperado. PR #42: 931 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-031 | S1-T17 — Registro administrativo de usuarios | Usuario recién registrado inicia sesión (login simulado). | Se vincula en el primer acceso y entra con el rol asignado. | Según lo esperado. PR #42: 931 aprobadas, 0 omitidas. | Aprobado |

### Registro de clientes (US-053)

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-032 | S1-T18 — Entidad Cliente | Creación de un cliente nuevo. | El cliente queda Activo por defecto. | Activo. PR #11: pruebas correctas (sin cifra). | Aprobado |
| CP-S1-033 | S1-T19 — Validaciones de clientes | RUT «21.552.175-0017» ya registrado. | Alta rechazada con «Ya existe un cliente registrado con ese RUT.»; la consulta usa 215521750017. | Según lo esperado. PR #15: 61/61 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-034 | S1-T19 — Validaciones de clientes | Cliente válido: RUT «21.552.175-0017», correo «cliente@ejemplo.com», WhatsApp «+598 (99) 123-456». | Sin errores; RUT 215521750017 y WhatsApp +59899123456. | Según lo esperado. PR #15: 61/61 aprobadas. | Aprobado |
| CP-S1-035 | S1-T19 — Validaciones de clientes | RUT con dígito verificador incorrecto «215521750018»; formato incorrecto «12345678901», «1234567890123», «21552175001A». | RUT rechazado. | Rechazados. PR #15: 61/61 aprobadas. | Aprobado |
| CP-S1-036 | S1-T19 — Validaciones de clientes (RNF-021, ampliada en PR #34) | Estructura inválida: «000000000000», prefijos 00, 23 y 99, serie 000000, sufijos 455 y 002. Válidos: «010000010011», «220000010011», «21-000001-001-4». | Rechaza las estructuras inválidas y acepta las válidas. | Según lo esperado. PR #34: 675 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-037 | S1-T19 — Validaciones de clientes | Ambos correos vacíos; contacto «cliente.gmail.com»; facturación «facturacion.gmail.com»; solo uno de los dos correos válido. | Rechazo con «Debe informar al menos un correo electrónico.» o con el error del correo inválido; un solo correo válido se acepta. | Según lo esperado. PR #15: 61/61 aprobadas. | Aprobado |
| CP-S1-038 | S1-T19 — Validaciones de clientes | WhatsApp «093966189», «59899123456», «+598ABC123», «+09899123456», «+598123456789012345». | Rechazo por no cumplir el formato internacional E.164. | Rechazados. PR #15: 61/61 aprobadas. | Aprobado |
| CP-S1-039 | S1-T20 — Formulario y registro de clientes | Alta por formulario con país Uruguay y número «099 123 456». | Cliente guardado Activo con WhatsApp +59899123456 y mensaje «El cliente se registró correctamente.» | Según lo esperado. PR #21: 258/258 aprobadas. | Aprobado |
| CP-S1-040 | S1-T20 — Formulario y registro de clientes | Número inválido para el país (AR «011 123», UY «099123»); país «ZZ» o sin país; RUT «215521750018». | No se guarda; se muestra el error y se conservan país y número. | Según lo esperado. PR #21: 258/258 aprobadas. | Aprobado |
| CP-S1-041 | S1-T20 / S1-T27 — Registro de clientes | RUT registrado por otra solicitud entre la validación y el guardado; 10 altas simultáneas con el mismo RUT en SQL Server. | Mensaje de RUT duplicado; se guarda un solo cliente. | Según lo esperado. PR #44: 984/984 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-042 | S1-T20 / S1-T30 — Registro de clientes | Alta por Administrador, Supervisor y Operador; POST sin sesión. | Los tres roles registran; sin sesión se rechaza sin ejecutar. | Según lo esperado. Aprobado en la reejecución del cierre del Sprint 1 (1169/1169). | Aprobado |

### Listado, búsqueda y filtros de clientes (US-052)

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-043 | S1-T21 / S1-T22 — Listado y búsqueda de clientes | Clientes Ana Pereira (RUT 215521750017) y Juan Gonzalez (RUT 215521750025); búsqueda por nombre «Ana» y por RUT «215521750017». | Se muestra solo Ana Pereira en ambos casos. | Según lo esperado. PR #24: 275 aprobadas. | Aprobado |
| CP-S1-044 | S1-T22 — Filtros de clientes | Filtro Activo (Ana Pereira) e Inactivo (Juan Gonzalez); combinados «Ana» + Activo y «215521750025» + Inactivo sobre 4 clientes. | Solo los clientes que cumplen todos los criterios (Ana Pereira; Ana Garcia). | Según lo esperado. PR #24: 275 aprobadas. | Aprobado |
| CP-S1-045 | S1-T32 — Pruebas de listados y filtros | Criterios sin coincidencias («Inexistente», «999999999999», «%», «_», «Pereira» + Inactivo) y base sin clientes. | Lista vacía y mensaje de sin resultados. | Según lo esperado. Sin resultado en PR #38; PR #42: 931 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-046 | S1-T32 — Pruebas de listados y filtros | Listado con Ana Garcia sin correo de contacto. | Cada dato en su columna; el correo ausente figura como «Sin informar». | Según lo esperado. PR #42: 931 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-047 | S1-T32 — Pruebas de listados y filtros | Consulta por cada rol, pedido sin sesión y consultas repetidas con filtros. | Los tres roles consultan; sin sesión no se ven datos; consultar no modifica clientes. | Según lo esperado. PR #42: 931 aprobadas, 0 omitidas. | Aprobado |

### Proveedores (US-074)

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-048 | S1-T23 — Listado de proveedores | Sin proveedores; con proveedores «Zeta» y «Alfa». | Mensaje «Todavía no hay proveedores registrados.»; listado ordenado Alfa, Zeta con RUT y teléfono. | Según lo esperado. PR #49 sin cifra; PR #52: 1145 aprobadas. | Aprobado |
| CP-S1-049 | S1-T23 — Listado de proveedores | Opción «Registrar proveedor» vista por Administrador, Supervisor y Operador. | Visible para Administrador y Supervisor; no se ofrece al Operador. | Según lo esperado. PR #52: 1145 aprobadas. | Aprobado |
| CP-S1-050 | S1-T24 — Accesos al módulo de proveedores | Listado pedido por los tres roles, sin sesión y por un usuario inactivado con la sesión abierta. | Los tres roles acceden; sin sesión y el usuario inactivado no acceden. | Según lo esperado. PR #52: 1145 aprobadas. | Aprobado |

### Catálogo de materiales (US-040)

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-051 | S1-T26 — Listado y filtros de materiales | Materiales «Cable de cobre», «Cable de aluminio» y «Pintura blanca»: búsqueda «  cAbLe   DE   cObRe  », unidad Litro, proveedor, y «Cable» + Metro + Fivisa. | Solo los materiales que cumplen todos los criterios, sin distinguir mayúsculas ni espacios. | Según lo esperado. PR #50: todas aprobadas (sin cifra) y prueba manual de filtros. | Aprobado |
| CP-S1-052 | S1-T26 — Listado y filtros de materiales | Referencias de «Cable de cobre»: Fivisa 150 UYU (01/11/2026 10:00) y Barraca Central 7,50 USD (02/11/2026 11:00). | Proveedor, precio, moneda y fecha de última actualización de cada referencia. | Según lo esperado. PR #50: todas aprobadas. | Aprobado |
| CP-S1-053 | S1-T25 — Relaciones y consultas del catálogo | Material «Cinta aisladora» sin referencias; filtros sin coincidencias. | Aparece sin precios y no aparece al filtrar por proveedor; sin coincidencias devuelve lista vacía. | Según lo esperado. PR #55: 1169/1169 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-054 | S1-T25 — Relaciones y consultas del catálogo | SQL Server: un material con dos proveedores (150 UYU y 7,5 USD); par material-proveedor repetido; precio 0, -1 y -0,01; moneda 0, 3 y 99; claves inexistentes; borrado con precios. | Cada relación con precio, moneda y fecha propios; la base rechaza los demás casos. | Según lo esperado. PR #55: 1169/1169 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-055 | S1-T26 / S1-T30 — Permisos de materiales | Alta de materiales abierta o enviada por cada rol; Operador con sesión y token válidos. | Solo Administrador y Supervisor registran; el listado es accesible a los tres roles. | Según lo esperado. PR #55: 1169/1169 aprobadas. | Aprobado |

### Modelo de datos y decisiones técnicas

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-056 | S1-T27 — Revisión del modelo de datos | SQL Server: cliente con nombre de 201 caracteres, nombre o WhatsApp en blanco, RUT «21552175-001», «21552175001» o «21552175001²», sin correos; usuario con nombre o correo en blanco. | La base rechaza cada caso; acepta nombre de 200 caracteres y un solo correo. | Según lo esperado. PR #44: 984/984 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-057 | S1-T27 — Revisión del modelo de datos | Migraciones: script completo frente a MigrateAsync; script idempotente aplicado dos veces; migración con un cliente que no cumple las restricciones. | Mismo esquema; el script idempotente no falla; la migración no se aplica con datos inválidos. | Según lo esperado. PR #44: 984/984 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-058 | S1-T27 — Revisión del modelo de datos | Nombre de material de 200 y 201 caracteres, y en blanco. | Acepta 200; rechaza 201 y el nombre en blanco. | Según lo esperado. PR #44: 984/984 aprobadas, 0 omitidas. | Aprobado |
| CP-S1-059 | S1-T34 — Decisiones técnicas y duplicados | Material «Caño de cobre» repetido como «  CAÑO   de COBRE »; 8 altas simultáneas del mismo nombre; otro error de la base. | Se guarda uno solo y se informa el duplicado; otro error no se presenta como duplicado. | Según lo esperado. PR #45: 996/996 aprobadas, 0 omitidas. | Aprobado |

### Interfaz visual inicial

| ID | Tarea | Caso de prueba y datos de entrada | Resultado esperado | Resultado obtenido | Estado |
|---|---|---|---|---|---|
| CP-S1-060 | S1-T29 — Interfaz visual inicial | Revisión manual con Administrador a 1440, 768 y 375 px: formularios enviados vacíos, modal de proveedores, menú lateral en celular y foco con teclado. | Sin desbordamiento horizontal; validaciones visibles; menú y modal funcionales. | Comprobado manualmente (PR #42) y suite de 931 aprobadas, 0 omitidas. No se verificaron visualmente los mensajes de éxito ni el menú de Supervisor y Operador. | Aprobado |
