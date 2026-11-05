param(
    [string]$Grpcurl = 'C:\scratch\grpcurl\grpcurl.exe',
    [string]$Address = 'localhost:5199',
    [string]$Out = "$PSScriptRoot\out\port.json"
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path $Grpcurl)) { throw "grpcurl not found: $Grpcurl" }

function Item($i) {
    if ($null -eq $i) { return $null }
    [ordered]@{
        id = [int]$i.id; name = $i.name; description = $i.description
        price = [decimal]$i.price.units + [decimal]$i.price.nanos / [decimal]1000000000
        pictureFileName = $i.picture_filename
        catalogBrandId = [int]$i.catalog_brand_id; catalogTypeId = [int]$i.catalog_type_id
        brand = if ($null -eq $i.catalog_brand) { $null } else { $i.catalog_brand.brand }
        type = if ($null -eq $i.catalog_type) { $null } else { $i.catalog_type.type }
    }
}
function Items($r) { ,@($r.items | Where-Object { $null -ne $_ } | ForEach-Object { Item $_ }) }
function Call($name, $op, $body, [scriptblock]$map) {
    $json = ConvertTo-Json -InputObject $body -Depth 10 -Compress
    $process = New-Object Diagnostics.Process
    try {
        $process.StartInfo = New-Object Diagnostics.ProcessStartInfo
        $process.StartInfo.FileName = $Grpcurl
        $process.StartInfo.Arguments = "-plaintext -d @ $Address eshop.catalog.v1.Catalog/$op"
        $process.StartInfo.UseShellExecute = $false
        $process.StartInfo.RedirectStandardInput = $true
        $process.StartInfo.RedirectStandardOutput = $true
        $process.StartInfo.RedirectStandardError = $true
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.StandardInput.Write($json)
        $process.StandardInput.Close()
        $process.WaitForExit()
        $code = $process.ExitCode
        $output = $stdout.Result
        $errorText = $stderr.Result
        if ($code -ne 0) {
            $status = if ($errorText -match 'Code:\s*(\w+)') { $Matches[1] } else { 'TransportError' }
            $detail = if ($errorText -match '(?s)Message:\s*(.*)') { $Matches[1].Trim() } else { $errorText.Trim() }
            $res[$name] = [ordered]@{ status = $status; detail = $detail }
        } else {
            $r = ($output -join "`n") | ConvertFrom-Json
            $res[$name] = [ordered]@{ status = 'OK'; value = (& $map $r) }
        }
    } finally { $process.Dispose() }
}
$res = [ordered]@{}
Call 'GetCatalogBrands' 'GetCatalogBrands' @{} { param($r) ,@($r.brands | % { [ordered]@{ id = [int]$_.id; brand = $_.brand } }) }
Call 'GetCatalogTypes' 'GetCatalogTypes' @{} { param($r) ,@($r.types | % { [ordered]@{ id = [int]$_.id; type = $_.type } }) }
foreach ($f in @(@(0,0), @(2,0), @(0,1), @(2,1), @(99,0))) {
    Call "GetCatalogItems(brand=$($f[0]),type=$($f[1]))" 'GetCatalogItems' @{ brandIdFilter = $f[0]; typeIdFilter = $f[1] } { param($r) Items $r }
}
Call 'FindCatalogItem(1)' 'FindCatalogItem' @{ id = 1 } { param($r) Item $r.item }
Call 'FindCatalogItem(999)' 'FindCatalogItem' @{ id = 999 } { param($r) Item $r.item }
Call 'GetAvailableStock(2017-09-20,1)' 'GetAvailableStock' @{ date = '2017-09-20T00:00:00Z'; catalogItemId = 1 } { param($r) [int]$r.available_stock }
Call 'GetAvailableStock(2017-01-01,1)' 'GetAvailableStock' @{ date = '2017-01-01T00:00:00Z'; catalogItemId = 1 } { param($r) [int]$r.available_stock }
$disc = {
    param($r)
    $d = $r.discount
    if ($null -eq $d) { $null } else {
        [ordered]@{ id = [int]$d.id; size = [math]::Round([double]$d.size, 6)
            start = ([datetime]$d.start).ToString('yyyy-MM-dd'); end = ([datetime]$d.end).ToString('yyyy-MM-dd') }
    }
}
Call 'GetDiscount(2017-09-19)' 'GetDiscount' @{ day = '2017-09-19T00:00:00Z' } $disc
Call 'GetDiscount(2018-01-01)' 'GetDiscount' @{ day = '2018-01-01T00:00:00Z' } $disc
Call 'CreateAvailableStock(overwrite 2,2017-09-20,50)' 'CreateAvailableStock' @{ catalogItemsStock = @{ stockId = 0; catalogItemId = 2; date = '2017-09-20T00:00:00Z'; availableStock = 50 } } { param($r) 'void' }
Call 'GetAvailableStock(2017-09-20,2) after overwrite' 'GetAvailableStock' @{ date = '2017-09-20T00:00:00Z'; catalogItemId = 2 } { param($r) [int]$r.available_stock }
Call 'CreateAvailableStock(new 3,2017-10-01,7)' 'CreateAvailableStock' @{ catalogItemsStock = @{ stockId = 0; catalogItemId = 3; date = '2017-10-01T00:00:00Z'; availableStock = 7 } } { param($r) 'void' }
Call 'GetAvailableStock(2017-10-01,3) after create' 'GetAvailableStock' @{ date = '2017-10-01T00:00:00Z'; catalogItemId = 3 } { param($r) [int]$r.available_stock }
$new = [ordered]@{ id = 0; name = 'Parity Item'; description = 'Parity Item'; price = @{ units = '9'; nanos = 990000000 }; pictureFilename = 'parity.png'; catalogBrandId = 1; catalogTypeId = 1 }
Call 'CreateCatalogItem(Parity Item)' 'CreateCatalogItem' @{ catalogItem = $new } { param($r) 'void' }
Call 'FindCatalogItem(13) after create' 'FindCatalogItem' @{ id = 13 } { param($r) Item $r.item }
$new.id = 13; $new.price = @{ units = '19'; nanos = 990000000 }; $new.name = 'Parity Item Updated'
Call 'UpdateCatalogItem(13)' 'UpdateCatalogItem' @{ catalogItem = $new } { param($r) 'void' }
Call 'FindCatalogItem(13) after update' 'FindCatalogItem' @{ id = 13 } { param($r) Item $r.item }
Call 'RemoveCatalogItem(13)' 'RemoveCatalogItem' @{ catalogItem = $new } { param($r) 'void' }
Call 'FindCatalogItem(13) after remove' 'FindCatalogItem' @{ id = 13 } { param($r) Item $r.item }
Call 'GetCatalogItems count after remove' 'GetCatalogItems' @{ brandIdFilter = 0; typeIdFilter = 0 } { param($r) @($r.items).Count }
New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
ConvertTo-Json -InputObject $res -Depth 10 | Set-Content -Encoding UTF8 $Out
Write-Host "wrote $Out"
