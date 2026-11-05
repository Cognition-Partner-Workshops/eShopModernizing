import hashlib
import html
import json
import re
import sys
from html.parser import HTMLParser
from http.cookiejar import CookieJar
from urllib.parse import urlencode, urljoin
from urllib.request import Request, build_opener, HTTPCookieProcessor, HTTPRedirectHandler

ROOT = r"C:\Users\Administrator\scratch\parity"
APPS = {"legacy": "http://localhost:5101/", "port": "http://localhost:5100/"}

class Page(HTMLParser):
    def __init__(self):
        super().__init__(); self.texts=[]; self.rows=[]; self.row=[]; self.in_row=False
        self.links=[]; self.inputs=[]; self.selects=[]; self.select=None; self.field=None; self.in_link=False
    def handle_starttag(self, tag, attrs):
        a=dict(attrs)
        if tag == "tr": self.in_row=True; self.row=[]
        if tag == "a" and "href" in a: self.links.append({"text":"", "href":a["href"], "class":a.get("class","")}); self.in_link=True
        if tag == "input": self.inputs.append(a)
        if tag == "select": self.select={"name":a.get("name",""),"options":[]}; self.selects.append(self.select)
        if tag == "option" and self.select is not None: self.field=a.get("value","")
    def handle_endtag(self, tag):
        if tag == "tr" and self.in_row: self.rows.append(self.row); self.in_row=False
        if tag == "select": self.select=None
        if tag == "option": self.field=None
        if tag == "a": self.in_link=False
    def handle_data(self, data):
        s=re.sub(r"\s+", " ", html.unescape(data)).strip()
        if not s: return
        self.texts.append(s)
        if self.in_row: self.row.append(s)
        if self.links and self.in_link: self.links[-1]["text"] += (" " if self.links[-1]["text"] else "") + s
        if self.select is not None and self.field is not None: self.select["options"].append({"value":self.field,"text":s})

class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl): return None

class Client:
    def __init__(self, base): self.base=base; self.opener=build_opener(HTTPCookieProcessor(CookieJar()),NoRedirect())
    def request(self,path,method="GET",data=None,accept=None):
        u=urljoin(self.base,path); body=None; headers={"User-Agent":"parity-harness"}
        if accept: headers["Accept"]=accept
        if data is not None: body=urlencode(data,doseq=True).encode(); headers["Content-Type"]="application/x-www-form-urlencoded"
        try:
            r=self.opener.open(Request(u,data=body,headers=headers,method=method),timeout=20)
            return {"status":r.status,"location":r.headers.get("Location"),"content_type":r.headers.get("Content-Type",""),"body":r.read()}
        except Exception as e:
            if hasattr(e,"read"): return {"status":e.code,"location":e.headers.get("Location"),"content_type":e.headers.get("Content-Type",""),"body":e.read()}
            return {"status":0,"error":str(e),"body":b"","content_type":""}
    def page(self,path):
        r=self.request(path); p=Page(); p.feed(r["body"].decode("utf-8","replace")); r["parsed"]=p; return r

def clean(s): return re.sub(r"\s+", " ", html.unescape(s or "")).strip()
def rows(r):
    out=[]
    for tr in re.findall(r'<tr\b[^>]*>(.*?)</tr>', r["body"].decode("utf8","replace"),re.I|re.S):
        cells=[clean(re.sub(r'<[^>]+>','',x)) for x in re.findall(r'<td\b[^>]*>(.*?)</td>',tr,re.I|re.S)]
        if len(cells) >= 6:
            m=re.search(r'/items/(\d+)/pic',tr)
            if not m: raise ValueError('Catalog row has no item id')
            out.append({"id":int(m.group(1)),"name":cells[1],"brand":cells[3],"type":cells[4],"price":cells[5]})
    return out
def page_data(r):
    text=" ".join(r["parsed"].texts)
    m=re.search(r"Showing\s+(\d+)\s+of\s+(\d+)\s+products\s+-\s+Page\s+(\d+)\s+-\s+(\d+)",text,re.I)
    return {"status":r["status"],"items":rows(r),"pagination":m.group(0) if m else None,
            "links":sorted({x["text"].strip():x["href"] for x in r["parsed"].links if x["text"].strip() in ("Next","Previous")}.items())}
def norm_json(body):
    try: return json.loads(body.decode("utf8"))
    except Exception: return body.decode("utf8","replace")
def result(a,b,explain=None):
    ok=a==b; return {"legacy":a,"port":b,"result":"explained" if explain and not ok else ("match" if ok else "red"), **({"explanation":explain} if explain and not ok else {})}
def record(check,a,b,explain=None): return {"check":check,**result(a,b,explain)}
def token(client,path):
    r=client.page(path); vals=[x.get("value") for x in r["parsed"].inputs if x.get("name")=="__RequestVerificationToken"]
    return vals[0] if vals else None,r
