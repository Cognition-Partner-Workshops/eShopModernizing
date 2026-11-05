param([string]$OutDir = "$PSScriptRoot\out")
$ErrorActionPreference = 'Stop'
$legacy = Get-Content "$OutDir\legacy.json" -Raw | ConvertFrom-Json
$port = Get-Content "$OutDir\port.json" -Raw | ConvertFrom-Json

function Compact($value) {
    if ($null -eq $value) { return 'null' }
    ConvertTo-Json -InputObject $value -Depth 20 -Compress
}
function Display($entry) {
    if ($entry.status -eq 'OK' -and $entry.value -is [System.Array]) {
        $ids = @($entry.value | ForEach-Object { $_.id })
        return "$($entry.value.Count) items: ids $(Compact $ids)"
    }
    Compact $entry
}
function Is-Number($value) {
    $value -is [byte] -or $value -is [int16] -or $value -is [int32] -or
    $value -is [int64] -or $value -is [single] -or $value -is [double] -or $value -is [decimal]
}
function Differences($left, $right, [string]$path = 'value') {
    if ($null -eq $left -and $null -eq $right) { return }
    if ($null -eq $left -or $null -eq $right) {
        "$($path): legacy $(Compact $left), port $(Compact $right)"
        return
    }
    if ((Is-Number $left) -and (Is-Number $right)) {
        if ([decimal]$left -ne [decimal]$right) { "$($path): legacy $left, port $right" }
        return
    }
    if ($left -is [System.Array] -and $right -is [System.Array]) {
        if ($left.Count -ne $right.Count) { "$path.count: legacy $($left.Count), port $($right.Count)" }
        for ($i = 0; $i -lt [math]::Min($left.Count, $right.Count); $i++) {
            Differences $left[$i] $right[$i] "$path[]"
        }
        return
    }
    if ($left -is [System.Management.Automation.PSCustomObject] -and $right -is [System.Management.Automation.PSCustomObject]) {
        $fields = @($left.PSObject.Properties.Name) + @($right.PSObject.Properties.Name) | Sort-Object -Unique
        foreach ($field in $fields) {
            if ($field -notin $left.PSObject.Properties.Name -or $field -notin $right.PSObject.Properties.Name) {
                "$path.$($field): missing on one side"
            } else { Differences $left.$field $right.$field "$path.$field" }
        }
        return
    }
    if ((Compact $left) -cne (Compact $right)) { "$($path): legacy $(Compact $left), port $(Compact $right)" }
}
function Fingerprint($source) {
    [ordered]@{
        brands = @($source.GetCatalogBrands.value | ForEach-Object { "$($_.id):$($_.brand)" })
        types = @($source.GetCatalogTypes.value | ForEach-Object { "$($_.id):$($_.type)" })
        items = @($source.'GetCatalogItems(brand=0,type=0)'.value | ForEach-Object {
            [ordered]@{ id = $_.id; name = $_.name; price = [decimal]$_.price }
        })
    }
}
$names = @($legacy.PSObject.Properties.Name) + @($port.PSObject.Properties.Name) | Select-Object -Unique
$rows = @(
    foreach ($name in $names) {
        $l = $legacy.$name; $p = $port.$name
        $differences = @(Differences $l $p '' | Select-Object -Unique)
        $result = 'DIFF'; $reason = ''
        if ($differences.Count -eq 0) { $result = 'MATCH' }
        elseif ($name -match '^(FindCatalogItem|GetDiscount)\(' -and $l.status -eq 'OK' -and
            'value' -in $l.PSObject.Properties.Name -and $null -eq $l.value -and $p.status -eq 'NotFound') {
            $result = 'MAPPED'
            $reason = 'Documented legacy null -> gRPC NotFound mapping'
        } else {
            $reason = ($differences -join '; ').TrimStart('.')
            if ($name -like 'GetCatalogItems(*') {
                $navigation = @($differences | Where-Object { $_ -match '^\.value\[\]\.(brand|type): legacy null, port ' })
                $other = @($differences | Where-Object { $_ -notin $navigation })
                if ($navigation.Count -gt 0) {
                    $reason = (@('items[].brand/type: legacy null, port populated') + $other) -join '; '
                }
            }
            if ($name -eq 'RemoveCatalogItem(13)' -and $l.detail -match 'InternalServiceFault') {
                $reason = 'status/detail: legacy InternalServiceFault (detached EF6 entity), port OK/void'
            }
            if ($name -eq 'FindCatalogItem(13) after remove') {
                $reason = 'status/value: legacy item retained after failed remove, port NotFound after successful remove'
            }
        }
        [ordered]@{ check = $name; legacy = Display $l; port = Display $p; result = $result; reason = $reason }
    }
)
$legacyFingerprint = Fingerprint $legacy
$portFingerprint = Fingerprint $port
$left = Compact $legacyFingerprint | ConvertFrom-Json
$right = Compact $portFingerprint | ConvertFrom-Json
$identical = @(Differences $left $right).Count -eq 0
$fingerprint = [ordered]@{ legacy = $legacyFingerprint; port = $portFingerprint; identical = $identical }
ConvertTo-Json -InputObject $fingerprint -Depth 20 | Set-Content -Encoding UTF8 "$OutDir\fingerprint.json"
$markdown = @(
    "Fingerprint (brands id:name, types id:name, every item id/name/price): identical=$($identical.ToString().ToLowerInvariant()); see [fingerprint.json](fingerprint.json).",
    '',
    '| check | legacy | port | result | reason |',
    '| --- | --- | --- | --- | --- |'
)
foreach ($row in $rows) {
    $cells = @($row.check, $row.legacy, $row.port, $row.result, $row.reason) | ForEach-Object { "$_".Replace('|', '\|').Replace([string][char]13, ' ').Replace([string][char]10, ' ') }
    $markdown += '| ' + ($cells -join ' | ') + ' |'
}
$markdown | Set-Content -Encoding UTF8 "$OutDir\parity.md"
ConvertTo-Json -InputObject $rows -Depth 20 | Set-Content -Encoding UTF8 "$OutDir\parity-rows.json"
Write-Host "wrote $OutDir\parity.md, parity-rows.json and fingerprint.json"
