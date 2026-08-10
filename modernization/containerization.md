# Containerization (NET-71)

The three modernized hosts run as Linux containers, with SQL Server 2022 as the catalog database.
Nothing about the containers is Windows-specific: the legacy IIS/LocalDB hosting model is gone, all
configuration comes from environment variables, and every image runs as a non-root user.

| Service | Image | Container port | Default host port | Protocol |
| --- | --- | --- | --- | --- |
| `web` | `eshop/web` (`src/eShop.Web/Dockerfile`) | 8080 | 8080 | HTTP/1.1 |
| `api` | `eshop/catalog-api` (`src/eShop.Catalog.Api/Dockerfile`) | 8080 | 8081 | HTTP/1.1 |
| `grpc` | `eshop/catalog-grpc` (`src/eShop.Catalog.Grpc/Dockerfile`) | 8080 | 8082 | **h2c (HTTP/2 only)** |
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | 1433 | TDS |

## Quick start (database mode — the default)

```bash
cp .env.example .env          # then set MSSQL_SA_PASSWORD to a strong value
docker compose up -d          # builds the images on first run
docker compose ps             # all four services report (healthy)
```

`docker compose up` starts SQL Server, waits for its health check, then starts the API, which
applies the EF Core migrations and seeds the catalog (NET-65 initializer); `grpc` and `web` start
after the API so exactly one process ever writes the schema. Note that `/health` is a liveness
probe, so the ordering is a hint rather than a guarantee that seeding has completed — the other two
hosts only read, and an empty catalog renders as an empty list rather than an error.

Endpoints once the stack is up:

- catalog UI: <http://localhost:8080/>
- API: <http://localhost:8081/api/brands>, `/api/files`, `/items/{id}/pic`, Swagger at `/swagger`
- gRPC: `grpcurl -plaintext localhost:8082 list`
- health: `/health` (liveness) and `/ready` (readiness) on every service

Tear down with `docker compose down`, or `docker compose down -v` to also drop the database volume
(`sqlserver-data`) so the next boot re-creates and re-seeds the catalog from scratch.

## Mock-data mode (no database)

The fast path for demos and CI smoke tests: the hosts serve the in-memory catalog (the legacy
`UseMockData=true` app setting) and no SQL Server container is started.

```bash
docker compose -f docker-compose.yml -f docker-compose.mock.yml up -d api grpc web
```

Naming the three services keeps `sqlserver` out of the run; the override sets
`Catalog__UseMockData=true` and drops the `depends_on` edges. `scripts/compose-smoke.sh` runs this
mode end to end (up → health → API + gRPC assertions → down).

`docker-compose.yml` still interpolates `MSSQL_SA_PASSWORD`, so mock mode needs *some* value even
though nothing uses it: keep a `.env` around, or prefix the command with
`MSSQL_SA_PASSWORD=unused-in-mock-mode` (which is what the smoke script does).

## Configuration

Everything is supplied through environment variables — no connection string, password or LocalDB
path is baked into an image. `.env.example` documents the compose-level variables; copy it to
`.env` (git-ignored) and edit. The application-level variables the containers set are:

| Variable | Meaning |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` (default) or `Development` |
| `ASPNETCORE_HTTP_PORTS` | Kestrel port inside the container; `8080` in all three images |
| `Catalog__UseMockData` | `false` in database mode, `true` in mock mode |
| `Catalog__InitializeDatabaseOnStartup` | `true` on the API only — one process owns migrate + seed |
| `Catalog__UseCustomizationData` | seed from `Setup/*.csv` and extract `Setup/CatalogItems.zip` |
| `ConnectionStrings__Catalog` | catalog connection string (the legacy `ConnectionString` variable is still honoured as a fallback) |
| `CatalogWeb__PicturesBaseUrl` | address the browser uses for item pictures; must be the **published** API address (`http://localhost:8081` by default), not the compose service name |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | optional; enables the Azure Monitor OpenTelemetry exporter (NET-62) |

The default connection string is
`Server=sqlserver,1433;Database=CatalogDb;User Id=sa;Password=***;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True`.
`TrustServerCertificate=True` is required because the SQL Server container uses a self-signed
certificate; point `CATALOG_CONNECTION_STRING` at a managed instance (and a least-privilege login)
for anything beyond local development.

## Image design

- **Multi-stage.** `mcr.microsoft.com/dotnet/sdk:8.0` builds and publishes; only the publish output
  is copied into `mcr.microsoft.com/dotnet/aspnet:8.0`. The SDK, the NuGet cache and the sources
  never reach the runtime image.
- **Build context is the repository root** (the images need `Directory.Build.props`,
  `Directory.Packages.props`, `BannedSymbols.txt` and `global.json`), so always build with
  `docker build -f src/<project>/Dockerfile .`. `.dockerignore` keeps `bin/`, `obj/`, `.git/`, the
  tests and the legacy .NET Framework solutions out of the context.
- **Layer-cached restore.** The project and props files are copied first and `dotnet restore` runs
  against them, so editing source code re-uses the restore layer.
- **Non-root.** The final stage switches to `$APP_UID` (uid 1654, the `app` user shipped in the
  .NET 8 images). Nothing in the containers writes to disk: Serilog logs compact JSON to stdout and
  the file sink is off by default.
- **HEALTHCHECK.** Every image probes its own `/health`, which is what gates `depends_on:
  condition: service_healthy` in compose. The gRPC image adds `--http2-prior-knowledge` because its
  Kestrel endpoint is HTTP/2 only. `curl` is installed in the runtime stage for this probe and is
  the only package added to the base image.

## Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `set MSSQL_SA_PASSWORD in .env` on `docker compose up` | no `.env` (or an empty password). `cp .env.example .env` and set a password that satisfies the SQL Server policy: ≥ 8 characters with upper case, lower case, digits and symbols. |
| `sqlserver` never becomes healthy, log says the password is not strong enough | same policy failure; SQL Server exits during first-time setup. Fix the password and `docker compose down -v` to discard the half-initialised volume. |
| API is unhealthy, log shows `A network-related or instance-specific error` | the API was started without SQL Server (`docker compose up api` alone). Bring the whole file up, or use the mock override. |
| `curl http://localhost:8082/health` hangs or returns nothing | the gRPC host is HTTP/2 only; use `curl --http2-prior-knowledge` (or `grpcurl -plaintext`). |
| Catalog UI renders but item images are broken | `CatalogWeb__PicturesBaseUrl` must be reachable **from the browser**. Set `CATALOG_API_PUBLIC_URL` to the address the API is published on. |
| Catalog looks stale after changing the seed data | the data is only seeded into an empty table. `docker compose down -v` drops the volume so the initializer re-seeds. |
| Port already allocated | override `WEB_PORT` / `API_PORT` / `GRPC_PORT` / `SQLSERVER_PORT` in `.env`. |
| SQL Server refuses to start on arm64 | `mcr.microsoft.com/mssql/server:2022-latest` is amd64-only; run mock mode, or use an external SQL Server via `CATALOG_CONNECTION_STRING`. |

## Verified

Both modes were brought up and exercised for real on Linux (Docker 27.4.1, Compose v2.32.1):
all four services reach `(healthy)`, `/health` returns `{"status":"Healthy"}` on Web, API and gRPC,
the API answers `/api/brands`, `/api/brands/{id}` (404 on an unknown id), `/api/files` and
`/items/1/pic` (`200 image/png`), `grpcurl` lists and calls all 10 catalog operations, and the
freshly created `CatalogDb` holds 4 catalog types, 5 brands and 12 items.
