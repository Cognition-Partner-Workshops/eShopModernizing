package com.eshop.catalog.controller;

import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.service.ICatalogService;

import jakarta.validation.Valid;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.validation.BindingResult;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.ModelAttribute;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.server.ResponseStatusException;

@Controller
@RequestMapping("/catalog")
public class CatalogController {

    private static final Logger log = LoggerFactory.getLogger(CatalogController.class);

    private final ICatalogService service;

    public CatalogController(ICatalogService service) {
        this.service = service;
    }

    @GetMapping
    public String index(@RequestParam(defaultValue = "10") int pageSize,
                        @RequestParam(defaultValue = "0") int pageIndex,
                        Model model) {
        log.info("Now loading... /Catalog/Index?pageSize={}&pageIndex={}", pageSize, pageIndex);
        var paginatedItems = service.getCatalogItemsPaginated(pageSize, pageIndex);
        paginatedItems.getData().forEach(item ->
                item.setPictureUri("/items/" + item.getId() + "/pic"));
        model.addAttribute("model", paginatedItems);
        return "catalog/index";
    }

    @GetMapping("/details/{id}")
    public String details(@PathVariable Integer id, Model model) {
        log.info("Now loading... /Catalog/Details?id={}", id);
        if (id == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST);
        }
        CatalogItem catalogItem = service.findCatalogItem(id);
        if (catalogItem == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        catalogItem.setPictureUri("/items/" + catalogItem.getId() + "/pic");
        model.addAttribute("catalogItem", catalogItem);
        return "catalog/details";
    }

    @GetMapping("/create")
    public String createForm(Model model) {
        log.info("Now loading... /Catalog/Create");
        model.addAttribute("brands", service.getCatalogBrands());
        model.addAttribute("types", service.getCatalogTypes());
        model.addAttribute("catalogItem", new CatalogItem());
        return "catalog/create";
    }

    @PostMapping("/create")
    public String create(@Valid @ModelAttribute CatalogItem catalogItem,
                         BindingResult result,
                         Model model) {
        log.info("Now processing... /Catalog/Create?catalogItemName={}", catalogItem.getName());
        if (result.hasErrors()) {
            model.addAttribute("brands", service.getCatalogBrands());
            model.addAttribute("types", service.getCatalogTypes());
            return "catalog/create";
        }
        service.createCatalogItem(catalogItem);
        return "redirect:/catalog";
    }

    @GetMapping("/edit/{id}")
    public String editForm(@PathVariable Integer id, Model model) {
        log.info("Now loading... /Catalog/Edit?id={}", id);
        if (id == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST);
        }
        CatalogItem catalogItem = service.findCatalogItem(id);
        if (catalogItem == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        catalogItem.setPictureUri("/items/" + catalogItem.getId() + "/pic");
        model.addAttribute("brands", service.getCatalogBrands());
        model.addAttribute("types", service.getCatalogTypes());
        model.addAttribute("catalogItem", catalogItem);
        return "catalog/edit";
    }

    @PostMapping("/edit/{id}")
    public String edit(@Valid @ModelAttribute CatalogItem catalogItem,
                       BindingResult result,
                       Model model) {
        log.info("Now processing... /Catalog/Edit?id={}", catalogItem.getId());
        if (result.hasErrors()) {
            model.addAttribute("brands", service.getCatalogBrands());
            model.addAttribute("types", service.getCatalogTypes());
            return "catalog/edit";
        }
        service.updateCatalogItem(catalogItem);
        return "redirect:/catalog";
    }

    @GetMapping("/delete/{id}")
    public String deleteConfirm(@PathVariable Integer id, Model model) {
        log.info("Now loading... /Catalog/Delete?id={}", id);
        if (id == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST);
        }
        CatalogItem catalogItem = service.findCatalogItem(id);
        if (catalogItem == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        catalogItem.setPictureUri("/items/" + catalogItem.getId() + "/pic");
        model.addAttribute("catalogItem", catalogItem);
        return "catalog/delete";
    }

    @PostMapping("/delete/{id}")
    public String delete(@PathVariable Integer id) {
        log.info("Now processing... /Catalog/DeleteConfirmed?id={}", id);
        CatalogItem catalogItem = service.findCatalogItem(id);
        service.removeCatalogItem(catalogItem);
        return "redirect:/catalog";
    }
}
