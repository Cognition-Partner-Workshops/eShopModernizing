param(
    [string]$GrpcHostProject,
    [string]$LegacySite,
    [switch]$StartClient,
    [string]$GrpcAddress = 'http://localhost:5200'
)
$ErrorActionPreference = 'Stop'
$watch = [Diagnostics.Stopwatch]::StartNew()
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sdk = Join-Path $env:ProgramFiles 'dotnet/sdk/8.0.427/dotnet.dll'
if (!(Test-Path $sdk)) { throw 'The required .NET SDK 8.0.427 is not installed.' }
$hostDirectory = if ($GrpcHostProject) { Split-Path (Resolve-Path $GrpcHostProject).Path }
$site = if ($LegacySite) { (Resolve-Path $LegacySite).Path }
Get-CimInstance Win32_Process | Where-Object {
    ($_.Name -eq 'eShop.WinForms.Client.exe' -and $_.ExecutablePath -and $_.ExecutablePath.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) -or
    ($hostDirectory -and $_.Name -in @('dotnet.exe', 'eShop.Catalog.Grpc.exe') -and $_.CommandLine -and $_.CommandLine.Contains($hostDirectory)) -or
    ($site -and $_.Name -eq 'iisexpress.exe' -and $_.CommandLine -and $_.CommandLine.Contains($site))
} | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
& (Join-Path $PSScriptRoot 'verify-contract.ps1')
& dotnet $sdk build (Join-Path $root 'eShop.WinForms.sln') -c Release
if ($LASTEXITCODE) { throw 'Client solution build failed.' }
if ($GrpcHostProject) {
    & dotnet $sdk build $GrpcHostProject -c Release
    if ($LASTEXITCODE) { throw 'Scratch backend build failed.' }
    $previous = $env:Catalog__UseMockData
    try {
        $env:Catalog__UseMockData = 'true'
        $assembly = Join-Path $hostDirectory 'bin/Release/net8.0/eShop.Catalog.Grpc.dll'
        Start-Process dotnet -ArgumentList "`"$assembly`" --urls $GrpcAddress" -WorkingDirectory $hostDirectory | Out-Null
    } finally { $env:Catalog__UseMockData = $previous }
}
if ($site) {
    Start-Process (Join-Path $env:ProgramFiles 'IIS Express/iisexpress.exe') -ArgumentList "/path:`"$site`" /port:62314" | Out-Null
}
if ($StartClient) {
    $previous = $env:CatalogService__Address
    try {
        $env:CatalogService__Address = $GrpcAddress
        Start-Process (Join-Path $root 'src/eShop.WinForms.Client/bin/Release/net8.0-windows/eShop.WinForms.Client.exe') | Out-Null
    } finally { $env:CatalogService__Address = $previous }
}
$watch.Stop()
Write-Output ("Reset completed in {0:F3} seconds. Mock host state is recreated when -GrpcHostProject is supplied; SQL mode is not reset or tested." -f $watch.Elapsed.TotalSeconds)
