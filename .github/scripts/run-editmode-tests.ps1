<#
.SYNOPSIS
    Corre los tests EditMode del proyecto con el Unity instalado y resume el resultado.

.DESCRIPTION
    Lo usa la CI (.github/workflows/tests.yml) y también sirve en local con Unity cerrado:

        powershell -ExecutionPolicy Bypass -File .github/scripts/run-editmode-tests.ps1

    La versión de Unity sale de ProjectSettings/ProjectVersion.txt. Si existe la variable
    GITHUB_STEP_SUMMARY (GitHub Actions), añade ahí una tabla con el resultado.
    Devuelve 0 si todos los tests pasan y 1 en cualquier otro caso.

    IMPORTANTE: este archivo se guarda en UTF-8 CON BOM. Windows PowerShell 5.1 lee como ANSI
    los scripts sin BOM y rompe las tildes (ver .editorconfig).
#>
param(
    [string] $ProjectPath = (Get-Location).Path,
    [string] $ResultsPath = (Join-Path (Get-Location).Path 'test-results'),
    [string] $EditorsPath = 'C:\Program Files\Unity\Hub\Editor'
)

$ErrorActionPreference = 'Stop'

$versionFile = Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt'
$versionLine = Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(\S+)'
$version = $versionLine.Matches[0].Groups[1].Value
$unity = Join-Path $EditorsPath "$version\Editor\Unity.exe"
if (-not (Test-Path $unity)) {
    Write-Output "::error::Unity $version no está instalado en $unity"
    exit 1
}

New-Item -ItemType Directory -Force $ResultsPath | Out-Null
$xml = Join-Path $ResultsPath 'editmode.xml'
$log = Join-Path $ResultsPath 'editmode.log'
Remove-Item $xml, $log -ErrorAction SilentlyContinue

# Unity.exe es una aplicación de ventana: sin -Wait, PowerShell no espera a que termine.
$arguments = @(
    '-batchmode', '-nographics',
    '-projectPath', "`"$ProjectPath`"",
    '-runTests', '-testPlatform', 'EditMode',
    '-testResults', "`"$xml`"",
    '-logFile', "`"$log`""
)
Write-Output "Ejecutando tests EditMode con Unity $version…"
$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru
Write-Output "Unity terminó con código $($process.ExitCode)"

if (-not (Test-Path $xml)) {
    Write-Output "::error::Unity no generó resultados (código $($process.ExitCode)). Últimas líneas del log:"
    if (Test-Path $log) {
        Get-Content $log -Tail 40 -Encoding UTF8
    }
    exit 1
}

$document = [xml](Get-Content $xml -Raw -Encoding UTF8)
$run = $document.'test-run'
$summary = "### Tests EditMode`n`n| Resultado | Total | Pasan | Fallan | Omitidos |`n|---|---|---|---|---|`n"
$summary += "| $($run.result) | $($run.total) | $($run.passed) | $($run.failed) | $($run.skipped) |`n"

$failedCases = $document.SelectNodes("//test-case[@result='Failed']")
if ($failedCases.Count -gt 0) {
    $summary += "`n**Tests fallidos:**`n"
    foreach ($case in $failedCases) {
        $message = ($case.failure.message.InnerText -split "`n")[0].Trim()
        Write-Output "::error title=Test fallido::$($case.fullname): $message"
        $summary += "- ``$($case.fullname)``: $message`n"
    }
}

if ($env:GITHUB_STEP_SUMMARY) {
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::AppendAllText($env:GITHUB_STEP_SUMMARY, $summary, $utf8)
}

Write-Output "Resultado: $($run.result) · $($run.passed) de $($run.total) tests pasan"

if ($run.failed -ne '0' -or $process.ExitCode -ne 0) {
    exit 1
}
exit 0
