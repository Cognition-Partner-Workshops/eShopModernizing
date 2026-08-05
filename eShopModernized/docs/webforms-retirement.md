# Web Forms UI — retirement record and route map

**Ticket:** NET-70 (I-11, Phase 3) · **Decision:** D-06 / NET-57 — *retire the Web Forms UI; do not
rewrite it*. · **Epic:** NET-51.

This document is the disposition record for `eShopLegacyWebFormsSolution/`. It enumerates every
page, master page, user control and URL the Web Forms application ships, maps each to the
modernized `eShop.Web` (ASP.NET Core 8 MVC) route that replaces it, and states explicitly which
capabilities are **not** carried forward.

No code changes accompany this document. Where a redirect is required it is specified here as a
follow-up for the ticket that owns `eShop.Web` (NET-69) or the cutover ticket (NET-73) — this
ticket does not edit `eShop.Web`.

Sources, all at the tip of `main`:

- `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/App_Start/RouteConfig.cs`
- `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/*.aspx`, `Catalog/*.aspx`, `Site.Master`,
  `Site.Mobile.Master`, `ViewSwitcher.ascx`, `Web.config`, `Global.asax.cs`
- `eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/CatalogController.cs`,
  `Controllers/PicController.cs`, `App_Start/RouteConfig.cs`, `Views/Shared/_Layout.cshtml`
- Confluence: *.NET Behavioral Baseline* (parity oracle) and *.NET Modernization Decision Log*
  (D-06, contradictions C-09 and C-11).

---

## 1. Decision

The Web Forms application is **retired**. It is not ported to Razor Pages and no Web Forms code
is carried into `eShopModernized/`. The six catalog routes it serves are functionally equivalent
to the MVC UI — same service layer, same domain model, same seed data, same pagination defaults —
so the modernized MVC UI (`eShop.Web`) is the single replacement for both legacy front ends.

`eShopLegacyWebFormsSolution/` stays on `main` untouched until cutover; deleting it belongs to the
cutover ticket, not to this one.

### Why the two front ends are equivalent

| Claim | Evidence |
| --- | --- |
| Same service contract | `Services/ICatalogService.cs` is identical to the MVC interface; `CatalogService.cs` differs only by namespace and two `.ToList()` calls |
| Same domain model and seeding | `Models/*`, `Models/Infrastructure/CatalogDBInitializer.cs` and `Setup/*.csv` are byte-identical to the MVC copies apart from namespaces |
| Same pagination semantics | `Default.aspx.cs:15-16` (`DefaultPageSize = 10`, `DefaultPageIndex = 0`) vs `CatalogController.cs:22` (`pageSize = 10, pageIndex = 0`) |
| Same rendered chrome | `Site.Master` and `Views/Shared/_Layout.cshtml` render the same header/hero/footer markup; the only textual difference is the title suffix `(Web Forms)` vs `(MVC)` |

---

## 2. Page and artefact inventory

Every user-facing artefact in `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms`:

