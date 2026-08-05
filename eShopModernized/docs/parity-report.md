# Parity report — legacy baseline vs. the modernized estate

**Ticket:** NET-73 (I-14, Phase 4 — the parity gate) · **Epic:** NET-51 · **Verdict: PASS.**

Every row of the golden runtime baseline (Confluence *.NET Behavioral Baseline* §6, captured
2026-07-27 against `eShopLegacyMVC` in mock-data mode) was replayed against the **containerized**
modernized stack. 85 automated checks pass, 0 fail. There are **no unexplained mismatches**: every
difference is one of the eight accepted deltas listed in §5, all of them agreed earlier in the
programme. The ten SOAP/WCF operations have no golden output — the legacy service could never be
run at discovery — so their gRPC replacements are **contract-verified** instead (§4).

One defect was found and fixed by this ticket: the two Web Forms pager redirects that NET-70 §4.3
specified as a follow-up had never been implemented, so `/Default/index/{index}/size/{size}` and
`/Default` returned 404. They now return 301 (§6).

---

## 1. Environment

| | |
| --- | --- |
| Host | Ubuntu 22.04 x86-64, Docker 27.4.1, Docker Compose v2.32.1 |
| Runtime | `mcr.microsoft.com/dotnet/aspnet:8.0` (build stage `sdk:8.0`), .NET 8 (LTS) |
| Database | `mcr.microsoft.com/mssql/server:2022-latest` — SQL Server 2022 (RTM-CU26) 16.0.4265.3, one consolidated `CatalogDb` |
| Services | `web` :8080 · `catalog-api` :8081 · `catalog-grpc` :8082 (h2c) · `sqlserver` :1433 |
| Data mode | `Catalog__UseMockData=false` — the real EF Core 8 path against SQL Server, migrated and seeded by `eShop.Catalog.Api` |
| Tools | `curl` 7.81.0, `grpcurl` v1.9.1 |
| Legacy oracle | `eShopLegacyMVC` on .NET Framework 4.7.2, mock-data mode, port 8080 (golden outputs captured 2026-07-27) |

Note the topology difference that is *not* a behavioural delta: the legacy application was a single
IIS process serving the MVC UI, the Web API endpoints and the picture route together. The
modernized estate splits them across `eShop.Web` (:8080) and `eShop.Catalog.Api` (:8081). The
`/items/{id}/pic` route is deliberately served by **both**, so the UI renders thumbnails with only
`eShop.Web` running, exactly as the legacy single-process app did. Every URL path below is
unchanged; only the origin differs, which is a deployment concern for the reverse proxy at cutover.

## 2. How to reproduce

```bash
cd eShopModernized

docker compose build
docker compose down -v            # start from an empty volume so the seed set is pristine
docker compose up -d --wait       # blocks until all four containers report healthy

./tests/parity/replay-golden-baseline.sh    # exits 0 only when every check matches
```

The script needs `curl` and `grpcurl` on `PATH`. It replays §6.1 (read paths), the retired Web
Forms pager URLs, the ten gRPC operations and §6.2 (the CRUD round-trip, which mutates the
catalog), printing one `PASS`/`FAIL` line per check with the verdict class in the second column.
Run it against a freshly seeded stack; `docker compose down -v` afterwards.

The in-process equivalents live in the xunit suites and run in CI without Docker:
`tests/eShop.Web.Tests/Integration/CatalogGoldenRouteTests.cs` (§6.1 MVC rows),
`CatalogCrudRoundTripTests.cs` (§6.2), `WebFormsPagerRedirectTests.cs` (§6, the redirects),
`tests/eShop.Catalog.Api.Tests` (the REST rows) and `tests/eShop.Catalog.Grpc.Tests` (the ten RPCs).

## 3. Parity table

Verdict values: **Yes** — reproduced · **Intentional change** — an accepted, explained difference ·
**Contract-verified** — no golden output exists, validated against the documented contract.

### 3.1 Baseline §6.1 — endpoint → status / response

