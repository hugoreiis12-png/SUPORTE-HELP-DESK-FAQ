param(
  [string]$Root = (Join-Path (Split-Path -Parent $PSScriptRoot) "backend")
)

$ErrorActionPreference = "Stop"
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

$packageRoot = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Add-Failure {
  param([string]$Message)
  $failures.Add($Message)
}

function Add-Warning {
  param([string]$Message)
  $warnings.Add($Message)
}

function Assert-Path {
  param(
    [string]$Path,
    [string]$Message
  )
  if (-not (Test-Path -LiteralPath $Path)) {
    Add-Failure $Message
  }
}

$projectNames = @(
  "Comprove.HelpDesk.Domain",
  "Comprove.HelpDesk.Application",
  "Comprove.HelpDesk.Infrastructure",
  "Comprove.HelpDesk.Api",
  "Comprove.HelpDesk.Worker",
  "Comprove.HelpDesk.Contracts",
  "Comprove.HelpDesk.Tests.Unit",
  "Comprove.HelpDesk.Tests.Integration"
)

$solutionPath = Join-Path $Root "Comprove.HelpDesk.sln"
Assert-Path $solutionPath "Solution não encontrada: $solutionPath"

foreach ($projectName in $projectNames) {
  $folder = if ($projectName -like "*Tests*") { "tests" } else { "src" }
  $projectPath = Join-Path $Root (Join-Path $folder (Join-Path $projectName "$projectName.csproj"))
  Assert-Path $projectPath "Projeto não encontrado: $projectPath"
}

$globalJsonPath = Join-Path $Root "global.json"
Assert-Path $globalJsonPath "global.json não encontrado em $Root"

$propsPath = Join-Path $Root "Directory.Build.props"
Assert-Path $propsPath "Directory.Build.props não encontrado em $Root"

$nugetConfigPath = Join-Path $Root "NuGet.Config"
Assert-Path $nugetConfigPath "NuGet.Config não encontrado em $Root"

$globalContent = ""
if (Test-Path -LiteralPath $globalJsonPath) {
  $globalContent = Get-Content -LiteralPath $globalJsonPath -Raw
  if (-not $globalContent.Contains("10.0.100")) {
    Add-Failure "global.json não fixa SDK 10.0.100"
  }
  if (-not $globalContent.Contains("latestFeature")) {
    Add-Failure "global.json não usa rollForward=latestFeature"
  }
}

$propsContent = ""
if (Test-Path -LiteralPath $propsPath) {
  $propsContent = Get-Content -LiteralPath $propsPath -Raw
  if (-not $propsContent.Contains("net10.0")) {
    Add-Failure "Directory.Build.props não define net10.0"
  }
}

if (Test-Path -LiteralPath $nugetConfigPath) {
  $nugetContent = Get-Content -LiteralPath $nugetConfigPath -Raw
  if (-not $nugetContent.Contains("api.nuget.org/v3/index.json")) {
    Add-Failure "NuGet.Config não define a fonte nuget.org"
  }
}

$sourceFiles = @(
  Get-ChildItem -LiteralPath $Root -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '\\(bin|obj|\.git)\\' -and $_.Extension -in @(".cs", ".json", ".csproj", ".props")
  }
)

if ($sourceFiles.Count -eq 0) {
  Add-Failure "Nenhum arquivo .cs/.json/.csproj/.props encontrado em $Root"
}

$allContent = ""
foreach ($sourceFile in $sourceFiles) {
  $allContent += (Get-Content -LiteralPath $sourceFile.FullName -Raw -Encoding UTF8) + "`n"
}

$csprojFiles = @($sourceFiles | Where-Object { $_.Extension -eq ".csproj" })
$csprojContent = ""
foreach ($csprojFile in $csprojFiles) {
  $csprojContent += (Get-Content -LiteralPath $csprojFile.FullName -Raw -Encoding UTF8) + "`n"
}

if ($csprojContent -notmatch "MongoDB\.Driver") {
  Add-Failure "Pacote MongoDB.Driver não referenciado"
}

if ($csprojContent -notmatch "RabbitMQ\.Client") {
  Add-Failure "Pacote RabbitMQ.Client não referenciado"
}

if ($csprojContent -match "EntityFrameworkCore") {
  Add-Failure "EF Core encontrado; a arquitetura exige MongoDB.Driver sem EF Core"
}

