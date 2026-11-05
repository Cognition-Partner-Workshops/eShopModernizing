# Web Forms catalog port

A port of a legacy .NET line-of-business app to a self-contained ASP.NET Core 8
Razor Pages application, with an xUnit/WebApplicationFactory characterization
suite. No references to the legacy projects or their packages.
The local global.json pins SDK 8.0.427; run commands from this directory:

```powershell
dotnet build eShopModernizedWebForms.sln -c Release
dotnet test eShopModernizedWebForms.sln -c Release
dotnet format eShopModernizedWebForms.sln --verify-no-changes
dotnet run --project src/eShopModernizedWebForms --urls http://localhost:5100
```

## Parity decisions

- UseMockData defaults to true in appsettings and in code. The original mock service,
  domain annotations, seed data, and pagination calculation are copied with namespace
  changes only (and removal of System.Web imports). The service is a singleton.
  Changes are shared across sessions and reset when the process restarts.
- UseMockData=false throws a clear NotSupportedException at startup; SQL mode is
  deliberately not implemented or tested against SQL.
- URLs: /, /Default, /Default/index/{index}/size/{size}, /Catalog/Create,
  /Catalog/Edit/{id}, /Catalog/Details/{id}, /Catalog/Delete/{id}, /About, /Contact.
  /Default.aspx, /About.aspx, and /Contact.aspx are same-content aliases.
  Successful form POSTs redirect to /. Antiforgery is enforced.
- The actual legacy pager uses "Showing 10 of 12 products - Page 1 - 2", not
  "Page 1 of 2". It displays the requested page size even on short/empty pages.
  The markup, hidden classes, and links retain this behavior.
- Name is required and form price is capped at 1 million, as in the .aspx validators,
  in addition to the copied CatalogItem price regex/range and stock annotations.
  Validation runs on the server. Web Forms generated client-validation JavaScript
  is not carried over. Numeric model-binding errors use ASP.NET Core messages.
  Restock/Max stock range errors retain the CatalogItem annotation's "Stock" wording.
- Picture upload is unavailable; Create uses dummy.png and Edit preserves the
  existing picture regardless of submitted values. The picture field is read-only.
- Unknown numeric ids return 404 instead of the legacy null dereference / server
  error. Noninteger ids are unmatched routes (404). Details/Delete compose brand
  and type for direct entry, avoiding the legacy dependency on visiting the list
  first to initialize navigation properties. These are deliberate safe deviations.
- Other mock edge semantics are preserved, including zero-size division failure
  and Max() failure when creating after deleting all items.
- Content, fonts, images, Pics, and favicon are copied unchanged to wwwroot.
  CSS loading follows App_Start/BundleConfig.cs order. The legacy master header,
  footer, titles, and session machine/start-time text are retained.
- No root Directory.Build.props or Directory.Packages.props existed; no central
  package overrides or isolation files were needed. Packages are local project
  references with explicit versions. The local .gitignore excludes bin/obj.

## Verification scope

Tests characterize all 12 seed items and brands/types, list ordering/paging/empty
state, direct Details for all ids, Create/Edit/Delete with antiforgery, validation
failures, unknown ids, mock edge behavior, informational pages and static assets.
The unsupported SQL startup path is tested, not SQL behavior.
Visual browser parity has not been checked.

## Runtime parity tooling

From the repository root on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File eShopModernizedWebFormsSolution/scripts/reset.ps1 -Start
python eShopModernizedWebFormsSolution/scripts/parity.py
```

The reset script stops only listeners on ports 5200/5201 (including the specific
IIS Express HTTP.sys worker), builds both solutions, recopies the legacy site to
the current user's scratch/webforms-legacy-site, sets mock mode in that copy,
and optionally starts both sites. The repository's legacy files are never edited.
The port runs on 5200 and legacy IIS Express on 5201. Native backslashes are
required for the IIS Express /path argument in this environment.
Live process logs stay outside the repository in scratch/webforms-runtime-logs;
the artifacts directory contains the shareable HTTP parity evidence.

The Python standard-library probe scrapes runtime HTML and control names, keeps
cookies, submits all served hidden form values, and writes artifacts/parity.json,
artifacts/parity.md, and artifacts/shared_data_fingerprint.json. Prices are
normalized to two decimal places; whitespace and HTML entities are normalized.
No source files are read by the probe. Always reset before running: it performs
real CRUD mutations and a deliberately invalid Create POST.

Runtime differences are left red, not hidden or patched into unsafe behavior:

- Legacy Details renders names/descriptions through unencoded asp:Label.Text.
  Items 9 and 11 contain literal <T> markup, so their text differs from the
  correctly HTML-encoded port. The list, which uses encoded binding, and the
  shared runtime seed fingerprint still agree.
- Legacy Create accepts a direct HTTP POST with Price=-1. Its click handler checks
  ModelState.IsValid rather than Page.IsValid and manually constructs the item.
  The port rejects the request and leaves the catalog unchanged.
- Legacy /About and /Contact are 404; RouteConfig does not register them.
  /About.aspx, /Contact.aspx, and /Default.aspx work on both sides and are
  compared at the same URLs. The port retains its extensionless routes too.
- Missing Details ids, including a just-deleted item, return legacy HTTP 500
  versus port HTTP 404. The port does not reproduce legacy null dereferences.

These baseline discrepancies require a parity-policy decision, not a port bug fix.
No visual browser comparison has been performed.
