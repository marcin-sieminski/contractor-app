#!/usr/bin/env pwsh
# Uruchamia wszystkie testy jednostkowe projektu ContractorApp.
# Użycie: ./run-tests.ps1 [-Verbose] [-Filter <pattern>]
#
# Parametry:
#   -Verbose    Pełne logi xUnit (domyślnie: normal)
#   -Filter     Filtr nazw testów, np. -Filter "Tax*" lub -Filter "FullyQualifiedName~NIP"

param(
    [switch]$Verbose,
    [string]$Filter = ""
)

$ErrorActionPreference = "Stop"
$slnPath = Join-Path $PSScriptRoot "mvp-1\backend\ContractorApp.slnx"

if (-not (Test-Path $slnPath)) {
    Write-Error "Nie znaleziono pliku rozwiązania: $slnPath"
    exit 1
}

$verbosity = if ($Verbose) { "detailed" } else { "normal" }

$args = @(
    "test", $slnPath,
    "--configuration", "Release",
    "--logger", "console;verbosity=$verbosity"
)

if ($Filter) {
    $args += "--filter", $Filter
}

Write-Host "Uruchamiam testy: $slnPath" -ForegroundColor Cyan
Write-Host ""

dotnet @args
exit $LASTEXITCODE
