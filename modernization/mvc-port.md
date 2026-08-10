# MVC 5 → ASP.NET Core MVC (NET-69)

`src/eShop.Web` is the port of `eShopLegacyMVCSolution/src/eShopLegacyMVC`'s presentation layer:
`CatalogController`, the Razor views and the static assets. The legacy project is untouched and
keeps building on Windows until the parity gate (NET-73) is passed.

## Routes and status codes (Behavioral Baseline section 6)

The default route is `{controller=Catalog}/{action=Index}/{id?}`, so the catalog index is the
application root exactly as it was under `RouteConfig`. That is what makes the three mutating
actions redirect to `/` (`Location: /`) instead of `/Catalog`.

| Route | Behaviour |
| --- | --- |
| `GET /`, `/Catalog`, `/Catalog/Index` | 200, `<title>Index - Catalog manager (MVC)</title>`, 10 rows |
| `GET /Catalog/Index?pageSize=&pageIndex=` | 200, page honoured (`Showing … of … products - Page …`) |
| `GET /Catalog/{Details,Edit,Delete}/{id}` | 200 known id, 404 unknown id, **400** when the id is omitted |
| `GET /Catalog/Create` | 200, form carries `__RequestVerificationToken` |
| `POST /Catalog/{Create,Edit,Delete}` | 302 → `/`, `[ValidateAntiForgeryToken]` on all three |

Deliberate deviation: a POST without a valid anti-forgery token answers **400** (the ASP.NET Core
`AntiforgeryValidationException` result) where the legacy app surfaced the exception as a 500. CSRF
enforcement itself is unchanged, and both are "the request is rejected".

## Decisions

* **View models instead of `ViewBag` SelectLists.** `CatalogItemFormViewModel` carries the item plus
  the brand/type `SelectList`s that used to live in `ViewBag.CatalogBrandId` / `ViewBag.CatalogTypeId`.
  Because the item is nested, the Create/Edit inputs are named `Item.<Property>`; the POST actions
  bind a `CatalogItem` parameter named `item`, so the unprefixed legacy field names (`Name`,
  `Price`, …) still bind too. Both shapes are covered by tests. The legacy
  `[Bind(Include = "Id,Name,…")]` allowlist is preserved as `[Bind(…)]`.
* **Session state removed.** `Session["MachineName"]` / `Session["SessionStartTime"]` were only ever
  rendered in the footer. They are replaced by the `HostInfo` singleton (machine name + process
  start time), which renders the same `"<machine>, <timestamp>"` string without InProc session
  state, so the app stays horizontally scalable.
* **Bundling removed.** `System.Web.Optimization` bundles became plain static files under
  `wwwroot/` (`css/`, `js/`, `images/`, `fonts/`), served by `UseStaticFiles`. The same CSS files
  (`bootstrap.css`, `custom.css`, `base.css`, `site.css`) and scripts (jQuery, jQuery validation,
  Bootstrap, respond, modernizr) are referenced individually from `_Layout.cshtml`.
* **`HandleErrorAttribute` removed.** `Program.cs` already wires `UseExceptionHandler("/Home/Error")`;
  `HomeController.Error` renders the ported `Views/Shared/Error.cshtml` (with `Layout = null`, so the
  error page is a single well-formed document).
* **Picture URIs.** The legacy controller built an absolute link to the `items/{id}/pic` route, which
  now lives in `eShop.Catalog.Api`. `CatalogWeb:PicturesBaseUrl` sets the base address; empty (the
  default) means the current request's scheme and host.

  Running the web app on its own therefore renders broken thumbnails, because nothing serves
  `/items/{id}/pic` in that process. Run both services and point the web app at the API:

  ```bash
  dotnet run --project src/eShop.Catalog.Api                       # listens on http://localhost:5100
  CatalogWeb__PicturesBaseUrl=http://localhost:5100 \
    dotnet run --project src/eShop.Web                             # http://localhost:5000
  ```

  `appsettings.Development.json` already sets that base URL, so `dotnet run` in Development only
  needs the API to be up.

## Tests

`tests/eShop.Web.Tests` is a real xUnit project with a `ProjectReference` to `src/eShop.Web` — the
legacy test project linked the source files instead, and that coupling is not reproduced.

* All 48 legacy MSTest tests are ported: 17 `CatalogControllerTests`, 15 `MockCatalogServiceTests`,
  6 `PaginatedItemsViewModelTests`, 10 `PreconfiguredDataTests`. The Moq mock became the hand-written
  `RecordingCatalogService`; the `ViewBag` assertions became view-model assertions.
* `CatalogPagesGoldenOutputTests` hosts the app with `WebApplicationFactory` and asserts the golden
  outputs above, including the CSRF-protected round trips (create → item visible, edit → renamed,
  delete → 404).
* `ModernizedSurfaceTests` asserts the error view, the static assets and that no assembly in the web
  tier references `System.Web*`.
