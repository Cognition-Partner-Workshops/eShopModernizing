# Parity gate (NET-73)

The acceptance gate for the .NET modernization programme. Every golden output recorded in the
Confluence page **".NET Behavioral Baseline"** (space `NET`, page `58687519`, sections 2.1 and 6) is
replayed against the modernized stack and compared field by field.

Re-run it at any time:

```bash
scripts/parity-gate.sh                                   # builds + starts the mock-data compose stack, tears it down after
scripts/parity-gate.sh --no-compose                      # against an already-running stack (WEB_BASE_URL/API_BASE_URL/GRPC_ADDRESS)
scripts/parity-gate.sh --report modernization/parity-report-table.md   # regenerate the table below
```

The gate needs `docker` (unless `--no-compose`), `curl` and
[`grpcurl`](https://github.com/fullstorydev/grpcurl). It exits non-zero on any unexplained
mismatch. Because the baseline CRUD round-trip deletes catalog item 1, the gate mutates state and
expects a freshly started stack — which is what the default (compose) mode gives it.

## Result of the recorded run

| | |
| --- | --- |
| Stack | `docker compose -f docker-compose.yml -f docker-compose.mock.yml up api grpc web` (mock data, the same `UseMockData=true` mode the baseline was captured in) |
| Web UI | <http://localhost:8080> · API <http://localhost:8081> · gRPC `localhost:8082` (h2c) |
| Checks | **43 matched · 14 intentional/accepted differences · 0 unexplained mismatches** |
| Solution gate | `dotnet build eShop.sln` 0 warnings / 0 errors · `dotnet test eShop.sln` 218 passed / 0 failed |

## Per-component migration status

| Legacy component | Baseline verification | Modernized replacement | Cutover verdict |
| --- | --- | --- | --- |
| `eShopLegacyMVC` (MVC 5 UI) | Runtime-verified | `src/eShop.Web` (NET-69) | **Signed off** — every section 6.1/6.2 golden output replayed |
| `eShopLegacyMVC` Web API 2 (`api/Brands`, `api/Files`, `items/{id}/pic`) | Runtime-verified | `src/eShop.Catalog.Api` (NET-67) | **Signed off** — with the two accepted API deltas below |
| `eShopLegacy.Utilities` (`BinaryFormatter`) | Runtime-verified indirectly | `eShop.Shared.Contracts.BrandDtoSerializer` (NET-63) | **Signed off** — JSON replaces the binary payload |
| `eShopLegacyMVC.Tests` (48 MSTest) | Runtime-verified | `tests/eShop.Web.Tests` (xUnit) | **Signed off** — all 48 ported, plus golden-output tests |
| `eShopWCFService` (SOAP, 10 operations) | Static-only (needs Windows + IIS/LocalDB) | `src/eShop.Catalog.Grpc` (NET-66) | **Contract-verified** — all 10 operations exercised over gRPC against the same seed data; no SOAP recording exists to diff against |
| `eShopWinForms` | Static-only | `src/eShop.WinForms.Client` (NET-68) | **Contract-verified** — shares `catalog.proto` with the service; published as a win-x64 CI artifact |
| `eShopLegacyWebForms` | Static-only (could not be hosted) | Retired (NET-70), pages map onto the MVC UI | **Retired** — URL aliases now served by `src/eShop.Web` |
| EF6 + LocalDB | Runtime-verified indirectly | EF Core 8 + SQL Server 2022 container (NET-64/NET-65) | **Signed off** — same table shapes, HiLo sequences and seed data |

## Parity table

The generated table lives in [`parity-report-table.md`](parity-report-table.md) — one row per
baseline endpoint/operation with the legacy expectation, the observed modernized value and the
verdict. Regenerate it with `scripts/parity-gate.sh --report modernization/parity-report-table.md`.

## Accepted differences (and why)

1. **`GET /api/files` returns JSON, not a `BinaryFormatter` stream** (NET-63/NET-67). The legacy
   action serialized `BrandDTO[]` with `BinaryFormatter`, which is removed from .NET 8 and is a
   known remote-code-execution vector. The documented replacement is `BrandDto[]` as PascalCase
   JSON; the field set and values are unchanged, and `BannedApiAnalyzers` (RS0030) plus
   `scripts/check-no-binaryformatter.sh` keep `BinaryFormatter` out of the tree.
2. **`DELETE /api/brands/{id}` answers 405** (NET-67). The legacy action was explicitly a demo no-op
   that reported success without deleting anything. Rather than reproduce a lie, the verb is simply
   not implemented; the route still exists for GET, so ASP.NET Core answers `405 Method Not Allowed`.
3. **A POST without a valid anti-forgery token answers 400, not 500** (NET-69). CSRF enforcement is
   unchanged — the request is still rejected — but ASP.NET Core surfaces
   `AntiforgeryValidationException` as a 400 instead of letting it escape as a 500.
4. **Static files are served by the app** (`/css/site.css` 200). The legacy 404 for `/Content/site.css`
   was an artefact of the baseline harness: on Windows, IIS served those files outside the managed
   pipeline. `UseStaticFiles` over `wwwroot/` is the ASP.NET Core equivalent, and the bundle contents
   are the same files.
5. **The retired Web Forms URLs redirect (301) instead of rendering `.aspx` pages** (NET-70). Five of
   the seven functional Web Forms routes were already identical to the MVC ones; the two that were
   not (`/Default` and `/Default/index/{index}/size/{size}`) are now permanent redirects to
   `/Catalog/Index`, closing the open item NET-70 left behind. `/Pics/{fileName}` is replaced by
   `GET /items/{id}/pic` on the catalog API. `About.aspx`/`Contact.aspx` were unmodified project
   template placeholders and are not reproduced.
6. **gRPC returns `NOT_FOUND` where SOAP returned `null`** (`FindCatalogItem`, `GetDiscount`,
   `UpdateCatalogItem`/`RemoveCatalogItem` on an unknown id), and `INVALID_ARGUMENT` for the
   argument shapes that made the legacy service throw an opaque `FaultException`. proto3 cannot
   distinguish "absent message" from "default message", so the null-returning operations need a
   status code to stay unambiguous. `GetAvailableStock` deliberately keeps the legacy behaviour of
   returning `0` when there is no stock row for the date.
7. **`/health`, `/ready` and `/swagger` are additions** (NET-62/NET-67) with no legacy counterpart.
   They are required for container orchestration and API discovery and do not change any documented
   behaviour.
8. **Response body sizes differ by a few hundred bytes** on the HTML pages (e.g. `/Catalog` 14,397 vs
   the recorded 14,402 bytes). The rendered structure, title, row count and links are identical; the
   deltas come from Razor/ASP.NET Core markup details (anti-forgery token length, no
   `X-AspNetMvc-Version`/`ASP.NET_SessionId` headers, no `__VIEWSTATE`-era whitespace). The gate
   therefore asserts structure and values, not byte counts — except for `GET /items/1/pic`, which is
   a binary asset and *is* byte-exact (151,640 bytes).
9. **`GET /api` still 404s.** The legacy `[Route("api")]` action in `Controllers/Api/CatalogController.cs`
   was verified-unreachable dead code and was deliberately not ported; the modernized API answers 404
   for the same path, so the observable behaviour matches.

## Deliberately not verified

* **No SOAP-vs-gRPC byte diff.** The baseline could not run `eShopWCFService`, the Web Forms UI or
  the WinForms client (Windows + IIS/LocalDB only), so those three components are marked
  *contract-verified*: the gRPC operations are exercised against the same seed data and mapped
  one-to-one from the `[OperationContract]` list, but there is no recorded SOAP response to diff
  against. Promoting them to runtime-verified requires re-capturing the baseline on a Windows agent
  before the legacy N-tier solution is deleted.
* **Database mode.** The gate runs in mock-data mode, exactly as the baseline capture did, so the
  comparison is apples to apples. The EF Core 8 mapping, migrations, HiLo sequences and seeding are
  covered separately by `tests/eShop.Catalog.Data.Tests` and were verified against SQL Server 2022 in
  NET-64/NET-65; `docker compose up` exercises the same path end to end.
* **The legacy MVC/N-tier solutions are still in the repository** and still build on Windows. Deleting
  them is a follow-up decision, not part of this gate.

## Deployment

```bash
cp .env.example .env            # set MSSQL_SA_PASSWORD
docker compose up -d            # sqlserver + api (migrate/seed) + grpc + web
```

| Service | URL | Notes |
| --- | --- | --- |
| Catalog UI | <http://localhost:8080> | ASP.NET Core MVC |
| Catalog API | <http://localhost:8081> | `/api/brands`, `/api/files`, `/items/{id}/pic`, `/swagger` |
| Catalog gRPC | `localhost:8082` | h2c only — `grpcurl -plaintext localhost:8082 list` |
| SQL Server | `localhost:1433` | `mcr.microsoft.com/mssql/server:2022-latest` |

Key environment variables: `ConnectionStrings__Catalog`, `Catalog__UseMockData`,
`Catalog__InitializeDatabaseOnStartup` (API only), `Catalog__UseCustomizationData`,
`CatalogWeb__PicturesBaseUrl`, `APPLICATIONINSIGHTS_CONNECTION_STRING`. Full detail in
[`containerization.md`](containerization.md).
