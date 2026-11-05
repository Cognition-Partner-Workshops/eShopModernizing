param(
    [switch]$Start,
    [switch]$PortOnly,
    [string]$ReferenceAssembliesPath = 'C:\scratch\ra461\build',
    [string]$Nuget = 'C:\scratch\nuget.exe',
    [string]$Grpcurl = 'C:\scratch\grpcurl\grpcurl.exe',
    [string]$MSBuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
    [string]$IISExpress = 'C:\Program Files\IIS Express\iisexpress.exe',
    [int]$TimeoutSeconds = 120
)
$ErrorActionPreference = 'Stop'
$timer = [Diagnostics.Stopwatch]::StartNew()
$grpcRoot = Split-Path $PSScriptRoot -Parent
$repo = [IO.Path]::GetFullPath((Join-Path $grpcRoot '..\..'))
$project = Join-Path $grpcRoot 'src\eShop.Catalog.Grpc\eShop.Catalog.Grpc.csproj'
$wcf = Join-Path $repo 'eShopLegacyNTier\src\eShopWCFService'
$logs = Join-Path $grpcRoot 'parity\out'

function Run([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Exe failed with exit code $LASTEXITCODE" }
}
function Stop-Services {
    $ports = if ($PortOnly) { @(5199) } else { @(5199, 62314) }
    $listeners = @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $ports } | Select-Object -ExpandProperty OwningProcess -Unique)
    foreach ($process in Get-CimInstance Win32_Process) {
        $isPort = $process.Name -eq 'dotnet.exe' -and
            ($process.CommandLine -match 'eShop\.Catalog\.Grpc' -or $process.ProcessId -in $listeners)
        $isAppHost = $process.Name -match '^eShop\.Catalog\.Grpc(\.exe)?$'
        $isLegacy = !$PortOnly -and $process.Name -eq 'iisexpress.exe'
        if ($isPort -or $isAppHost -or $isLegacy) {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
        }
    }
}
function Ensure-LegacyTools {
    if (!(Test-Path $Nuget)) {
        New-Item -ItemType Directory -Force (Split-Path $Nuget) | Out-Null
        Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/v6.11.1/nuget.exe' -OutFile $Nuget -UseBasicParsing
    }
    $refs = Join-Path $ReferenceAssembliesPath '.NETFramework\v4.6.1\mscorlib.dll'
    if (!(Test-Path $refs)) {
        New-Item -ItemType Directory -Force $ReferenceAssembliesPath | Out-Null
        $download = Join-Path $logs 'reference-assemblies.zip'
        $extract = Join-Path $logs 'reference-assemblies'
        Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net461/1.0.3/microsoft.netframework.referenceassemblies.net461.1.0.3.nupkg' -OutFile $download -UseBasicParsing
        Expand-Archive $download -DestinationPath $extract -Force
        Copy-Item "$extract\build\*" $ReferenceAssembliesPath -Recurse -Force
        if (!(Test-Path $refs)) { throw "Reference assemblies not found at $refs after extraction" }
    }
}
function Reset-LegacyDatabase {
    $connection = New-Object System.Data.SqlClient.SqlConnection 'Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30'
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 60
        $command.CommandText = "IF DB_ID(N'eShopDatabase') IS NOT NULL BEGIN ALTER DATABASE [eShopDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [eShopDatabase]; END"
        [void]$command.ExecuteNonQuery()
    } finally { $connection.Dispose() }
}
function Start-Services {
    if (!(Test-Path $Grpcurl)) { throw "grpcurl not found: $Grpcurl" }
    if (!$PortOnly) {
        Start-Process $IISExpress -ArgumentList "/path:`"$wcf`" /port:62314" -RedirectStandardOutput "$logs\legacy-host.log" -RedirectStandardError "$logs\legacy-host-error.log" | Out-Null
    }
    Start-Process 'dotnet' -WorkingDirectory $grpcRoot -ArgumentList "run --project `"$project`" --no-build -c Release --no-launch-profile" -RedirectStandardOutput "$logs\port-host.log" -RedirectStandardError "$logs\port-host-error.log" | Out-Null
    $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            & $Grpcurl -plaintext 'localhost:5199' list 2> "$logs\grpc-ready-error.log" | Out-Null
            $portReady = $LASTEXITCODE -eq 0
        } finally { $ErrorActionPreference = $previousPreference }
        $legacyReady = [bool]$PortOnly
        if (!$PortOnly) {
            try { $legacyReady = (Invoke-WebRequest 'http://localhost:62314/CatalogService.svc?wsdl' -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch { $legacyReady = $false }
        }
        if ($portReady -and $legacyReady) { Write-Host 'Requested services are ready.'; return }
        Start-Sleep -Seconds 1
    } while ([datetime]::UtcNow -lt $deadline)
    throw "Services did not become ready within $TimeoutSeconds seconds. See $logs"
}
try {
    New-Item -ItemType Directory -Force $logs | Out-Null
    Stop-Services
    Push-Location $grpcRoot
    try { Run 'dotnet' @('build', 'eShopGrpc.sln', '-c', 'Release') } finally { Pop-Location }
    if (!$PortOnly) {
        Ensure-LegacyTools
        Run $Nuget @('restore', "$wcf\packages.config", '-PackagesDirectory', "$repo\eShopLegacyNTier\src\packages")
        Run $MSBuild @("$wcf\eShopWCFService.csproj", '-p:Configuration=Release', "-p:TargetFrameworkRootPath=$ReferenceAssembliesPath")
        Reset-LegacyDatabase
    }
    if ($Start) { Start-Services }
} finally {
    $timer.Stop()
    Write-Host ("Total elapsed seconds: {0:N2}" -f $timer.Elapsed.TotalSeconds)
}
