# Before/after validation — legacy MVC vs. the modernized stack, both running

The parity report (`docs/parity-report.md`) replays the *recorded* golden baseline. This document
records a different exercise: both applications running at the same time, driven side by side.

| | Before | After |
| --- | --- | --- |
| App | `eShopLegacyMVC`, .NET Framework 4.7.2, `UseMockData=true` | `eShop.Web` + `eShop.Catalog.Api` + `eShop.Catalog.Grpc` on .NET 8, EF Core against SQL Server 2022 |
| Host | Mono 6.8 + a custom `System.Web` `HttpListener` host (no Windows/IIS available) | `docker compose up -d --wait`, four healthy containers |
| Origin | `http://localhost:8090` | `http://localhost:8080` (web), `:8081` (api) |

Host caveats on the "before" side, none of which are application behaviour: Mono applies no binding
redirects (the disposable copy pins MVC 5.2.7.0 in `Views/Web.config`), the Application Insights
modules cannot load, and Linux file lookup is case-sensitive, so the host resolves static files
case-insensitively the way IIS does. Mock-data mode means EF6 SQL, HiLo and database initialization
are *not* exercised on the legacy side — R9 in the parity report.

## Read paths

| Endpoint | Before: status / content-type / bytes | After: status / content-type / bytes |
| --- | --- | --- |
| `GET /Catalog` | 200 `text/html` 14,452 | 200 `text/html; charset=utf-8` 14,560 |
| `GET /Catalog/Index` | 200 `text/html` 14,452 | 200 `text/html; charset=utf-8` 14,560 |
| `GET /Catalog/Index?pageSize=5&pageIndex=1` | 200 `text/html` 8,791 | 200 `text/html; charset=utf-8` 8,760 |
| `GET /Catalog/Details/1` | 200 `text/html` 3,270 | 200 `text/html; charset=utf-8` 3,078 |
| `GET /Catalog/Details/999` | 404 | 404 |
| `GET /Catalog/Details` (no id) | 400 | 400 |
| `GET /Catalog/Create` | 200 `text/html` 7,880 | 200 `text/html; charset=utf-8` 7,710 |
| `GET /Catalog/Edit/1` | 200 `text/html` 9,082 | 200 `text/html; charset=utf-8` 8,995 |
| `GET /Catalog/Delete/1` | 200 `text/html` 3,689 | 200 `text/html; charset=utf-8` 3,620 |
| `GET /items/1/pic` | 200 `image/png` 151,640 | 200 `image/png` 151,640 (byte-identical) |
| `GET /items/0/pic` | 400 | 400 |
| `GET /items/999/pic` | 404 | 404 |
| `GET /api/brands` | 200 `application/json` 138 | 200 `application/json` 138 (byte-identical) |
| `GET /api/brands/1` | 200 `application/json` 24 | 200 `application/json` 24 (byte-identical) |
| `GET /api/brands/999` | 404 | 404 |
| `GET /api/files` | 200 `text/html` 719 (BinaryFormatter stream) | 200 `application/json` 138 — accepted delta, parity report §5.1 |
| `GET /api` | 404 | 404 |
| `GET /nope` | 404 | 404 |

HTML byte counts differ by under 2%: the session/machine-name footer is gone (C-09) and the layout
was restyled. The rendered tables carry the same twelve products with the same names, brands, types,
prices, picture names, stock, restock and max-stock values, the same ten-row page size and the same
Edit/Details/Delete actions.

## Write path

Driven against both apps in one run: create → edit → delete through the real forms, with the
anti-forgery token taken from each `GET` page.

| Step | Before | After | Match |
| --- | --- | --- | --- |
| `POST /Catalog/Create` | 302 → `/` | 302 → `/` | yes |
| item count after create | +1 | +1 | yes |
| `POST /Catalog/Edit` | 302 → `/` | 302 → `/` | yes |
| name after edit | updated | updated | yes |
| `POST /Catalog/Delete` | 302 → `/` | 302 → `/` | yes |
| item count after delete | back to the original | back to the original | yes |
| `GET /Catalog/Details/{deleted}` | 404 | 404 | yes |
| id of the created item | next HiLo value | next HiLo value (a different absolute number: separate databases, and the legacy side is mock data) | yes, semantically |
| `POST /Catalog/Create` without a token | 500 | 400 | accepted delta, parity report §5.4 |

This run is what surfaced the id-allocation defect fixed in parity report §6.2: repeated creates on
the modernized side returned 500 because the id was never allocated from `dbo.catalog_hilo`.