| # | Artefact | Type | Master page | Behaviour | Disposition |
| --- | --- | --- | --- | --- | --- |
| 1 | `Default.aspx` (+ `.cs`) | Page | `Site.Master` | Paginated catalog grid via `ListView`; `GetCatalogItemsPaginated(size, index)`; pager built with `GetRouteUrl("ProductsByPageRoute", …)` | Replaced by `Catalog/Index` |
| 2 | `Catalog/Create.aspx` (+ `.cs`) | Page | `Site.Master` | Create form; brand/type dropdowns from `GetCatalogBrands()`/`GetCatalogTypes()`; postback `Create_Click` → `CreateCatalogItem` → `Response.Redirect("~")` | Replaced by `Catalog/Create` |
| 3 | `Catalog/Edit.aspx` (+ `.cs`) | Page | `Site.Master` | Prefilled edit form (bound on non-postback only); `Save_Click` → `UpdateCatalogItem` → `Response.Redirect("~")` | Replaced by `Catalog/Edit/{id}` |
| 4 | `Catalog/Details.aspx` (+ `.cs`) | Page | `Site.Master` | Read-only detail view of `FindCatalogItem(id)` | Replaced by `Catalog/Details/{id}` |
| 5 | `Catalog/Delete.aspx` (+ `.cs`) | Page | `Site.Master` | Delete confirmation; `Delete_Click` → `RemoveCatalogItem` → `Response.Redirect("~")` | Replaced by `Catalog/Delete/{id}` |
| 6 | `About.aspx` (+ `.cs`) | Page | `Site.Master` | Static Visual Studio template text ("Your application description page.") plus a `log4net` info line | **Retired — not replaced** (§4.1) |
| 7 | `Contact.aspx` (+ `.cs`) | Page | `Site.Master` | Static Visual Studio template text (One Microsoft Way / `Support@example.com`) plus a `log4net` info line | **Retired — not replaced** (§4.1) |
| 8 | `Site.Master` (+ `.cs`) | Master page | — | Chrome: `ScriptManager` (MsAjax, jQuery, Bootstrap, Respond, WebForms.js), header brand, hero title, `MainContent` placeholder, footer; footer renders `Session["MachineName"], Session["SessionStartTime"]` | Chrome replaced by `Views/Shared/_Layout.cshtml`; session line is an accepted delta (§5) |
| 9 | `Site.Mobile.Master` (+ `.cs`) | Master page | — | "Mobile Master Page" heading, `FeaturedContent`/`MainContent` placeholders, hosts `ViewSwitcher` | **Retired — not replaced** (§4.2) |
| 10 | `ViewSwitcher.ascx` (+ `.cs`) | User control | — | Renders "Desktop view \| Switch to Mobile" using `Microsoft.AspNet.FriendlyUrls` 1.0.2 | **Retired — not replaced** (§4.2) |
| 11 | `App_Start/RouteConfig.cs` | Routing | — | Six `MapPageRoute` entries (§3) | Replaced by ASP.NET Core endpoint routing |
| 12 | `App_Start/BundleConfig.cs`, `Bundle.config` | Bundling | — | `~/Content/css` style bundle, `~/bundles/{WebFormsJs,MsAjaxJs,modernizr}` script bundles, `respond` script resource mapping | Replaced by static assets under `eShop.Web/wwwroot` (D-05) |
| 13 | `Content/`, `Scripts/`, `images/`, `fonts/`, `favicon.ico`, `Pics/` | Static assets | — | Served by IIS | Equivalent assets live in `eShop.Web/wwwroot`; `Pics/` is superseded by `GET /items/{id}/pic` (§3, row 9) |
| 14 | `Global.asax.cs` | Host | — | Autofac container + `Autofac.Integration.Web` property injection, `Session_Start` writes `MachineName`/`SessionStartTime`, log4net `LogicalThreadContext` properties, EF6 `Database.SetInitializer` | Replaced by `Program.cs` + `IServiceCollection` (D-01); property injection is not carried forward |

Non-UI code (`Models/`, `Services/`, `Modules/`, `ViewModel/`, `Setup/`) is a namespace-only copy
of the MVC tree and is already superseded by `eShop.Catalog.Domain` / `eShop.Catalog.Data`
(NET-53, NET-65). It carries no Web-Forms-specific behaviour.

---

## 3. Route map — Web Forms URL → modernized MVC route

Web Forms routes come from `App_Start/RouteConfig.cs:9-38`; route names are quoted exactly as they
appear in the code. Modernized routes are the MVC conventional route
`{controller}/{action}/{id}` with `Catalog/Index` as the default, plus the attribute route
`items/{catalogItemId:int}/pic`.

