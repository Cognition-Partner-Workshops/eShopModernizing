# Newly added legacy BEFORE baseline harness

This is a **newly authored baseline harness**, not an original eShopWCFService test
suite. It directly instantiates the unchanged production
`eShopWCFService.CatalogService(new EntityModel())`; it does not use the mock,
the gRPC port, or copied service/seed code. Assertions pin the observed legacy
behavior, independently of the modern implementation.

The non-SDK test project targets .NET Framework **4.6.1**, MSTest v2 **2.2.10**,
and Entity Framework **6.1.3**. Use **Visual Studio MSBuild**, not `dotnet build`.
`LegacyBaseline.sln` is deliberately separate: its project reference builds the
legacy ASP.NET/WCF project, which requires Visual Studio's .NET Framework and web
application targets. The main solution remains .NET 8 only.

There is one unique `eShopWcf_LegacyTest_<guid>` LocalDB database per test class.
Class initialization sets `ConnectionString` before constructing the production
service and causes EF6 to create/seed the database. Test methods use fresh contexts
so tracking/navigation fixup from earlier calls cannot mask detached behavior.
Writes clean up their rows, tests are nonparallel, and class cleanup drops the
database and restores the previous environment variable. No existing catalog
database is used.

## Restore, build, run

Run from the repository root in PowerShell on Windows with the installed toolchain:

```powershell
nuget restore eShopLegacyNTier/eShopLegacyNTier.sln -PackagesDirectory eShopLegacyNTier/src/packages -NonInteractive -Source https://api.nuget.org/v3/index.json
nuget restore eShopModernizedWcfSolution/tests/eShopWcf.LegacyBaseline.Tests/packages.config -PackagesDirectory eShopModernizedWcfSolution/packages -NonInteractive -Source https://api.nuget.org/v3/index.json
& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' eShopModernizedWcfSolution/LegacyBaseline.sln /t:Build /p:Configuration=Release /m:1 /nr:false
& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' eShopModernizedWcfSolution/tests/eShopWcf.LegacyBaseline.Tests/bin/Release/eShopWcf.LegacyBaseline.Tests.dll /TestAdapterPath:eShopModernizedWcfSolution/packages/MSTest.TestAdapter.2.2.10/build/_common '/Logger:trx;LogFileName=legacy-tests.trx' /ResultsDirectory:C:\Users\Administrator\evidence\legacy-tests /Platform:x64 '/Framework:.NETFramework,Version=v4.6.1'
```

The project reference does not change any legacy source files. Package and build
outputs are ignored. The runner is the BuildTools VSTest, with the NuGet-restored
MSTest adapter explicitly supplied. The legacy tests cover all ten operations:
seeded scalar values, populated Find navigations, unpopulated list navigations,
missing/null results, negative filters, stock date-only lookup/insertion/upsert,
max-plus-one identities, decimal `12.3456`, detached update including null scalars,
unknown update, detached and tracked remove, and every discount start/end boundary.

The unknown-update and detached-remove tests also write the actual exception type
and message into the TRX. These EF6 details are intentionally distinct from the
modern port's explicit unknown-update/delete `NOT_FOUND` behavior.

## Recorded production legacy results

The LocalDB run passed **55/55** tests, with no failures or skips. Actual results:

- Updating unknown ID `99999` throws
  `System.Data.Entity.Infrastructure.DbUpdateConcurrencyException`:
  "Store update, insert, or delete statement affected an unexpected number of rows (0)."
- Removing `new CatalogItem { Id = existingId }` in a **fresh context** throws
  `System.InvalidOperationException`:
  "The object cannot be deleted because it was not found in the ObjectStateManager."
  The row remains present. This is not a same-key/already-tracked-entity conflict.
- Removing the actual item returned by `FindCatalogItem` in the same service/context
  succeeds; a new context then finds no row.
- A create/read round-trip preserves `12.3456M` exactly. All twelve seeded items,
  five brands, four types, six stock rows, and six discount intervals match the
  pinned assertions. All discount start/end boundaries are inclusive.

TRX: `C:\Users\Administrator\evidence\legacy-tests\legacy-tests.trx`.
Class cleanup successfully deleted the test database; a subsequent `sys.databases`
query found zero databases matching `eShopWcf_LegacyTest_<guid>`.
