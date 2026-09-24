using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Services;
using Microsoft.Extensions.Logging;

namespace eShopLegacyMVC.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ILogger<CatalogController> _log;
        private readonly ICatalogService service;

        public CatalogController(ICatalogService service, ILogger<CatalogController> logger)
        {
            this.service = service;
            _log = logger;
        }

        // GET /[?pageSize=3&pageIndex=10]
        public ActionResult Index(int pageSize = 10, int pageIndex = 0)
        {
            _log.LogInformation("Now loading... /Catalog/Index?pageSize={PageSize}&pageIndex={PageIndex}", pageSize, pageIndex);
            var paginatedItems = service.GetCatalogItemsPaginated(pageSize, pageIndex);
            ChangeUriPlaceholder(paginatedItems.Data);
            return View(paginatedItems);
        }

        // GET: Catalog/Details/5
        public ActionResult Details(int? id)
        {
            _log.LogInformation("Now loading... /Catalog/Details?id={Id}", id);
            if (id == null)
            {
                return BadRequest();
            }
            CatalogItem catalogItem = service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }
            AddUriPlaceHolder(catalogItem);

            return View(catalogItem);
        }

        // GET: Catalog/Create
        public ActionResult Create()
        {
            _log.LogInformation("Now loading... /Catalog/Create");
            ViewBag.CatalogBrandId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogBrands(), "Id", "Brand");
            ViewBag.CatalogTypeId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogTypes(), "Id", "Type");
            return View(new CatalogItem());
        }

        // POST: Catalog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind("Id,Name,Description,Price,PictureFileName,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder")] CatalogItem catalogItem)
        {
            _log.LogInformation("Now processing... /Catalog/Create?catalogItemName={Name}", catalogItem.Name);
            if (ModelState.IsValid)
            {
                service.CreateCatalogItem(catalogItem);
                return RedirectToAction("Index");
            }

            ViewBag.CatalogBrandId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            return View(catalogItem);
        }

        // GET: Catalog/Edit/5
        public ActionResult Edit(int? id)
        {
            _log.LogInformation("Now loading... /Catalog/Edit?id={Id}", id);
            if (id == null)
            {
                return BadRequest();
            }
            CatalogItem catalogItem = service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }
            AddUriPlaceHolder(catalogItem);
            ViewBag.CatalogBrandId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            return View(catalogItem);
        }

        // POST: Catalog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind("Id,Name,Description,Price,PictureFileName,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder")] CatalogItem catalogItem)
        {
            _log.LogInformation("Now processing... /Catalog/Edit?id={Id}", catalogItem.Id);
            if (ModelState.IsValid)
            {
                service.UpdateCatalogItem(catalogItem);
                return RedirectToAction("Index");
            }
            ViewBag.CatalogBrandId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogBrands(), "Id", "Brand", catalogItem.CatalogBrandId);
            ViewBag.CatalogTypeId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(service.GetCatalogTypes(), "Id", "Type", catalogItem.CatalogTypeId);
            return View(catalogItem);
        }

        // GET: Catalog/Delete/5
        public ActionResult Delete(int? id)
        {
            _log.LogInformation("Now loading... /Catalog/Delete?id={Id}", id);
            if (id == null)
            {
                return BadRequest();
            }
            CatalogItem catalogItem = service.FindCatalogItem(id.Value);
            if (catalogItem == null)
            {
                return NotFound();
            }
            AddUriPlaceHolder(catalogItem);

            return View(catalogItem);
        }

        // POST: Catalog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            _log.LogInformation("Now processing... /Catalog/DeleteConfirmed?id={Id}", id);
            CatalogItem catalogItem = service.FindCatalogItem(id);
            service.RemoveCatalogItem(catalogItem);
            return RedirectToAction("Index");
        }

        private void ChangeUriPlaceholder(IEnumerable<CatalogItem> items)
        {
            foreach (var catalogItem in items)
            {
                AddUriPlaceHolder(catalogItem);
            }
        }

        private void AddUriPlaceHolder(CatalogItem item)
        {
            item.PictureUri = this.Url.RouteUrl(PicController.GetPicRouteName, new { catalogItemId = item.Id }, this.Request.Scheme);
        }
    }
}
