# Containers

Linux containers for the modernized estate: one image per service plus a SQL Server 2022
container, wired together by `eShopModernized/docker-compose.yml`. No Windows containers, no
LocalDB and no integrated security — every setting comes from the environment.

## Quick start

```bash
cd eShopModernized
docker compose build
docker compose up -d --wait          # blocks until every container reports healthy

curl http://localhost:8080/health    # web
curl http://localhost:8081/health    # catalog API
curl --http2-prior-knowledge http://localhost:8082/health   # gRPC (HTTP/2 only)
curl http://localhost:8081/api/brands
grpcurl -plaintext localhost:8082 list

docker compose down -v               # stop and drop the database volume
```

`docker compose down` (without `-v`) keeps the `catalog-db` volume, so the next `up` reuses the
already-migrated and seeded database.

## Services and ports

| Service | Image | Container port | Default host port | Override |
| --- | --- | --- | --- | --- |
| `web` | `eshop-modernized/web:local` | 8080 (HTTP/1.1) | 8080 | `WEB_PORT` |
| `catalog-api` | `eshop-modernized/catalog-api:local` | 8080 (HTTP/1.1) | 8081 | `CATALOG_API_PORT` |
| `catalog-grpc` | `eshop-modernized/catalog-grpc:local` | 8080 (**h2c**, `Kestrel:EndpointDefaults:Protocols=Http2`) | 8082 | `CATALOG_GRPC_PORT` |
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | 1433 | `SQLSERVER_PORT` |

The gRPC endpoint speaks HTTP/2 with no upgrade from HTTP/1.1, so probes and clients must use
prior knowledge (`curl --http2-prior-knowledge`, `grpcurl -plaintext`). Plain `curl` against port
8082 fails by design.

## Environment variables

Compose reads these from your shell or from an `.env` file next to `docker-compose.yml`:

| Variable | Default | Purpose |
| --- | --- | --- |
| `MSSQL_SA_PASSWORD` | `Pass@word1` | SA password of the dev SQL Server **and** the password in the connection string handed to the services. Development-only default; set a real value for anything shared. |
| `CATALOG_DB_NAME` | `CatalogDb` | Name of the consolidated catalog database. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Host environment; `Development` turns on the Swagger UI in the API. `appsettings.Development.json` sets `Catalog:UseMockData=true`, but the `Catalog__UseMockData=false` environment variable set by compose wins, so the containers keep using SQL Server either way. |
| `CATALOG_USE_CUSTOMIZATION_DATA` | `false` | Seeds from `Setup/*.csv` and re-extracts `Setup/CatalogItems.zip` over the pictures folder instead of using the built-in preconfigured data. |
| `WEB_PORT`, `CATALOG_API_PORT`, `CATALOG_GRPC_PORT`, `SQLSERVER_PORT` | 8080 / 8081 / 8082 / 1433 | Host port bindings. |

Each app service additionally receives, from the compose file:

- `ConnectionStrings__Catalog` — `Server=sqlserver,1433;Database=…;User Id=sa;Password=…;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True`
  (the dev SQL Server presents a self-signed certificate).
- `Catalog__UseMockData=false` — the containers always run against the real database.
- `ASPNETCORE_URLS=http://+:8080` — baked into the images.

## Image layout

All three Dockerfiles follow the same shape and are built from the **solution root**
(`eShopModernized/`) as context, because `Directory.Build.props`, `Directory.Packages.props` and
`BannedSymbols.txt` sit there and central package management needs them at restore time:

```bash
docker build -f src/eShop.Catalog.Api/Dockerfile -t eshop-modernized/catalog-api:local .
```

1. `mcr.microsoft.com/dotnet/sdk:8.0` build stage: the MSBuild props and the `.csproj` files are
   copied first and `dotnet restore` runs on that alone, so editing source does not invalidate the
   restore layer; then the sources are copied and `dotnet publish -c Release --no-restore` runs.
2. `mcr.microsoft.com/dotnet/aspnet:8.0` runtime stage: only the publish output is copied, so no
   SDK ends up in the shipped image. It runs as the image's non-root `app` user (uid 1654) and
   listens on port 8080.

`/app/Pics` and `/app/logs` are chowned to `app` before the user switch: the picture seeding
deletes and re-extracts the pictures folder, and Serilog's file sink writes under `logs/`.

The catalog item pictures have a single owner: the host that serves `GET /items/{id}/pic` ships its
own `Pics/` folder (`eShop.Catalog.Api`, `eShop.Web`). `eShop.Catalog.Data` contributes the seeding
*input* (`Setup/`) only. Until NET-72 it also linked byte-identical copies of the pictures in, which
put two sources on the same `Pics/<n>.png` publish path and made `dotnet publish` fail with
`NETSDK1152`; the publish now succeeds with no `ErrorOnDuplicatePublishOutputFiles` override.

### Health checks

Every image declares a `HEALTHCHECK` against `/health` (the liveness probe from
`eShop.Shared.HealthChecks`), which is what `docker compose up --wait` and the `depends_on`
conditions observe. `/ready` additionally covers the catalog database and is available on the same
ports for readiness gating.

The SQL Server container has a compose-level health check that runs
`sqlcmd -Q 'SELECT 1'`, and the three app services declare
`depends_on: { sqlserver: { condition: service_healthy } }`, so they only start once the server
answers queries.

## Startup and seeding order

Only `catalog-api` migrates and seeds: `AddCatalogSeeding()` is wired into that host alone. It runs
`Database.MigrateAsync()` and the catalog seeder in a hosted service **before** Kestrel starts
serving, so the API reports healthy only once the database is ready — hence its longer
`--start-period`.

`catalog-grpc` and `web` start in parallel with that work and tolerate an unseeded database: their
`/health` endpoint is process-level, and neither runs migrations. A gRPC call issued in the seconds
before seeding completes can therefore see an empty or missing catalog; retry, or gate on
`curl http://localhost:8081/ready`.

Seed content matches the behavioral baseline: five brands (`Azure`, `.NET`, `Visual Studio`,
`SQL Server`, `Other`), the catalog types, and the catalog items with their pictures served from
`/items/{id}/pic`.

## Troubleshooting

- **`docker compose up` exits with an options validation error** — `ConnectionStrings__Catalog` is
  empty. It is set by compose; if you run an image by hand, pass it explicitly.
- **SQL Server never becomes healthy** — the SA password must satisfy the SQL Server complexity
  policy (8+ characters, three of upper/lower/digit/symbol). Check `docker compose logs sqlserver`.
- **`curl http://localhost:8082/health` hangs or fails** — that endpoint is HTTP/2 only; add
  `--http2-prior-knowledge`.
- **Stale data after changing the seed assets** — the seeder skips tables that already hold rows.
  `docker compose down -v` drops the volume so the next start reseeds.
