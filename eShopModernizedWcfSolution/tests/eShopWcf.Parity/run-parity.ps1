[CmdletBinding()]
param(
    [ValidateSet('Legacy', 'Csv')][string]$Dataset = 'Legacy',
    [ValidateRange(1024, 65535)][int]$LegacyPort = 5100,
    [ValidateRange(1024, 65535)][int]$ModernPort = 5200,
    [string]$ArtifactsRoot = 'C:\Users\Administrator\evidence\parity',
    [ValidateRange(30, 900)][int]$TimeoutSeconds = 180,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$repo = (Resolve-Path (Join-Path $solutionRoot '..')).Path
$artifacts = [IO.Path]::GetFullPath((Join-Path $ArtifactsRoot $Dataset))
[void](New-Item -ItemType Directory -Force $artifacts)
$utf8 = New-Object System.Text.UTF8Encoding($false)
$owned = New-Object 'System.Collections.Generic.List[object]'
$commands = New-Object 'System.Collections.Generic.List[object]'
$dotnet = (Get-Command dotnet.exe).Source
$vs = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools'
$msbuild = Join-Path $vs 'MSBuild\Current\Bin\MSBuild.exe'
$iis = 'C:\Program Files\IIS Express\iisexpress.exe'
$legacyPath = Join-Path $repo 'eShopLegacyNTier\src\eShopWCFService'
$modernDll = Join-Path $solutionRoot 'src\eShopWcf.Catalog.Grpc\bin\Release\net8.0\eShopWcf.Catalog.Grpc.dll'
$harness = Join-Path $PSScriptRoot 'bin\Release\net8.0\eShopWcf.Parity.dll'
$baseline = 'eShopWcfParity_Baseline_' + $Dataset
$legacyDb = 'eShopWcfParity_' + $Dataset + '_Legacy'
$modernDb = 'eShopWcfParity_' + $Dataset + '_Modern'
$backup = Join-Path $artifacts ($baseline + '.bak')
$summary = [ordered]@{
    dataset = $Dataset; commit = (& git -C $repo rev-parse HEAD); workingTreeDirty = [bool](& git -C $repo status --porcelain)
    legacyPort = $LegacyPort; modernPort = $ModernPort; baseline = $baseline; legacyDatabase = $legacyDb; modernDatabase = $modernDb
    backup = $backup; backupSha256 = $null; initialTables = $null; commands = $commands; phases = @(); tables = $null; error = $null; exitCode = 2
}

function Write-Utf8([string]$Path, [string]$Value) { [IO.File]::WriteAllText($Path, $Value, $utf8) }
function Sql-Quote([string]$Value) { return $Value.Replace("'", "''") }
function Connection([string]$Name) { return "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=$Name;Integrated Security=True;Connect Timeout=15;TrustServerCertificate=True" }
function Start-Child([string]$Label, [string]$Executable, [string[]]$Arguments, [hashtable]$Environment = @{}) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $Executable
    $info.Arguments = ($Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
    $info.WorkingDirectory = $solutionRoot
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($key in $Environment.Keys) { $info.EnvironmentVariables[$key] = $Environment[$key] }
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    [void]$process.Start()
    [void]$process.Handle
    return [pscustomobject]@{ Label = $Label; Process = $process; Out = $process.StandardOutput.ReadToEndAsync(); Err = $process.StandardError.ReadToEndAsync(); Saved = $false; Command = ('"' + $Executable + '" ' + $info.Arguments) }
}
function Save-Child($Child) {
    if ($Child.Saved) { return }
    Write-Utf8 (Join-Path $artifacts ($Child.Label + '.stdout.log')) $Child.Out.GetAwaiter().GetResult()
    Write-Utf8 (Join-Path $artifacts ($Child.Label + '.stderr.log')) $Child.Err.GetAwaiter().GetResult()
    $Child.Saved = $true
}
function Stop-Child($Child) {
    if (-not $Child.Process.HasExited) { $Child.Process.Kill() }
    if (-not $Child.Process.WaitForExit(10000)) { throw "Owned process $($Child.Process.Id) failed to stop." }
    Save-Child $Child
}
function Run-Bounded([string]$Label, [string]$Executable, [string[]]$Arguments, [switch]$AllowFailure) {
    $child = Start-Child $Label $Executable $Arguments
    try {
        if (-not $child.Process.WaitForExit($TimeoutSeconds * 1000)) {
            Stop-Child $child
            throw "$Label exceeded $TimeoutSeconds seconds."
        }
        Save-Child $child
        $code = $child.Process.ExitCode
        $commands.Add([ordered]@{ name = $Label; command = $child.Command; exitCode = $code })
        Write-Host "$Label EXIT: $code"
        if ($code -ne 0 -and -not $AllowFailure) { throw "$Label failed: inspect $artifacts\$Label.stdout.log and .stderr.log" }
        return $code
    }
    finally { $child.Process.Dispose() }
}
function Sql-Table([string]$Sql, [string]$Database = 'master') {
    $connection = New-Object Data.SqlClient.SqlConnection((Connection $Database))
    $command = $connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 90
    try {
        $connection.Open()
        $reader = $command.ExecuteReader()
        try { $table = New-Object Data.DataTable; $table.Load($reader); return ,$table }
        finally { $reader.Dispose() }
    }
    finally { $command.Dispose(); $connection.Dispose() }
}
function Sql-Execute([string]$Sql) {
    $connection = New-Object Data.SqlClient.SqlConnection((Connection 'master'))
    $command = $connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 90
    try { $connection.Open(); [void]$command.ExecuteNonQuery() }
    finally { $command.Dispose(); $connection.Dispose() }
}
function Assert-FreePort([int]$Port) {
    if (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue) { throw "Port $Port is already occupied; no unrelated process will be stopped." }
}
function Wait-Port($Child, [int]$Port) {
    $limit = [DateTime]::UtcNow.AddSeconds(60)
    while ([DateTime]::UtcNow -lt $limit) {
        if ($Child.Process.HasExited) { Save-Child $Child; throw "$($Child.Label) exited during startup." }
        $tcp = New-Object Net.Sockets.TcpClient
        try { $pending = $tcp.ConnectAsync('localhost', $Port); if ($pending.Wait(500) -and $tcp.Connected) { return } }
        catch { }
        finally { $tcp.Dispose() }
        Start-Sleep -Milliseconds 250
    }
    throw "$($Child.Label) did not listen within 60 seconds."
}
function Start-Server([string]$Kind, [string]$Database, [string]$Label) {
    $environment = @{ ConnectionString = (Connection $Database); Seed__Dataset = $Dataset }
    if ($Kind -eq 'legacy') { $child = Start-Child $Label $iis @("/path:$legacyPath", "/port:$LegacyPort", '/systray:false') $environment; $port = $LegacyPort }
    else { $child = Start-Child $Label $dotnet @($modernDll, '--urls', "http://localhost:$ModernPort") $environment; $port = $ModernPort }
    $owned.Add($child)
    Wait-Port $child $port
    return $child
}
function Bootstrap-Legacy {
    $body = '<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><GetCatalogBrands xmlns="http://tempuri.org/"/></s:Body></s:Envelope>'
    $response = Invoke-WebRequest "http://localhost:$LegacyPort/CatalogService.svc" -Method Post -Headers @{ SOAPAction = '"http://tempuri.org/ICatalogService/GetCatalogBrands"' } -ContentType 'text/xml; charset=utf-8' -Body $body -UseBasicParsing -TimeoutSec 60
    Write-Utf8 (Join-Path $artifacts 'baseline-soap.xml') $response.Content
    [xml]$xml = $response.Content
    if ($xml.SelectSingleNode("//*[local-name()='Fault']")) { throw 'Legacy baseline initialization returned a SOAP fault.' }
}
function Restore-Copy([string]$Name) {
    $files = Sql-Table "RESTORE FILELISTONLY FROM DISK=N'$(Sql-Quote $backup)'"
    $data = @($files.Rows | Where-Object { $_.Type -eq 'D' })
    $log = @($files.Rows | Where-Object { $_.Type -eq 'L' })
    if ($data.Count -ne 1 -or $log.Count -ne 1) { throw 'Expected one data and one log file in baseline backup.' }
    $mdf = Sql-Quote (Join-Path $artifacts ($Name + '.mdf'))
    $ldf = Sql-Quote (Join-Path $artifacts ($Name + '.ldf'))
    Sql-Execute "IF DB_ID(N'$Name') IS NOT NULL ALTER DATABASE [$Name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$Name] FROM DISK=N'$(Sql-Quote $backup)' WITH REPLACE, CHECKSUM, MOVE N'$(Sql-Quote $data[0].LogicalName)' TO N'$mdf', MOVE N'$(Sql-Quote $log[0].LogicalName)' TO N'$ldf'; ALTER DATABASE [$Name] SET MULTI_USER;"
}
function Dump-Tables([string]$Database, [string]$Path) {
    $queries = [ordered]@{
        CatalogBrands = 'Id, Brand'; CatalogTypes = 'Id, Type';
        CatalogItems = 'Id, Description, Name, Price, Picturefilename, CatalogBrandId, CatalogTypeId';
        CatalogItemsStock = 'StockId, Date, CatalogItemId, AvailableStock'; DiscountItems = 'Id, Size, Start, [End]'
    }
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($table in $queries.Keys) {
        $key = 'Id'; if ($table -eq 'CatalogItemsStock') { $key = 'StockId' }
        $data = Sql-Table "SELECT $($queries[$table]) FROM dbo.[$table] ORDER BY [$key]" $Database
        $lines.Add('# ' + $table + "`t" + (($data.Columns | ForEach-Object { $_.ColumnName }) -join "`t"))
        foreach ($row in $data.Rows) {
            $values = foreach ($column in $data.Columns) {
                $value = $row[$column]
                if ($value -is [DBNull]) { '\N' }
                elseif ($value -is [string]) { ConvertTo-Json -InputObject $value -Compress }
                elseif ($value -is [DateTime]) { $value.ToString('yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture) }
                elseif ($value -is [double]) { $value.ToString('R', [Globalization.CultureInfo]::InvariantCulture) }
                elseif ($value -is [decimal]) { $value.ToString('G29', [Globalization.CultureInfo]::InvariantCulture) }
                else { $value.ToString([Globalization.CultureInfo]::InvariantCulture) }
            }
            $lines.Add($table + "`t" + ($values -join "`t"))
        }
    }
    Write-Utf8 $Path (($lines -join "`n") + "`n")
}

try {
    if ($LegacyPort -eq $ModernPort) { throw 'Legacy and modern ports must differ.' }
    Assert-FreePort $LegacyPort
    Assert-FreePort $ModernPort
    if (-not $SkipBuild) {
        [void](Run-Bounded 'legacy-build' $msbuild @((Join-Path $legacyPath 'eShopWCFService.csproj'), '/t:Build', '/p:Configuration=Release', '/m:1', '/nr:false', "/bl:$(Join-Path $artifacts 'legacy-build.binlog')"))
        [void](Run-Bounded 'modern-build' $dotnet @('build', 'eShopModernizedWcfSolution.sln', '-c', 'Release'))
    }
    if (-not (Test-Path $harness) -or -not (Test-Path $modernDll)) { throw 'Release binaries missing. Run without -SkipBuild.' }
    if (-not (Test-Path $backup)) {
        if ($Dataset -eq 'Legacy') { $seed = Start-Server 'legacy' $baseline 'baseline-legacy'; Bootstrap-Legacy }
        else { $seed = Start-Server 'modern' $baseline 'baseline-modern' }
        Stop-Child $seed
        Sql-Execute "BACKUP DATABASE [$baseline] TO DISK=N'$(Sql-Quote $backup)' WITH COPY_ONLY, INIT, CHECKSUM"
    }
    Sql-Execute "RESTORE VERIFYONLY FROM DISK=N'$(Sql-Quote $backup)' WITH CHECKSUM"
    $summary.backupSha256 = (Get-FileHash $backup -Algorithm SHA256).Hash
    Restore-Copy $legacyDb
    Restore-Copy $modernDb
    $initialLeft = Join-Path $artifacts 'initial-legacy.tsv'
    $initialRight = Join-Path $artifacts 'initial-modern.tsv'
    Dump-Tables $legacyDb $initialLeft
    Dump-Tables $modernDb $initialRight
    $summary.initialTables = [ordered]@{ legacySha256 = (Get-FileHash $initialLeft).Hash; modernSha256 = (Get-FileHash $initialRight).Hash }
    if ($summary.initialTables.legacySha256 -ne $summary.initialTables.modernSha256) { throw 'Restored copies do not have identical normalized table contents.' }
    foreach ($phase in @('full', 'reads-after-restart')) {
        $left = Start-Server 'legacy' $legacyDb ($phase + '-legacy')
        $right = Start-Server 'modern' $modernDb ($phase + '-modern')
        $code = Run-Bounded $phase $dotnet @($harness, '--legacy-url', "http://localhost:$LegacyPort/CatalogService.svc", '--modern-url', "http://localhost:$ModernPort", '--output', $artifacts, '--phase', $phase) -AllowFailure
        Stop-Child $right
        Stop-Child $left
        $prefix = ''; if ($phase -eq 'reads-after-restart') { $prefix = 'restart-' }
        $result = Get-Content (Join-Path $artifacts ($prefix + 'results.json')) -Raw | ConvertFrom-Json
        $summary.phases += [ordered]@{ phase = $phase; exitCode = $code; counts = $result.counts; normalizedSha256 = $result.normalizedSha256; error = $result.error }
    }
    $leftDump = Join-Path $artifacts "final-$Dataset-legacy.tsv"
    $rightDump = Join-Path $artifacts "final-$Dataset-modern.tsv"
    Dump-Tables $legacyDb $leftDump
    Dump-Tables $modernDb $rightDump
    $leftHash = (Get-FileHash $leftDump -Algorithm SHA256).Hash
    $rightHash = (Get-FileHash $rightDump -Algorithm SHA256).Hash
    $difference = & git diff --no-index --no-ext-diff --text -- $leftDump $rightDump
    $diffCode = $LASTEXITCODE
    if ($diffCode -gt 1) { throw "Table diff failed with exit $diffCode" }
    Write-Utf8 (Join-Path $artifacts 'final-tables.diff') ($difference -join "`n")
    $summary.tables = [ordered]@{ legacySha256 = $leftHash; modernSha256 = $rightHash; equal = ($leftHash -eq $rightHash); diffExitCode = $diffCode }
    $summary.exitCode = [int](($summary.phases | Measure-Object -Property exitCode -Maximum).Maximum)
}
catch { $summary.error = $_.Exception.ToString(); $summary.exitCode = 2; Write-Warning $summary.error }
finally {
    foreach ($child in $owned) {
        try { Stop-Child $child }
        catch { $summary.error = [string]$summary.error + "`nCleanup: " + $_.Exception.Message; $summary.exitCode = 2 }
        finally { $child.Process.Dispose() }
    }
    Write-Utf8 (Join-Path $artifacts 'summary.json') ($summary | ConvertTo-Json -Depth 20)
    $text = "# $Dataset parity summary`n`nCommit: $($summary.commit); dirty working tree: $($summary.workingTreeDirty)`n`nPorts: $LegacyPort / $ModernPort`n`nDatabases: $baseline / $legacyDb / $modernDb`n`nBackup SHA256: $($summary.backupSha256)`n`n"
    foreach ($phase in $summary.phases) { $text += "## $($phase.phase)`nExit: $($phase.exitCode); normalized SHA256: $($phase.normalizedSha256)`n`n$($phase.counts | ConvertTo-Json -Compress)`n`n" }
    $text += "Tables: $($summary.tables | ConvertTo-Json -Compress)`n`nError: $($summary.error)`n`nExit: $($summary.exitCode)`n"
    Write-Utf8 (Join-Path $artifacts 'summary.md') $text
    Write-Output $text
}
exit $summary.exitCode
