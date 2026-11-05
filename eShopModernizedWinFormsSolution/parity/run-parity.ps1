[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$WcfSite,
    [Parameter(Mandatory = $true)][string]$GrpcDll,
    [Parameter(Mandatory = $true)][string]$GrpcWorkingDirectory,
    [Parameter(Mandatory = $true)][string]$BackupDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [Parameter(Mandatory = $true)][string]$SqlCmd,
    [Parameter(Mandatory = $true)][string]$IisExpress,
    [Parameter(Mandatory = $true)][string]$Dotnet,
    [Parameter(Mandatory = $true)][string]$Grpcurl,
    [string]$SqlInstance = '(localdb)\MSSQLLocalDB',
    [string]$LegacyDatabase = 'eShopWinForms_Legacy',
    [string]$ModernDatabase = 'eShopWinForms_Grpc',
    [int]$LegacyPort = 62314,
    [int]$ModernPort = 5200,
    [string]$LegacyHarness = "$PSScriptRoot\Harness.Legacy\bin\Release\Harness.Legacy.exe",
    [string]$ModernHarness = "$PSScriptRoot\Harness.Modern\bin\Release\net8.0-windows\Harness.Modern.exe",
    [string]$LegacyImages = "$PSScriptRoot\..\..\eShopLegacyNTier\src\eShopWinForms\Assets\Images\Catalog",
    [int]$HarnessTimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$exitCode = 1
$mayReset = $false
$runsFailed = $false
$serverGeneration = 0

function Sql-Literal([string]$Value) { return "'" + $Value.Replace("'", "''") + "'" }

function Stop-Servers {
    $owned = @(Get-CimInstance Win32_Process | Where-Object {
        ($_.Name -eq 'iisexpress.exe' -and $_.CommandLine -and $_.CommandLine.Contains($WcfSite)) -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -and $_.CommandLine.Contains($GrpcDll))
    })
    $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $LegacyPort,$ModernPort -ErrorAction SilentlyContinue)
    foreach ($owner in @($listeners | Select-Object -ExpandProperty OwningProcess | Sort-Object -Unique)) {
        # IIS Express registers its listener with HTTP.sys (System/PID 4).
        if ($owner -eq 4 -and @($owned | Where-Object Name -eq 'iisexpress.exe').Count -gt 0) { continue }
        if (@($owned | Where-Object ProcessId -eq $owner).Count -eq 0) {
            throw "Refusing to stop unrelated port owner $owner."
        }
    }
    foreach ($process in $owned) {
        Stop-Process -Id $process.ProcessId -Force
        $stopped = Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue
        if ($null -ne $stopped -and -not $stopped.WaitForExit(10000)) { throw "Server $($process.ProcessId) did not stop." }
    }
}

function Reset-Databases {
    $sql = ''
    foreach ($database in @($LegacyDatabase, $ModernDatabase)) {
        $backup = Join-Path $BackupDirectory ($database + '_parity_v1.bak')
        $sql += "ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; "
        $sql += "RESTORE DATABASE [$database] FROM DISK=$(Sql-Literal $backup) WITH REPLACE, CHECKSUM; "
        $sql += "ALTER DATABASE [$database] SET MULTI_USER; "
    }
    & $SqlCmd -S $SqlInstance -d master -b -l 30 -t 90 -Q $sql
    if ($LASTEXITCODE -ne 0) { throw "Database restore failed: $LASTEXITCODE" }
}

