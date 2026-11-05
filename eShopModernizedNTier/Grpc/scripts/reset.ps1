param(
    [switch]$Start,
    [switch]$Stop,
    [switch]$PortOnly,
    [switch]$ResetDatabase,
    [string]$ReferenceAssembliesPath = 'C:\scratch\ra461\build',
    [string]$Nuget = 'C:\scratch\nuget.exe',
    [string]$Grpcurl = 'C:\scratch\grpcurl\grpcurl.exe',
    [string]$MSBuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
    [string]$IISExpress = 'C:\Program Files\IIS Express\iisexpress.exe',
    [int]$TimeoutSeconds = 120
)
$ErrorActionPreference = 'Stop'
if ($Stop -and ($Start -or $ResetDatabase)) { throw '-Stop cannot be combined with -Start or -ResetDatabase.' }
if ($PortOnly -and $ResetDatabase) { throw '-PortOnly cannot be combined with -ResetDatabase.' }
$timer = [Diagnostics.Stopwatch]::StartNew()
$grpcRoot = Split-Path $PSScriptRoot -Parent
$repo = [IO.Path]::GetFullPath((Join-Path $grpcRoot '..\..'))
$wcf = Join-Path $repo 'eShopLegacyNTier\src\eShopWCFService'
$work = Join-Path $env:LOCALAPPDATA 'eShopGrpcParity'
$site = Join-Path $work 'legacy-site'
$stateFile = Join-Path $work 'state.json'
$logs = Join-Path $grpcRoot 'parity\out'
$ports = if ($PortOnly) { @(5199) } else { @(5199, 62314) }

