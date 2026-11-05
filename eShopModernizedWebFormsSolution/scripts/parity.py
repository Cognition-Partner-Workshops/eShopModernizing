"""HTTP-only parity probe. Run reset.ps1 -Start before each run for fresh state."""
import argparse
import hashlib
import http.cookiejar
from html.parser import HTMLParser
import json
from pathlib import Path
import re
import urllib.error
import urllib.parse
import urllib.request
from decimal import Decimal


class Node:
    def __init__(self, tag="", attrs=()):
        self.tag = tag
        self.attrs = dict(attrs)
        self.children = []

    def text(self):
        def raw(node):
            return "".join(raw(c) if isinstance(c, Node) else c for c in node.children)
        return " ".join(raw(self).split())

    def all(self, tag):
        found = []
        for child in self.children:
            if isinstance(child, Node):
                if child.tag == tag:
                    found.append(child)
                found.extend(child.all(tag))
        return found


class Document(HTMLParser):
    VOID = {"area", "base", "br", "col", "embed", "hr", "img", "image",
            "input", "link", "meta", "param", "source", "track", "wbr"}

    def __init__(self, html):
        super().__init__(convert_charrefs=True)
        self.root = Node()
        self.stack = [self.root]
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        node = Node(tag, attrs)
        self.stack[-1].children.append(node)
        if tag not in self.VOID:
            self.stack.append(node)

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)
        if tag not in self.VOID:
            self.handle_endtag(tag)

    def handle_endtag(self, tag):
        for i in range(len(self.stack) - 1, 0, -1):
            if self.stack[i].tag == tag:
                del self.stack[i:]
                break

    def handle_data(self, data):
        self.stack[-1].children.append(data)


def price(value):
    return str(Decimal(value).quantize(Decimal("0.01")))


