param(
  [string]$Tenant = "tenant-dev",
  [string]$Subject = "usuario-dev",
  [ValidateSet("requester", "agent")]
  [string]$Role = "requester",
  [string]$Name = "Usuario Dev",
  [string]$Email = "dev@comprove.local",
  [string]$Unit = "Operacao Comprove",
  [string]$Phone = "",
  [int]$MinutesValid = 30,
  [string]$Issuer = "bubble-comprove",
  [string]$Audience = "helpdesk-api"
)

# Token JWT HS256 de DESENVOLVIMENTO, assinado com a chave local de scripts\Start-Api.ps1. Nunca use em produção.
$ErrorActionPreference = "Stop"
$keysFile = Join-Path $env:LOCALAPPDATA "HelpDesk\dev-keys.json"
if (-not (Test-Path -LiteralPath $keysFile)) { throw "Chaves de desenvolvimento não encontradas. Rode scripts\Start-Api.ps1 uma vez." }
$signingKey = (Get-Content -LiteralPath $keysFile -Raw | ConvertFrom-Json).BubbleSigningKey

function ConvertTo-Base64Url([byte[]]$Bytes) { [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') }
function ConvertTo-Json64([object]$Value) { ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes(($Value | ConvertTo-Json -Compress))) }

$now = [DateTimeOffset]::UtcNow
$roles = if ($Role -eq "agent") { @("support_agent") } else { @() }
$header = ConvertTo-Json64 @{ alg = "HS256"; typ = "JWT" }
$payload = ConvertTo-Json64 ([ordered]@{
  sub = $Subject
  tenant_id = $Tenant
  name = $Name
  email = $Email
  unit = $Unit
  phone = $Phone
  roles = $roles
  iss = $Issuer
  aud = $Audience
  jti = [guid]::NewGuid().ToString("N")
  iat = $now.ToUnixTimeSeconds()
  exp = $now.AddMinutes($MinutesValid).ToUnixTimeSeconds()
})

$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($signingKey))
$signature = ConvertTo-Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$header.$payload")))
"$header.$payload.$signature"
