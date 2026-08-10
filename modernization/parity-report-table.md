| Surface | Endpoint / operation | Legacy (baseline) | Modernized | Match? |
| --- | --- | --- | --- | --- |
| MVC UI | `GET /` | 200 text/html, 10 rows | 200 text/html | Yes |
| MVC UI | `GET /Catalog` | 200, identical to / | 200 text/html | Yes |
| MVC UI | `GET /Catalog/Index` | 200, byte-identical to /Catalog | identical | Yes |
| MVC UI | `GET /Catalog (row count)` | 10 catalog rows | 10 | Yes |
| MVC UI | `GET /Catalog (title)` | <title>Index - Catalog manager (MVC)</title> | <title>Index - Catalog manager (MVC)</title> | Yes |
| MVC UI | `GET /Catalog/Index?pageSize=2&pageIndex=1` | 200, 2 rows (second page) | 200 2 | Yes |
| MVC UI | `GET /Catalog/Details/1` | 200 detail view | 200 | Yes |
| MVC UI | `GET /Catalog/Details/999` | 404 unknown id | 404 | Yes |
| MVC UI | `GET /Catalog/Details (no id)` | 400 BadRequest | 400 | Yes |
| MVC UI | `GET /Catalog/Create` | 200 form with __RequestVerificationToken | 200 1 | Yes |
| MVC UI | `GET /Catalog/Edit/1` | 200 prefilled form | 200 | Yes |
| MVC UI | `GET /Catalog/Edit (no id)` | 400 BadRequest | 400 | Yes |
| MVC UI | `GET /Catalog/Delete/1` | 200 confirmation view | 200 | Yes |
| MVC UI | `GET /Catalog/Delete (no id)` | 400 BadRequest | 400 | Yes |
| MVC UI | `GET /nope` | 404 not found | 404 | Yes |
| MVC UI | `GET /Content/site.css` | 404 (IIS served static files) | 404 -> 200 | Intentional change |
| MVC UI | `GET /Default (retired Web Forms route)` | 200 Default.aspx (Web Forms) | 301 | Intentional change |
| MVC UI | `GET /Default/index/1/size/2 (retired)` | 200 paginated Default.aspx | 301 | Intentional change |
| MVC write path | `POST /Catalog/Create (with token)` | 302 Location: / | 302 http://localhost:8080/ | Yes |
| MVC write path | `  → created item visible in the index` | new item listed | 1 | Yes |
| MVC write path | `POST /Catalog/Create (no anti-forgery cookie)` | 500 HttpAntiForgeryException | 400 | Intentional change |
| MVC write path | `POST /Catalog/Edit (id=1, Renamed Hoodie)` | 302 Location: / | 302 http://localhost:8080/ | Yes |
| MVC write path | `  → GET /Catalog/Details/1 renders the new name` | Renamed Hoodie | 1 | Yes |
| MVC write path | `POST /Catalog/Delete (id=1)` | 302 Location: / | 302 http://localhost:8080/ | Yes |
| MVC write path | `  → GET /Catalog/Details/1 afterwards` | 404 | 404 | Yes |
| HTTP API | `GET /api/brands` | 200 application/json, 5 brands | 200 application/json | Yes |
| HTTP API | `  → body` | [{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}] | [{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}] | Yes |
| HTTP API | `GET /api/brands/1` | 200 {"Id":1,"Brand":"Azure"} | 200 {"Id":1,"Brand":"Azure"} | Yes |
| HTTP API | `GET /api/brands/99` | 404 empty body | 404 [] | Yes |
| HTTP API | `DELETE /api/brands/1` | 200 (no-op demo action) | 405 | Intentional change |
| HTTP API | `GET /api/files` | 200 BinaryFormatter payload (719 bytes, text/html) | 200 application/json [{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}] | Intentional change |
| HTTP API | `GET /items/1/pic` | 200 image/png, 151640 bytes | 200 image/png 151640 | Yes |
| HTTP API | `GET /items/0/pic` | 400 non-positive id | 400 | Yes |
| HTTP API | `GET /items/999/pic` | 404 absent picture | 404 | Yes |
| HTTP API | `GET /api` | 404 (unreachable dead route) | 404 | Yes |
| HTTP API | `GET /swagger/index.html` | n/a (no OpenAPI in the legacy app) | 200 | Intentional change |
| gRPC (WCF equivalent) | `reflection: eshop.catalog.v1.Catalog operations` | 10 SOAP [OperationContract] members | 10 | Yes |
| gRPC (WCF equivalent) | `GetCatalogBrands` | 5 brands, Azure/.NET/Visual Studio/SQL Server/Other | 5 | Yes |
| gRPC (WCF equivalent) | `GetCatalogTypes` | 4 types (Mug, T-Shirt, Sheet, USB Memory Stick) | 4 | Yes |
| gRPC (WCF equivalent) | `FindCatalogItem(1)` | item 1 = .NET Bot Black Hoodie, 19.50 | "name":".NETBotBlackHoodie","units":"19","nanos":500000000 | Yes |
| gRPC (WCF equivalent) | `FindCatalogItem(999)` | null | NotFound | Intentional change |
| gRPC (WCF equivalent) | `GetCatalogItems(0,0)` | 0 = no filter → all 12 seeded items | 12 | Yes |
| gRPC (WCF equivalent) | `GetCatalogItems(2,0)` | brand filter → 6 .NET items | 6 | Yes |
| gRPC (WCF equivalent) | `GetCatalogItems(2,2)` | brand+type filter → 3 items | 3 | Yes |
| gRPC (WCF equivalent) | `GetAvailableStock(2017-09-20, 1)` | 100 (seeded stock row) | {"available_stock":100} | Yes |
| gRPC (WCF equivalent) | `GetAvailableStock(no stock row)` | 0 | {} | Yes |
| gRPC (WCF equivalent) | `GetDiscount(2017-09-20)` | discount 1, size 0.3f | "size":0.30000001192092896,"start":"2017-09-18T00:00:00Z","end":"2017-09-21T00:00:00Z","id":1 | Yes |
| gRPC (WCF equivalent) | `GetDiscount(no discount that day)` | null | NotFound | Intentional change |
| gRPC (WCF equivalent) | `CreateAvailableStock then GetAvailableStock` | stock row upserted for the date | {}{"available_stock":42} | Yes |
| gRPC (WCF equivalent) | `CreateCatalogItem` | item added (void) | {} | Yes |
| gRPC (WCF equivalent) | `UpdateCatalogItem` | item updated (void) | {}1 | Yes |
| gRPC (WCF equivalent) | `RemoveCatalogItem` | item removed (void) | {}NotFound | Yes |
| gRPC (WCF equivalent) | `RemoveCatalogItem(unknown id)` | opaque FaultException | NotFound | Intentional change |
| gRPC (WCF equivalent) | `FindCatalogItem(-1)` | undefined (empty or fault) | InvalidArgument | Intentional change |
| Operations | `GET /health (web, api)` | n/a | 200 200 | Intentional change |
| Operations | `GET /health (grpc, h2c)` | n/a | 200 | Intentional change |
| Operations | `GET /ready (web, api)` | n/a | 200 200 | Intentional change |
