param(
  [string]$InfraRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "infra")
)

$ErrorActionPreference = "Stop"
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$composePath = Join-Path $InfraRoot "docker-compose.yml"
$mongoContainer = "helpdesk-mongo"
$mongoPort = 27017
$healthTimeoutSeconds = 120

function Assert-DockerRunning {
  & docker info *> $null
  if ($LASTEXITCODE -ne 0) {
    throw "Docker daemon não está rodando. Abra o Docker Desktop e aguarde 'Engine running'."
  }
}

function Assert-PortFree {
  param([int]$Port, [string]$Hint)
  $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
  if ($listener) {
    $owner = (Get-Process -Id $listener[0].OwningProcess -ErrorAction SilentlyContinue).ProcessName
    throw "Porta $Port já está em uso por '$owner' (PID $($listener[0].OwningProcess)). $Hint"
  }
}

function Wait-MongoHealthy {
  $deadline = (Get-Date).AddSeconds($healthTimeoutSeconds)
  while ((Get-Date) -lt $deadline) {
    $status = (& docker inspect --format '{{.State.Health.Status}}' $mongoContainer 2>$null)
    if ($status -eq "healthy") { return }
    Start-Sleep -Seconds 3
  }
  throw "Container $mongoContainer não ficou healthy em ${healthTimeoutSeconds}s. Veja: docker logs $mongoContainer"
}

Assert-DockerRunning
Assert-PortFree -Port $mongoPort -Hint "Pare o serviço Windows do MongoDB antes: Stop-Service MongoDB (PowerShell como administrador)."
& docker compose --file $composePath up -d
if ($LASTEXITCODE -ne 0) { throw "Falha ao subir a infra local" }
Wait-MongoHealthy
# Inicializa a replica set rs0 (idempotente). O mongo-init.js fica montado em /scripts no container.
& docker exec $mongoContainer mongosh --quiet --file /scripts/mongo-init.js
if ($LASTEXITCODE -ne 0) { throw "Falha ao inicializar a replica set rs0" }
& docker compose --file $composePath ps
