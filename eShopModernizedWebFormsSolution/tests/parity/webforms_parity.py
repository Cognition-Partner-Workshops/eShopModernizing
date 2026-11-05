"""Before/after behavioural harness: drives the legacy ASPX app and the .NET 8 port
through the same HTTP scenario and diffs normalised results.

Usage: python webforms_parity.py --legacy http://localhost:50586 --modern http://localhost:50587 --out result.json
Both apps must start from the same seed (mock mode, or freshly created databases).
"""
import argparse
import hashlib
import html
import http.cookiejar
import json
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36"


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


class Client:
    def __init__(self, base, legacy):
        self.base = base.rstrip("/")
        self.legacy = legacy
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect)

    def request(self, path, data=None):
        req = urllib.request.Request(self.base + path, data=data, headers={"User-Agent": UA})
        try:
            with self.opener.open(req, timeout=120) as resp:
                return resp.status, resp.headers.get("Location"), resp.read().decode("utf-8", "replace")
        except urllib.error.HTTPError as err:
            return err.code, err.headers.get("Location"), err.read().decode("utf-8", "replace")

    def get(self, path):
        return self.request(path)

    def submit(self, path, fields, button):
        status, _, page = self.get(path)
        if status != 200:
            # The form could not be rendered (e.g. legacy YSOD), so there is nothing to post back.
            return "form GET %d: %s" % (status, error_summary(page)), None, page
        if self.legacy:
            form = dict(re.findall(r'name="(__[A-Z]+)" id="[^"]*" value="([^"]*)"', page))
            form.update({"ctl00$MainContent$" + k: v for k, v in fields.items()})
            match = re.search(r'<input type="submit" name="([^"]+)" value="(\[ %s \])"' % button, page)
            if match is None:
                return "no [ %s ] button" % button, None, page
            name, value = match.groups()
            form[name] = value
        else:
            form = dict(fields)
            form["__RequestVerificationToken"] = re.search(
                r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', page).group(1)
        return self.request(path, urllib.parse.urlencode(form).encode())


def text(fragment):
    return " ".join(html.unescape(re.sub(r"<[^>]+>", " ", fragment)).split())


def error_summary(page):
    m = re.search(r"<title>(.*?)</title>", page, re.S)
    return text(m.group(1)) if m else text(page)[:200]


def title(page):
    m = re.search(r"<title>(.*?)</title>", page, re.S)
    return text(m.group(1)) if m else None


def catalog(page):
    rows = []
    for row in re.findall(r"<tr>(.*?)</tr>", page, re.S):
        cells = re.findall(r"<td[^>]*>(.*?)</td>", row, re.S)
        if not cells:
            continue
        # Legacy Default.aspx emits <image ... src='...'>, which browsers parse as <img>.
        img = re.search(r"<im(?:g|age)[^>]*src=[\"']([^\"']+)", row)
        links = re.findall(r'href="(/Catalog/[A-Za-z]+/\d+)"', row)
        rows.append({"img": img and img.group(1).replace("../", "/"),
                     "cells": [text(c) for c in cells], "links": links})
    pager = re.search(r"Showing[^<]*", page)
    nav = re.findall(r'id="MainContent_Pagination(Previous|Next)" class="([^"]+)" href="([^"]+)"', page)
    return {"rows": rows, "pager": pager and " ".join(pager.group(0).split()), "nav": nav}


def details(page):
    pairs = re.findall(r"<dt>(.*?)</dt>\s*<dd[^>]*>(.*?)</dd>", page, re.S)
    img = re.search(r'<img class="col-md-6 esh-picture" src="([^"]+)"', page)
    return {"fields": [[text(k), text(v)] for k, v in pairs], "img": img and img.group(1).replace("../", "/")}


def select_options(page):
    return [[v, text(t)] for v, t in re.findall(r'<option[^>]*value="(\d+)"[^>]*>([^<]*)</option>', page)]


def validators(page):
    found = []
    for span in re.findall(r"<span[^>]*>[^<]*</span>", page):
        message = text(span)
        if "required" in message or "between" in message:
            found.append(message)
    return found


ITEM = {"Name": "Parity Mug", "Description": "Parity mug description", "Brand": "3", "Type": "4",
        "Price": "21.75", "Stock": "7", "Restock": "2", "Maxstock": "40"}


def find_item_id(client, name):
    for index in range(0, 5):
        _, _, page = client.get("/Default/index/%d/size/10" % index)
        for row in catalog(page)["rows"]:
            if name in row["cells"] and row["links"]:
                return int(row["links"][0].rsplit("/", 1)[1])
    return None


def scenario(client):
    """Returns an ordered list of (step, observation) for one app."""
    obs = []
    for path in ["/", "/Default", "/Default/index/1/size/10", "/Default/index/0/size/5",
                 "/Catalog/Create", "/Catalog/Edit/1", "/Catalog/Details/1", "/Catalog/Delete/1",
                 "/Pics/1.png", "/Pics/dummy.png", "/images/brand.png", "/Content/Site.css"]:
        status, _, page = client.get(path)
        obs.append(("GET " + path, {"status": status, "title": title(page) if status == 200 and "<title>" in page else None}))
    for path in ["/", "/Default/index/1/size/10", "/Default/index/2/size/5"]:
        obs.append(("list " + path, catalog(client.get(path)[2])))
    for item_id in [1, 9, 12]:
        obs.append(("details %d" % item_id, details(client.get("/Catalog/Details/%d" % item_id)[2])))
    _, _, create_page = client.get("/Catalog/Create")
    obs.append(("create form options", select_options(create_page)))
    obs.append(("create form client validators", validators(create_page)))
    _, _, edit_page = client.get("/Catalog/Edit/1")
    obs.append(("edit form options", select_options(edit_page)))
    obs.append(("edit form picture", re.findall(r'value="([^"]*)" readonly="readonly"', edit_page)))

    status, location, _ = client.submit("/Catalog/Create", ITEM, "Create")
    obs.append(("POST create valid", {"status": status, "location": location}))
    new_id = find_item_id(client, ITEM["Name"])
    obs.append(("created item details", details(client.get("/Catalog/Details/%s" % new_id)[2])))
    obs.append(("list after create", catalog(client.get("/Default/index/1/size/10")[2])))

    edit = dict(ITEM, Name="Parity Mug v2", Price="1,234.50", Stock="9")
    if client.legacy:
        edit["BrandDropDownList"] = edit.pop("Brand")
        edit["TypeDropDownList"] = edit.pop("Type")
    edit["PictureFileName"] = "dummy.png"
    status, location, _ = client.submit("/Catalog/Edit/%s" % new_id, edit, "Save")
    obs.append(("POST edit valid", {"status": status, "location": location}))
    obs.append(("edited item details", details(client.get("/Catalog/Details/%s" % new_id)[2])))

    status, location, _ = client.submit("/Catalog/Delete/%s" % new_id, {}, "Delete")
    obs.append(("POST delete", {"status": status, "location": location}))
    obs.append(("details after delete status", client.get("/Catalog/Details/%s" % new_id)[0] != 200))
    obs.append(("list after delete", catalog(client.get("/")[2])["pager"]))
    return obs, new_id


def shown_errors(page):
    return [text(span) for span in re.findall(r"<span[^>]*>[^<]*</span>", page)
            if ("required" in span or "between" in span) and re.search(r"display:\s*inline", span.split(">")[0])]


def invalid_posts(client):
    """Server-side handling of posts that bypass client-side validation (no JS)."""
    results = {}
    for field, value in [("Name", ""), ("Price", "abc"), ("Price", "1.234"), ("Stock", "-1"), ("Restock", "x")]:
        fields = dict(ITEM, Name="Invalid %s %s" % (field, value))
        fields[field] = value
        status, location, page = client.submit("/Catalog/Create", fields, "Create")
        results["%s=%r" % (field, value)] = {"status": status, "redirect": location,
                                             "shown_errors": shown_errors(page)}
    results["pager_after_invalid_posts"] = catalog(client.get("/")[2])["pager"]
    return results


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--legacy", required=True)
    parser.add_argument("--modern", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--invalid", action="store_true", help="also probe no-JS invalid posts (mutates data; run last)")
    args = parser.parse_args()

    legacy_obs, legacy_id = scenario(Client(args.legacy, legacy=True))
    modern_obs, modern_id = scenario(Client(args.modern, legacy=False))
    steps = []
    for (name, legacy_value), (_, modern_value) in zip(legacy_obs, modern_obs):
        steps.append({"step": name, "match": legacy_value == modern_value, "legacy": legacy_value, "modern": modern_value})
    result = {"legacy": args.legacy, "modern": args.modern, "created_ids": [legacy_id, modern_id],
              "steps": steps, "passed": sum(s["match"] for s in steps), "failed": sum(not s["match"] for s in steps)}
    for s in steps:
        s["sha256"] = hashlib.sha256(json.dumps(s["legacy"], sort_keys=True).encode()).hexdigest()
    if args.invalid:
        result["invalid_posts"] = {"legacy": invalid_posts(Client(args.legacy, True)),
                                   "modern": invalid_posts(Client(args.modern, False))}
    with open(args.out, "w", encoding="utf-8") as handle:
        json.dump(result, handle, indent=2)
    for s in steps:
        print(("PASS " if s["match"] else "FAIL ") + s["step"])
    print("passed=%d failed=%d" % (result["passed"], result["failed"]))
    return 0 if result["failed"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
