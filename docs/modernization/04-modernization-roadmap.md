# .NET Modernization Roadmap

**Repository:** [Cognition-Partner-Workshops/eShopModernizing](https://github.com/Cognition-Partner-Workshops/eShopModernizing) · **Commit:** `c654bd3` · **Generated:** 2026-07-27 by Devin (.NET Discovery playbook, Phase 4).

**Jira Epic:** [NET-51 — .NET Modernization](https://cognition-partner-workshops.atlassian.net/browse/NET-51) (8 discovery tasks NET-52…NET-59, 14 implementation tasks NET-60…NET-73).

**Discovery pages:** [.NET Codebase Inventory](./01-codebase-inventory.md) · [.NET Behavioral Baseline](./02-behavioral-baseline.md) (parity oracle) · [Proposed .NET Modernization Boundaries](./03-modernization-boundaries.md)

## 1. Summary

- **Components identified:** 8 (see Boundaries).
- **Estimated total effort:** ≈ **34 dev-weeks** (≈ 26 if the Web Forms UI is retired instead of rewritten) — roughly 2 quarters for a team of 3, including the parity gate.
- **Recommended starting point:** **C1 Platform Foundation** (NET-52 / NET-60…NET-63) immediately followed by **C2 Catalog Domain & Data** (NET-53 / NET-64, NET-65) — lowest dependency, highest leverage, because all three front-ends and the SOAP service currently carry their own copy of the domain and EF6 model.
- **First decision to force:** retire or rewrite the Web Forms UI (NET-57). It is the single largest work item (XL) and the MVC UI already covers the same functionality.

## 2. Phased timeline

| Phase | Goal | Components | Jira | Effort |
| --- | --- | --- | --- | --- |
| **Discovery** | Close the open decisions and capture the missing Windows baselines | All | [NET-52](https://cognition-partner-workshops.atlassian.net/browse/NET-52)…[NET-59](https://cognition-partner-workshops.atlassian.net/browse/NET-59) | 2–3 weeks |
| **Phase 0 — Foundation** | .NET 8 solution skeleton, shared domain, configuration, DI, logging, safe serialization | C1 | [NET-60](https://cognition-partner-workshops.atlassian.net/browse/NET-60), [NET-61](https://cognition-partner-workshops.atlassian.net/browse/NET-61), [NET-62](https://cognition-partner-workshops.atlassian.net/browse/NET-62), [NET-63](https://cognition-partner-workshops.atlassian.net/browse/NET-63) | M (≈ 3 weeks) |
| **Phase 1 — Data Layer** | EF6 → EF Core 8, one catalog database, HiLo + seeding parity | C2 | [NET-64](https://cognition-partner-workshops.atlassian.net/browse/NET-64), [NET-65](https://cognition-partner-workshops.atlassian.net/browse/NET-65) | L (≈ 5 weeks) |
| **Phase 2 — Service Layer** | WCF → gRPC (+ REST facade), Web API 2 → ASP.NET Core, client regeneration | C3, C4, C7 | [NET-66](https://cognition-partner-workshops.atlassian.net/browse/NET-66), [NET-67](https://cognition-partner-workshops.atlassian.net/browse/NET-67), [NET-68](https://cognition-partner-workshops.atlassian.net/browse/NET-68) | L (≈ 8 weeks) |
| **Phase 3 — Presentation Layer** | MVC 5 → ASP.NET Core MVC; Web Forms retire-or-rewrite | C5, C6 | [NET-69](https://cognition-partner-workshops.atlassian.net/browse/NET-69), [NET-70](https://cognition-partner-workshops.atlassian.net/browse/NET-70) | L–XL (≈ 6–14 weeks) |
| **Phase 4 — Containerization & cutover** | Dockerfiles, compose, Linux CI/CD, automated parity gate | C8 | [NET-71](https://cognition-partner-workshops.atlassian.net/browse/NET-71), [NET-72](https://cognition-partner-workshops.atlassian.net/browse/NET-72), [NET-73](https://cognition-partner-workshops.atlassian.net/browse/NET-73) | M (≈ 4 weeks) |

## 3. Backlog

### 3.1 Discovery tasks

| Issue | Component | Key question it closes |
| --- | --- | --- |
| [NET-52 — D-01 Platform Foundation](https://cognition-partner-workshops.atlassian.net/browse/NET-52) | C1 | Config schema, DI map, logging map, serialization replacement |
| [NET-53 — D-02 Catalog Domain & Data](https://cognition-partner-workshops.atlassian.net/browse/NET-53) | C2 | Canonical model across three copies; database consolidation; EF6 baseline with a real DB |
| [NET-54 — D-03 Catalog HTTP API](https://cognition-partner-workshops.atlassian.net/browse/NET-54) | C3 | Consumers of the binary `/api/files`; picture-serving strategy; dead `/api` route |
| [NET-55 — D-04 Catalog SOAP Service](https://cognition-partner-workshops.atlassian.net/browse/NET-55) | C4 | gRPC vs CoreWCF; `.proto` design; SOAP golden baseline |
| [NET-56 — D-05 MVC Web UI](https://cognition-partner-workshops.atlassian.net/browse/NET-56) | C5 | View port plan, session/bundling replacements, test-project coupling |
| [NET-57 — D-06 Web Forms UI](https://cognition-partner-workshops.atlassian.net/browse/NET-57) | C6 | **Retire vs rewrite** — the biggest cost driver |
| [NET-58 — D-07 WinForms Client](https://cognition-partner-workshops.atlassian.net/browse/NET-58) | C7 | Keep on net8.0-windows vs retire |
| [NET-59 — D-08 Containerization & CI/CD](https://cognition-partner-workshops.atlassian.net/browse/NET-59) | C8 | Container topology, dev database, CI transition plan |

### 3.2 Implementation tasks

| Issue | Phase | Outcome |
| --- | --- | --- |
| [NET-60 — I-01](https://cognition-partner-workshops.atlassian.net/browse/NET-60) | 0 | .NET 8 solution skeleton + shared domain library, central package management |
| [NET-61 — I-02](https://cognition-partner-workshops.atlassian.net/browse/NET-61) | 0 | Web.config → appsettings.json; Autofac → Microsoft.Extensions.DependencyInjection; Global.asax → Program.cs |
| [NET-62 — I-03](https://cognition-partner-workshops.atlassian.net/browse/NET-62) | 0 | log4net + App Insights → Serilog/OpenTelemetry, correlation ids, health checks |
| [NET-63 — I-04](https://cognition-partner-workshops.atlassian.net/browse/NET-63) | 0 | `BinaryFormatter` removed (risk R3); `/api/files` returns JSON |
| [NET-64 — I-05](https://cognition-partner-workshops.atlassian.net/browse/NET-64) | 1 | EF Core 8 `CatalogDbContext` + migrations replacing three EF6 contexts |
| [NET-65 — I-06](https://cognition-partner-workshops.atlassian.net/browse/NET-65) | 1 | HiLo sequences, seeding (CSV/ZIP) and single consolidated database |
| [NET-66 — I-07](https://cognition-partner-workshops.atlassian.net/browse/NET-66) | 2 | 10 WCF operations re-platformed to gRPC (CoreWCF fallback) |
| [NET-67 — I-08](https://cognition-partner-workshops.atlassian.net/browse/NET-67) | 2 | Brands/Files/Pic endpoints on ASP.NET Core with OpenAPI; dead `/api` deleted |
| [NET-68 — I-09](https://cognition-partner-workshops.atlassian.net/browse/NET-68) | 2 | WinForms client on net8.0-windows with generated client, or retired |
| [NET-69 — I-10](https://cognition-partner-workshops.atlassian.net/browse/NET-69) | 3 | MVC 5 controllers/views → ASP.NET Core MVC; all 48 tests ported |
| [NET-70 — I-11](https://cognition-partner-workshops.atlassian.net/browse/NET-70) | 3 | Web Forms retired or rewritten as Razor Pages |
| [NET-71 — I-12](https://cognition-partner-workshops.atlassian.net/browse/NET-71) | 4 | Dockerfiles + docker-compose with SQL Server |
| [NET-72 — I-13](https://cognition-partner-workshops.atlassian.net/browse/NET-72) | 4 | CI on ubuntu-latest, images published, vulnerable-package gate |
| [NET-73 — I-14](https://cognition-partner-workshops.atlassian.net/browse/NET-73) | 4 | Automated parity gate replaying the golden baseline |

## 4. Sequencing constraints

```
Discovery (NET-52..59)
   │
   ▼
Phase 0  I-01 ─▶ I-02 ─▶ I-03        (I-04 in parallel, needs D-03)
   │
   ▼
Phase 1  I-05 ─▶ I-06                (blocks every front-end)
   │
   ├──▶ Phase 2  I-07 (WCF→gRPC) ─▶ I-09 (WinForms client)
   │             I-08 (Web API→Core)
   │
   ├──▶ Phase 3  I-10 (MVC)   [needs I-08]
   │             I-11 (Web Forms decision from D-06)
   │
   └──▶ Phase 4  I-12 ─▶ I-13 ─▶ I-14 (parity gate closes the migration)
```

## 5. Definition of done for the migration

- Every retained component runs on .NET 8 in a Linux container (except the optional WinForms client).
- The parity suite (NET-73) reproduces the golden outputs in the [Behavioral Baseline](./02-behavioral-baseline.md), with only documented accepted deltas.
- No `BinaryFormatter`, no binding redirects, no `packages.config`, no log4net, one version per NuGet package.
- CI runs on ubuntu-latest, builds images, and fails on vulnerable packages.
- The legacy solutions are deleted from the repository once their replacements are cut over.

## 6. Known gaps carried into execution

- Web Forms, WCF and WinForms behaviour is **static-only**; their baselines must be captured on a Windows agent (IIS Express + LocalDB) before those components are rewritten.
- The MVC baseline was captured with `UseMockData=true`, so EF6 query/seed behaviour is not yet part of the parity oracle (closed by NET-53).
- No authentication exists anywhere in the estate today; adding it is a net-new workstream, not part of parity.
