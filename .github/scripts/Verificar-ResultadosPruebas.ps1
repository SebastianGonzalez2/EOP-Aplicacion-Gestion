# S3-T01 — Revisa los informes que deja "dotnet test" y arma un resumen.
#
# Para qué hace falta: si no hay SQL Server, las pruebas de integración se marcan como "omitidas"
# y "dotnet test" termina en verde igual. Este script hace fallar la ejecución cuando se omite
# cualquier prueba que no esté en la lista de omisiones permitidas.
#
# Se puede correr en la computadora de cada uno, después de generar los informes:
#   .\.github\scripts\Verificar-ResultadosPruebas.ps1 -CarpetaResultados TestResults

param(
    [string]$CarpetaResultados = "TestResults"
)

$ErrorActionPreference = "Stop"

# Únicas pruebas que pueden quedar sin ejecutar: llaman a la API real de datosUruguay y no queremos
# que la integración continua dependa de un servicio externo.
# Si aparece otra omisión justificada, hay que agregarla acá para que quede a la vista en el PR.
$clasesQuePuedenOmitirse = @(
    "EOP.Gestion.Tests.Integracion.DatosUruguayRealTests"
)

$problemas = New-Object System.Collections.Generic.List[string]
$resumen = New-Object System.Collections.Generic.List[string]
$resumen.Add("## Resultados de las pruebas")
$resumen.Add("")

# --- 1. Informes TRX -------------------------------------------------------------------------

$informes = @()
if (Test-Path -LiteralPath $CarpetaResultados) {
    $informes = @(Get-ChildItem -LiteralPath $CarpetaResultados -Filter "*.trx" -File)
}

if ($informes.Count -eq 0) {
    $problemas.Add("No se encontró ningún informe TRX en '$CarpetaResultados': no se puede saber qué pruebas se ejecutaron.")
}

$cantidadTotal = 0
$cantidadAprobadas = 0
$cantidadSqlEjecutadas = 0
$pruebasFallidas = New-Object System.Collections.Generic.List[string]
$omisionesPermitidas = New-Object System.Collections.Generic.List[string]
$omisionesNoPermitidas = New-Object System.Collections.Generic.List[string]

foreach ($informe in $informes) {
    # Load respeta la codificación del XML (los motivos de omisión tienen tildes).
    $documento = New-Object System.Xml.XmlDocument
    $documento.Load($informe.FullName)

    # El resultado de cada prueba no dice a qué clase pertenece: eso está en las definiciones.
    $clasePorPrueba = @{}
    foreach ($definicion in $documento.TestRun.TestDefinitions.UnitTest) {
        $clasePorPrueba[$definicion.id] = $definicion.TestMethod.className
    }

    foreach ($resultado in $documento.TestRun.Results.UnitTestResult) {
        $cantidadTotal++
        $clase = $clasePorPrueba[$resultado.testId]

        if ($resultado.outcome -eq "NotExecuted") {
            $motivo = $resultado.Output.ErrorInfo.Message
            $detalle = "$($resultado.testName) — $motivo"

            if ($clasesQuePuedenOmitirse -contains $clase) {
                $omisionesPermitidas.Add($detalle)
            }
            else {
                $omisionesNoPermitidas.Add($detalle)
            }
        }
        else {
            if ($resultado.outcome -eq "Passed") {
                $cantidadAprobadas++
            }
            else {
                $pruebasFallidas.Add($resultado.testName)
            }

            if ($clase -like "*SqlServerTests") {
                $cantidadSqlEjecutadas++
            }
        }
    }
}

$cantidadOmitidas = $omisionesPermitidas.Count + $omisionesNoPermitidas.Count

$resumen.Add("| Total | Aprobadas | Fallidas | Omitidas | Ejecutadas de clases SqlServerTests |")
$resumen.Add("|---:|---:|---:|---:|---:|")
$resumen.Add("| $cantidadTotal | $cantidadAprobadas | $($pruebasFallidas.Count) | $cantidadOmitidas | $cantidadSqlEjecutadas |")
$resumen.Add("")