class Site:
    def __init__(self, base, legacy):
        self.base = base
        self.legacy = legacy
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

    def get(self, path, data=None):
        request = urllib.request.Request(self.base + path, data=data)
        if data is not None:
            request.add_header("Content-Type", "application/x-www-form-urlencoded")
        try:
            response = self.opener.open(request, timeout=60)
        except urllib.error.HTTPError as error:
            response = error
        body = response.read()
        return response.status, body, urllib.parse.urlsplit(response.url).path

    def page(self, path):
        status, body, _ = self.get(path)
        return status, Document(body.decode("utf-8-sig", errors="replace")).root

    def summary(self, path):
        status, doc = self.page(path)
        titles = doc.all("title")
        return {"status": status, "title": titles[0].text() if titles else None}

    def listing(self, path):
        status, doc = self.page(path)
        items = []
        for tr in doc.all("tr"):
            cells = tr.all("td")
            links = [a for a in tr.all("a") if re.search(r"/Catalog/Details/\d+", a.attrs.get("href", ""))]
            if links and len(cells) >= 10:
                item_id = int(re.search(r"/Catalog/Details/(\d+)", links[0].attrs["href"])[1])
                items.append({"id": item_id, "name": cells[1].text(), "description": cells[2].text(),
                              "brand": cells[3].text(), "type": cells[4].text(), "price": price(cells[5].text())})
        pager = [n.text() for n in doc.all("span") if n.text().startswith("Showing ")]
        hidden = {a.text(): "esh-pager-item--hidden" in a.attrs.get("class", "")
                  for a in doc.all("a") if a.text() in ("Previous", "Next")}
        return {"status": status, "items": items, "pager": pager[0] if pager else None, "hidden": hidden}

    def details(self, item_id):
        status, doc = self.page(f"/Catalog/Details/{item_id}")
        terms = doc.all("dt")
        values = doc.all("dd")
        fields = {k.text(): v.text() for k, v in zip(terms, values)}
        result = {"status": status}
        if fields:
            result.update({key.lower(): fields.get(key) for key in ("Name", "Description", "Brand", "Type")})
            result["price"] = price(fields["Price"])
        return result

    def form(self, path):
        status, doc = self.page(path)
        if status != 200:
            raise RuntimeError(f"{self.base}{path}: cannot scrape form: HTTP {status}")
        controls = {}
        submit = None
        for node in doc.all("input"):
            name = node.attrs.get("name")
            if not name:
                continue
            kind = node.attrs.get("type", "text")
            if kind == "submit":
                submit = (name, node.attrs.get("value", ""))
            elif kind not in ("button", "checkbox", "radio"):
                controls[name] = node.attrs.get("value", "")
        for select in doc.all("select"):
            options = select.all("option")
            selected = next((o for o in options if "selected" in o.attrs), options[0] if options else None)
            if selected:
                controls[select.attrs["name"]] = selected.attrs.get("value", selected.text())
        return doc, controls, submit

    def key(self, controls, field):
        legacy_fields = {"Name": "Name", "Description": "Description", "CatalogBrandId": "Brand",
                         "CatalogTypeId": "Type", "Price": "Price", "AvailableStock": "Stock",
                         "RestockThreshold": "Restock", "MaxStockThreshold": "Maxstock",
                         "PictureFileName": "PictureFileName"}
        if not self.legacy:
            return "Item." + field
        suffix = legacy_fields[field]
        keys = [k for k in controls if k.split("$")[-1] == suffix]
        if not keys and field in ("CatalogBrandId", "CatalogTypeId"):
            suffix += "DropDownList"
            keys = [k for k in controls if k.split("$")[-1] == suffix]
        if len(keys) != 1:
            raise RuntimeError(f"Cannot identify {field} control from {list(controls)}")
        return keys[0]

    def edit_fields(self, item_id):
        _, controls, _ = self.form(f"/Catalog/Edit/{item_id}")
        result = {field: controls[self.key(controls, field)] for field in
                  ("Name", "Description", "CatalogBrandId", "CatalogTypeId", "Price",
                   "PictureFileName", "AvailableStock", "RestockThreshold", "MaxStockThreshold")}
        result["Price"] = price(result["Price"])
        return result

    def fingerprint(self):
        doc, _, _ = self.form("/Catalog/Create")
        options = {}
        for select in doc.all("select"):
            name = select.attrs["name"]
            key = "brands" if ("Brand" in name) else "types"
            options[key] = [{"id": int(o.attrs["value"]), "name": o.text()} for o in select.all("option")]
        listing = self.listing("/Default/index/0/size/100")
        if listing["status"] != 200:
            raise RuntimeError("Cannot scrape fingerprint list")
        options["items"] = [{k: item[k] for k in ("id", "name", "price")} for item in listing["items"]]
        return options

    def post(self, path, fields):
        _, controls, submit = self.form(path)
        for field, value in fields.items():
            controls[self.key(controls, field)] = value
        if submit:
            controls[submit[0]] = submit[1]
        # All hidden controls, including chunked VIEWSTATE, EVENTVALIDATION and
        # Razor antiforgery tokens, are submitted exactly as served.
        status, body, final_path = self.get(path, urllib.parse.urlencode(controls).encode())
        return {"status": status, "path": final_path}, Document(body.decode("utf-8-sig", errors="replace")).root


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--legacy", default="http://localhost:5201")
    parser.add_argument("--port", default="http://localhost:5200")
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[1] / "artifacts")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    sites = [Site(args.legacy, True), Site(args.port, False)]
    rows = []

    def compare(check, values):
        rows.append({"check": check, "legacy": values[0], "port": values[1],
                     "result": "match" if values[0] == values[1] else "red"})

    for path in ("/", "/Default"):
        compare(f"GET {path} status+title", [s.summary(path) for s in sites])
    for index, size in ((0, 10), (1, 10), (0, 5)):
        listings = [s.listing(f"/Default/index/{index}/size/{size}") for s in sites]
        compare(f"page {index} size {size} item ids",
                [{"status": x["status"], "ids": [i["id"] for i in x["items"]]} for x in listings])
        compare(f"page {index} size {size} pager text", [x["pager"] for x in listings])
        compare(f"page {index} size {size} prev/next hidden", [x["hidden"] for x in listings])

    fingerprints = [s.fingerprint() for s in sites]
    fingerprint = {"legacy": fingerprints[0], "port": fingerprints[1],
                   "identical": fingerprints[0] == fingerprints[1]}
    compare("shared_data_fingerprint", fingerprints)
    for item_id in range(1, 13):
        compare(f"Details/{item_id} fields", [s.details(item_id) for s in sites])
    compare("Edit/1 GET fields", [s.edit_fields(1) for s in sites])

    if any(len(f["items"]) != 12 for f in fingerprints):
        raise RuntimeError("Fresh-state precondition failed: expected 12 initial items. Run reset.ps1 -Start.")
    fields = {"Name": "Parity Runtime Item", "Description": "Runtime parity description",
              "CatalogBrandId": "2", "CatalogTypeId": "1", "Price": "17.25",
              "AvailableStock": "20", "RestockThreshold": "3", "MaxStockThreshold": "100"}
    compare("Create POST response", [s.post("/Catalog/Create", fields)[0] for s in sites])
    created = []
    for site in sites:
        items = site.listing("/Default/index/0/size/100")["items"]
        matches = [i for i in items if i["name"] == fields["Name"]]
        created.append(matches)
    compare("Create item id+list values", created)
    if any(len(items) != 1 for items in created):
        raise RuntimeError("Creation failed; cannot continue mutation checks")
    ids = [items[0]["id"] for items in created]
    compare("Edit POST response", [s.post(f"/Catalog/Edit/{item_id}",
            {"Name": "Parity Runtime Changed", "Price": "23.75"})[0] for s, item_id in zip(sites, ids)])
    compare("Edit changed Details", [s.details(item_id) for s, item_id in zip(sites, ids)])
    compare("Delete POST response", [s.post(f"/Catalog/Delete/{item_id}", {})[0] for s, item_id in zip(sites, ids)])
    compare("Delete absent from list", [item_id not in [i["id"] for i in s.listing("/Default/index/0/size/100")["items"]]
                                       for s, item_id in zip(sites, ids)])
    compare("Deleted Details status", [s.details(item_id)["status"] for s, item_id in zip(sites, ids)])

    rejected = []
    for site in sites:
        before = site.fingerprint()["items"]
        response, doc = site.post("/Catalog/Create", fields | {"Name": "Parity Invalid Price", "Price": "-1"})
        after = site.fingerprint()["items"]
        # Compare the outcome, not framework-specific error markup.
        rejected.append({"status": response["status"], "stayed_on_create": response["path"] == "/Catalog/Create",
                         "catalog_unchanged": before == after,
                         "price_validation_visible": "Price" in doc.text() and "positive number" in doc.text(),
                         "unexpected_items": [i for i in after if i not in before]})
    compare("Invalid Create bad price rejected", rejected)
    for path in ("/About", "/Contact"):
        compare(f"GET {path} status+title", [s.summary(path) for s in sites])
    for path in ("/About.aspx", "/Contact.aspx", "/Default.aspx"):
        compare(f"GET {path} status+title", [s.summary(path) for s in sites])
    for path in ("/Pics/1.png", "/Content/site.css"):
        values = []
        for site in sites:
            status, body, _ = site.get(path)
            values.append({"status": status, "bytes": len(body), "sha256": hashlib.sha256(body).hexdigest()})
        compare(f"Asset {path}", values)
    compare("Unknown Details/999 status", [s.details(999)["status"] for s in sites])

    (args.output / "parity.json").write_text(json.dumps(rows, indent=2), encoding="utf-8")
    (args.output / "shared_data_fingerprint.json").write_text(json.dumps(fingerprint, indent=2), encoding="utf-8")
    lines = ["| Check | Legacy | Port | Result |", "|---|---|---|---|"]
    for row in rows:
        def cell(value):
            return json.dumps(value, ensure_ascii=True, separators=(",", ":")).replace("|", "\\|")
        lines.append(f'| {row["check"]} | {cell(row["legacy"])} | {cell(row["port"])} | {row["result"]} |')
    table = "\n".join(lines)
    (args.output / "parity.md").write_text(table + "\n", encoding="utf-8")
    print(table)
    print("\nshared_data_fingerprint:")
    print(json.dumps(fingerprint, indent=2))
    print(f'\nRows: {len(rows)}; match: {sum(r["result"] == "match" for r in rows)}; red: {sum(r["result"] == "red" for r in rows)}')


if __name__ == "__main__":
    main()
