# WCF catalog → .NET 8 gRPC

This solution is independent of the other solutions in the repository. It ports the production
EF6 WCF service, not `CatalogServiceMock`. The protobuf contract is unchanged from PR #226.

## Prerequisites

Windows with .NET 8 SDK and SQL Server LocalDB 2022 (or a SQL Server connection string).
`global.json` selects .NET 8 even when .NET 9 is installed. All tests use real SQL Server LocalDB;
there is no SQLite/in-memory fallback and no silently skipped database test.

Run commands from this directory so the SDK pin is applied:

```powershell
dotnet build eShopModernizedWcfSolution.sln -c Release
dotnet format eShopModernizedWcfSolution.sln --verify-no-changes
dotnet test eShopModernizedWcfSolution.sln -c Release --logger "trx;LogFileName=modern-tests.trx" --results-directory C:\Users\Administrator\evidence\modern-tests
```

The tests create uniquely named databases and drop them on disposal. The standalone Kestrel
smoke test uses `eShopWcf_Smoke`, port 15200, then stops the server and drops that database.
Reserve that database name for the smoke test; do not put valuable data in it.

## Run

```powershell
$env:ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=eShopWcf_Demo;Integrated Security=true;TrustServerCertificate=true'
dotnet run --project src/eShopWcf.Catalog.Grpc -- --urls http://localhost:5200
```

`ConnectionString` overrides `ConnectionStrings:CatalogDb` (including
`ConnectionStrings__CatalogDb` in the environment). The fallback database is `eShopDatabase`.
Only a database that does not exist is created and seeded. Existing databases are never migrated,
repaired or reseeded. Back up an existing database before allowing any client to mutate it.
`Seed__Dataset=Csv` selects the canonical CSV dataset on initial creation; the default is `Legacy`.

The plaintext listener is HTTP/2-only. Reflection is enabled and `/health` reports host liveness.
Use TLS when exposing the service beyond local development; authentication is not added by this port.

See [the contract](modernization/grpc-contract.md) for RPC/status mapping and
[schema compatibility](modernization/schema-compatibility.md) for the observed EF6 schema.
