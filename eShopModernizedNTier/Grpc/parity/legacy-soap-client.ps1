# Calls every ICatalogService operation on the legacy WCF SOAP service and writes normalized JSON.
param(
    [string]$Url = 'http://localhost:62314/CatalogService.svc',
    [string]$Out = "$PSScriptRoot\out\legacy.json"
)
$ErrorActionPreference = 'Stop'
$m = 'http://schemas.datacontract.org/2004/07/eShopWCFService.Models'
# CatalogItemsStock is declared in namespace eShopWCFService (not .Models)
$ms = 'http://schemas.datacontract.org/2004/07/eShopWCFService'

function Invoke-Soap([string]$op, [string]$body) {
    $env = "<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'><s:Body><$op xmlns='http://tempuri.org/'>$body</$op></s:Body></s:Envelope>"
    try {
        $r = Invoke-WebRequest -Uri $Url -Method Post -Body $env -ContentType 'text/xml; charset=utf-8' `
            -Headers @{ SOAPAction = "`"http://tempuri.org/ICatalogService/$op`"" } -UseBasicParsing
        [xml]$x = $r.Content
        return @{ ok = $true; xml = $x }
    } catch {
        $text = if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $_.ErrorDetails.Message } else { $_.Exception.Message }
        # PowerShell 5.1 strips the XML tags from ErrorDetails, so parse the plain text.
        $plain = ($text -replace '<[^>]+>', ' ').Trim()
        $fault = if ($plain -match '(?:\w+:)?(\w+Fault)\s*(.*?\.)') { "$($Matches[1]): $($Matches[2])" } else { $plain }
        return @{ ok = $false; fault = "$fault" }
    }
}
function Nil($n) { $n -eq $null -or ($n.Attributes -and $n.GetAttribute('nil', 'http://www.w3.org/2001/XMLSchema-instance') -eq 'true') }
function Item($n) {
    if (Nil $n) { return $null }
    [ordered]@{ id = [int]$n.Id; name = $n.Name; description = $n.Description; price = [decimal]$n.Price
        pictureFileName = $n.Picturefilename; catalogBrandId = [int]$n.CatalogBrandId; catalogTypeId = [int]$n.CatalogTypeId
        brand = if (Nil $n.CatalogBrand) { $null } else { $n.CatalogBrand.Brand }
        type = if (Nil $n.CatalogType) { $null } else { $n.CatalogType.Type } }
}
function Items($r, $op) { ,@($r.xml.Envelope.Body."${op}Response"."${op}Result".ChildNodes | ForEach-Object { Item $_ }) }
function Result($r, $op) { $r.xml.Envelope.Body."${op}Response"."${op}Result" }
function ItemXml($i) {
    "<catalogItem xmlns:a='$m'><a:CatalogBrandId>$($i.catalogBrandId)</a:CatalogBrandId><a:CatalogTypeId>$($i.catalogTypeId)</a:CatalogTypeId><a:Description>$($i.description)</a:Description><a:Id>$($i.id)</a:Id><a:Name>$($i.name)</a:Name><a:Picturefilename>$($i.pictureFileName)</a:Picturefilename><a:Price>$($i.price)</a:Price></catalogItem>"
}
function Call($name, $op, $body, [scriptblock]$map) {
    $r = Invoke-Soap $op $body
    $res[$name] = if ($r.ok) { [ordered]@{ status = 'OK'; value = (& $map $r) } } else { [ordered]@{ status = 'FAULT'; detail = $r.fault } }
}
$res = [ordered]@{}
Call 'GetCatalogBrands' 'GetCatalogBrands' '' { param($r) ,@((Result $r 'GetCatalogBrands').ChildNodes | % { [ordered]@{ id = [int]$_.Id; brand = $_.Brand } }) }
Call 'GetCatalogTypes' 'GetCatalogTypes' '' { param($r) ,@((Result $r 'GetCatalogTypes').ChildNodes | % { [ordered]@{ id = [int]$_.Id; type = $_.Type } }) }
foreach ($f in @(@(0,0), @(2,0), @(0,1), @(2,1), @(99,0))) {
    Call "GetCatalogItems(brand=$($f[0]),type=$($f[1]))" 'GetCatalogItems' "<brandIdFilter>$($f[0])</brandIdFilter><typeIdFilter>$($f[1])</typeIdFilter>" { param($r) Items $r 'GetCatalogItems' }
}
Call 'FindCatalogItem(1)' 'FindCatalogItem' '<id>1</id>' { param($r) Item (Result $r 'FindCatalogItem') }
Call 'FindCatalogItem(999)' 'FindCatalogItem' '<id>999</id>' { param($r) Item (Result $r 'FindCatalogItem') }
Call 'GetAvailableStock(2017-09-20,1)' 'GetAvailableStock' '<date>2017-09-20T00:00:00</date><catalogItemId>1</catalogItemId>' { param($r) [int](Result $r 'GetAvailableStock') }
Call 'GetAvailableStock(2017-01-01,1)' 'GetAvailableStock' '<date>2017-01-01T00:00:00</date><catalogItemId>1</catalogItemId>' { param($r) [int](Result $r 'GetAvailableStock') }
$disc = { param($r) $d = Result $r 'GetDiscount'; if (Nil $d) { $null } else { [ordered]@{ id = [int]$d.Id; size = [math]::Round([double]$d.Size, 6); start = ([datetime]$d.Start).ToString('yyyy-MM-dd'); end = ([datetime]$d.End).ToString('yyyy-MM-dd') } } }
Call 'GetDiscount(2017-09-19)' 'GetDiscount' '<day>2017-09-19T00:00:00</day>' $disc
Call 'GetDiscount(2018-01-01)' 'GetDiscount' '<day>2018-01-01T00:00:00</day>' $disc
$stock = { param($c, $d, $n) "<catalogItemsStock xmlns:a='$ms'><a:AvailableStock>$n</a:AvailableStock><a:CatalogItemId>$c</a:CatalogItemId><a:Date>${d}T00:00:00</a:Date><a:StockId>0</a:StockId></catalogItemsStock>" }
Call 'CreateAvailableStock(overwrite 2,2017-09-20,50)' 'CreateAvailableStock' (& $stock 2 '2017-09-20' 50) { param($r) 'void' }
Call 'GetAvailableStock(2017-09-20,2) after overwrite' 'GetAvailableStock' '<date>2017-09-20T00:00:00</date><catalogItemId>2</catalogItemId>' { param($r) [int](Result $r 'GetAvailableStock') }
Call 'CreateAvailableStock(new 3,2017-10-01,7)' 'CreateAvailableStock' (& $stock 3 '2017-10-01' 7) { param($r) 'void' }
Call 'GetAvailableStock(2017-10-01,3) after create' 'GetAvailableStock' '<date>2017-10-01T00:00:00</date><catalogItemId>3</catalogItemId>' { param($r) [int](Result $r 'GetAvailableStock') }
$new = [ordered]@{ id = 0; name = 'Parity Item'; description = 'Parity Item'; price = 9.99; pictureFileName = 'parity.png'; catalogBrandId = 1; catalogTypeId = 1 }
Call 'CreateCatalogItem(Parity Item)' 'CreateCatalogItem' (ItemXml $new) { param($r) 'void' }
Call 'FindCatalogItem(13) after create' 'FindCatalogItem' '<id>13</id>' { param($r) Item (Result $r 'FindCatalogItem') }
$new.id = 13; $new.price = 19.99; $new.name = 'Parity Item Updated'
Call 'UpdateCatalogItem(13)' 'UpdateCatalogItem' (ItemXml $new) { param($r) 'void' }
Call 'FindCatalogItem(13) after update' 'FindCatalogItem' '<id>13</id>' { param($r) Item (Result $r 'FindCatalogItem') }
Call 'RemoveCatalogItem(13)' 'RemoveCatalogItem' (ItemXml $new) { param($r) 'void' }
Call 'FindCatalogItem(13) after remove' 'FindCatalogItem' '<id>13</id>' { param($r) Item (Result $r 'FindCatalogItem') }
Call 'GetCatalogItems count after remove' 'GetCatalogItems' '<brandIdFilter>0</brandIdFilter><typeIdFilter>0</typeIdFilter>' { param($r) (Items $r 'GetCatalogItems').Count }
New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
$res | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 $Out
Write-Host "wrote $Out"
