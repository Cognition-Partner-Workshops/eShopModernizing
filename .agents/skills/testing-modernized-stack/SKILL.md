---
name: testing-modernized-stack
description: How to run and browser-test the modernized .NET 8 eShop stack (web/api/grpc) on Linux via Docker Compose, including the parity gate, mock-data mode and the retired Web Forms redirects.
---

# Testing the modernized eShop stack (.NET 8, Linux containers)

The legacy solutions are Windows/.NET Framework, but the **modernized** projects (`src/eShop.Web`,
`src/eShop.Catalog.Api`, `src/eShop.Catalog.Grpc`) run fine on a Linux box through Docker Compose.
Prefer this over trying to build/run the app directly when doing end-to-end UI testing.

## Bring the stack up (mock-data mode — no SQL Server, fast)

```bash
cd <repo root>
MSSQL_SA_PASSWORD=unused docker compose -f docker-compose.yml -f docker-compose.mock.yml \
  up -d --build api grpc web
```

- `MSSQL_SA_PASSWORD` must be set to *anything* even in mock mode — the base compose file
  interpolates it with `:?` and the command fails otherwise.
- Ports: web `http://localhost:8080`, api `http://localhost:8081`, grpc `localhost:8082` (h2c —
  clients need prior knowledge: `grpcurl -plaintext`, `curl --http2-prior-knowledge`).
- The first build takes several minutes; subsequent builds are cached. Wait for
  `docker ps` to show all three containers `(healthy)` before driving the UI.
- Mock mode seeds 12 catalog items, 5 brands, 4 types in memory. State is per-container, so any
  create/edit/delete you do persists until you recreate the containers.
- Tear down with `MSSQL_SA_PASSWORD=unused docker compose -f docker-compose.yml -f docker-compose.mock.yml down --remove-orphans`.

## Parity gate

`scripts/parity-gate.sh` (needs docker, curl and `grpcurl`, which lives at `/usr/local/bin/grpcurl`
on the Devin box) brings the mock stack up itself, replays the behavioral baseline and tears the
stack down on exit unless `KEEP_UP=1`. Use `--no-compose` against an already-running stack, but
note the gate **mutates catalog state** (it deletes item 1), so re-runs need a fresh stack.
Set `MSSQL_SA_PASSWORD` before invoking it. A healthy run prints
`parity gate: N matched, M intentional/accepted differences, 0 mismatches` and exits 0.

## Driving the catalog UI

- Root `/` is the catalog index (`{controller=Catalog}/{action=Index}/{id?}`); `/Catalog` and
  `/Catalog/Index` are the same page.
- Thumbnails are `<img>` tags pointing at the **API's** public URL
  (`CatalogWeb__PicturesBaseUrl`, default `http://localhost:8081`) — i.e.
  `http://localhost:8081/items/{id}/pic`. If thumbnails come up broken but the DOM has `<img>`
  tags, suspect this env var rather than the view. Always confirm images with a *screenshot*, not
  the DOM.
- Newly created items get `dummy.png` and therefore a legitimately broken thumbnail — the API only
  has pictures for the 12 seeded ids. Don't report that as a bug.
- CRUD path: "Create New" button → form (Name, Description, Brand/Type selects, Price, Stock,
  Restock, Max stock) → `[ Create ]`; per-row `Edit | Details | Delete` links; each POST redirects
  to `/`. Pager reads `Showing {pageSize} of {total} products - Page {n} - {pages}` with
  Previous/Next links.
- Retired Web Forms aliases (`src/eShop.Web/Program.cs`): `GET /Default` → 301 `/Catalog/Index`,
  `GET /Default/index/{pageIndex}/size/{pageSize}` → 301
  `/Catalog/Index?pageIndex=…&pageSize=…`. These are `MapGet`, so a **HEAD** request (`curl -I`)
  returns `405 Method Not Allowed`, not 301 — use `curl -s -o /dev/null -D -` to inspect the
  redirect headers.
- Unknown ids: `/Catalog/Details/999` → bare HTTP 404 (Chrome's own error page); missing id →
  400. `/Home/Error` renders the plain "Error." view.

## Building / unit tests

Use `~/.dotnet/dotnet` — the `dotnet` on PATH may be 10.x while `global.json` pins 8.0.x.

## Devin Secrets Needed

None — everything above runs locally with no credentials.
