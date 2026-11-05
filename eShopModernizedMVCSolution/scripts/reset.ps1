param([switch]$Start)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$scratch = Join-Path $env:USERPROFILE 'scratch'
$sdkDir = Join-Path $scratch 'sdk8'
$legacyCopy = Join-Path $scratch 'legacy-site'
$legacySource = Join-Path $repo 'eShopLegacyMVCSolution\src\eShopLegacyMVC'
$solution = Join-Path $repo 'eShopModernizedMVCSolution\eShopModernizedMVC.sln'
$project = Join-Path $repo 'eShopModernizedMVCSolution\src\eShopModernizedMVC\eShopModernizedMVC.csproj'
$msbuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
$iis = 'C:\Program Files\IIS Express\iisexpress.exe'

New-Item -ItemType Directory -Force $sdkDir | Out-Null
Set-Content -Path (Join-Path $sdkDir 'global.json') -Encoding UTF8 -Value '{"sdk":{"version":"8.0.427","rollForward":"disable"}}'

function Invoke-Build {
    param([string]$Executable, [string[]]$Arguments, [string]$Log)
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & $Executable @Arguments *> $Log
        $code = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    if ($code -ne 0) { throw "Build command failed (exit $code). See $Log" }
    Write-Output "Build succeeded: $Log"
}

function Start-Background {
    param([string]$Command, [string]$Directory, [string]$Name)
    $stdout = Join-Path $scratch "$Name-stdout.log"
    $stderr = Join-Path $scratch "$Name-stderr.log"
    $result = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
        CommandLine = "cmd.exe /d /s /c `"set `"UseMockData=true`" && $Command > `"$stdout`" 2> `"$stderr`"`""
        CurrentDirectory = $Directory
    }
    if ($result.ReturnValue -ne 0) { throw "Could not start $Name (code $($result.ReturnValue))." }
    Get-Process -Id $result.ProcessId
}

$processIds = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -ieq 'iisexpress.exe' -or
    ($_.Name -ieq 'dotnet.exe' -and $_.CommandLine -match 'eShopModernizedMVC')
} | Select-Object -ExpandProperty ProcessId)
$processIds += @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalPort -in @(5100, 5101) } |
    Select-Object -ExpandProperty OwningProcess)
foreach ($processId in ($processIds | Sort-Object -Unique)) {
    if ($processId -gt 4) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) { $process | Stop-Process -Force; $process.WaitForExit() }
    }
}

Push-Location $sdkDir
try {
    $version = & dotnet --version
    if ($LASTEXITCODE -ne 0 -or $version -ne '8.0.427') { throw 'SDK 8.0.427 is required.' }
    Write-Output "SDK: $version"
    Invoke-Build 'dotnet' @('build', $solution, '-c', 'Release') (Join-Path $scratch 'reset-port-build.log')
    $nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
    if ($nuget) { $nugetPath = $nuget.Source }
    else {
        $nugetPath = Join-Path $scratch 'nuget.exe'
        if (-not (Test-Path $nugetPath)) {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetPath -UseBasicParsing
        }
    }
    Invoke-Build $nugetPath @('restore', (Join-Path $repo 'eShopLegacyMVCSolution\eShopLegacyMVC.sln')) (Join-Path $scratch 'reset-legacy-restore.log')
    Invoke-Build $msbuild @((Join-Path $legacySource 'eShopLegacyMVC.csproj'), '/p:Configuration=Release') (Join-Path $scratch 'reset-legacy-build.log')
    if (Test-Path $legacyCopy) { Remove-Item $legacyCopy -Recurse -Force }
    New-Item -ItemType Directory $legacyCopy | Out-Null
    Get-ChildItem $legacySource -Force | Copy-Item -Destination $legacyCopy -Recurse -Force
    $configPath = Join-Path $legacyCopy 'Web.config'
    [xml]$config = Get-Content $configPath
    $mock = $config.SelectSingleNode('/configuration/appSettings/add[@key="UseMockData"]')
    if (-not $mock) { throw 'Legacy UseMockData setting is missing.' }
    $mock.SetAttribute('value', 'true')
    $config.Save($configPath)

    if ($Start) {
        $legacy = Start-Background "`"$iis`" /path:`"$legacyCopy`" /port:5101" $legacyCopy 'legacy'
        $port = Start-Background "dotnet run --project `"$project`" --no-build --no-launch-profile -c Release --urls http://localhost:5100" $sdkDir 'port'
        $deadline = (Get-Date).AddMinutes(2)
        $pending = @(5100, 5101)
        while ($pending.Count -gt 0 -and (Get-Date) -lt $deadline) {
            foreach ($listenPort in @($pending)) {
                try {
                    $response = Invoke-WebRequest "http://localhost:$listenPort/" -UseBasicParsing -TimeoutSec 5
                    if ($response.StatusCode -eq 200) {
                        Write-Output "HTTP 200: http://localhost:$listenPort/"
                        $pending = @($pending | Where-Object { $_ -ne $listenPort })
                    }
                }
                catch { }
            }
            if ($legacy.HasExited -or $port.HasExited) { throw 'A demo process exited. See scratch runtime logs.' }
            if ($pending.Count -gt 0) { Start-Sleep -Seconds 1 }
        }
        if ($pending.Count -gt 0) { throw "Timed out waiting for ports: $pending. See scratch runtime logs." }
        Write-Output "Running: legacy launcher PID $($legacy.Id), port launcher PID $($port.Id)"
    }
}
finally { Pop-Location }