$testProjects = @($csprojFiles | Where-Object { $_.Name -like "*Tests*.csproj" })
if ($testProjects.Count -lt 2) {
  Add-Failure "Esperados pelo menos 2 projetos de teste; encontrados $($testProjects.Count)"
}

$requiredTokens = @(
  @{ Token = "helpdesk_database"; Message = "Database lógica helpdesk_database não encontrada" },
  @{ Token = "outbox_events"; Message = "Collection outbox_events não encontrada" },
  @{ Token = "processed_messages"; Message = "Collection processed_messages não encontrada" },
  @{ Token = "ticket_history"; Message = "Collection ticket_history não encontrada" },
  @{ Token = "ticket_comments"; Message = "Collection ticket_comments não encontrada" },
  @{ Token = "ticket_attachments"; Message = "Collection ticket_attachments não encontrada" },
  @{ Token = "ticket_categories"; Message = "Collection ticket_categories não encontrada" },
  @{ Token = "support_queues"; Message = "Collection support_queues não encontrada" },
  @{ Token = "support_teams"; Message = "Collection support_teams não encontrada" },
  @{ Token = "sla_policies"; Message = "Collection sla_policies não encontrada" },
  @{ Token = "helpdesk.topic"; Message = "Exchange helpdesk.topic não encontrado" },
  @{ Token = "helpdesk.ticket.created"; Message = "Evento helpdesk.ticket.created não encontrado" },
  @{ Token = "helpdesk.ticket.resolved"; Message = "Evento helpdesk.ticket.resolved não encontrado" },
  @{ Token = "ticket.created.classification"; Message = "Fila ticket.created.classification não encontrada" },
  @{ Token = "ticket.classified.dispatch"; Message = "Fila ticket.classified.dispatch não encontrada" },
  @{ Token = "ticket.resolved.notify"; Message = "Fila ticket.resolved.notify não encontrada" },
  @{ Token = "integration.comprove.query"; Message = "Fila integration.comprove.query não encontrada" },
  @{ Token = "tenant_id"; Message = "Filtro tenant_id não encontrado" },
  @{ Token = "event_id"; Message = "event_id não encontrado" },
  @{ Token = "idempotency_key"; Message = "idempotency_key não encontrado" },
  @{ Token = "correlation_id"; Message = "correlation_id não encontrado" },
  @{ Token = "sla_deadline"; Message = "sla_deadline não encontrado" },
  @{ Token = "paused_at"; Message = "paused_at não encontrado" },
  @{ Token = "resolved_at"; Message = "resolved_at não encontrado" },
  @{ Token = "SUP-"; Message = "Protocolo SUP- não encontrado" },
  @{ Token = "Recebido"; Message = "Estado Recebido não encontrado" },
  @{ Token = "EmTriagem"; Message = "Estado EmTriagem não encontrado" },
  @{ Token = "AguardandoAtendimento"; Message = "Estado AguardandoAtendimento não encontrado" },
  @{ Token = "EmAtendimento"; Message = "Estado EmAtendimento não encontrado" },
  @{ Token = "AguardandoSolicitante"; Message = "Estado AguardandoSolicitante não encontrado" },
  @{ Token = "Resolvido"; Message = "Estado Resolvido não encontrado" },
  @{ Token = "Fechado"; Message = "Estado Fechado não encontrado" }
)

foreach ($entry in $requiredTokens) {
  if (-not $allContent.Contains($entry.Token)) {
    Add-Failure $entry.Message
  }
}

$bannedTokens = @(
  @{ Token = "processed_events"; Message = "processed_events encontrado; use processed_messages" },
  @{ Token = "erp_database"; Message = "erp_database encontrado no código do Help Desk" },
  @{ Token = "WeatherForecast"; Message = "Resto do template WeatherForecast encontrado" }
)

foreach ($entry in $bannedTokens) {
  if ($allContent.Contains($entry.Token)) {
    Add-Failure $entry.Message
  }
}

$httpGetCount = [regex]::Matches($allContent, "HttpGet").Count
$httpPostCount = [regex]::Matches($allContent, "HttpPost").Count
$httpPatchCount = [regex]::Matches($allContent, "HttpPatch").Count

