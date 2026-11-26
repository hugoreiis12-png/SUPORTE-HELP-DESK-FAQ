﻿param(
  [string]$FrontendRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "front_end\SUPORTE-HELP-DESK-")
)

$ErrorActionPreference = "Stop"
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$failures = New-Object System.Collections.Generic.List[string]

function Invoke-Step {
  param([string]$Name, [scriptblock]$Action)
  Write-Host "==> $Name"
  & $Action
  if ($LASTEXITCODE -ne 0) { $failures.Add("$Name falhou") }
}

if (-not (Test-Path -LiteralPath (Join-Path $FrontendRoot "package.json"))) {
  throw "package.json não encontrado em $FrontendRoot"
}

Push-Location $FrontendRoot
try {
  $packageManager = if (Get-Command bun -ErrorAction SilentlyContinue) { "bun" } else { "npm" }
  Invoke-Step "install ($packageManager)" { & $packageManager install }
  Invoke-Step "lint (tsc --noEmit)" { & $packageManager run lint }
  Invoke-Step "build (vite build)" { & $packageManager run build }

  $hardcodedLocalhost = Get-ChildItem -LiteralPath (Join-Path $FrontendRoot "src") -Recurse -File -Include *.ts, *.tsx |
    Select-String -Pattern "localhost:\d+"
  if ($hardcodedLocalhost) {
    $failures.Add("localhost hardcoded em src (URL do backend vem de VITE_API_BASE_URL)")
  }
}
finally {
  Pop-Location
}

if ($failures.Count -gt 0) {
  Write-Host "FALHAS:"
  $failures | ForEach-Object { Write-Host "  - $_" }
  Write-Host "VALIDAÇÃO FRONT: FAIL"
  exit 1
}

Write-Host "VALIDAÇÃO FRONT: PASS"
