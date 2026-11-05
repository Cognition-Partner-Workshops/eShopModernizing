param(
    [string]$Evidence = 'C:\Users\Administrator\evidence\parity',
    [ValidateSet('Prepare','Run','Restart','Stop')][string]$Action = 'Run',
    [string]$Run = 'run1'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$repo = Split-Path $root
$legacy = Join-Path $repo 'eShopLegacyMVCSolution\src\eShopLegacyMVC'
$site = Join-Path $Evidence 'legacy-site'
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$iis = 'C:\Program Files\IIS Express\iisexpress.exe'
$sourceDb = 'Microsoft.eShopOnContainers.Services.CatalogDb'
$backup = Join-Path $Evidence 'baseline.bak'
New-Item -ItemType Directory -Force $Evidence | Out-Null
function Sql([string]$Database, [string]$Text) {
    $c = New-Object System.Data.SqlClient.SqlConnection "Server=(localdb)\MSSQLLocalDB;Database=$Database;Integrated Security=True;MultipleActiveResultSets=True"
    $c.Open()
    try {
        $q = $c.CreateCommand(); $q.CommandTimeout = 120; $q.CommandText = $Text
        $table = New-Object System.Data.DataTable
        $reader = $q.ExecuteReader(); $table.Load($reader)
        return ,$table
    } finally { $c.Dispose() }
}
function Drop([string]$Name) {
    if ($Name -notmatch '^eShopParity_') { throw "Refusing to reset $Name" }
    Sql master "IF DB_ID(N'$Name') IS NOT NULL BEGIN ALTER DATABASE [$Name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Name]; END" | Out-Null
}
function Restore([string]$Name) {
    Drop $Name
    $files = Sql master "RESTORE FILELISTONLY FROM DISK=N'$backup'"
    $moves = @(); $i = 0
    foreach ($f in $files.Rows) {
        $extension = if ($f.Type -eq 'L') { 'ldf' } else { 'mdf' }
        $moves += "MOVE N'$($f.LogicalName)' TO N'$Evidence\$Name-$i.$extension'"; $i++
    }
    Sql master "RESTORE DATABASE [$Name] FROM DISK=N'$backup' WITH $($moves -join ', '), RECOVERY" | Out-Null
}
function LegacyConfig([string]$Database) {
    [xml]$config = Get-Content "$site\Web.config"
    ($config.configuration.appSettings.add | Where-Object key -eq UseMockData).value = 'false'
    ($config.configuration.appSettings.add | Where-Object key -eq UseCustomizationData).value = 'true'
    $config.configuration.connectionStrings.add.connectionString = "Server=(localdb)\MSSQLLocalDB;Database=$Database;Integrated Security=True;MultipleActiveResultSets=True"
    $config.Save("$site\Web.config")
}
function StartLegacy {
    $p = Start-Process $iis -ArgumentList "/path:$site /port:5101 /systray:false" -PassThru -RedirectStandardOutput "$Evidence\$Run-legacy-server.log" -RedirectStandardError "$Evidence\$Run-legacy-server.err.log"
    $p.Id | Set-Content "$Evidence\legacy.pid"
}
function StartModern([string]$Database, [int]$Port = 5102) {
    $env:UseMockData = 'false'; $env:UseCustomizationData = 'true'
    $env:ConnectionStrings__CatalogDBContext = "Server=(localdb)\MSSQLLocalDB;Database=$Database;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $p = Start-Process $dotnet -WorkingDirectory $root -ArgumentList "run --project src\eShopModernizedMVC --no-build --no-launch-profile --urls http://localhost:$Port" -PassThru -RedirectStandardOutput "$Evidence\$Run-modern-$Port.log" -RedirectStandardError "$Evidence\$Run-modern-$Port.err.log"
    $p.Id | Set-Content "$Evidence\modern-$Port.pid"
}
function StopServers {
    Get-ChildItem $Evidence -Filter *.pid | ForEach-Object {
        $processId = [int](Get-Content $_.FullName)
        if (Get-Process -Id $processId -ErrorAction SilentlyContinue) { taskkill /PID $processId /T /F | Out-Null }
        Remove-Item $_.FullName
    }
}
function Ready([int]$Port) {
    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    do {
        try { $r = Invoke-WebRequest "http://localhost:$Port/" -UseBasicParsing -TimeoutSec 15; if ($r.StatusCode -eq 200) { return } }
        catch { $last = $_.Exception.Message }
        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Server $Port not ready: $last"
}
function Dump([string]$Database, [string]$Label) {
    $queries = [ordered]@{
        columns = "SELECT t.name AS [table], c.name AS [column], ty.name AS type,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id JOIN sys.types ty ON c.user_type_id=ty.user_type_id ORDER BY t.name,c.column_id"
        foreignKeys = "SELECT OBJECT_NAME(f.parent_object_id) AS [table],COL_NAME(fc.parent_object_id,fc.parent_column_id) AS [column],OBJECT_NAME(f.referenced_object_id) AS referencedTable,COL_NAME(fc.referenced_object_id,fc.referenced_column_id) AS referencedColumn,f.delete_referential_action_desc AS deleteAction FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON f.object_id=fc.constraint_object_id ORDER BY [table],[column]"
        sequences = "SELECT name,TYPE_NAME(user_type_id) AS type,CONVERT(bigint,start_value) AS start,CONVERT(bigint,increment) AS increment,CONVERT(bigint,current_value) AS currentValue,is_cached FROM sys.sequences ORDER BY name"
        Catalog = 'SELECT * FROM Catalog ORDER BY Id'
        CatalogBrand = 'SELECT * FROM CatalogBrand ORDER BY Id'
        CatalogType = 'SELECT * FROM CatalogType ORDER BY Id'
    }
    foreach ($key in $queries.Keys) {
        $t = Sql $Database $queries[$key]
        $rows = @($t.Rows | ForEach-Object { $o=[ordered]@{}; foreach($c in $t.Columns){$v=$_[$c.ColumnName]; $o[$c.ColumnName]=if($v -is [DBNull]){$null}else{$v}}; $o })
        ConvertTo-Json -InputObject $rows -Depth 8 | Set-Content "$Evidence\$Label-$key.json" -Encoding UTF8
    }
    $data = @('Catalog','CatalogBrand','CatalogType') | ForEach-Object { Get-Content "$Evidence\$Label-$_.json" -Raw }
    ($data -join "`n") | Set-Content "$Evidence\$Label-data.json" -Encoding UTF8
    Get-FileHash "$Evidence\$Label-data.json" -Algorithm SHA256 | Format-List | Out-File "$Evidence\$Label-data.sha256"
}
if ($Action -eq 'Stop') { StopServers; return }
if ($Action -eq 'Restart') {
    StopServers; LegacyConfig eShopParity_Legacy; StartLegacy; StartModern eShopParity_Modern; Ready 5101; Ready 5102; return
}
StopServers
if ($Action -eq 'Prepare') {
    if (Test-Path $backup) { throw 'Baseline already exists; preserve it or explicitly remove the evidence directory to reseed.' }
    if ((Sql master "SELECT name FROM sys.databases WHERE name=N'$sourceDb'").Rows.Count) { throw 'Legacy seed database already exists; refusing to overwrite it.' }
    New-Item -ItemType Directory -Force $site | Out-Null
    Copy-Item "$legacy\*" $site -Recurse -Force
    Get-ChildItem "$legacy\Setup" -File | Where-Object Extension -in '.csv','.zip' | Get-FileHash -Algorithm SHA256 | Export-Csv "$Evidence\canonical-hashes.csv" -NoTypeInformation
    LegacyConfig $sourceDb
    StartLegacy
    try { Ready 5101 } finally { StopServers }
    Sql master "BACKUP DATABASE [$sourceDb] TO DISK=N'$backup' WITH INIT, COPY_ONLY" | Out-Null
    Restore eShopParity_Baseline
    Dump eShopParity_Baseline baseline
    Drop eShopParity_ModernFresh
    StartModern eShopParity_ModernFresh 5103
    try { Ready 5103; Dump eShopParity_ModernFresh fresh } finally { StopServers }
    return
}
Restore eShopParity_Legacy; Restore eShopParity_Modern
LegacyConfig eShopParity_Legacy
StartLegacy; StartModern eShopParity_Modern
Ready 5101; Ready 5102
Dump eShopParity_Legacy "$Run-before-legacy"; Dump eShopParity_Modern "$Run-before-modern"
$env:LEGACY_BASE_URL='http://localhost:5101'; $env:MODERN_BASE_URL='http://localhost:5102'
$env:PARITY_EVIDENCE="$Evidence\$Run"; $env:PARITY_PHASE='exercise'
New-Item -ItemType Directory -Force $env:PARITY_EVIDENCE | Out-Null
Push-Location $root
try {
    & $dotnet test tests\eShopModernizedMVC.ParityTests --no-build --logger trx --results-directory "$Evidence\$Run\trx" *> "$Evidence\$Run\tests.log"
    $exerciseExit = $LASTEXITCODE
    & $PSCommandPath -Evidence $Evidence -Action Restart -Run "$Run-restart"
    $env:PARITY_PHASE='persistence'
    & $dotnet test tests\eShopModernizedMVC.ParityTests --no-build --logger trx --results-directory "$Evidence\$Run\restart-trx" *> "$Evidence\$Run\restart-tests.log"
    $restartExit = $LASTEXITCODE
    Dump eShopParity_Legacy "$Run-after-legacy"; Dump eShopParity_Modern "$Run-after-modern"
    "exercise exit=$exerciseExit; persistence exit=$restartExit" | Tee-Object "$Evidence\$Run\exit-codes.txt"
    if ($exerciseExit -or $restartExit) { throw 'Parity test failures; inspect evidence.' }
} finally { Pop-Location; StopServers }
