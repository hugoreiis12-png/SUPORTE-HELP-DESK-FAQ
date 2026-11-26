param(
  [ValidateSet("Api", "Worker")]
  [string]$Project = "Api",
  [int]$TunnelPort = 27018,
  # Expõe a API e libera o front (porta 3000) do IP da rede local no CORS.
  [switch]$Lan
)

$ErrorActionPreference = "Stop"
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$root = Split-Path -Parent $PSScriptRoot
$keysFile = Join-Path $env:LOCALAPPDATA "HelpDesk\dev-keys.json"

function New-RandomKey {
  $bytes = New-Object byte[] 48
  [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
  [Convert]::ToBase64String($bytes)
}

# Chaves de desenvolvimento ficam FORA do repositório e são reaproveitadas entre execuções (New-DevToken usa a mesma).
function Get-DevKeys {
  if (Test-Path -LiteralPath $keysFile) { return Get-Content -LiteralPath $keysFile -Raw | ConvertFrom-Json }
  New-Item -ItemType Directory -Force -Path (Split-Path -Parent $keysFile) | Out-Null
  $keys = [pscustomobject]@{ BubbleSigningKey = New-RandomKey; StorageLinkKey = New-RandomKey }
  $keys | ConvertTo-Json | Set-Content -LiteralPath $keysFile -Encoding UTF8
  return $keys
}

function Get-LanAddress {
  $adapter = Get-NetIPConfiguration | Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq "Up" } | Select-Object -First 1
  if (-not $adapter) { throw "Nenhuma interface de rede com gateway IPv4 encontrada para o modo -Lan." }
  $adapter.IPv4Address.IPAddress
}

function Assert-TunnelOpen {
  if (-not (Get-NetTCPConnection -LocalPort $TunnelPort -State Listen -ErrorAction SilentlyContinue)) {
    throw "Túnel SSH fechado (porta $TunnelPort). Abra: ssh -N -L ${TunnelPort}:127.0.0.1:27018 <usuario>@192.168.0.142"
  }
}

function Start-RabbitMq {
  & docker info *> $null
  if ($LASTEXITCODE -ne 0) { throw "Docker não está rodando. Abra o Docker Desktop." }
  & docker compose --file (Join-Path $root "infra\docker-compose.yml") up -d rabbitmq
  if ($LASTEXITCODE -ne 0) { throw "Falha ao subir o RabbitMQ" }
}

function Read-MongoPassword {
  $secure = Read-Host "Senha do usuario helpdesk_app (MongoDB)" -AsSecureString
  $plain = [System.Net.NetworkCredential]::new("", $secure).Password
  [Uri]::EscapeDataString($plain)
}

Assert-TunnelOpen
Start-RabbitMq
$keys = Get-DevKeys
$password = Read-MongoPassword

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
$env:ConnectionStrings__HelpDeskMongo = "mongodb://helpdesk_app:$password@127.0.0.1:${TunnelPort}/helpdesk_database?authSource=admin&directConnection=true"
$env:RabbitMq__Password = "guest"
$env:BubbleSecurity__SigningKey = $keys.BubbleSigningKey
$env:Storage__LinkSigningKey = $keys.StorageLinkKey

$bindUrl = "http://localhost:5112"
if ($Lan -and $Project -eq "Api") {
  $lanIp = Get-LanAddress
  # Índice 2 acrescenta à lista do appsettings.json (que já tem 2 origens).
  $env:Cors__AllowedOrigins__2 = "http://${lanIp}:3000"
  $env:Storage__PublicBaseUrl = "http://${lanIp}:5112/"
  $bindUrl = "http://0.0.0.0:5112"
  Write-Host "Modo rede: API em http://${lanIp}:5112 | front em http://${lanIp}:3000"
}

$projectPath = Join-Path $root "backend\src\Comprove.HelpDesk.$Project"
$arguments = @("run", "--project", $projectPath)
if ($Project -eq "Api") { $arguments += @("--urls", $bindUrl) }
Write-Host "Iniciando $Project (Ctrl+C para parar)..."
& dotnet @arguments