def form_fields(p):
    return {"inputs":sorted([{"name":x["name"],"type":x.get("type","text"),"value":x.get("value","")} for x in p.inputs if x.get("name") and x["name"]!="__RequestVerificationToken"],key=lambda x:x["name"]),"antiforgery":any(x.get("name")=="__RequestVerificationToken" for x in p.inputs),"selects":p.selects}
def detail_value(r):
    body=r["body"].decode("utf8","replace")
    fields={clean(re.sub(r'<[^>]+>','',a)):clean(re.sub(r'<[^>]+>','',b)) for a,b in re.findall(r'<dt\b[^>]*>(.*?)</dt>\s*<dd\b[^>]*>(.*?)</dd>',body,re.I|re.S)}
    if r["status"]==200 and fields: return {"status":r["status"],"fields":fields}
    if "json" in r["content_type"]: return {"status":r["status"],"content_type":r["content_type"],"json":norm_json(r["body"])}
    title=re.search(r'<title>(.*?)</title>',body,re.I|re.S)
    return {"status":r["status"],"content_type":r["content_type"],"length":len(r["body"]),"title":clean(title.group(1)) if title else None}
def binary_brands(body):
    strings=[]
    for m in re.finditer(rb'\x06....([\x01-\x7f])',body,re.S):
        n=m.group(1)[0]; start=m.end(); value=body[start:start+n]
        if len(value)==n and all(32<=x<127 for x in value):
            strings.append({"Id":int.from_bytes(body[m.start()-4:m.start()],"little"),"Brand":value.decode("ascii")})
    return sorted(strings,key=lambda x:x["Id"])
def validation(r):
    body=r["body"].decode("utf8","replace")
    messages=[clean(re.sub(r'<[^>]+>','',x)) for x in re.findall(r'<span\b[^>]*field-validation-error[^>]*>(.*?)</span>',body,re.I|re.S)]
    return {"status":r["status"],"messages":[x for x in messages if x]}
def md_value(value):
    if isinstance(value,dict) and "items" in value and "status" in value:
        item_text="; ".join(f"{x['id']}:{x['name']}/{x['brand']}/{x['type']}/{x['price']}" for x in value["items"])
        return f"status={value['status']}; items=[{item_text}]; pagination={value.get('pagination')}; links={value.get('links')}"
    return json.dumps(value,ensure_ascii=False,separators=(",",":"))

