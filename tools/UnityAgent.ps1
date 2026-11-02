param(
    [Parameter(Position=0)][ValidateSet('Setup','Start','Launch','Status','Client')][string]$Action = 'Status',
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
function Start-PinnedServer {
    $health = Get-ServerHealth
    if ($health) {
        if ($health.version -ne $expectedVersion) { throw "Port 8080 is serving $($health.version); review the existing process before replacing it." }
        Write-Output "MCP $expectedVersion is already available at $baseUrl."
        return
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
    Write-Output "MCP $expectedVersion ready. Logs: $logRoot"
}
function Get-ProjectEditor {
    # An Editor opened from Unity Hub carries -projectPath; the lock file catches any other launch.
    $needle = $projectRoot.TrimEnd('\')
    $editors = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {
        $m = [regex]::Match([string]$_.CommandLine, '"?-projectpath"?\s+(?:"([^"]+)"|(\S+))', 'IgnoreCase')
        if (-not $m.Success) { return $false }
        $path = if ($m.Groups[1].Success) { $m.Groups[1].Value } else { $m.Groups[2].Value }
        try { [IO.Path]::GetFullPath($path).TrimEnd('\') -ieq $needle } catch { $false } })
    if ($editors.Count -gt 0) { return $editors }
    $lock = Join-Path $projectRoot 'Temp/UnityLockfile'
    if (Test-Path -LiteralPath $lock) {
        try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Dispose() }
        catch { throw "Temp/UnityLockfile is held but no Unity.exe for this project was found. Check open Editors manually; do not launch a second one." }
    }
    return @()
}
function Wait-ProjectInstance([int]$timeoutSeconds) {
    # The client exits non-zero (stderr) until the Editor registers; keep polling instead of stopping.
    $ErrorActionPreference = 'Continue'
    $file = Join-Path $projectRoot 'artifacts/orchestra/instances.json'
    $deadline = [DateTime]::UtcNow.AddSeconds($timeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        Remove-Item -LiteralPath $file -ErrorAction SilentlyContinue
        try { & $pythonPath (Join-Path $toolRoot 'client.py') --quiet --output $file resource 'mcpforunity://instances' 2>$null | Out-Null } catch {}
        if ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $file)) {
            # A same-named worktree may also register; accept only the instance whose projectRoot is this checkout.
            $names = [regex]::Matches((Get-Content -LiteralPath $file -Raw), 'GraduationProject@[0-9a-fA-F]+') | ForEach-Object { $_.Value } | Select-Object -Unique
            foreach ($name in $names) {
                $info = Join-Path $projectRoot 'artifacts/orchestra/project-info.json'
                try { & $pythonPath (Join-Path $toolRoot 'client.py') --quiet --instance $name --output $info resource 'mcpforunity://project/info' 2>$null | Out-Null } catch {}
                if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $info)) { continue }
                $root = [regex]::Match((Get-Content -LiteralPath $info -Raw), 'projectRoot\\?":\s*\\?"([^"\\]+)').Groups[1].Value
                if ($root -and ([IO.Path]::GetFullPath($root).TrimEnd('\') -ieq $projectRoot.TrimEnd('\'))) { return $name }
            }
        }
        Start-Sleep -Seconds 5
    }
    throw "No GraduationProject instance reached the MCP server within $timeoutSeconds s. Check the Editor Console (Tools > Unity Agent > Connect Local MCP)."
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
        Start-PinnedServer
        Write-Output 'Unity: Tools > Unity Agent > Connect Local MCP (an open Editor reconnects automatically after domain reload).'
    }
    'Launch' {
        # WORK-007: server + this project's Editor + MCP instance, without ever opening a second Editor.
        Require-Environment
        Start-PinnedServer
        $editors = @(Get-ProjectEditor)
        if ($editors.Count -gt 0) {
            Write-Output "Unity Editor already open for this project (PID $($editors[0].ProcessId))."
        } else {
            $version = [regex]::Match((Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Raw), 'm_EditorVersion:\s*(\S+)').Groups[1].Value
            $unityPath = if ($env:UNITY_EDITOR_PATH) { $env:UNITY_EDITOR_PATH } else { "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
            if (-not (Test-Path -LiteralPath $unityPath)) { throw "Unity $version not found at $unityPath. Install it via Unity Hub or set UNITY_EDITOR_PATH." }
            $actual = (Get-Item -LiteralPath $unityPath).VersionInfo.ProductVersion
            if (-not $actual -or -not $actual.StartsWith($version)) { throw "$unityPath reports $actual, not $version; opening the project with another version would prompt an upgrade." }
            $editor = Start-Process -FilePath $unityPath -ArgumentList @('-projectPath', "`"$projectRoot`"") -PassThru
            Write-Output "Started Unity $version (PID $($editor.Id)); waiting for the MCP instance."
        }
        $instance = Wait-ProjectInstance 900
        Write-Output "MCP instance ready: $instance"
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