| Endpoint/Operation | Legacy (baseline) | Modernized | Match? |
| --- | --- | --- | --- |
| `GET /Catalog` | 200, `text/html`, 14,402 B, `<title>Index - Catalog manager (MVC)</title>`, 10 rows, `items/{id}/pic` image links | 200, `text/html; charset=utf-8`, 14,560 B, same title, 10 rows, same image links | **Yes** (bytes differ — C-09, §5.3) |
| `GET /Catalog/Index` | 200, byte-identical to `/Catalog` | 200, byte-identical to `/Catalog` (md5 equal) | **Yes** |
| `GET /` | 200, catalog list (legacy `RouteConfig` default) | 200, byte-identical to `/Catalog` | **Yes** |
| `GET /Catalog/Index?pageSize=2&pageIndex=1` | 200, 5,377 B, 2 rows, second page | 200, 5,322 B, 2 rows, "Showing 2 of 12 products - Page 2 - 6" | **Yes** (bytes — C-09) |
| `GET /Catalog/Details/1` | 200, 3,220 B detail view | 200, 3,078 B, `.NET Bot Black Hoodie`, `$19.50`, `items/1/pic` | **Yes** (bytes — C-09) |
| `GET /Catalog/Details/999` | 404, unknown id | 404, empty body | **Yes** |
| `GET /Catalog/Details` (no id) | 400 (`HttpStatusCodeResult(BadRequest)`) | 400, empty body | **Yes** |
| `GET /Catalog/Create` | 200, 7,830 B, `__RequestVerificationToken` hidden field | 200, 7,710 B, anti-forgery field present, brand/type `<select>` lists populated | **Yes** (bytes — C-09) |
| `GET /Catalog/Edit/1` | 200, 9,032 B prefilled form | 200, 8,995 B, `value=".NET Bot Black Hoodie"`, `.NET` brand preselected | **Yes** (bytes — C-09) |
| `GET /Catalog/Delete/1` | 200, 3,639 B confirmation view | 200, 3,620 B, "Are you sure you want to delete this?" + anti-forgery field | **Yes** (bytes — C-09) |
| `GET /items/1/pic` | 200, `image/png`, 151,640 B from `~/Pics/1.png` | 200, `image/png`, **151,640 B** — byte-identical, served from `CatalogOptions.PicsFolder` | **Yes** |
| `GET /items/0/pic` | 400, non-positive id rejected | 400, empty body | **Yes** |
| `GET /api/brands` | 200, `[{"Id":1,"Brand":"Azure"},…,{"Id":5,"Brand":"Other"}]` | 200, byte-identical JSON (PascalCase preserved) | **Yes** |
| `GET /api/brands/1` | 200, `{"Id":1,"Brand":"Azure"}` | 200, identical | **Yes** |
| `GET /api/brands/99` | 404, empty body | 404, empty body (0 bytes) | **Yes** |
| `GET /api/files` | 200, 719 B **BinaryFormatter** payload served as `text/html`, `Cache-Control: no-cache` | 200, `application/json`, `BrandDTO[]` — the same five brands | **Intentional change** (§5.1, C-07) |
| `DELETE /api/brands/{id}` | success, but a no-op (nothing deleted) | **405** — the endpoint does not exist | **Intentional change** (§5.2) |
| `GET /api` | 404 — unreachable `[Route("api")]` dead code | 404 — the dead action was deleted | **Yes** |
| `GET /Content/site.css` | 404 — IIS served static files outside the managed pipeline (harness artefact) | n/a — bundling is gone; `GET /css/site.css` → 200 `text/css` from `wwwroot` | **Intentional change** (§5.5) |
| `GET /nope` | 404 default not-found | 404 | **Yes** |

### 3.2 Baseline §6.2 — write path (CRUD round-trip)

| Step | Legacy (baseline) | Modernized | Match? |
| --- | --- | --- | --- |
| `POST /Catalog/Create` with a valid `__RequestVerificationToken` + cookie | 302, `Location: /`; "Smoke Test Item" then in `GET /Catalog/Index?pageSize=20` | 302, `Location: /`; the item is in `GET /Catalog/Index?pageSize=50` | **Yes** |
| `POST /Catalog/Create` **without** the anti-forgery cookie | 500 `System.Web.Mvc.HttpAntiForgeryException` — write refused | **400** — write refused, item absent from the list afterwards | **Intentional change** (§5.4) |
| `POST /Catalog/Edit` (id=1, `Name="Renamed Hoodie"`) | 302, `Location: /`; `GET /Catalog/Details/1` renders "Renamed Hoodie" | 302, `Location: /`; `Details/1` renders "Renamed Hoodie" | **Yes** |
| `POST /Catalog/Delete` (id=1) | 302, `Location: /`; `GET /Catalog/Details/1` then 404 | 302, `Location: /`; `Details/1` then 404 | **Yes** |

### 3.3 Baseline §6.1 — seed data (C-04)