def main():
    cs={k:Client(v) for k,v in APPS.items()}; records=[]; data={}
    for path in ["/", "/Catalog/Index", "/Catalog/Index?pageSize=10&pageIndex=1", "/Catalog/Index?pageSize=5&pageIndex=2"]:
        vals={k:page_data(cs[k].page(path)) for k in cs}; data[path]=vals
        records.append(record("GET "+path, vals["legacy"], vals["port"]))
    fps={}
    for k,c in cs.items():
        allp=c.page("/Catalog/Index?pageSize=100"); brands=c.request("/api/brands",accept="application/json"); create=c.page("/Catalog/Create")
        fps[k]={"items":rows(allp),"brands":norm_json(brands["body"]),"types":next((x["options"] for x in create["parsed"].selects if x["name"]=="CatalogTypeId"),[])}
    with open(ROOT+"\\fingerprint.json","w",encoding="utf8") as f: json.dump(fps,f,indent=2)
    records.append(record("fingerprint items/brands/types",fps["legacy"],fps["port"]))
    for path in ["/Catalog/Details/1","/Catalog/Details/9999","/Catalog/Details","/api"]:
        vals={k:detail_value(cs[k].request(path,accept="application/json")) for k in cs}; records.append(record("GET "+path,vals["legacy"],vals["port"]))
    forms={}; created={};
    for k,c in cs.items():
        t,r=token(c,"/Catalog/Create"); forms[k]=form_fields(r["parsed"])
        payload={"__RequestVerificationToken":t,"Name":"Parity New Item","Description":"Parity description","CatalogBrandId":"1","CatalogTypeId":"1","Price":"987.65","AvailableStock":"1","RestockThreshold":"1","MaxStockThreshold":"2","OnReorder":"false"}
        post=c.request("/Catalog/Create","POST",payload); follow=c.page("/Catalog/Index?pageSize=100"); found=[x for x in rows(follow) if x["name"]=="Parity New Item"]; created[k]={"post":{"status":post["status"],"location":post.get("location")},"item":found[-1] if found else None}
        created[k]["last_page"]=page_data(c.page("/Catalog/Index?pageSize=10&pageIndex=1"))
        created[k]["details"]=detail_value(c.request("/Catalog/Details/"+str(created[k]["item"]["id"]))) if created[k]["item"] else None
        bad=c.request("/Catalog/Create","POST",dict(payload,Name="",Price="bad")); created[k]["invalid"]=validation(bad)
    records.append(record("GET /Catalog/Create fields/options",forms["legacy"],forms["port"])); records.append(record("POST /Catalog/Create",created["legacy"]["post"],created["port"]["post"])); records.append(record("POST invalid Create",created["legacy"]["invalid"],created["port"]["invalid"])); records.append(record("created item/index last page",created["legacy"]["last_page"],created["port"]["last_page"])); records.append(record("created item Details",created["legacy"]["details"],created["port"]["details"]))
    edited={}; deleted={}
    for k,c in cs.items():
        item=created[k]["item"]; iid=item["id"] if item else 13; t,edit_get=token(c,"/Catalog/Edit/"+str(iid)); payload={"__RequestVerificationToken":t,"Id":str(iid),"Name":"Parity Edited Item","Description":"Parity description","CatalogBrandId":"1","CatalogTypeId":"1","Price":"123.45","AvailableStock":"1","RestockThreshold":"1","MaxStockThreshold":"2","OnReorder":"false"}; p=c.request("/Catalog/Edit/"+str(iid),"POST",payload); edited[k]={"get":{"status":edit_get["status"],"form":form_fields(edit_get["parsed"])},"post":{"status":p["status"],"location":p.get("location")},"detail":detail_value(c.request("/Catalog/Details/"+str(iid)))}
    records.append(record("GET Edit fields/options",edited["legacy"]["get"],edited["port"]["get"])); records.append(record("POST Edit + Details",{"post":edited["legacy"]["post"],"detail":edited["legacy"]["detail"]},{"post":edited["port"]["post"],"detail":edited["port"]["detail"]}))
    for k,c in cs.items():
        iid=created[k]["item"]["id"]; confirm=c.page("/Catalog/Delete/"+str(iid)); t,_=token(c,"/Catalog/Delete/"+str(iid)); p=c.request("/Catalog/Delete/"+str(iid),"POST",{"__RequestVerificationToken":t,"id":str(iid)}); deleted[k]={"confirm":confirm["status"],"post":{"status":p["status"],"location":p.get("location")},"after":detail_value(c.request("/Catalog/Details/"+str(iid))),"total":page_data(c.page("/Catalog/Index?pageSize=100"))["pagination"]}
    records.append(record("Delete confirmation/post/not-found/total",deleted["legacy"],deleted["port"]))
    for path in ["/items/1/pic","/items/2/pic","/items/3/pic","/items/4/pic","/items/5/pic","/items/6/pic","/items/7/pic","/items/8/pic","/items/9/pic","/items/10/pic","/items/11/pic","/items/12/pic","/items/9999/pic"]:
        vals={k:(lambda r:{"status":r["status"],"content_type":r.get("content_type"),"length":len(r["body"]),"sha256":hashlib.sha256(r["body"]).hexdigest()})(cs[k].request(path)) for k in cs}; records.append(record("GET "+path,vals["legacy"],vals["port"]))
    files={k:cs[k].request("/api/files",accept="application/json") for k in cs}; fv={k:{"status":v["status"],"content_type":v["content_type"],"length":len(v["body"]),"brands":norm_json(v["body"]) if k=="port" else binary_brands(v["body"])} for k,v in files.items()}; records.append(record("GET /api/files",fv["legacy"],fv["port"],"legacy BinaryFormatter versus port JSON; Id/Brand sets match" if fv["legacy"]["status"]==fv["port"]["status"]==200 and fv["legacy"]["brands"]==fv["port"]["brands"] else None))
    for path in ["/api/brands","/api/brands/1","/api/brands/9999"]:
        vals={k:cs[k].request(path,accept="application/json") for k in cs}; records.append(record("GET "+path,{"status":vals["legacy"]["status"],"body":norm_json(vals["legacy"]["body"])},{"status":vals["port"]["status"],"body":norm_json(vals["port"]["body"])}))
    vals={k:cs[k].request("/api/brands/9999","DELETE",accept="application/json") for k in cs}; records.append(record("DELETE /api/brands/9999",{ "status":vals["legacy"]["status"],"body":norm_json(vals["legacy"]["body"])},{"status":vals["port"]["status"],"body":norm_json(vals["port"]["body"])}))
    with open(ROOT+"\\parity.json","w",encoding="utf8") as f: json.dump(records,f,indent=2)
    lines=["| check | legacy | port | result |","|---|---|---|---|"]
    for x in records:
        legacy=md_value(x["legacy"]).replace("|","/")
        port=("same as legacy" if x["result"]=="match" else md_value(x["port"])).replace("|","/")
        lines.append("| "+x["check"].replace("|","/")+" | "+legacy+" | "+port+" | "+x["result"]+(" ("+x.get("explanation","")+")" if x.get("explanation") else "")+" |")
    with open(ROOT+"\\parity.md","w",encoding="utf8") as f: f.write("\n".join(lines)+"\n")
    print("records",len(records),"red",sum(x["result"]=="red" for x in records),"match",sum(x["result"]=="match" for x in records),"explained",sum(x["result"]=="explained" for x in records))
if __name__ == "__main__":
    import os
    os.makedirs(ROOT,exist_ok=True); main()