# De las fallidas mostramos solo el nombre; el mensaje completo queda en el log y en el TRX.
if ($pruebasFallidas.Count -gt 0) {
    $resumen.Add("### Pruebas fallidas")
    foreach ($nombre in $pruebasFallidas) {
        $resumen.Add("- ``$nombre``")
    }
    $resumen.Add("")
}

if ($omisionesNoPermitidas.Count -gt 0) {
    $problemas.Add("Hay $($omisionesNoPermitidas.Count) pruebas omitidas que no están permitidas (revisar que SQL Server esté disponible).")
    $resumen.Add("### Omisiones NO permitidas")
    foreach ($detalle in $omisionesNoPermitidas) {
        $resumen.Add("- $detalle")
    }
    $resumen.Add("")
}

if ($omisionesPermitidas.Count -gt 0) {
    $resumen.Add("### Omisiones permitidas")
    foreach ($detalle in $omisionesPermitidas) {
        $resumen.Add("- $detalle")
    }
    $resumen.Add("")
}

# Por si algún día un filtro o un cambio de configuración deja afuera las pruebas de SQL Server:
# en ese caso no figurarían como omitidas, directamente no estarían.
if ($informes.Count -gt 0 -and $cantidadSqlEjecutadas -eq 0) {
    $problemas.Add("No se ejecutó ninguna prueba de las clases *SqlServerTests.")
}

# --- 2. Cobertura ----------------------------------------------------------------------------

# Cada ejecución deja el informe en una subcarpeta con nombre al azar. Miramos solo ese primer
# nivel, porque el logger TRX guarda además una copia más adentro.
$informesCobertura = @()
if (Test-Path -LiteralPath $CarpetaResultados) {
    foreach ($subcarpeta in Get-ChildItem -LiteralPath $CarpetaResultados -Directory) {
        $ruta = Join-Path $subcarpeta.FullName "coverage.cobertura.xml"
        if (Test-Path -LiteralPath $ruta) {
            $informesCobertura += $ruta
        }
    }
}

$resumen.Add("## Cobertura de código")
$resumen.Add("")
$resumen.Add("Es informativa: no hay un porcentaje mínimo. No incluye las migraciones de EF Core.")
$resumen.Add("")

if ($informesCobertura.Count -eq 0) {
    $problemas.Add("No se generó el informe de cobertura.")
}

# El XML trae las proporciones entre 0 y 1, siempre con punto decimal.
function ConvertTo-Porcentaje([string]$proporcion) {
    $cultura = [System.Globalization.CultureInfo]::InvariantCulture
    $valor = [double]::Parse($proporcion, $cultura) * 100
    return $valor.ToString("0.0", $cultura) + " %"
}

foreach ($ruta in $informesCobertura) {
    $documento = New-Object System.Xml.XmlDocument
    $documento.Load($ruta)
    $cobertura = $documento.coverage

    $resumen.Add("| Proyecto | Líneas | Ramas |")
    $resumen.Add("|---|---:|---:|")
    foreach ($paquete in $cobertura.packages.package) {
        $resumen.Add("| $($paquete.name) | $(ConvertTo-Porcentaje $paquete.'line-rate') | $(ConvertTo-Porcentaje $paquete.'branch-rate') |")
    }
    $resumen.Add("| **Total** | **$(ConvertTo-Porcentaje $cobertura.'line-rate')** ($($cobertura.'lines-covered') de $($cobertura.'lines-valid')) | **$(ConvertTo-Porcentaje $cobertura.'branch-rate')** ($($cobertura.'branches-covered') de $($cobertura.'branches-valid')) |")
    $resumen.Add("")
}

# --- 3. Salida -------------------------------------------------------------------------------

$resumen | ForEach-Object { Write-Output $_ }

# En GitHub Actions este archivo es el resumen que se ve en la página de la ejecución.
if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $resumen -Encoding UTF8
}

if ($problemas.Count -gt 0) {
    foreach ($problema in $problemas) {
        # El prefijo ::error:: hace que GitHub destaque el mensaje en la ejecución.
        Write-Output "::error::$problema"
    }
    exit 1
}

Write-Output "Control de omisiones e informes correcto. El resultado de las pruebas lo informa 'dotnet test'."
exit 0