| Assertion | Legacy (baseline) | Modernized | Match? |
| --- | --- | --- | --- |
| Item count | 12 catalog items, 5 brands, 4 types | 12 / 5 / 4 in `CatalogDb` | **Yes** |
| Brands | `Azure`, `.NET`, `Visual Studio`, `SQL Server`, `Other` (ids 1–5) | identical, same ids and order | **Yes** |
| Item 1 picture | `1.png` (MVC seed set) | `1.png` | **Yes** (C-04: the MVC seed set is canonical) |
| Item 12 name | `Prism White TShirt` (MVC seed set) | `Prism White TShirt` | **Yes** (C-04) |
| Ordering | `OrderBy(Id)` | `OrderBy(Id)` preserved deliberately in `CatalogService` | **Yes** |

### 3.4 Retired front ends (D-06 / D-07)

| Legacy surface | Legacy (baseline) | Modernized | Match? |
| --- | --- | --- | --- |
| Web Forms catalog routes (`/`, `/Catalog/{Create,Edit,Details,Delete}`) | **No golden output** — the app needs Windows + IIS; static-only at discovery | Retired (D-06); the six routes map onto the MVC UI, which *is* runtime-verified above | **Intentional change** — see `docs/webforms-retirement.md` §7 |
| `GET /Default/index/{index}/size/{size}` (Web Forms pager) | Path-segment pagination, linked on every catalog page | **301** → `/Catalog/Index?pageIndex={index}&pageSize={size}` (implemented by this ticket, §6) | **Yes** |
| `GET /Default` | Web Forms catalog list | **301** → `/` | **Yes** |
| `/Default/index/abc/size/xyz` | n/a | 404 — segments are `int`-constrained, so bad input falls through | **Yes** |
| `/About`, `/Contact`, mobile `ViewSwitcher`, `/Pics/{file}` | Static template boilerplate / dead code / internal asset paths | Retired, no replacement, no redirect | **Intentional change** — `docs/webforms-retirement.md` §4.1, §4.2, §4.4 |
| WinForms desktop client (Main Catalog + Inventory tabs) | **No golden output** — Windows-only, never runtime-verified | Retired (D-07); replaced by the MVC UI plus the cross-platform gRPC CLI (`samples/eShop.Catalog.GrpcClient`) | **Intentional change** — `docs/winforms-retirement.md` |
| Discount banner, stock look-up, shipment entry | WinForms only | CLI verbs `get-discount`, `get-stock`, `create-stock`, `catalog`, `inventory` — **no browser screen** | **Intentional change** — C-14 / gap G-1 (§5.7) |

## 4. Contract-verified: the ten SOAP/WCF operations

The legacy WCF service (`eShopLegacyNTier/src/eShopWCFService`) could not be run at discovery
(Windows/IIS + LocalDB), so **no golden SOAP output exists**. Each operation is therefore validated
against the documented contract in `src/eShop.Catalog.Grpc/Protos/catalog.proto`, which records the
legacy signature and the D-04 type mapping for every RPC. All ten are exposed over server
reflection and were exercised with `grpcurl` against the container on :8082.

| # | Legacy WCF operation | gRPC RPC | Contract check | Verdict |
| --- | --- | --- | --- | --- |
| 1 | `CatalogItem FindCatalogItem(int)` | `FindCatalogItem` | `id=1` → the item with nested `catalog_type`/`catalog_brand`; `id=999` → `NOT_FOUND` (legacy returned `null`, D-04) | **Contract-verified** ⚠ C-08 |
| 2 | `List<CatalogBrand> GetCatalogBrands()` | `GetCatalogBrands` | 5 brands, ids 1–5, same order as `/api/brands` | **Contract-verified** |
| 3 | `List<CatalogItem> GetCatalogItems(int,int)` | `GetCatalogItems` | `(0,0)` → all 12 items (0 = "no filter", as in the legacy implementation); `(2,0)` → only `catalog_brand_id == 2` | **Contract-verified** |
| 4 | `List<CatalogType> GetCatalogTypes()` | `GetCatalogTypes` | 4 types (`Mug`, `T-Shirt`, `Sheet`, `USB Memory Stick`) | **Contract-verified** |
| 5 | `int GetAvailableStock(DateTime,int)` | `GetAvailableStock` | no row → `0` (legacy returned 0, not an error); after a shipment → `42` | **Contract-verified** |
| 6 | `void CreateAvailableStock(CatalogItemsStock)` | `CreateAvailableStock` | returns `google.protobuf.Empty`; the value is readable back through `GetAvailableStock` | **Contract-verified** |
| 7 | `void CreateCatalogItem(CatalogItem)` | `CreateCatalogItem` | returns `Empty`; the item exists afterwards | **Contract-verified** ⚠ C-08 |
| 8 | `void UpdateCatalogItem(CatalogItem)` | `UpdateCatalogItem` | returns `Empty`; `FindCatalogItem` shows the new name | **Contract-verified** ⚠ C-08 |
| 9 | `void RemoveCatalogItem(CatalogItem)` | `RemoveCatalogItem` | returns `Empty`; `FindCatalogItem` then `NOT_FOUND` | **Contract-verified** ⚠ C-08 |
| 10 | `DiscountItem GetDiscount(DateTime)` | `GetDiscount` | nothing running → `NOT_FOUND` (legacy returned `null`, D-04) | **Contract-verified** |