| # | Route name | Web Forms URL | Handler | Modernized URL in `eShop.Web` | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | `""` (unnamed) | `/Default` | `~/Default.aspx` | `/` → `Catalog/Index` | Equivalent (path differs: `/Default` vs `/`) — alias recommended, §4.3 |
| 2 | — (application root) | `/` | `~/Default.aspx` (default document) | `/` → `Catalog/Index` | Identical |
| 3 | `ProductsByPageRoute` | `/Default/index/{index}/size/{size}` | `~/Default.aspx` | `/Catalog/Index?pageIndex={index}&pageSize={size}` | **Shape differs** — path segments → query string. Redirect required, §4.3 |
| 4 | `CreateProductRoute` | `/Catalog/Create` | `~/Catalog/Create.aspx` | `/Catalog/Create` (GET form, POST create) | Identical |
| 5 | `EditProductRoute` | `/Catalog/Edit/{id}` | `~/Catalog/Edit.aspx` | `/Catalog/Edit/{id}` | Identical |
| 6 | `ProductDetailsRoute` | `/Catalog/Details/{id}` | `~/Catalog/Details.aspx` | `/Catalog/Details/{id}` | Identical |
| 7 | `DeleteProductRoute` | `/Catalog/Delete/{id}` | `~/Catalog/Delete.aspx` | `/Catalog/Delete/{id}` | Identical |
| 8 | — (no route entry) | `/About`, `/About.aspx`, `/Contact`, `/Contact.aspx` | `About.aspx`, `Contact.aspx` | none | **No counterpart — retired**, §4.1 |
| 9 | — (static file) | `/Pics/{pictureFileName}` | IIS static handler | `/items/{id}/pic` | **Shape differs** — filename → id. Retired URL shape, §4.4 |
| 10 | FriendlyUrls (never registered) | `/__FriendlyUrls_SwitchView/{view}?ReturnUrl=…` | `ViewSwitcher.ascx` + `Site.Mobile.Master` | none | **No counterpart — retired (dead code in legacy)**, §4.2 |
| 11 | — (bundles) | `/Content/css`, `/bundles/WebFormsJs`, `/bundles/MsAjaxJs`, `/bundles/modernizr` | `System.Web.Optimization` | static files under `wwwroot` | Not a public contract — retired |

Both applications also accept the legacy `.aspx` physical paths (`/Default.aspx`,
`/Catalog/Edit.aspx?…`) because `MapPageRoute` does not disable direct page access. Those URLs are
not part of the contract carried forward; only the routed forms in the table above are.

---

## 4. Contradiction C-11 — dispositions

> **C-11 (Decision Log §11):** "*True for the 6 catalog routes, but Web Forms also ships
> `About.aspx`, `Contact.aspx` and a FriendlyUrls mobile `ViewSwitcher.ascx` + `Site.Mobile.Master`
> with no MVC counterpart, and the paginated route uses path segments rather than a query string.
> Accept as deltas or add redirects at cutover. Nothing in the repository links to them.*"

Each item is decided below as either **(a) not replaced — retired capability** or **(b) needs a
redirect/route alias in `eShop.Web`**.

### 4.1 `/About` and `/Contact` — (a) not replaced, retired

**Decision: retired. No redirect, no replacement page.**

Justification:

- Both pages are unmodified Visual Studio Web Forms template boilerplate. `About.aspx` renders
  "Your application description page. Use this area to provide additional information."
  `Contact.aspx` renders Microsoft's own sample address (One Microsoft Way, Redmond) and the
  placeholder mailboxes `Support@example.com` / `Marketing@example.com`. Neither contains a single
  piece of eShop content.
- Neither page has any dynamic behaviour: the code-behinds do nothing except emit one `log4net`
  info line.
- Nothing links to them. `Site.Master` has no navigation menu — the only link in the chrome is the
  brand image pointing at `~/`. A repository-wide search finds no reference to `About.aspx` or
  `Contact.aspx` outside the two files themselves and the `.csproj`.
- They are absent from the MVC application entirely, and the MVC application is the surviving
  front end. Adding them to `eShop.Web` would be *new* content, not parity.

Residual risk (accepted): if the Web Forms origin is repointed at `eShop.Web` at cutover, any
external bookmark of `/About` or `/Contact` returns 404. Nothing in the repository consumes these
URLs and the content has no value, so a 404 is the correct outcome. If an operator wants to avoid
404s on the repointed origin, the cheapest mitigation is a catch-all redirect to `/` — that is an
operations choice, not a parity requirement, and is **not** requested of NET-69.

