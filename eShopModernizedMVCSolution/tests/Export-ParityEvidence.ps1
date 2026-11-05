param([string]$Evidence = 'C:\Users\Administrator\evidence\parity')
$ErrorActionPreference = 'Stop'
$differences = @()
foreach ($section in 'columns','foreignKeys','sequences') {
    $legacy = Get-Content "$Evidence\baseline-$section.json" -Raw | ConvertFrom-Json
    $modern = Get-Content "$Evidence\fresh-$section.json" -Raw | ConvertFrom-Json
    foreach ($row in $legacy) {
        $key = if ($section -eq 'sequences') { $row.name } else { "$($row.table).$($row.column)" }
        $other = @($modern | Where-Object { if ($section -eq 'sequences') { $_.name -eq $row.name } else { $_.table -eq $row.table -and $_.column -eq $row.column } })
        if (!$other.Count) { $differences += "$section $key : legacy only"; continue }
        foreach ($property in $row.PSObject.Properties.Name) {
            if ($row.$property -ne $other[0].$property) { $differences += "$section $key $property : $($row.$property) => $($other[0].$property)" }
        }
    }
}
$differences | Set-Content "$Evidence\schema-differences.txt"
$lines = @('# HTTP/SQL parity operations', '', '| Operation | Legacy | Modern | Classification |','|---|---|---|---|')
$operations = Get-Content "$Evidence\run2\operations.json" -Raw | ConvertFrom-Json
foreach ($row in $operations) {
    if ($null -ne $row.legacy.Status) {
        $a = "$($row.legacy.Status); $($row.legacy.ContentType); $($row.legacy.Length) bytes"
        $b = "$($row.modern.Status); $($row.modern.ContentType); $($row.modern.Length) bytes"
        if ($row.operation -like 'picture *' -and $row.legacy.Status -eq 200) { $a += "; SHA256 $($row.legacy.Value)"; $b += "; SHA256 $($row.modern.Value)" }
    } elseif ($null -ne $row.result) {
        $a = "$($row.result.Status); validation fields $($row.result.Value)"; $b = 'recorded per database'
    } else { $a = $row.legacy; $b = $row.modern }
    $lines += "| $($row.operation) | $a | $b | $($row.classification) |"
}
$lines | Set-Content "$Evidence\operations.md"
Get-ChildItem $Evidence -Recurse -File | Where-Object { $_.Extension -in '.json','.csv','.bak','.trx' -and $_.Name -ne 'hashes.csv' -and $_.FullName -notlike '*legacy-site*' } | Get-FileHash -Algorithm SHA256 | Export-Csv "$Evidence\hashes.csv" -NoTypeInformation
$a = Get-Content "$Evidence\run1-after-legacy-data.json" -Raw
foreach ($name in 'run1-after-modern','run2-after-legacy','run2-after-modern') {
    if ((Get-Content "$Evidence\$name-data.json" -Raw) -ne $a) { throw "Determinism mismatch: $name" }
}
'All four post-run normalized application data dumps are identical.' | Tee-Object "$Evidence\determinism.txt"
