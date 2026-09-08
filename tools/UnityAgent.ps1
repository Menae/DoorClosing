param(
    [Parameter(Position=0)][ValidateSet('Setup','Start','Status','Client')][string]$Action = 'Status',
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$ClientArguments
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$toolRoot = Join-Path $PSScriptRoot 'unity-agent'
$pythonPath = Join-Path $toolRoot '.venv/Scripts/python.exe'
$serverPath = Join-Path $toolRoot '.venv/Scripts/mcp-for-unity.exe'
$expectedVersion = '10.2.0'
$baseUrl = 'http://127.0.0.1:8080'
$env:UV_CACHE_DIR = Join-Path $projectRoot 'artifacts/cache/uv'
$env:UV_PYTHON_INSTALL_DIR = Join-Path $projectRoot 'artifacts/cache/uv-python'
$env:DISABLE_TELEMETRY = 'true'
$env:UNITY_MCP_DISABLE_TELEMETRY = 'true'
$env:MCP_DISABLE_TELEMETRY = 'true'
$env:UNITY_MCP_TELEMETRY_ENABLED = 'false'

function Get-ServerHealth {
    try { Invoke-RestMethod -Uri ($baseUrl + '/health') -TimeoutSec 3 }
    catch { return $null }
}
function Require-Environment {
    if (-not (Test-Path -LiteralPath $pythonPath)) { throw 'Run tools/UnityAgent.ps1 Setup first.' }
}
switch ($Action) {
    'Setup' {
        $uvCommand = Get-Command uv -ErrorAction SilentlyContinue
        $uvPath = if ($uvCommand) { $uvCommand.Source } else { Join-Path $env:USERPROFILE '.local/bin/uv.exe' }
        if (-not (Test-Path -LiteralPath $uvPath)) { throw 'uv is required; see docs/DEVELOPMENT.md.' }
        & $uvPath sync --locked --project $toolRoot
        if ($LASTEXITCODE -ne 0) { throw 'Pinned tool environment setup failed.' }
    }
    'Start' {
        Require-Environment
        $health = Get-ServerHealth
        if ($health) {
            if ($health.version -ne $expectedVersion) { throw "Port 8080 is serving $($health.version); review the existing process before replacing it." }
            Write-Output "MCP $expectedVersion is already available at $baseUrl."
            break
        }
        $logRoot = Join-Path $projectRoot ('artifacts/server/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
        [IO.Directory]::CreateDirectory($logRoot) | Out-Null
        $serverProcess = Start-Process -FilePath $serverPath -ArgumentList @('--transport','http','--http-url',$baseUrl) -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logRoot 'stdout.log') -RedirectStandardError (Join-Path $logRoot 'stderr.log')
        $ready = $false
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            $health = Get-ServerHealth
            if ($health -and $health.version -eq $expectedVersion) { $ready = $true; break }
            if ($serverProcess.HasExited) { throw "Server exited. Inspect $logRoot" }
            Start-Sleep -Milliseconds 300
        }
        if (-not $ready) { throw "Server did not become ready. Inspect $logRoot; do not launch duplicates." }
        Write-Output "MCP $expectedVersion ready. Unity: Tools > Unity Agent > Connect Local MCP. Logs: $logRoot"
    }
    'Status' {
        $health = Get-ServerHealth
        if (-not $health) { throw 'Local MCP is unavailable. Run tools/UnityAgent.ps1 Start.' }
        $health | ConvertTo-Json -Compress
        Require-Environment
        & $pythonPath (Join-Path $toolRoot 'client.py') resource 'mcpforunity://instances'
        if ($LASTEXITCODE -ne 0) { throw 'MCP instance query failed.' }
    }
    'Client' {
        Require-Environment
        & $pythonPath (Join-Path $toolRoot 'client.py') @ClientArguments
        if ($LASTEXITCODE -ne 0) { throw "MCP client failed with exit code $LASTEXITCODE" }
    }
}