### 4.2 Mobile view: `ViewSwitcher.ascx` + `Site.Mobile.Master` — (a) not replaced, retired

**Decision: retired. No redirect, no replacement.**

Justification — this capability is not merely unused, it is **dead code in the legacy application**:

- `Microsoft.AspNet.FriendlyUrls` is referenced (`packages.config:19-20`) but
  `routes.EnableFriendlyUrls(…)` is **never called**. `RouteConfig.RegisterRoutes` only registers
  the six `MapPageRoute` entries, so the `AspNet.FriendlyUrls.SwitchView` route does not exist at
  runtime.
- `ViewSwitcher.ascx.cs:32-37` handles exactly that case by setting `this.Visible = false` and
  returning. The switcher therefore never renders.
- No page uses `Site.Mobile.Master`: all six `.aspx` pages declare
  `MasterPageFile="~/Site.Master"`, and there are no `*.Mobile.aspx` files, so the mobile master is
  never selected. Its code-behind `Page_Load` is empty.
- Consequently `/__FriendlyUrls_SwitchView/Mobile` returns 404 in the legacy application too. There
  is no user-visible behaviour to lose and no URL that ever worked to redirect.

Mobile support in the modernized UI is the same as in the MVC UI: a responsive Bootstrap layout
with `<meta name="viewport" content="width=device-width, initial-scale=1.0">`, not a separate
mobile master page.

### 4.3 Paginated catalog route `/Default/index/{index}/size/{size}` — (b) redirect required

**Decision: a redirect/route alias must ship in `eShop.Web`.** This is the one C-11 item that is a
real, reachable, linked capability: `Default.aspx.cs:50,54` generates these URLs for the
Previous/Next pager on every catalog page, so they are exactly the URLs a user would bookmark or a
synthetic monitor would record.

Required mappings (301 Moved Permanently, preserving the values):

| Source (Web Forms) | Target (`eShop.Web`) |
| --- | --- |
| `GET /Default/index/{index}/size/{size}` | `/Catalog/Index?pageIndex={index}&pageSize={size}` |
| `GET /Default` | `/` (i.e. `Catalog/Index`) |

Notes for the implementer:

- `{index}` is a zero-based page index and `{size}` is the page size, matching the MVC
  `pageIndex`/`pageSize` query parameters one-for-one; no value translation is needed.
- Constrain both segments to `int` so `/Default/index/abc/size/xyz` does not match; unmatched
  requests should fall through to the normal 404.
- This is only relevant if the Web Forms origin (host name) is repointed at `eShop.Web` at
  cutover. If the Web Forms deployment is simply decommissioned and its host retired, no redirect
  is reachable and the alias is inert — harmless either way.

**Follow-up note for NET-69 / NET-73:** implement these two redirects in `eShop.Web` (endpoint
routing, e.g. two `MapGet` endpoints returning `Results.RedirectPermanent(...)`, or a
`RewriteOptions` rule). This ticket deliberately does not touch `src/eShop.Web`. If the cutover
plan confirms that the Web Forms host is decommissioned rather than repointed, the redirects may be
dropped — record that decision on NET-73.

### 4.4 Picture URLs `/Pics/{pictureFileName}` — (a) not replaced, retired URL shape

Not called out by name in C-11, but it is the fourth place where the Web Forms URL surface differs
from MVC, so it is decided here for completeness.

**Decision: retired URL shape. No redirect.** Web Forms embeds images as static file references
(`Default.aspx:53` `src='/Pics/<%#:Item.PictureFileName%>'`, `Catalog/Details.aspx:8`
`ImageUrl='<%#"/Pics/" + product.PictureFileName%>'`). The MVC UI — and therefore the modernized
UI — serves images through `GET /items/{id}/pic` (`PicController.cs:24`), which is already ported
in `eShop.Catalog.Api`. `/Pics/{file}` is an internal asset path emitted by Web Forms markup, never
a linked or bookmarked page URL, and a redirect would require a filename → id reverse lookup for no
benefit.

---

## 5. Accepted deltas

