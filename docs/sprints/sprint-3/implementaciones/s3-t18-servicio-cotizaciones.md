# S3-T18 — Servicio de cotizaciones USD/UYU (US-012)

**Sprint:** 3 — Elaboración económica de presupuestos

**Requerimientos:** US-012 · RF-029 (reglas de cotización), RF-031, RF-077, RF-103, RF-104 · RNF-011, RNF-012, RNF-013 · DT-12

**Antecedentes:** [S3-T03 — prueba de concepto](../../../pruebas-concepto/S3-T03-datosuruguay.md) ·
[S3-T25 — revisión de reglas de cálculo y cotización](../../../arquitectura/revision-reglas-calculo-cotizacion.md) ·
[decisiones de dependencias del Sprint 3](../decisiones-tecnicas-y-dependencias.md)

Esta tarea deja un **servicio centralizado y reutilizable** que obtiene la cotización del dólar publicada por el
BCU a través de datosUruguay. No la usa todavía ninguna pantalla ni caso de uso: lo van a consumir las tareas
de margen, totales y confirmación del presupuesto, y las de facturas y costos adicionales.

## 1. Qué incluye y qué no

**Incluye**

- Obtener la cotización **vigente** (la de hoy en Uruguay o, si todavía no se publicó, la última anterior).
- Obtener la cotización **histórica** de una fecha (la de ese día o la última publicación válida anterior).
- Devolver siempre el valor (`decimal`, sin redondear) **y** la fecha efectiva de publicación.
- Distinguir «no hay publicación» (`null`) de «no se pudo consultar» (`CotizacionNoDisponibleException`).
- Caché compartida en memoria con expiración absoluta y una sola consulta por fecha ante pedidos simultáneos.
- Registro (logging) de cada llamada y de cada falla, sin datos sensibles.

**No incluye** (corresponde a otras tareas, según el pedido de S3-T18)

- Guardar la cotización aplicada en `Presupuesto`, `FacturaCompra`, `FacturaVenta` o `CostoAdicional`. No se
  creó ninguna tabla ni migración, ni se modificó el MER.
- El margen (S3-T19), los totales y redondeos (S3-T20), el resumen y la confirmación económica (US-013).
- Pantallas. La atribución a datosUruguay se tendrá que mostrar en las pantallas que muestren cotizaciones
  (ver sección 6).

## 2. Piezas

| Pieza | Capa | Qué hace |
|---|---|---|
| `IServicioCotizaciones` | `Core/Interfaces/Cotizaciones` | Contrato que usan los casos de uso: `ObtenerCotizacionVigenteAsync` y `ObtenerCotizacionHistoricaAsync(fecha)`. |
| `CotizacionUsd` | `Core/DTOs/Cotizaciones` | `Valor` (UYU por USD), `FechaPublicacion`, `FechaSolicitada` y `EsDeLaFechaSolicitada`. |
| `CotizacionNoDisponibleException` + `MotivoCotizacionNoDisponible` | `Core/Comun` | Falla técnica del proveedor, con un mensaje que se puede mostrar al usuario y el motivo. |
| `ClienteDatosUruguay` | `Infrastructure/Integraciones/DatosUruguay` | Pide **una página** a la API, valida el JSON y traduce cada falla a la excepción. Es el único que conoce la URL y el formato del proveedor. |
| `RespuestaDatosUruguay` (interna) y `PaginaCotizacionesDatosUruguay` | ídem | El molde del JSON y la página ya validada. |
| `BuscadorCotizacionesDatosUruguay` | ídem | Aplica la regla de selección y recorre rangos y páginas. |
| `ServicioCotizacionesDatosUruguay` | ídem | Implementa la interfaz: fecha de hoy en Uruguay, caché y turnos por fecha. |
| `InyeccionDeDependencias.AgregarServicioCotizaciones` | `Infrastructure` | Registra la caché, el `HttpClient` tipado (IHttpClientFactory) y el servicio como Singleton. |

`Core` no conoce `HttpClient` ni `IMemoryCache`; lo protege `ArquitecturaTests.Core_NoDebeDependerDeLaCacheNiDeHttp`.

## 3. Cómo funciona

Recorrido de un pedido, por ejemplo `ObtenerCotizacionHistoricaAsync(domingo 22/11/2026)`:

1. **Servicio.** Calcula la fecha de hoy en Uruguay con `ZonaHorariaUruguay` y el `TimeProvider`. Si la fecha pedida
   es posterior a hoy, lanza `ArgumentOutOfRangeException` (una fecha futura todavía no tiene cotización).
2. **Caché.** Busca la clave `cotizacion-usd:2026-11-22`. Si está y no venció, la devuelve sin llamar a la API.
3. **Turno de la fecha.** Si no está, toma el turno (`SemaphoreSlim`) de esa fecha. Si otro pedido de la misma fecha
   ya está consultando, espera; al entrar vuelve a mirar la caché y, como el otro ya la llenó, no consulta de nuevo.