**Contradiction C-08 — no parity evidence at all for four operations.** `FindCatalogItem`,
`CreateCatalogItem`, `UpdateCatalogItem` and `RemoveCatalogItem` are on the WCF contract but the
only SOAP consumer in the estate — the WinForms client — never called them. They therefore have
neither a golden output *nor* an observed consumer behaviour to compare against; the checks above
verify the shape and the side effect, nothing more. They are carried forward on the contract alone.

Type mappings validated on the wire (D-04): `System.Decimal` → `DecimalValue` (invariant-culture
string, `"19.50"` round-trips exactly), `System.DateTime` → `google.protobuf.Timestamp`, `void` →
`Empty`, `null` → `StatusCode.NOT_FOUND`, and the `Picturefilename` → `picture_file_name` spelling
fix from the WCF `[DataContract]` to the canonical domain entity.

## 5. Accepted-delta register

All eight were agreed earlier in the programme; they are recorded here, not re-litigated.

### 5.1 `GET /api/files`: BinaryFormatter stream → `application/json`

Legacy: 719 bytes of `BinaryFormatter` payload (starting `00 01 00 00 00 FF FF FF FF …
"eShopLegacyMVC, Version=1…"`) served as `text/html`. Modernized: `application/json` with the
`BrandDTO[]` shape — the same five brands, byte-identical to `GET /api/brands`.

*Justification:* `BinaryFormatter` is removed under NET-63 (risk R3) and banned by an analyzer;
it is unsupported and remotely exploitable in .NET 8. **Residual risk (C-07):** the endpoint's
consumer cannot be identified from source, so an out-of-repo consumer that deserializes the binary
stream cannot be excluded. Any such consumer breaks at cutover and must move to JSON.

### 5.2 `DELETE /api/brands/{id}`: no-op success → 405

The legacy Web API action returned success without deleting anything. The modernized API does not
define the verb, so the route answers **405 Method Not Allowed**. Removing a lying endpoint is
preferable to reproducing it; no caller in the repository invokes it.

### 5.3 C-09 — rendered byte counts differ site-wide

The legacy `_Layout.cshtml` rendered the InProc session values `MachineName` and
`SessionStartTime` into the footer of **every** page. D-05 drops InProc session state, so that
line is gone and every golden byte count is unreproducible by design. Parity is asserted on
**structure and semantics** — status code, title, row count, image-link shape, anti-forgery field,
prefilled values, links — never on `Content-Length`. The observed deltas (−37 B to +158 B per page)
are consistent with removing one footer line and the small markup differences of Tag Helpers.

### 5.4 POST without an anti-forgery token: 500 → 400

Legacy: `System.Web.Mvc.HttpAntiForgeryException` surfaced as a 500. Modernized: ASP.NET Core's
`[ValidateAntiForgeryToken]` filter answers 400. **The write is refused either way** — verified by
re-reading the list afterwards — and 400 is the correct class for a malformed request. CSRF
protection is active in both.

### 5.5 `GET /Content/site.css`: golden 404 → static files served with 200

The golden 404 was an artefact of the capture harness: on Windows, IIS served static files outside
the managed pipeline, and the Linux harness had no IIS. It was never an application rule. ASP.NET
Core serves `wwwroot` through `UseStaticFiles()` on every platform, so `GET /css/site.css` returns
200 `text/css`. `System.Web.Optimization` bundling is replaced by plain static assets (D-05), so
the `/Content/…` and `/bundles/…` paths no longer exist.

### 5.6 C-04 — the MVC seed set is canonical

The three legacy applications shipped slightly different seed data. The MVC set wins: item 1 keeps
`1.png` and item 12 is `Prism White TShirt` (not `Prism White T-Shirt`, which is item 3). The
consolidated `CatalogDb` replaces both
`Microsoft.eShopOnContainers.Services.CatalogDb` and `eShopDatabase`.

### 5.7 D-06 / D-07 / C-14 — Web Forms and WinForms are retired

The Web Forms UI is retired (D-06) — its six catalog routes are functionally equivalent to the MVC
UI, which carries the golden outputs; `/About`, `/Contact`, the dead mobile `ViewSwitcher` and the
`/Pics/{file}` URL shape are dropped without replacement
(`docs/webforms-retirement.md` §4). The WinForms client is retired (D-07); its workflow moves to
the web UI plus the gRPC CLI (`docs/winforms-retirement.md`).