| ID | Delta | Detail | Rationale |
| --- | --- | --- | --- |
| **C-09** | Session values rendered in the footer | `Site.Master.cs:17` sets `SessionInfoLabel.Text = $"{Session["MachineName"]}, {Session["SessionStartTime"]}"`, populated by `Global.asax.cs Session_Start`. The MVC UI does the same at `Views/Shared/_Layout.cshtml:35-36`. D-05 drops InProc session, so this line changes on **every page** of both front ends | Accepted as a delta for the retired Web Forms UI: with the application retired there is no Web Forms footer to preserve. The MVC-side resolution (remove the line, or re-source it from `Environment.MachineName` + process start time) belongs to NET-69 and governs the modernized UI. Recording it here means the Web Forms golden HTML, if it is ever captured on Windows, will differ from `eShop.Web` by that one line by design |
| **D-06.1** | `/About`, `/Contact` | Static template pages, dropped without replacement | §4.1 |
| **D-06.2** | Mobile ViewSwitcher / `Site.Mobile.Master` | Dead code in the legacy app; dropped without replacement | §4.2 |
| **D-06.3** | Paginated route shape | Path segments → query string; covered by a redirect | §4.3 |
| **D-06.4** | `/Pics/{file}` image URLs | Superseded by `/items/{id}/pic` | §4.4 |
| **D-06.5** | ViewState / postback model | `Create_Click`, `Save_Click`, `Delete_Click` post back to the same URL and rely on ViewState; the MVC equivalents are `POST /Catalog/{Create,Edit,Delete}` with `[ValidateAntiForgeryToken]` and a 302 to `/` | The modernized UI follows the MVC contract, which is the runtime-verified baseline. Anti-forgery protection is a security improvement, not a regression |
| **D-06.6** | Bad-input error semantics | Web Forms code-behinds do `Convert.ToInt32(RouteData.Values["id"])` with no null/404 handling, so a missing or unknown id throws (500 / `NullReferenceException`). MVC returns 400 for a missing id and 404 for an unknown one | The MVC semantics (baseline §6.1) are carried forward; the Web Forms failure modes are not reproduced |
| **D-06.7** | `Create` does not accept `PictureFileName` | `Create.aspx.cs:34-44` never sets `PictureFileName` (only `Edit.aspx.cs` does); the MVC `Create` action binds it | The MVC behaviour is carried forward |
| **D-06.8** | Default data mode differs | Web Forms `Web.config:15` ships `UseMockData=true`; MVC ships `UseMockData=false` | The modernized default follows the MVC/EF path (`Catalog:UseMockData=false`), per NET-65 |
| **C-12** | Vulnerability-scan blind spot closed | `dotnet list package --vulnerable` reports nothing for `packages.config` projects, and Web Forms is `packages.config`-only | Retiring the app is what removes the blind spot; no action needed |

---

## 6. Capabilities not carried forward

The complete list, for the cutover communication:

1. **The Web Forms application itself** — no ASP.NET Core Web Forms/Razor Pages equivalent is
   produced. All catalog functionality is served by `eShop.Web` (MVC).
2. **`/About` and `/Contact` pages** — retired, no replacement, no redirect (§4.1).
3. **Mobile view switching** (`ViewSwitcher.ascx`, `Site.Mobile.Master`,
   `Microsoft.AspNet.FriendlyUrls`) — retired; already non-functional in the legacy app (§4.2).
4. **Path-segment pagination URLs** — replaced by query-string pagination, with a redirect
   (§4.3).
5. **`/Pics/{pictureFileName}` static image URLs** — replaced by `/items/{id}/pic` (§4.4).
6. **InProc session footer values** (`MachineName`, `SessionStartTime`) — see C-09 (§5).
7. **ViewState/postback interaction model, `ScriptManager`, MsAjax and `System.Web.Optimization`
   bundles, `<%$RouteUrl:…%>` expression builders** — replaced by ASP.NET Core routing, tag
   helpers and `wwwroot` static assets.
8. **Autofac `PropertyInjectionModule` property injection into pages** — replaced by constructor
   injection through `IServiceCollection`.