4. **Buscador, paso 1.** Pide `from=2026-11-08&to=2026-11-22&limit=100&offset=0` (14 días hacia atrás), recorre
   todas las páginas y elige la publicación **válida** más reciente: fecha igual o anterior a la pedida y valor
   mayor que cero. Encuentra la del viernes 20/11.
5. **Buscador, paso 2** (solo si el paso 1 no encontró ninguna válida). No concluye que no exista: pide
   `to=<día anterior al rango>` **sin** `from`, página por página (el proveedor las ordena de la más reciente a la
   más antigua), y se queda con la primera válida. Si el proveedor confirma que no hay más páginas
   (`has_more=false`) sin haber encontrado ninguna, la respuesta es `null`.
6. **Servicio.** Guarda la cotización en la caché (15 minutos si la fecha pedida es hoy; 24 horas si es anterior) y la
   devuelve: `CotizacionUsd(Valor: 40.629, FechaPublicacion: 2026-11-20, FechaSolicitada: 2026-11-22)`.

Si en cualquier paso falla la comunicación, sale `CotizacionNoDisponibleException` y **no se guarda nada** en la caché.

### Escenarios y resultado

| Escenario | Resultado |
|---|---|
| Hay publicación ese día | Esa cotización; `EsDeLaFechaSolicitada = true` |
| Fin de semana, feriado o día sin publicación | La última válida anterior, con su fecha efectiva |
| Publicación con valor cero o negativo | Se descarta (queda en el log) y se usa la anterior válida |
| Publicación con fecha posterior a la pedida | Se descarta (queda en el log); nunca se usa |
| La última válida está fuera del rango de 14 días | Paso 2: se encuentra igual, recorriendo páginas |
| No existe ninguna válida en o antes de la fecha | `null` (solo después de que el proveedor confirma que no hay más resultados) |
| Fecha pedida posterior a hoy | `ArgumentOutOfRangeException`, sin consultar la API |
| Sin conexión / DNS / conexión rechazada | Excepción, motivo `FallaDeConexion` |
| Más de 10 s sin respuesta | Excepción, motivo `TiempoDeEsperaAgotado` |
| HTTP 429 (límite de solicitudes) | Excepción, motivo `LimiteDeSolicitudesExcedido`; se registra el `Retry-After` |
| Otro error HTTP (400, 404, 500, 503…) | Excepción, motivo `ErrorDelProveedor` |
| JSON roto, incompleto, otra serie, otra fuente u otra unidad, número como texto | Excepción, motivo `RespuestaInvalida` |
| Página vacía que anuncia más páginas, o resultados fuera de orden en el paso 2 | Excepción, motivo `RespuestaInvalida` |
| Más de 10 páginas sin encontrar una válida y quedan más | Excepción, motivo `BusquedaIncompleta` (no se afirma ausencia) |
| Quien llama cancela | `OperationCanceledException` (no se registra como falla del proveedor) |

## 4. Cómo lo van a consumir las tareas siguientes

El servicio se pide por inyección de dependencias (`IServicioCotizaciones`) en el caso de uso que lo necesite:

```csharp
CotizacionUsd? cotizacion;

try
{
    // Presupuesto en elaboración (RF-029): la cotización actual.
    cotizacion = await _servicioCotizaciones.ObtenerCotizacionVigenteAsync(cancellationToken);

    // Factura o costo adicional (RF-077, RF-103, RF-104): la de su fecha.
    // cotizacion = await _servicioCotizaciones.ObtenerCotizacionHistoricaAsync(fechaEmision, cancellationToken);
}
catch (CotizacionNoDisponibleException excepcion)
{
    // Falla temporal del proveedor: NO es "no hay publicación" ni habilita usar otra fecha.
    // Cada caso de uso decide según su regla (rechazar el guardado, no permitir confirmar, etc.)
    // y puede mostrar excepcion.Message al usuario.
}

if (cotizacion == null)
{
    // El proveedor confirmó que no existe ninguna cotización válida en o antes de esa fecha.
}
```

Al conservar la cotización aplicada, cada tarea guarda `cotizacion.Valor` en `CotizacionConfirmada` /
`CotizacionAplicada`, y **`cotizacion.FechaPublicacion`** (no la fecha pedida) en `FechaCotizacionConfirmada` /
`FechaCotizacionAplicada`. Desde ese momento el valor guardado en la base es el que vale (RF-029, RF-103): la caché
no lo reemplaza.

Usos previstos:

| Tarea / requerimiento | Método | Qué hace con la falta de cotización |
|---|---|---|
| Margen y totales del presupuesto (S3-T19, S3-T20; RF-027, RF-029) | `ObtenerCotizacionVigenteAsync` | Lo define RF-029 (ver sección 8) |
| Confirmación de datos económicos (RF-031, US-013) | `ObtenerCotizacionVigenteAsync` | No permite confirmar |
| Facturas de venta y de compra (RF-077, RF-104) | `ObtenerCotizacionHistoricaAsync(fecha de emisión)` | Rechaza el guardado |
| Costos adicionales (RF-103) | `ObtenerCotizacionHistoricaAsync(fecha del costo)` | Rechaza el guardado |

## 5. Configuración

No hay nada que configurar ni ningún secreto (RNF-011): la API es pública, gratuita y no usa clave. La dirección
(`https://datosuruguay.com/api/v1/`, siempre HTTPS) y el tiempo máximo de espera (10 s) son constantes de
`ClienteDatosUruguay`. El `HttpClient` lo crea `IHttpClientFactory`; como el servicio es Singleton, el manejador
renueva sus conexiones cada 15 minutos (`PooledConnectionLifetime`) para respetar cambios de DNS.

## 6. Condiciones de uso y atribución de datosUruguay

Revisadas el 25/11/2026 en `https://datosuruguay.com/licencia-de-datos` y `https://datosuruguay.com/api`, y en
los encabezados y el `meta` de las respuestas de la API.

- **Licencia:** Creative Commons Atribución 4.0 (CC BY 4.0), para la web, los widgets y la API. Permite el uso
  comercial. Cada respuesta lo indica en `meta.license` y en el encabezado `Link` (`rel="license"` y
  `rel="terms-of-service"`).
- **Atribución obligatoria** (`meta.attribution.required = true`): texto «Datos de datosUruguay.com» con un
  **enlace real** a la página del indicador (`meta.attribution.url`, hoy `https://datosuruguay.com/dolar`). Un
  texto sin enlace no cumple. También piden nombrar al organismo que produce el dato: el **BCU**.
- **Pedidos del proveedor:** usar la API y no extraer datos del HTML, y no copiar la base completa para
  republicarla. El servicio cumple los dos: consulta la API y solo pide los días que necesita.
- **Límites de uso** (por IP, compartidos entre todos sus endpoints): 20 solicitudes cada 10 segundos y 300 por
  hora. Al superarlos responde HTTP 429 con `Retry-After`. Sin registro ni clave. Las respuestas se cachean 15
  minutos (`Cache-Control: max-age=900, public`).
- **Carácter de los datos:** datosUruguay es un recopilador, no un organismo oficial; indica que los datos son
  informativos y no constituyen asesoramiento financiero, y no ofrece garantía de disponibilidad.

**Pendiente para las tareas con pantallas:** toda vista que muestre una cotización o un importe convertido con ella
(tabla del presupuesto, resumen, facturas, indicadores del proyecto, Excel) debe incluir la atribución con enlace,
por ejemplo «Cotización BCU — Datos de datosUruguay.com», y conviene mostrar la fecha de publicación usada. En esta
tarea no se agregó ninguna pantalla.

## 7. Pruebas automatizadas

Todas son deterministas: usan `ProveedorDatosUruguaySimulado` (un `HttpMessageHandler` que imita a la API con
filtros, orden, `limit`/`offset` y `has_more`) y `RelojAjustable` (controla la fecha de Uruguay y el reloj de
`MemoryCache`), así que no dependen de internet ni de la hora real.

| Archivo | Qué cubre |
|---|---|
| `Unitarias/ClienteDatosUruguayTests.cs` | Armado de la consulta; deserialización de una respuesta real; precisión decimal; 18 variantes de respuesta inválida o incompleta; errores HTTP; 429 con `Retry-After`; falla de conexión; tiempo agotado; cancelación; registro de cada llamada. |
| `Unitarias/BuscadorCotizacionesDatosUruguayTests.cs` | Día con publicación; sábado y domingo; feriado; valores cero y negativos; fechas posteriores; paginación del rango sin depender del orden; búsqueda hacia atrás y sus páginas; corte en la primera válida; ausencia real; fallas en cada paso; resultados fuera de orden; tope de páginas. |
| `Unitarias/ServicioCotizacionesDatosUruguayTests.cs` | Vigente con la fecha de Uruguay y no la de UTC; históricas; fecha futura; reutilización de la caché; caché compartida; expiración a los 15 min y a las 24 h; la expiración no dispara consultas; fechas distintas sin contaminación; cambio de día; errores, ausencias y respuestas inválidas no se guardan; una entrada vencida no tapa una falla; concurrencia (10 pedidos simultáneos → 1 consulta; fechas distintas no se esperan; reintento tras una falla); cancelación mientras se espera el turno o la respuesta. |
| `Unitarias/RegistroServicioCotizacionesTests.cs` | Registro en el contenedor: Singleton, `IMemoryCache`, `TimeProvider` y `HttpClient` con URL HTTPS y tiempo máximo. |
| `Unitarias/ArquitecturaTests.cs` | `Core` no depende de la caché ni de HTTP. |