function Save-State {
    $json = ConvertTo-Json -InputObject $state -Depth 8
    [IO.File]::WriteAllText("$stateFile.new", $json)
    Move-Item "$stateFile.new" $stateFile -Force
}
function Matching-Process($record) {
    $process = Get-Process -Id $record.pid -ErrorAction SilentlyContinue
    if (!$process) { return $null }
    [void]$process.Handle
    if ($process.StartTime.ToUniversalTime().Ticks.ToString() -cne $record.startTimeUtcTicks -or
        ![string]::Equals($process.Path, $record.executablePath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "PID $($record.pid) no longer matches its recorded start time/path. Refusing to terminate it."
    }
    return $process
}
function Proof($records = $state.hosts) {
    Write-Host 'Get-Process for recorded owned PIDs:'
    foreach ($record in @($records | Where-Object { $_.port -in $ports })) {
        $process = Matching-Process $record
        if ($process) { $process | Select-Object Id, StartTime, Path | Format-Table | Out-Host }
        else { Write-Host "Owned PID $($record.pid) is not running." }
    }
    Write-Host 'Get-NetTCPConnection for 62314/5199 (observations only):'
    Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in @(62314, 5199) } |
        Select-Object LocalAddress, LocalPort, OwningProcess | Format-Table | Out-Host
}
function Owned-HttpListener {
    $records = @($state.hosts | Where-Object { $_.port -eq 62314 })
    if ($records.Count -ne 1 -or !(Matching-Process $records[0])) { return $false }
    $queues = & netsh http show servicestate view=requestq verbose=yes
    if ($LASTEXITCODE -ne 0) { throw 'Cannot verify HTTP.sys request queue ownership.' }
    foreach ($queue in (($queues -join "`n") -split '(?m)^Request queue name:')) {
        if ($queue -match '(?im)^\s*HTTP://LOCALHOST:62314/\s*$') {
            $ids = @([regex]::Matches($queue, '(?m)^\s*ID: (\d+), image:') |
                ForEach-Object { [int]$_.Groups[1].Value })
            return $ids.Count -gt 0 -and @($ids | Where-Object { $_ -ne $records[0].pid }).Count -eq 0
        }
    }
    return $false
}
function Assert-Ports {
    foreach ($listener in @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $ports })) {
        if ($listener.LocalPort -eq 62314 -and $listener.OwningProcess -eq 4 -and (Owned-HttpListener)) { continue }
        $record = @($state.hosts | Where-Object { $_.pid -eq $listener.OwningProcess -and $_.port -eq $listener.LocalPort })
        if ($record.Count -ne 1 -or !(Matching-Process $record[0])) {
            throw "Port $($listener.LocalPort) is held by unowned PID $($listener.OwningProcess). Stop it yourself or choose another session; it will not be killed."
        }
    }
}
function Stop-OwnedHosts {
    $records = @($state.hosts | Where-Object { $_.port -in $ports })
    Proof $records
    foreach ($record in $records) {
        $process = Matching-Process $record
        if ($process) {
            $process.Kill()
            if (!$process.WaitForExit(10000)) { throw "Owned PID $($record.pid) did not exit." }
        }
        $state.hosts = @($state.hosts | Where-Object { $_.pid -ne $record.pid })
        Save-State
    }
    Proof $records
}
function Run([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Exe failed with exit code $LASTEXITCODE" }
}
function Ensure-LegacyTools {
    if (!(Test-Path $Nuget)) {
        New-Item -ItemType Directory -Force (Split-Path $Nuget) | Out-Null
        Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/v6.11.1/nuget.exe' -OutFile $Nuget -UseBasicParsing
    }
    $refs = Join-Path $ReferenceAssembliesPath '.NETFramework\v4.6.1\mscorlib.dll'
    if (!(Test-Path $refs)) {
        New-Item -ItemType Directory -Force $ReferenceAssembliesPath | Out-Null
        $download = Join-Path $work 'reference-assemblies.zip'
        $extract = Join-Path $work 'reference-assemblies'
        Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net461/1.0.3/microsoft.netframework.referenceassemblies.net461.1.0.3.nupkg' -OutFile $download -UseBasicParsing
        Expand-Archive $download -DestinationPath $extract -Force
        Copy-Item "$extract\build\*" $ReferenceAssembliesPath -Recurse -Force
        if (!(Test-Path $refs)) { throw "Reference assemblies not found at $refs after extraction" }
    }
}
function Sql([string]$Text) {
    $connection = New-Object System.Data.SqlClient.SqlConnection 'Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30'
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 60
        $command.CommandText = $Text
        $value = $command.ExecuteScalar()
        if ($value -isnot [DBNull]) { return $value }
    } finally { $connection.Dispose() }
}
function Database-Exists {
    $null -ne (Sql "SELECT DB_ID(N'$($state.databaseName)')")
}
function Assert-DatabaseOwner {
    if ($state.databaseName -cne "eShopWcfParity_$(([guid]$state.ownerMarker).ToString('N'))") {
        throw 'Invalid run-owned database name in state. Default eShopDatabase is never used.'
    }
    if (Database-Exists) {
        $marker = Sql "SELECT CONVERT(nvarchar(36), value) FROM [$($state.databaseName)].sys.extended_properties WHERE class=0 AND name=N'eShopGrpcParityOwner'"
        if ($marker -cne $state.ownerMarker) {
            throw "Database $($state.databaseName) exists without the matching eShopGrpcParityOwner marker. Refusing to use, mark or drop it; investigate manually. eShopDatabase is untouched."
        }
    }
}
function Reset-OwnedDatabase {
    if (!$ResetDatabase) { return }
    Assert-DatabaseOwner
    if (Database-Exists) {
        $name = $state.databaseName
        Sql "USE [$name]; IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'eShopGrpcParityOwner' AND CONVERT(nvarchar(36), value)=N'$($state.ownerMarker)') THROW 50000, 'Database ownership changed; refusing drop', 1; USE master; ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];" | Out-Null
        Write-Host "Dropped owned database $name."
    }
}
function Prepare-Site {
    $siteMarker = Join-Path $site '.owner'
    if ((Test-Path $site) -and (!(Test-Path $siteMarker) -or
        (Get-Content $siteMarker -Raw).Trim() -cne $state.ownerMarker)) {
        throw "Scratch site $site exists without the matching owner marker. Refusing to overwrite it."
    }
    New-Item -ItemType Directory -Force $site | Out-Null
    [IO.File]::WriteAllText($siteMarker, $state.ownerMarker)
    Copy-Item "$wcf\bin" $site -Recurse -Force
    Copy-Item "$wcf\CatalogService.svc", "$wcf\Web.config" $site -Force
    [xml]$config = Get-Content "$site\Web.config" -Raw
    $entry = $config.configuration.connectionStrings.add | Where-Object { $_.name -eq 'EntityModel' }
    if (!$entry) { throw 'EntityModel connection string was not found in scratch Web.config.' }
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder.set_ConnectionString([string]$entry.connectionString)
    $builder.set_InitialCatalog($state.databaseName)
    $builder.set_DataSource('(localdb)\MSSQLLocalDB')
    $builder.set_IntegratedSecurity($true)
    $script:legacyConnectionString = $builder.get_ConnectionString()
    $entry.connectionString = $legacyConnectionString
    $config.Save("$site\Web.config")
}
function Launch-Owned([string]$Exe, [string]$Arguments, [string]$Directory, [int]$Port) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $Exe
    $info.Arguments = $Arguments
    $info.WorkingDirectory = $Directory
    $info.UseShellExecute = $false
    if ($Port -eq 62314) { $info.EnvironmentVariables['ConnectionString'] = $legacyConnectionString }
    if ($Port -eq 5199) {
        $info.EnvironmentVariables['UseMockData'] = 'true'
        $info.EnvironmentVariables['EnableGrpcReflection'] = 'true'
        $info.EnvironmentVariables['Kestrel__Endpoints__Grpc__Url'] = 'http://localhost:5199'
    }
    $process = [Diagnostics.Process]::Start($info)
    $record = [pscustomobject]@{
        pid = $process.Id; startTimeUtc = $process.StartTime.ToUniversalTime().ToString('o')
        startTimeUtcTicks = $process.StartTime.ToUniversalTime().Ticks.ToString()
        executablePath = [IO.Path]::GetFullPath($Exe); port = $Port
    }
    $state.hosts = @($state.hosts) + @($record)
    Save-State
}
function Start-OwnedHosts {
    Assert-Ports
    if (!(Test-Path $Grpcurl)) { throw "grpcurl not found: $Grpcurl" }
    $newDatabase = $false
    if (!$PortOnly) {
        Assert-DatabaseOwner
        $newDatabase = !(Database-Exists)
        Launch-Owned $IISExpress "/path:""$site"" /port:62314" $site 62314
    }
    $dotnet = (Get-Command dotnet -CommandType Application).Source
    $hostDir = Join-Path $grpcRoot 'src\eShop.Catalog.Grpc'
    $dll = Join-Path $hostDir 'bin\Release\net8.0\eShop.Catalog.Grpc.dll'
    Launch-Owned $dotnet """$dll""" $hostDir 5199
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
        if ($portReady -and $legacyReady) {
            Assert-Ports
            foreach ($record in @($state.hosts | Where-Object { $_.port -in $ports })) {
                if (!(Matching-Process $record)) { throw "Owned host PID $($record.pid) exited during startup." }
            }
            if ($newDatabase) {
                $body = '<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><GetCatalogBrands xmlns="http://tempuri.org/"/></s:Body></s:Envelope>'
                Invoke-WebRequest 'http://localhost:62314/CatalogService.svc' -Method Post -ContentType 'text/xml; charset=utf-8' -Headers @{ SOAPAction = '"http://tempuri.org/ICatalogService/GetCatalogBrands"' } -Body $body -UseBasicParsing -TimeoutSec $TimeoutSeconds | Out-Null
                Sql "USE [$($state.databaseName)]; IF EXISTS (SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'eShopGrpcParityOwner') THROW 50000, 'Ownership property already exists; refusing to replace it', 1; EXEC sys.sp_addextendedproperty @name=N'eShopGrpcParityOwner', @value=N'$($state.ownerMarker)';" | Out-Null
                Assert-DatabaseOwner
            }
            Write-Host "Requested owned services are ready. State: $stateFile"
            Proof
            return
        }
        Start-Sleep -Seconds 1
    } while ([datetime]::UtcNow -lt $deadline)
    throw "Services did not become ready within $TimeoutSeconds seconds. Owned PIDs remain recorded; use -Stop."
}
$lock = $null
try {
    New-Item -ItemType Directory -Force $work, $logs | Out-Null
    $lock = [IO.File]::Open((Join-Path $work 'state.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    if (Test-Path $stateFile) {
        $state = Get-Content $stateFile -Raw | ConvertFrom-Json
        if ($state.version -ne 1 -or $state.ownerMarker -cnotmatch '^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$' -or
            $null -eq $state.hosts) { throw 'Invalid ownership state; refusing to proceed.' }
    } else {
        $marker = [guid]::NewGuid()
        $state = [pscustomobject]@{ version = 1; ownerMarker = $marker.ToString(); databaseName = "eShopWcfParity_$($marker.ToString('N'))"; hosts = @() }
        Save-State
    }
    $site = Join-Path $site $state.ownerMarker
    if ($Stop) { Stop-OwnedHosts; return }
    Assert-Ports
    if (!$PortOnly) { Assert-DatabaseOwner }
    Stop-OwnedHosts
    Assert-Ports
    Push-Location $grpcRoot
    try { Run 'dotnet' @('build', 'eShopGrpc.sln', '-c', 'Release') } finally { Pop-Location }
    if (!$PortOnly) {
        Ensure-LegacyTools
        Run $Nuget @('restore', "$wcf\packages.config", '-PackagesDirectory', "$repo\eShopLegacyNTier\src\packages")
        Run $MSBuild @("$wcf\eShopWCFService.csproj", '-p:Configuration=Release', "-p:TargetFrameworkRootPath=$ReferenceAssembliesPath")
        Reset-OwnedDatabase
        Prepare-Site
    }
    if ($Start) { Start-OwnedHosts }
} finally {
    if ($lock) { $lock.Dispose() }
    $timer.Stop()
    Write-Host ("Total elapsed seconds: {0:N2}" -f $timer.Elapsed.TotalSeconds)
}
