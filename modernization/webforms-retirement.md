# Web Forms retirement (NET-70 / I-11)

`eShopLegacyWebFormsSolution/` (ASP.NET Web Forms, .NET Framework 4.7.2) is **removed** from the
repository. The decision was taken as D-06 on Jira NET-57: the Web Forms app is a second UI over the
same catalog CRUD that the ASP.NET MVC UI already provides, so it is retired rather than rewritten as
Razor Pages. Its replacement is the modernized ASP.NET Core MVC UI in `src/eShop.Web` (ported in
NET-69) on top of `eShop.Catalog.Data`.

This page records (1) the page-by-page functional-equivalence argument that justified the deletion,
(2) the old → new URL mapping to announce, and (3) the gaps that are deliberately *not* carried over.

## 1. Page-by-page equivalence

Web Forms routes come from `App_Start/RouteConfig.cs` (`MapPageRoute`) in the deleted project. The
modernized column is the ASP.NET Core MVC `CatalogController` in `src/eShop.Web`, which mirrors the
legacy MVC 5 `CatalogController` action-for-action.

| # | Web Forms page | Route(s) | Behaviour | Modernized equivalent | Covered |
| --- | --- | --- | --- | --- | --- |
| 1 | `Default.aspx` | `/`, `/Default` | Paged catalog list (`ListView` bound to `PaginatedItemsViewModel<CatalogItem>`, defaults size 10 / index 0), thumbnail, Create link, prev/next pager hidden at the ends | `CatalogController.Index(pageSize = 10, pageIndex = 0)` → `Views/Catalog/Index.cshtml` (+ `CatalogTable`), same `PaginatedItemsViewModel` shape and same hidden-pager rule | Yes |
| 2 | `Default.aspx` (paged) | `/Default/index/{index}/size/{size}` | Same list with explicit pagination from route values | `/Catalog/Index?pageIndex={index}&pageSize={size}` (query-string pagination, identical semantics) | Yes — URL shape differs, see §2 |
| 3 | `Catalog/Create.aspx` | `/Catalog/Create` | Form with brand/type drop-downs; POST creates the item and redirects to `~` | `CatalogController.Create()` GET + `[HttpPost] Create(CatalogItem)`, `SelectList` for brand/type, redirect to `Index` | Yes |
| 4 | `Catalog/Edit.aspx` | `/Catalog/Edit/{id}` | Loads item, brand/type drop-downs preselected; POST updates and redirects to `~` | `CatalogController.Edit(int? id)` GET + `[HttpPost] Edit(CatalogItem)` | Yes |
| 5 | `Catalog/Details.aspx` | `/Catalog/Details/{id}` | Read-only item view | `CatalogController.Details(int? id)` | Yes |
| 6 | `Catalog/Delete.aspx` | `/Catalog/Delete/{id}` | Confirmation view; POST deletes and redirects to `~` | `CatalogController.Delete(int? id)` GET + `[HttpPost, ActionName("Delete")] DeleteConfirmed(int id)` | Yes |
| 7 | `About.aspx` | `/About.aspx` | Static Bootstrap placeholder text ("Your application description page"), no catalog data | none | No — intentionally dropped (§3.1) |
| 8 | `Contact.aspx` | `/Contact.aspx` | Static placeholder contact page, no catalog data | none | No — intentionally dropped (§3.1) |
| 9 | `Site.Mobile.Master` + `ViewSwitcher.ascx` | `/__FriendlyUrls_SwitchView/{view}` | Desktop/mobile master switching via `Microsoft.AspNet.FriendlyUrls` | none — the modernized layout is responsive (Bootstrap), no server-side view switching | No — obsolete (§3.2) |
| 10 | `Site.Master` session label | every page | Footer label printing `Session["MachineName"]` and `Session["SessionStartTime"]` (set in `Session_Start`) | none — the modernized hosts are stateless; equivalent diagnostics come from the NET-62 telemetry (`/health`, `/ready`, structured Serilog logs) | No — deliberate (§3.3) |
| 11 | `/Pics/{fileName}` static images | static file | Item thumbnails served straight from the `Pics` folder | `GET /items/{id:int}/pic` on `eShop.Catalog.Api` (NET-67, `CatalogPictureStore`) | Yes |

Behavioural notes backing the "Yes" rows:

* Both UIs read the identical `ICatalogService` surface (`GetCatalogItemsPaginated`, `FindCatalogItem`,
  `GetCatalogBrands`, `GetCatalogTypes`, `CreateCatalogItem`, `UpdateCatalogItem`,
  `RemoveCatalogItem`); the Web Forms copies of `CatalogService` / `CatalogServiceMock` /
  `PaginatedItemsViewModel` were byte-for-byte siblings of the MVC ones, and both are now served by
  the single EF Core 8 implementation from NET-64.
