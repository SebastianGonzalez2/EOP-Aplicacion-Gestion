# S3-T03 — Prueba de concepto de integración con datosUruguay

**Fecha de ejecución:** 19/11/2026  
**Tarea:** [S3-T03 — Probar integración con datosUruguay](https://trello.com/c/kCYSq2bv)  
**Tipo:** prueba técnica exploratoria y manual (PowerShell).  
**Estado:** prueba de concepto favorable; integración en la aplicación pendiente.

## Objetivo y alcance

Comprobar que la API de datosUruguay permite obtener cotizaciones USD/UYU procedentes del BCU, recuperar valores publicados en fechas anteriores y observar qué ocurre ante solicitudes inválidas o conexiones fallidas.

Esta prueba responde al alcance de S3-T03. **No** implementa el servicio de cotizaciones de EOP, sus casos de uso, la persistencia de cotizaciones ni los cálculos de presupuestos. Esos trabajos corresponden a tareas posteriores, en particular [S3-T18](https://trello.com/c/A0mTDBNn) (servicio de cotizaciones).

**Trazabilidad:** RF-029 y US-012 (cotización actual y última válida anterior para el presupuesto); RNF-011 (secretos), RNF-012 (registro de errores), RNF-013 (tolerancia a fallos). El uso de cotizaciones históricas para registros económicos también está previsto en `AGENTS.md`, pero su implementación no se aborda aquí.

## Entorno y método

- Ejecución manual en PowerShell desde Windows, por un integrante del equipo.
- Evidencias: salidas de consola compartidas durante la tarea (no se ejecutaron estas pruebas dentro de la aplicación ASP.NET Core).
- Proveedor consultado: `https://datosuruguay.com/api/v1/exchange-rates/usd`.
- Período usado en la consulta: del 12 al 19 de noviembre de 2026, ambos incluidos.
- Los resultados de la consulta reflejan el momento de ejecución. La falta de un registro para el día 19 **no significa** que no pueda publicarse más tarde.

## Casos y resultados observados

| Caso | Procedimiento | Evidencia observada | Resultado |
|---|---|---|---|
| P1 — Consulta por rango | GET con `from=2026-11-12&to=2026-11-19&limit=30` | La API devolvió cinco registros de cotización con fecha, serie `usd` y valor positivo; `meta.source=BCU`, `meta.unit=UYU/USD`, `meta.last_updated=2026-11-18`, `meta.count=5` y `meta.has_more=false`. | Correcto |
| P2 — Último valor anterior | Filtrar localmente los registros descargados por fecha <= fecha solicitada, valor > 0, y elegir la fecha más reciente | Para 15/11/2026 se eligió 13/11/2026 (40,611 UYU/USD); para 19/11/2026 se eligió 18/11/2026 (40,643 UYU/USD). | Correcto, para el intervalo descargado |
| P3 — Entrada inválida | GET con `from=fecha-invalida&to=2026-11-19` | El proveedor respondió HTTP **400 Bad Request** y PowerShell ejecutó el bloque `catch`. | Correcto |
| P4 — Fallo de conexión simulado | GET a `http://127.0.0.1:1/api/v1/exchange-rates/usd` (dirección local inaccesible) | PowerShell mostró `Unable to connect to the remote server` y ejecutó el bloque `catch`, informando que no había cotización disponible. | Correcto como simulación local |

### Datos recibidos en P1

| Fecha de publicación | Cotización (UYU por USD) |
|---|---:|
| 2026-11-12 | 40,589 |
| 2026-11-13 | 40,611 |
| 2026-11-16 | 40,609 |
| 2026-11-17 | 40,578 |
| 2026-11-18 | 40,643 |

No se recibieron registros del sábado 14, domingo 15 ni jueves 19 en ese resultado. Es necesario distinguir una fecha sin publicación de un error de acceso a la API o de una búsqueda cuyo rango no alcance cotizaciones previas.

La respuesta también incluyó `meta.license=CC-BY-4.0` y `meta.attribution.required=true`, con el texto de atribución `Datos de datosUruguay.com`. La futura integración deberá contemplar las condiciones de uso/atribución del proveedor.

## Comandos para reproducir las pruebas

### P1 — Consulta de cotizaciones

```powershell
$uri = "https://datosuruguay.com/api/v1/exchange-rates/usd?from=2026-11-12&to=2026-11-19&limit=30"
$respuesta = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 20
$respuesta | ConvertTo-Json -Depth 8
```

### P2 — Selección de la última cotización anterior disponible

Este procedimiento usa **únicamente** el conjunto descargado en P1 y no realiza una nueva consulta.

```powershell
$fechasObjetivo = @("2026-11-15", "2026-11-19")

foreach ($fecha in $fechasObjetivo) {
    $cotizacion = $respuesta.data |
        Where-Object {
            $_.date -le $fecha -and $_.value -gt 0
        } |
        Sort-Object date -Descending |
        Select-Object -First 1

    if ($null -eq $cotizacion) {
        Write-Host "Fecha solicitada: $fecha"
        Write-Host "No se encontro cotizacion valida."
    }
    else {
        Write-Host "Fecha solicitada: $fecha"
        Write-Host "Fecha utilizada: $($cotizacion.date)"
        Write-Host "Cotizacion: $($cotizacion.value) UYU/USD"
    }

    Write-Host "------------------------------"
}
```

### P3 — Solicitud inválida

```powershell
$uriError = "https://datosuruguay.com/api/v1/exchange-rates/usd?from=fecha-invalida&to=2026-11-19"

try {
    $resultado = Invoke-RestMethod -Uri $uriError -Method Get -TimeoutSec 20 -ErrorAction Stop
    Write-Host "La API respondio sin error HTTP:"
    $resultado | ConvertTo-Json -Depth 8
}
catch {
    Write-Host "La solicitud produjo un error."
    Write-Host "Mensaje: $($_.Exception.Message)"

    if ($null -ne $_.Exception.Response) {
        Write-Host "Codigo HTTP: $([int]$_.Exception.Response.StatusCode)"
    }
}
```

### P4 — Conexión fallida simulada

```powershell
$uriFalla = "http://127.0.0.1:1/api/v1/exchange-rates/usd"

try {
    $resultado = Invoke-RestMethod -Uri $uriFalla -Method Get -TimeoutSec 5 -ErrorAction Stop
    Write-Host "Conexion exitosa."
}
catch {
    Write-Host "No fue posible conectar con el servicio externo."
    Write-Host "Detalle: $($_.Exception.Message)"
    Write-Host "La cotizacion no esta disponible."
}
```

## Conclusiones y límites

1. **Viabilidad preliminar favorable:** la API respondió con cotizaciones USD/UYU, fecha de publicación y metadatos suficientes para una integración de lectura.
2. **Última publicación anterior:** el algoritmo de ejemplo eligió correctamente una fecha anterior dentro del conjunto disponible. Todavía no se comprobó el comportamiento con búsquedas paginadas, un rango sin cotizaciones o fechas muy antiguas.
3. **Manejo de errores:** se observó HTTP 400 para una solicitud inválida. La prueba de conexión fallida fue contra un destino local, **no una caída real de datosUruguay**. No se probaron tiempos de espera agotados, HTTP 429 ni HTTP 500 del proveedor.
4. **No implica integración productiva:** no se creó servicio C#, no se probaron `HttpClient`, inyección de dependencias, logging propio de EOP, persistencia ni flujos funcionales. No corresponde afirmar que RF-029/US-012/RNF-013 estén implementados o aceptados por estas pruebas.
5. **Pruebas de compilación:** no aplica en S3-T03 porque solo se agrega documentación; no se modificó código de la solución.

## Consideraciones para S3-T18

- Encapsular el acceso a datosUruguay en un servicio de `Infrastructure/Integraciones/DatosUruguay`, mediante una interfaz definida en `Core`, conforme a `AGENTS.md`.
- Validar respuesta HTTP, estructura, fecha de publicación y cotización positiva.
- Implementar la búsqueda segura de la última cotización publicada **en o antes** de la fecha objetivo. Evitar cotizaciones futuras, estimaciones y promedios; no asumir que una lista vacía o una llamada fallida significan lo mismo.
- Registrar los fallos sin secretos ni datos sensibles (RNF-012) y respetar las restricciones cuando no haya cotización válida (RF-029, RNF-013).
- Diseñar pruebas automatizadas de días sin publicación, ausencia real de antecedentes, fallos de red, formato inválido y comportamiento al confirmar operaciones económicas.
- Revisar y cumplir la atribución indicada por la propia API.

**Veredicto S3-T03:** prueba técnica exploratoria realizada con resultados favorables. La integración efectiva y sus pruebas automáticas quedan fuera de esta tarea.
