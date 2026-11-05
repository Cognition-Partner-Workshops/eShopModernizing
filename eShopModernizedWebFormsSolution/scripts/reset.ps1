param([switch]$Start)

$ErrorActionPreference = 'Stop'
$solution = Split-Path $PSScriptRoot -Parent
$repo = Split-Path $solution -Parent
$scratch = Join-Path $env:USERPROFILE 'scratch\webforms-legacy-site'
$artifacts = Join-Path $solution 'artifacts'
$runtimeLogs = Join-Path $env:USERPROFILE 'scratch\webforms-runtime-logs'
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$nuget = 'C:\ProgramData\chocolatey\bin\nuget.exe'
$msbuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
$iis = 'C:\Program Files\IIS Express\iisexpress.exe'

function Stop-ParityListener([int]$Port) {
    $owners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique)
    foreach ($ownerId in $owners) {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId=$ownerId"
        if ($ownerId -eq 4 -and $Port -eq 5201) {
            $workers = @(Get-CimInstance Win32_Process -Filter "Name='iisexpress.exe'" |
                Where-Object { $_.CommandLine -match '/port:5201(\s|$)' -and
                    $_.CommandLine.Contains($scratch) })
            if ($workers.Count -ne 1) { throw 'Cannot uniquely identify the IIS Express worker for port 5201.' }
            $process = $workers[0]
            $ownerId = $process.ProcessId
        }
        if ($process.Name -notin @('iisexpress.exe', 'dotnet.exe', 'eShopModernizedWebForms.exe')) {
            throw "Refusing to stop unexpected process $($process.Name) ($ownerId) on port $Port."
        }
        $parent = Get-CimInstance Win32_Process -Filter "ProcessId=$($process.ParentProcessId)"
        $stopId = $ownerId
        if ($process.Name -eq 'eShopModernizedWebForms.exe' -and
            $parent.Name -eq 'dotnet.exe' -and
            $parent.CommandLine -match 'eShopModernizedWebForms' -and
            $parent.CommandLine -match '5200') {
            $stopId = $parent.ProcessId
        }
        & taskkill.exe /PID $stopId /T /F | Write-Output
        if ($LASTEXITCODE -ne 0) { throw "Failed stopping listener on port $Port." }
    }
}

function Invoke-Checked([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Exe failed with exit code $LASTEXITCODE." }
}

function Wait-Ready([string]$Url) {
    $deadline = (Get-Date).AddSeconds(90)
    do {
        try {
            $response = Invoke-WebRequest $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) { Write-Output "$Url HTTP 200"; return }
        } catch { }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "$Url did not become ready within 90 seconds. See $runtimeLogs."
}

function Start-Detached([string]$Command) {
    $result = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
        CommandLine = "powershell.exe -NoProfile -ExecutionPolicy Bypass -Command `"$Command`""
        CurrentDirectory = $solution
    }
    if ($result.ReturnValue -ne 0) { throw "Detached startup failed: $($result.ReturnValue)" }
    return $result.ProcessId
}

Stop-ParityListener 5200
Stop-ParityListener 5201
New-Item -ItemType Directory -Force $artifacts | Out-Null
Push-Location $repo
try {
    Invoke-Checked $nuget @('restore', 'eShopLegacyWebFormsSolution/eShopLegacyWebForms.sln')
    Invoke-Checked $msbuild @('eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/eShopLegacyWebForms.csproj', '/p:Configuration=Release')
    Push-Location $solution
    try { Invoke-Checked $dotnet @('build', 'eShopModernizedWebForms.sln', '-c', 'Release') }
    finally { Pop-Location }

    New-Item -ItemType Directory -Force (Split-Path $scratch -Parent) | Out-Null
    if (Test-Path $scratch) { Remove-Item $scratch -Recurse -Force }
    Copy-Item (Join-Path $repo 'eShopLegacyWebFormsSolution\src\eShopLegacyWebForms') $scratch -Recurse
    $configPath = Join-Path $scratch 'Web.config'
    [xml]$config = Get-Content $configPath
    $mock = $config.configuration.appSettings.add | Where-Object key -eq 'UseMockData'
    if ($null -eq $mock) { throw 'The copied legacy Web.config has no UseMockData setting.' }
    $mock.value = 'true'
    $config.Save($configPath)
    Write-Output "Scratch site: $scratch; UseMockData=$($mock.value)"

    if ($Start) {
        New-Item -ItemType Directory -Force $runtimeLogs | Out-Null
        # WMI detaches IO handles so Measure-Command can finish while apps stay up.
        $legacy = Start-Detached "& '$iis' '/path:$scratch' /port:5201 1> '$runtimeLogs\legacy-runtime.stdout.txt' 2> '$runtimeLogs\legacy-runtime.stderr.txt'"
        $port = Start-Detached "& '$dotnet' run -c Release --no-build --project src/eShopModernizedWebForms --urls http://localhost:5200 1> '$runtimeLogs\port-runtime.stdout.txt' 2> '$runtimeLogs\port-runtime.stderr.txt'"
        Write-Output "Started detached launchers: legacy PID $legacy; port PID $port"
        Wait-Ready 'http://localhost:5201/'
        Wait-Ready 'http://localhost:5200/'
    }
} finally { Pop-Location }
