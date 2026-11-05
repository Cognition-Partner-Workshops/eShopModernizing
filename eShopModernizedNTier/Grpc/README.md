# Standalone WCF-to-gRPC catalog (.NET 8)

This service ports the WCF contract of a legacy .NET line-of-business app.

Build and test from the repository root:

```sh
dotnet build eShopModernizedNTier/Grpc/eShopGrpc.sln -c Release
dotnet test eShopModernizedNTier/Grpc/eShopGrpc.sln -c Release
dotnet format eShopModernizedNTier/Grpc/eShopGrpc.sln --verify-no-changes
dotnet run --project eShopModernizedNTier/Grpc/src/eShop.Catalog.Grpc
grpcurl -plaintext localhost:5199 list
grpcurl -plaintext -d '{"id":1}' localhost:5199 eshop.catalog.v1.Catalog/FindCatalogItem
```

The folder-local SDK (8.0.427, latestFeature), build props and central package
versions isolate this solution from repository-wide configuration. No shared
host library or root solution is required. Source was extracted from PR #226.

## Configuration

UseMockData defaults to true in appsettings.json. Set UseMockData=false
(environment variable: UseMockData=false) and ConnectionStrings__Catalog for SQL
Server mode. Mock services are singleton, mutable in-memory stores reset on
restart. They use exactly the WCF seed: 12 items, 5 brands, 4 types, 6 stock rows,
and 6 discounts. Unlike the MVC seed in #226, picture filenames and item 12
match WCF; stock is date-specific, not a constant quantity on each item.
Discount IDs are 1..6 in seed order, matching legacy SQL identity assignment at runtime.

Kestrel binds to localhost:5199 using HTTP/2 only (h2c), not all interfaces.
Override the address with Kestrel__Endpoints__Grpc__Url. Reflection defaults to
enabled in Development and disabled otherwise; EnableGrpcReflection=true/false
overrides that default. reset.ps1 explicitly enables reflection for grpcurl and
the WinForms worker. grpcurl needs -plaintext.
Both this component and the legacy endpoint are unauthenticated plain HTTP;
authentication and TLS are out of scope for this local parity component.
Do not expose either endpoint publicly.

IIS Express uses HTTP.sys, so TCP diagnostics report system PID 4 for its
listener. The reset script accepts this only when the HTTP.sys request queue
for HTTP://LOCALHOST:62314/ belongs exclusively to the recorded, validated IIS
Express PID. It never terminates PID 4.

## Contract and parity

Protos/catalog.proto in eShop.Catalog.Grpc.Contracts exposes all ten operations:

- FindCatalogItem
- GetCatalogBrands
- GetCatalogItems
- GetCatalogTypes
- GetAvailableStock
- CreateAvailableStock
- CreateCatalogItem
- UpdateCatalogItem
- RemoveCatalogItem
- GetDiscount

GetCatalogItems uses 0 for no filter and combines nonzero brand/type filters.
CreateCatalogItem ignores the caller ID and uses max(Id)+1. Update and remove
persist until restart. CreateAvailableStock overwrites the first matching item
and calendar date; otherwise StockId is max+1. Missing stock returns 0.
Discount ranges include both endpoints. Dates use UTC protobuf timestamps;
only their calendar-date component is significant. Prices use exact units/nanos.

Explicit transport differences from WCF CatalogService.svc.cs:
- FindCatalogItem and GetDiscount return NotFound instead of legacy null.
- Missing update/remove targets return NotFound rather than an EF/WCF fault.
- Invalid/nonpositive IDs, negative filters/stock, missing payloads/dates and
  blank create names return InvalidArgument instead of generic WCF faults or
  permissive legacy behavior.
- Unhandled database/runtime failures use the standard gRPC Unknown status.

Remaining behavioral differences observed in the parity run:
- GetCatalogItems populates brand/type navigation properties on the port; the
  legacy operation returns null navigation properties. The underlying item IDs,
  names, prices, picture filenames and foreign keys match.
- Legacy RemoveCatalogItem always faults with InternalServiceFault because it
  removes a detached entity: EF6 reports that the "object cannot be deleted
  because it was not found in the ObjectStateManager". The port removes the
  item successfully. Consequently, the legacy item remains readable and its
  final count is 13; the port returns NotFound and its final count is 12.

