param([string]$WcfContractPath)
$ErrorActionPreference = 'Stop'
$local = Join-Path $PSScriptRoot '../src/eShop.Catalog.Grpc.Contracts/Protos/catalog.proto'
$hash = (Get-FileHash $local -Algorithm SHA256).Hash
$expected = '674B2619864259768913EFE06766501E736A5DC6686759A91370BC14FBF53112'
if ($hash -ne $expected) { throw 'Client contract differs from the reused contract.' }
if ($WcfContractPath) {
    if ((Get-FileHash $WcfContractPath -Algorithm SHA256).Hash -ne $hash) { throw 'Client and WCF component contracts differ.' }
    Write-Output "WCF/client byte parity passed: SHA256 $hash"
} else {
    Write-Output "Reuse-source byte parity passed: SHA256 $hash; WCF component comparison not performed."
}