9. **log4net per-page `Now loading... /X.aspx` info lines** — replaced by Serilog/OpenTelemetry
   request logging (D-01); the exact message text is not reproduced.

---

## 7. Parity statement

**What the parity claim rests on.** The Web Forms application was never runtime-verified. The
Behavioral Baseline records it as **static-only**: it compiles under Mono but cannot be hosted on
Linux (`System.Web.UI.ScriptResourceDefinition`/`ScriptResourceMapping` are missing and the ASPX
parser rejects the `<%$RouteUrl:…%>` expression builders); it needs Windows + IIS Express. There
are therefore **no golden Web Forms outputs** — no captured HTML, no status codes, no CRUD
round-trip — to compare `eShop.Web` against.

Consequently, parity for the retired Web Forms UI is asserted **through the MVC UI**:

| Web Forms behaviour | Covered by | Evidence |
| --- | --- | --- |
| Catalog list, default page (`size=10`, `index=0`) | MVC golden baseline | `GET /Catalog` → 200, 10 rows (Baseline §6.1) |
| Paginated catalog | MVC golden baseline | `GET /Catalog/Index?pageSize=2&pageIndex=1` → 200, 2 rows, second page (Baseline §6.1) |
| Details | MVC golden baseline | `GET /Catalog/Details/1` → 200; `/999` → 404; no id → 400 (Baseline §6.1) |
| Create form + create | MVC golden baseline | `GET /Catalog/Create` → 200; `POST` → 302 `Location: /`, item appears in the list (Baseline §6.1, §6.2) |
| Edit form + update | MVC golden baseline | `GET /Catalog/Edit/1` → 200 prefilled; `POST` → 302, `Details/1` renders the new name (Baseline §6.2) |
| Delete confirmation + delete | MVC golden baseline | `GET /Catalog/Delete/1` → 200; `POST` → 302, `Details/1` then 404 (Baseline §6.2) |
| Product images | MVC golden baseline | `GET /items/1/pic` → 200 `image/png`; `/items/0/pic` → 400 (Baseline §6.1) |
| Brand/type dropdown sources | MVC golden baseline (indirect) | `GET /api/brands` → the five-brand JSON array (Baseline §6.1); the same `GetCatalogBrands()`/`GetCatalogTypes()` calls back the Web Forms dropdowns |
| Page chrome (header, hero, footer) | MVC golden baseline | Rendered UI check, Baseline §6.4; the two layouts differ only by the `(MVC)`/`(Web Forms)` title suffix |
| `/About`, `/Contact` | **Not covered — static-only, retired** | No runtime capture exists and no replacement is produced (§4.1) |
| Mobile view switch | **Not covered — static-only and non-functional**, retired | FriendlyUrls never enabled (§4.2) |
| ViewState/postback mechanics, `__VIEWSTATE` payloads, `ScriptManager` script registrations | **Not covered — static-only, retired** | Web Forms-specific plumbing with no ASP.NET Core equivalent |
| Session footer values | **Not covered — accepted delta** | C-09 (§5) |

The five catalog capabilities the Web Forms UI actually provided (list, create, read, update,
delete) are each backed by a runtime-verified MVC golden output. Everything the Web Forms UI has
*in addition* to the MVC UI is either static template content or dead code, and none of it is
carried forward.

---

## 8. Follow-ups opened by this document

| For | Action |
| --- | --- |
| **NET-69** (`eShop.Web`) | Add the two redirects in §4.3: `/Default/index/{index:int}/size/{size:int}` → `/Catalog/Index?pageIndex={index}&pageSize={size}` and `/Default` → `/`, both 301. Also resolve C-09 for the MVC footer (remove the session line or re-source it from `Environment.MachineName` + process start time) |
| **NET-73** (cutover) | Confirm whether the Web Forms host is repointed at `eShop.Web` or decommissioned. If decommissioned, the §4.3 redirects are inert and may be dropped. Delete `eShopLegacyWebFormsSolution/` as part of cutover, and communicate the retired capabilities in §6 |