function Start-Servers {
    $script:serverGeneration++
    $prefix = Join-Path $OutputDirectory "servers-$serverGeneration"
    $iis = Start-Process $IisExpress -ArgumentList "/path:`"$WcfSite`" /port:$LegacyPort /systray:false" -PassThru `
        -RedirectStandardOutput "$prefix-iis.stdout.log" -RedirectStandardError "$prefix-iis.stderr.log"
    $env:Catalog__UseMockData = 'false'
    $env:UseMockData = 'false'
    $env:ConnectionStrings__Catalog = "Server=$SqlInstance;Database=$ModernDatabase;Integrated Security=True;TrustServerCertificate=True"
    $env:ASPNETCORE_URLS = "http://localhost:$ModernPort"
    $env:EShopLogging__FilePath = "$prefix-grpc.structured.log"
    $grpc = Start-Process $Dotnet -ArgumentList "`"$GrpcDll`"" -WorkingDirectory $GrpcWorkingDirectory -PassThru `
        -RedirectStandardOutput "$prefix-grpc.stdout.log" -RedirectStandardError "$prefix-grpc.stderr.log"
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    $soapReady = $false
    $grpcReady = $false
    $body = '<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><GetCatalogBrands xmlns="http://tempuri.org/"/></s:Body></s:Envelope>'
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($iis.HasExited -or $grpc.HasExited) { throw "A server exited; inspect $prefix*.log." }
        if (-not $soapReady) {
            try {
                $response = Invoke-WebRequest "http://localhost:$LegacyPort/CatalogService.svc" -Method Post -ContentType 'text/xml; charset=utf-8' `
                    -Headers @{ SOAPAction = '"http://tempuri.org/ICatalogService/GetCatalogBrands"' } -Body $body -UseBasicParsing -TimeoutSec 3
                $soapReady = $response.StatusCode -eq 200 -and $response.Content.Contains('GetCatalogBrandsResult')
            } catch { $soapReady = $false }
        }
        if (-not $grpcReady) {
            try {
                $services = & $Grpcurl -plaintext -max-time 3 "localhost:$ModernPort" list 2> "$prefix-readiness.stderr.log"
                $grpcReady = $LASTEXITCODE -eq 0 -and ($services -contains 'eshop.catalog.v1.Catalog')
            } catch { $grpcReady = $false }
        }
        if ($soapReady -and $grpcReady) {
            Write-Host "IIS_PID=$($iis.Id) GRPC_PID=$($grpc.Id) SOAP_READY=True GRPC_READY=True"
            return
        }
        Start-Sleep -Milliseconds 250
    }
    throw 'Server readiness timed out.'
}

function Invoke-Harness([string]$Kind, [bool]$Persisted) {
    $name = $Kind.ToLowerInvariant()
    if ($Persisted) { $name += '-persisted' }
    $output = Join-Path $OutputDirectory "$name.json"
    if (Test-Path $output) { Remove-Item $output }
    $arguments = "--output `"$output`""
    if ($Kind -eq 'Legacy') {
        $exe = $LegacyHarness
        $arguments += " --address http://localhost:$LegacyPort/CatalogService.svc --legacy-images `"$LegacyImages`""
    } else {
        $exe = $ModernHarness
        $arguments += " --address http://localhost:$ModernPort"
    }
    if ($Persisted) { $arguments += ' --verify-persisted' }
    $process = Start-Process $exe -ArgumentList $arguments -PassThru -RedirectStandardOutput "$output.stdout.log" -RedirectStandardError "$output.stderr.log"
    $handle = $process.Handle
    if (-not $process.WaitForExit($HarnessTimeoutSeconds * 1000)) {
        Stop-Process -Id $process.Id -Force
        throw "$name harness timed out (including modal dialog watchdog)."
    }
    $process.Refresh()
    Write-Host "$name HARNESS_EXIT=$($process.ExitCode)"
    if (-not (Test-Path $output)) { throw "$name did not produce JSON; inspect stderr." }
    if ($process.ExitCode -ne 0) { $script:runsFailed = $true }
}

function Canonical($Value) {
    if ($null -eq $Value) { return 'null' }
    return ConvertTo-Json -InputObject $Value -Depth 100 -Compress
}

function Compare-Node($Left, $Right, [string]$Path, $Differences) {
    if ((Canonical $Left) -ceq (Canonical $Right)) { return }
    if ($null -ne $Left -and $null -ne $Right -and $Left -is [pscustomobject] -and $Right -is [pscustomobject]) {
        $names = @(@($Left.PSObject.Properties.Name) + @($Right.PSObject.Properties.Name) | Sort-Object -Unique)
        foreach ($name in $names) {
            $l = $Left.PSObject.Properties[$name]
            $r = $Right.PSObject.Properties[$name]
            $leftValue = $null
            $rightValue = $null
            if ($l) { $leftValue = $l.Value }
            if ($r) { $rightValue = $r.Value }
            Compare-Node $leftValue $rightValue "$Path.$name" $Differences
        }
    } elseif ($Left -is [array] -and $Right -is [array] -and $Left.Count -eq $Right.Count) {
        for ($i = 0; $i -lt $Left.Count; $i++) { Compare-Node $Left[$i] $Right[$i] "$Path[$i]" $Differences }
    } else {
        $Differences.Add([pscustomobject]@{ Path = $Path; Legacy = (Canonical $Left); Modern = (Canonical $Right) })
    }
}

function Compare-Results([string]$Suffix) {
    $legacy = Get-Content (Join-Path $OutputDirectory "legacy$Suffix.json") -Raw | ConvertFrom-Json
    $modern = Get-Content (Join-Path $OutputDirectory "modern$Suffix.json") -Raw | ConvertFrom-Json
    $differences = New-Object 'System.Collections.Generic.List[object]'
    Compare-Node $legacy $modern '$' $differences
    $table = foreach ($scenario in $legacy.PSObject.Properties.Name) {
        $left = Canonical $legacy.$scenario
        $right = Canonical $modern.$scenario
        [pscustomobject]@{ Scenario = $scenario; Legacy = $left; Modern = $right; Match = $left -ceq $right }
    }
    $table | Export-Csv (Join-Path $OutputDirectory "comparison$Suffix.csv") -NoTypeInformation -Encoding UTF8
    ConvertTo-Json -InputObject @($differences.ToArray()) -Depth 100 | Set-Content (Join-Path $OutputDirectory "differences$Suffix.json") -Encoding UTF8
    $table | Select-Object Scenario,Match | Format-Table -AutoSize | Out-Host
    Write-Host "DIFFERENCES$Suffix=$($differences.Count)"
    return $differences.Count
}

try {
    if ($LegacyDatabase -eq $ModernDatabase) { throw 'Legacy and modern databases must be distinct.' }
    if ($LegacyPort -lt 1 -or $LegacyPort -gt 65535 -or $ModernPort -lt 1 -or $ModernPort -gt 65535 -or $LegacyPort -eq $ModernPort) {
        throw 'Choose distinct valid TCP ports.'
    }
    if ($HarnessTimeoutSeconds -lt 1) { throw 'HarnessTimeoutSeconds must be positive.' }
    foreach ($database in @($LegacyDatabase, $ModernDatabase)) {
        if ($database -notmatch '^[A-Za-z0-9_]+$') { throw "Unsafe database identifier: $database" }
        if (-not (Test-Path (Join-Path $BackupDirectory ($database + '_parity_v1.bak')))) { throw "Missing parity backup for $database" }
    }
    foreach ($path in @($WcfSite, $GrpcDll, $GrpcWorkingDirectory, $LegacyHarness, $ModernHarness, $LegacyImages, $SqlCmd, $IisExpress, $Dotnet, $Grpcurl)) {
        if (-not (Test-Path $path)) { throw "Missing prerequisite: $path" }
    }
    if (-not (Test-Path $OutputDirectory)) { New-Item -ItemType Directory -Path $OutputDirectory | Out-Null }
    $OutputDirectory = (Resolve-Path $OutputDirectory).Path
    $WcfSite = (Resolve-Path $WcfSite).Path
    $GrpcDll = (Resolve-Path $GrpcDll).Path
    $web = [xml](Get-Content (Join-Path $WcfSite 'Web.config') -Raw)
    $connection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $connection.set_ConnectionString(($web.configuration.connectionStrings.add | Where-Object name -eq 'EntityModel').connectionString)
    if ($connection.get_InitialCatalog() -ne $LegacyDatabase -or $connection.get_DataSource() -ne $SqlInstance) {
        throw 'WCF site connection string does not match the requested legacy database/SQL instance.'
    }
    $mayReset = $true
    foreach ($kind in @('Legacy', 'Modern')) {
        Stop-Servers
        Reset-Databases
        Start-Servers
        Invoke-Harness $kind $false
        Stop-Servers
        Start-Servers
        Invoke-Harness $kind $true
    }
    $different = (Compare-Results '') + (Compare-Results '-persisted')
    Get-FileHash (Join-Path $OutputDirectory 'legacy.json'),(Join-Path $OutputDirectory 'modern.json') -Algorithm SHA256 | Format-Table -AutoSize | Out-Host
    $exitCode = if ($runsFailed) { 1 } elseif ($different -gt 0) { 2 } else { 0 }
} catch {
    Write-Error $_ -ErrorAction Continue
    $exitCode = 1
} finally {
    if ($mayReset) {
        try {
            Stop-Servers
            Reset-Databases
            Start-Servers
            Write-Host 'FINAL_STATE=Both servers running; both databases restored to parity-v1.'
        } catch {
            Write-Error "Final cleanup failed: $_" -ErrorAction Continue
            $exitCode = 1
        }
    }
}
Write-Host "RUN_PARITY_EXIT=$exitCode"
exit $exitCode