Resultado (25/11/2026): las pruebas de esta tarea, **86 aprobadas y 2 omitidas** (las de la API real), repetidas 8
veces seguidas sin fallas. Suite completa con SQL Server real (LocalDB): **1871 aprobadas, 0 fallidas, 2 omitidas**
(solo las de la API real).

## 8. Prueba de conexión real

Separada de la suite habitual porque depende de internet y de un servicio externo:
`Integracion/DatosUruguayRealTests.cs`, que solo corre con `EOP_PRUEBAS_DATOSURUGUAY=1`.

```powershell
$env:EOP_PRUEBAS_DATOSURUGUAY = '1'; dotnet test tests/EOP.Gestion.Tests --filter "FullyQualifiedName~DatosUruguayRealTests"
```

**Ejecución del 25/11/2026, 13:52 (hora de Uruguay), desde la PC de desarrollo:**

| Prueba | Resultado | Detalle |
|---|---|---|
| `ObtieneLaCotizacionVigenteDelBcu` | Aprobada (657 ms) | Hoy, miércoles 25/11, todavía no había publicación; el servicio devolvió la del martes 24/11: **40,697 UYU/USD**, fecha efectiva 2026-11-24, fecha solicitada 2026-11-25. |
| `ObtieneLaCotizacionHistoricaDeUnFinDeSemanaConocido` | Aprobada (468 ms) | Domingo 15/11 → viernes 13/11, **40,611 UYU/USD**, el mismo valor observado en S3-T03. |

La consulta del paso 1 para la vigente (`from=2026-11-11&to=2026-11-25&limit=100&offset=0`) respondió HTTP 200 en
0,53 s con 10 publicaciones, `has_more=false`, `source=BCU` y `unit=UYU/USD`. También se comprobó a mano que la API
responde `400 bad_request` con `{"error":{"code","message","detail"}}` ante una fecha inválida, un `limit` fuera
de 1–5000 o `from` posterior a `to`, y una lista vacía con `has_more=false` para un período sin datos (1990).

Esta prueba verifica la conexión en ese momento; no garantiza la disponibilidad futura del proveedor.

## 9. Limitaciones y decisiones pendientes

1. **Contradicción documentada sobre el borrador sin cotización.** `AGENTS.md` (regla transversal de monedas) dice
   que, sin cotización utilizable, se rechaza el guardado; RF-029 y US-012/US-013 permiten guardar y seguir
   editando el borrador y solo impiden confirmar los datos económicos. Ya estaba señalada en S3-T25. **No se
   modificó ninguna de las dos reglas:** el servicio solo informa (`null` o excepción) y la decisión queda en los
   casos de uso. Hace falta que el equipo aclare la redacción antes de S3-T19/S3-T20.
2. **Publicación tardía del día anterior.** La cotización de un día anterior queda 24 horas en caché, como se
   pidió. Si alguien pide la de *ayer* antes de que el BCU/datosUruguay la publique (por ejemplo, si el proveedor
   publica con atraso), el servicio devuelve la de anteayer y la mantiene hasta 24 horas aunque mientras tanto se
   publique la de ayer. En la prueba real la publicación del 24/11 apareció ese mismo día (encabezado
   `Last-Modified` 17:11 de Uruguay), así que el riesgo es bajo, pero afecta a facturas con fecha de emisión de
   ayer registradas temprano. Una mitigación posible (que no se aplicó porque cambia la regla pedida) es usar la
   expiración corta cuando la fecha pedida es ayer y la publicación encontrada es anterior. Queda a decisión del equipo.
3. **Caché por instancia.** Si el App Service se escala a varias instancias, cada una tiene su propia caché y puede
   consultar la API por su cuenta (DT-12). Con una sola instancia, que es lo previsto, no aplica.
4. **Límite de solicitudes por IP.** Los 300 pedidos por hora son por IP de salida, compartida con cualquier otra
   aplicación del mismo plan de App Service. Con la caché, el uso esperado de EOP queda muy por debajo.
5. **Turnos por fecha.** El diccionario de turnos crece con cada fecha distinta consultada (unas decenas de bytes
   por fecha) y se vacía al reiniciar la aplicación. Para el volumen de EOP es despreciable.
6. **Tope de páginas.** Si no hay una cotización válida en las primeras 10 páginas (la del rango inicial y nueve
   hacia atrás, unas 900 publicaciones o tres años y medio de días hábiles), la respuesta es `BusquedaIncompleta` y no «no hay publicación». Solo ocurriría con fechas anteriores a
   la serie del proveedor o con datos inválidos acumulados.