Run parity/compare.ps1 against existing parity/out/legacy.json and port.json to
regenerate parity.md, parity-rows.json and fingerprint.json without calling
either service. Prices are compared numerically, so decimal scale alone is not
a difference. MATCH means equal full normalized results; MAPPED means the
documented FindCatalogItem/GetDiscount null-to-NotFound conversion; DIFF marks
other differences and includes a reason. The fingerprint compares brands
(id:name), types (id:name) and every initial item ID/name/price on both sides.
All generated parity/out files and build bin/obj directories are gitignored.

The retained EF Core backend and CSV seeding are from #226; the strict WCF seed
parity guarantee applies to mock mode. Database mode requires SQL Server and is
not exercised against a live server by this solution's tests.

## Reset and teardown safety

Run from the repository root in Windows PowerShell. Do not run these commands
while another session is recording or using the services.

```powershell
# Build and start both owned hosts; preserve an existing owned database.
.\eShopModernizedNTier\Grpc\scripts\reset.ps1 -Start

# Drop/reseed only this session's marked database, then start both hosts.
.\eShopModernizedNTier\Grpc\scripts\reset.ps1 -Start -ResetDatabase

# Stop only hosts recorded in this user's ownership state. No builds or SQL.
.\eShopModernizedNTier\Grpc\scripts\reset.ps1 -Stop

# Build only the port, without touching the legacy host or database.
.\eShopModernizedNTier\Grpc\scripts\reset.ps1 -PortOnly
.\eShopModernizedNTier\Grpc\scripts\reset.ps1 -PortOnly -Start
.\eShopModernizedNTier\Grpc\scripts\reset.ps1 -PortOnly -Stop
```

Ownership state is outside the repo at
`%LOCALAPPDATA%\eShopGrpcParity\state.json`.
Each directly launched host has a recorded PID, UTC start time/ticks,
executable path and port. The gRPC host runs directly as
`dotnet <Release DLL>` (no dotnet-run child to orphan).
Teardown validates the live process's start time and executable path before
terminating that exact process; no process-name scans or process-tree kills
are used. A reused PID or mismatched executable is refused. A state-file lock
serializes script invocations. Teardown prints Get-Process evidence for recorded
PIDs before/after stopping, plus Get-NetTCPConnection observations for both ports.
Those observations never authorize killing an unrecorded process.

Ports 62314 and 5199 remain unchanged, as do both parity client URLs. A required
port occupied by an unowned process causes a clear failure before stopping
owned hosts or rebuilding. Hosts launched by the earlier unsafe script are not
adopted: they have no valid ownership record. Their operator must stop them
manually when appropriate. This script will not terminate them.

The built WCF site is copied to
`%LOCALAPPDATA%\eShopGrpcParity\legacy-site\<owner-guid>`.
Only the scratch Web.config is rewritten. Its EntityModel connection points at
`(localdb)\MSSQLLocalDB` and the unique state-owned
`eShopWcfParity_<owner-guid-without-hyphens>` database, using Windows
authentication. The IIS Express process also receives that connection string
explicitly so an inherited ConnectionString cannot redirect it elsewhere.
The scratch directory has its own owner marker and an unmarked existing
directory is not overwritten. Legacy source and its Web.config stay unchanged.

Before use, an existing database must have the database-level extended property
`eShopGrpcParityOwner` matching the state marker. For a new unique database,
the script invokes GetCatalogBrands on its owned scratch host to run the legacy
EF initializer, then adds the ownership property. If initialization or marking
fails, an unmarked leftover database is refused on the next run, not adopted
or automatically dropped. Investigate it manually; do not forge a marker.

A database is dropped only with -ResetDatabase, after both the exact state-owned
name and matching extended property are verified. Without that switch it is
preserved. -Stop never accesses SQL; -PortOnly never touches the legacy host or
database. The default eShopDatabase is never targeted. Losing state does not
grant permission to delete old databases or terminate old hosts.
If startup fails, recorded hosts remain available for a later -Stop; no broad
automatic cleanup is attempted.

## Fresh-machine prerequisites