if ($httpGetCount -lt 2) {
  Add-Failure "Esperados pelo menos 2 endpoints HttpGet; encontrados $httpGetCount"
}

if ($httpPostCount -lt 4) {
  Add-Failure "Esperados pelo menos 4 endpoints HttpPost; encontrados $httpPostCount"
}

if ($httpPatchCount -lt 1) {
  Add-Failure "Endpoint HttpPatch para /meta não encontrado"
}

if ($allContent -notmatch "Hub") {
  Add-Warning "Nenhum Hub SignalR encontrado; confirmar B21"
}

if ($allContent.Contains("Crítica") -and $allContent.Contains("2h") -and $allContent.Contains("48h")) {
  Write-Host "Matriz SLA inicial encontrada"
}
else {
  Add-Warning "Matriz SLA inicial não encontrada literalmente no código"
}

$dotnetCandidates = @(
  (Join-Path $packageRoot ".dotnet\dotnet.exe"),
  (Join-Path $Root ".dotnet\dotnet.exe")
)

$dotnetExe = $dotnetCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $dotnetExe) {
  $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
  if ($dotnetCommand) {
    $dotnetExe = $dotnetCommand.Source
  }
  else {
    Add-Failure "dotnet não encontrado no PATH nem em backend-tasks\.dotnet"
  }
}

if ($dotnetExe -and (Test-Path -LiteralPath $solutionPath)) {
  & $dotnetExe --version
  if ($LASTEXITCODE -ne 0) {
    Add-Failure "dotnet --version falhou"
  }

  & $dotnetExe restore $solutionPath --nologo --configfile $nugetConfigPath
  if ($LASTEXITCODE -ne 0) {
    Add-Failure "dotnet restore falhou"
  }

  & $dotnetExe build $solutionPath -c Release --nologo --no-restore
  if ($LASTEXITCODE -ne 0) {
    Add-Failure "dotnet build falhou"
  }

  & $dotnetExe test $solutionPath -c Release --no-build --nologo
  if ($LASTEXITCODE -ne 0) {
    Add-Failure "dotnet test falhou"
  }
}

$infraRoot = Join-Path $packageRoot "infra"
$templateRootPath = $infraRoot
$infraFiles = @("docker-compose.yml", "mongo-init.js", "rabbitmq.conf", "rabbitmq-definitions.json")

foreach ($infraFile in $infraFiles) {
  $infraPath = Join-Path $infraRoot $infraFile
  $templatePath = Join-Path $templateRootPath $infraFile
  if (-not (Test-Path -LiteralPath $infraPath)) {
    Add-Failure "Arquivo de infra ausente: $infraPath (rode scripts\Start-Infra.ps1)"
  }
}

$rabbitDefinitionsPath = Join-Path $templateRootPath "rabbitmq-definitions.json"
$rabbitDefinitions = Get-Content -LiteralPath $rabbitDefinitionsPath -Raw | ConvertFrom-Json
$queueNames = @($rabbitDefinitions.queues | ForEach-Object { $_.name })
foreach ($queueName in $queueNames) {
  if ($queueName.EndsWith(".dlq")) { continue }
  if ($queueNames -notcontains "$queueName.dlq") {
    Add-Failure "Fila $queueName sem DLQ $queueName.dlq em rabbitmq-definitions.json"
  }
}

$dotnetListing = if ($dotnetExe) { & $dotnetExe --list-sdks 2>$null } else { @() }
if (-not ($dotnetListing -match "^10\.0\.")) {
  Add-Failure "SDK 10.0.x não instalado; rode Install-DotNetSdk.ps1"
}

foreach ($port in @(27017, 5672, 15672)) {
  $tcpResult = Test-NetConnection -ComputerName "127.0.0.1" -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
  if (-not $tcpResult) {
    Add-Warning "Porta $port não responde; suba a infra com scripts\Start-Infra.ps1"
  }
}

if ($warnings.Count -gt 0) {
  Write-Host "AVISOS:"
  foreach ($warning in $warnings) {
    Write-Host "  - $warning"
  }
}

if ($failures.Count -gt 0) {
  Write-Host "FALHAS:"
  foreach ($failure in $failures) {
    Write-Host "  - $failure"
  }
  Write-Host "VALIDAÇÃO: FAIL"
  exit 1
}

Write-Host "VALIDAÇÃO: PASS"