* Both apps used the same `Microsoft.eShopOnContainers.Services.CatalogDb` database and the same
  `UseMockData` switch (`Catalog:UseMockData` after NET-61), so there is no data the Web Forms app
  could see that the MVC UI cannot.
* The Web Forms Create page did not expose `PictureFileName` (Edit did); the MVC UI exposes it on
  both, which is a superset, not a gap.
* The Web Forms app was **static-only** in the Behavioral Baseline (Confluence ".NET Behavioral
  Baseline", section 1: it compiles but cannot be hosted off Windows/IIS Express), so there are no
  golden Web Forms responses to reproduce — the parity oracle for this UI is the MVC baseline, which
  NET-69 targets.

## 2. URL mapping to announce

Old host: the Web Forms site (its own IIS site/app pool). New host: the modernized `eShop.Web` app.
Everything below is a host-level change; announce it with the cut-over.

| Old Web Forms URL | New URL |
| --- | --- |
| `/` | `/` (routes to `Catalog/Index`) |
| `/Default` | `/Catalog/Index` |
| `/Default/index/{index}/size/{size}` | `/Catalog/Index?pageIndex={index}&pageSize={size}` |
| `/Catalog/Create` | `/Catalog/Create` (unchanged) |
| `/Catalog/Edit/{id}` | `/Catalog/Edit/{id}` (unchanged) |
| `/Catalog/Details/{id}` | `/Catalog/Details/{id}` (unchanged) |
| `/Catalog/Delete/{id}` | `/Catalog/Delete/{id}` (unchanged) |
| `/Pics/{fileName}` | `GET /items/{id}/pic` on the catalog API |
| `/About.aspx`, `/Contact.aspx` | no replacement (static placeholder content) |
| `/__FriendlyUrls_SwitchView/{view}` | no replacement (responsive layout) |

Five of the seven functional routes are already identical, so a redirect layer is only needed for
`/Default*` and `/Pics/*`.

**Redirects are intentionally not implemented in this ticket.** The legacy app is deleted, so there
is nothing there to redirect from; and adding the `/Default/index/{index}/size/{size}` alias means
editing `src/eShop.Web`, which is owned by NET-69 running in parallel (see the open item below).
Recommended options, cheapest first:

1. **Edge/reverse-proxy rule** at the old hostname (IIS URL Rewrite, nginx, App Gateway):
   `^/Default/index/([0-9]+)/size/([0-9]+)/?$` → `301 /Catalog/Index?pageIndex=$1&pageSize=$2`, and
   `^/Default/?$` → `301 /Catalog/Index`. This is the recommended route because the old and new apps
   are separate deployments.
2. **Route alias in `eShop.Web`** if the new app takes over the old hostname — one extra
   conventional route, e.g.
   `MapControllerRoute("webforms-legacy-paging", "Default/index/{pageIndex:int}/size/{pageSize:int}", new { controller = "Catalog", action = "Index" })`,
   plus `Default` → `Catalog/Index`.

## 3. Deliberate gaps (nothing silently dropped)

1. **`About.aspx` / `Contact.aspx`** — unmodified ASP.NET project-template placeholder pages
   ("Your application description page.", "Your contact page." with `One Microsoft Way`). They carry
   no catalog behaviour and no customer content; not reproduced.
2. **Mobile master page + `ViewSwitcher`** — `Microsoft.AspNet.FriendlyUrls` desktop/mobile view
   switching has no ASP.NET Core counterpart and is superseded by the responsive Bootstrap layout.
   `Site.Mobile.Master` was the stock project-template mobile shell (no branding, no catalog
   behaviour) hosting the same `MainContent` placeholder plus the switcher control.
3. **Session footer label** — the `Session["MachineName"]` / `Session["SessionStartTime"]` footer was
   an intentional demonstration of Web Forms session state. The modernized hosts are stateless
   (required for containers/scale-out); instance and request diagnostics come from the NET-62
   Serilog/OpenTelemetry pipeline and the `/health` and `/ready` endpoints instead.
4. **ViewState, `ScriptManager`/MsAjax, `<%$RouteUrl:… %>` expression builders, `Bundle.config`,
   Autofac.Web property injection** — framework plumbing with no user-visible behaviour; replaced by
   ASP.NET Core routing, tag helpers and constructor injection.

## 4. What was removed

* `eShopLegacyWebFormsSolution/` (the whole tree: `eShopLegacyWebForms.sln`,
  `src/eShopLegacyWebForms/**`, its `packages.config`, `Web.config`, ASPX pages, assets and
  `Pics`/`Setup` copies).
* Its entries in `README.md` (application table, repository structure).

Nothing else in the repository referenced the project: it was never part of `eShop.sln`, never built
by `.github/workflows/ci.yml` (the Windows job builds only the MVC solution, the utilities library and
the MVC tests), and no project reference, solution entry or script pointed at it. The only remaining
mention is a historical one in `modernization/database-consolidation.md`, describing which apps used
to share the `CatalogDb` database.