This session used Windows Server 2022, Windows PowerShell, .NET SDK 8.0.427,
Visual Studio 2022 Build Tools, IIS Express, SQL Server 2022 LocalDB
16.0.1000.6, NuGet and grpcurl 1.9.1. The scratch artifacts are C:\scratch\nuget.exe,
C:\scratch\ra461.nupkg, C:\scratch\ra461\build, C:\scratch\SqlLocalDB.msi,
C:\scratch\localdb.log, C:\scratch\grpcurl.zip and C:\scratch\grpcurl\grpcurl.exe.
The download URLs and LocalDB installation arguments below were recovered from
this session's command log; the SDK and VS Build Tools were already installed.

Install SDK 8.0.427 if absent (a fresh-machine equivalent, not an install
performed during this session):

```powershell
New-Item -ItemType Directory -Force C:\scratch | Out-Null
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile C:\scratch\dotnet-install.ps1 -UseBasicParsing
& C:\scratch\dotnet-install.ps1 -Version 8.0.427 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
dotnet --list-sdks
```

Install VS 2022 Build Tools with Web/.NET desktop build tooling if absent
(run installation elevated). This session's exact MSBuild path is shown below.
IIS Express must also be installed at C:\Program Files\IIS Express\iisexpress.exe;
install Microsoft's IIS Express 10.0 x64 package if that path is absent.

```powershell
Invoke-WebRequest https://aka.ms/vs/17/release/vs_buildtools.exe -OutFile C:\scratch\vs_buildtools.exe -UseBasicParsing
Start-Process C:\scratch\vs_buildtools.exe -ArgumentList '--quiet --wait --norestart --add Microsoft.VisualStudio.Workload.WebBuildTools --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools' -Wait
$MSBuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
Test-Path $MSBuild
Test-Path 'C:\Program Files\IIS Express\iisexpress.exe'
```

Download NuGet and restore the legacy packages from the repository root:

```powershell
Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile C:\scratch\nuget.exe -UseBasicParsing
& C:\scratch\nuget.exe restore eShopLegacyNTier/src/eShopWCFService/packages.config -PackagesDirectory eShopLegacyNTier/src/packages
```

Use the net461 reference assemblies NuGet package rather than assuming a
4.6.1 targeting pack is installed. PowerShell Expand-Archive requires a zip
extension; the package contents are identical:

```powershell
Invoke-WebRequest https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.net461/1.0.3 -OutFile C:\scratch\ra461.nupkg -UseBasicParsing
Copy-Item C:\scratch\ra461.nupkg C:\scratch\ra461.zip
Expand-Archive C:\scratch\ra461.zip -DestinationPath C:\scratch\ra461 -Force
& $MSBuild eShopLegacyNTier/src/eShopWCFService/eShopWCFService.csproj -p:Configuration=Release '-p:TargetFrameworkRootPath=C:\scratch\ra461\build'
```

LocalDB download and install (the exact MSI URL, path and installer arguments
used in this session; run the installer elevated):

```powershell
Invoke-WebRequest https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi -OutFile C:\scratch\SqlLocalDB.msi -UseBasicParsing
$p = Start-Process msiexec -ArgumentList '/i C:\scratch\SqlLocalDB.msi /qn IACCEPTSQLLOCALDBLICENSETERMS=YES /l*v C:\scratch\localdb.log' -Wait -PassThru
"exit=$($p.ExitCode)"
& 'C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe' create MSSQLLocalDB -s
& 'C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe' start MSSQLLocalDB
& 'C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe' info MSSQLLocalDB
```

The recorded create command used -s (create and start). The explicit start
command is included for subsequent sessions or an already-created instance.
Run LocalDB under the same Windows user that runs IIS Express and reset.ps1.

Download the grpcurl version used here:

```powershell
Invoke-WebRequest https://github.com/fullstorydev/grpcurl/releases/download/v1.9.1/grpcurl_1.9.1_windows_x86_64.zip -OutFile C:\scratch\grpcurl.zip -UseBasicParsing
Expand-Archive C:\scratch\grpcurl.zip -DestinationPath C:\scratch\grpcurl -Force
& C:\scratch\grpcurl\grpcurl.exe -version
```

After prerequisites, use reset.ps1 -Start (or -Start -ResetDatabase for a fresh
owned database), then run the parity clients. Do not use a direct IIS Express
launch against legacy source when using this ownership workflow.
