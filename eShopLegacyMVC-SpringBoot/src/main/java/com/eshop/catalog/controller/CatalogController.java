package com.eshop.catalog.controller;

import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.service.CatalogService;
import jakarta.validation.Valid;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.validation.BindingResult;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;

@Controller
@RequestMapping("/")
public class CatalogController {

  private static final Logger log = LoggerFactory.getLogger(CatalogController.class);

  private final CatalogService catalogService;

  public CatalogController(CatalogService catalogService) {
    this.catalogService = catalogService;
  }

  @GetMapping
  public String index(
      @RequestParam(defaultValue = "10") int pageSize,
      @RequestParam(defaultValue = "0") int pageIndex,
      Model model) {
    log.info("Now loading... /Catalog/Index?pageSize={}&pageIndex={}", pageSize, pageIndex);
    PaginatedItemsDto<CatalogItem> paginatedItems =
        catalogService.getCatalogItemsPaginated(pageSize, pageIndex);
    for (CatalogItem item : paginatedItems.data()) {
      item.setPictureUri("/items/" + item.getId() + "/pic");
    }
    model.addAttribute("paginatedItems", paginatedItems);
    model.addAttribute("title", "Index");
    return "catalog/index";
  }

  @GetMapping("/catalog/details/{id}")
  public String details(@PathVariable int id, Model model) {
    log.info("Now loading... /Catalog/Details?id={}", id);
    CatalogItem catalogItem = catalogService.findCatalogItem(id);
    if (catalogItem == null) {
      return "error";
    }
    catalogItem.setPictureUri("/items/" + catalogItem.getId() + "/pic");
    model.addAttribute("catalogItem", catalogItem);
    model.addAttribute("title", "Details");
    return "catalog/details";
  }

  @GetMapping("/catalog/create")
  public String createForm(Model model) {
    log.info("Now loading... /Catalog/Create");
    populateDropdowns(model);
    model.addAttribute("catalogItem", new CatalogItem());
    model.addAttribute("title", "Create");
    return "catalog/create";
  }

  @PostMapping("/catalog/create")
  public String create(@Valid CatalogItem catalogItem, BindingResult result, Model model) {
    log.info("Now processing... /Catalog/Create?catalogItemName={}", catalogItem.getName());
    if (result.hasErrors()) {
      populateDropdowns(model);
      model.addAttribute("title", "Create");
      return "catalog/create";
    }
    catalogService.createCatalogItem(catalogItem);
    return "redirect:/";
  }

  @GetMapping("/catalog/edit/{id}")
  public String editForm(@PathVariable int id, Model model) {
    log.info("Now loading... /Catalog/Edit?id={}", id);
    CatalogItem catalogItem = catalogService.findCatalogItem(id);
    if (catalogItem == null) {
      return "error";
    }
    catalogItem.setPictureUri("/items/" + catalogItem.getId() + "/pic");
    populateDropdowns(model);
    model.addAttribute("catalogItem", catalogItem);
    model.addAttribute("title", "Edit");
    return "catalog/edit";
  }

  @PostMapping("/catalog/edit/{id}")
  public String edit(
      @PathVariable int id, @Valid CatalogItem catalogItem, BindingResult result, Model model) {
    log.info("Now processing... /Catalog/Edit?id={}", catalogItem.getId());
    if (result.hasErrors()) {
      catalogItem.setPictureUri("/items/" + catalogItem.getId() + "/pic");
      populateDropdowns(model);
      model.addAttribute("title", "Edit");
      return "catalog/edit";
    }
    catalogService.updateCatalogItem(catalogItem);
    return "redirect:/";
  }

  @GetMapping("/catalog/delete/{id}")
  public String deleteForm(@PathVariable int id, Model model) {
    log.info("Now loading... /Catalog/Delete?id={}", id);
    CatalogItem catalogItem = catalogService.findCatalogItem(id);
    if (catalogItem == null) {
      return "error";
    }
    catalogItem.setPictureUri("/items/" + catalogItem.getId() + "/pic");
    model.addAttribute("catalogItem", catalogItem);
    model.addAttribute("title", "Delete");
    return "catalog/delete";
  }

  @PostMapping("/catalog/delete/{id}")
  public String deleteConfirmed(@PathVariable int id) {
    log.info("Now processing... /Catalog/DeleteConfirmed?id={}", id);
    CatalogItem catalogItem = catalogService.findCatalogItem(id);
    if (catalogItem == null) {
      return "error";
    }
    catalogService.removeCatalogItem(catalogItem);
    return "redirect:/";
  }

  private void populateDropdowns(Model model) {
    List<CatalogBrand> brands = catalogService.getCatalogBrands();
    List<CatalogType> types = catalogService.getCatalogTypes();
    model.addAttribute("brands", brands);
    model.addAttribute("types", types);
  }
}