**C-14 is a functional reduction, not parity:** the stock and discount workflows (`GetDiscount`,
`GetAvailableStock`, `CreateAvailableStock`) survive **only** in the CLI. Operators who used the
desktop Inventory tab have no browser screen — a command line replaces a GUI. Gap G-1 tracks this;
building a web screen for it is new scope.

### 5.8 D-01 — log lines and telemetry are not reproduced verbatim

log4net's per-request `Now loading... /X` lines and the Application Insights pipeline are replaced
by Serilog JSON + OpenTelemetry. The messages carry the same information but not the same text, and
`/health` + `/ready` are new. No golden output covers logging, so this is recorded for completeness.

## 6. Defect found and fixed by this ticket

`docs/webforms-retirement.md` §4.3 and §8 required `eShop.Web` to answer the two Web Forms pager
URLs with permanent redirects, and assigned the work to NET-69 or NET-73. NET-69 did not implement
them: `GET /Default/index/1/size/2` and `GET /Default` both returned **404** on the containerized
stack.

Because these are real, reachable, *linked* URLs — `Default.aspx.cs:50,54` generated them for the
Previous/Next pager on every catalog page, so they are exactly what a user bookmarks or a synthetic
monitor records — they are implemented rather than retired. Two endpoints were added to
`src/eShop.Web/Program.cs`:

```csharp
app.MapGet("/Default/index/{index:int}/size/{size:int}",
    (int index, int size) => Results.Redirect(
        FormattableString.Invariant($"/Catalog/Index?pageIndex={index}&pageSize={size}"),
        permanent: true));
app.MapGet("/Default", () => Results.Redirect("/", permanent: true));
```

`{index}`/`{size}` map one-for-one onto `pageIndex`/`pageSize`, both are `int`-constrained so bad
input falls through to the normal 404, and `docs/webforms-retirement.md` §8 is now closed.
Regression coverage: `tests/eShop.Web.Tests/Integration/WebFormsPagerRedirectTests.cs` (4 tests).

This is the only application-code change made by the parity gate.

## 7. Open risks

| ID | Risk | Status |
| --- | --- | --- |
| **R9** | **EF6 runtime behaviour was never verified.** The golden baseline was captured with `UseMockData=true`, so the legacy EF6 query, HiLo and seeding behaviour is *not* part of the parity oracle. The modernized data layer is therefore validated by the EF Core 8 test suite (66 tests in `eShop.Catalog.Data.Tests`, covering the migration, the seeder, HiLo allocation and the query shapes) and by the end-to-end replay above running against real SQL Server — **not** by a diff against a running legacy EF6 instance. | Open, accepted |
| **C-07** | An out-of-repo consumer of the BinaryFormatter `/api/files` payload cannot be excluded from source. | Open, accepted (§5.1) |
| **C-08** | Four gRPC RPCs have no consumer and therefore no parity evidence at all. | Open, accepted (§4) |
| **C-14 / G-1** | No browser UI for discounts, stock look-up or shipments; the CLI is the only surface. | Open — product decision |
| **G-2** | The seeder writes no `DiscountItems` and no `CatalogItemsStock` rows, so `GetDiscount` answers `NOT_FOUND` on a fresh database. Matches the golden baseline (which had none either), but means the discount path is only exercised with hand-inserted data. | Open, low |
| **—** | Web Forms and WinForms have no golden output at all, on any platform. Their parity claim rests on the MVC golden outputs plus the contract checks above, not on a recapture. Recapturing them on Windows was ruled out by D-06/D-07 (both are retired). | Closed by decision |
| **—** | The legacy solutions (`eShopLegacyMVCSolution/`, `eShopLegacyWebFormsSolution/`, `eShopLegacyNTier/`) are still on `main`. Deleting them is a cutover action, not a parity action. | Open — cutover |

## 8. Gate results

| Gate | Result |
| --- | --- |
| `dotnet build eShop.sln -c Release` | Build succeeded — **0 warnings, 0 errors** |
| `dotnet test eShop.sln -c Release` | **333 passed, 0 failed, 0 skipped** (Domain 18, Shared 12, Data 66, Api 29, Grpc 50, GrpcClient 81, Web 77) |
| `dotnet format --verify-no-changes` | clean |
| `dotnet list package --vulnerable` | no vulnerable packages |
| `docker compose up -d --wait` | all four containers healthy |
| `./tests/parity/replay-golden-baseline.sh` | **85 passed, 0 failed** |
