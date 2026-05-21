package com.eshop.catalog.controller;

import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.service.CatalogService;
import jakarta.validation.Valid;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.validation.BindingResult;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.ModelAttribute;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.servlet.support.ServletUriComponentsBuilder;

@Controller
public class CatalogController {

    private static final Logger log = LoggerFactory.getLogger(CatalogController.class);

    private final CatalogService catalogService;

    public CatalogController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping("/")
    public String index(@RequestParam(defaultValue = "10") int pageSize,
                        @RequestParam(defaultValue = "0") int pageIndex,
                        Model model) {
        if (pageSize < 1) {
            pageSize = 10;
        }
        if (pageIndex < 0) {
            pageIndex = 0;
        }
        log.info("Now loading... /Catalog/Index?pageSize={}&pageIndex={}", pageSize, pageIndex);
        PaginatedItemsDto<CatalogItem> paginatedItems =
                catalogService.getCatalogItemsPaginated(pageSize, pageIndex);
        changeUriPlaceholder(paginatedItems);
        model.addAttribute("paginatedItems", paginatedItems);
        return "catalog/index";
    }

    @GetMapping("/catalog/details/{id}")
    public String details(@PathVariable Integer id, Model model) {
        log.info("Now loading... /Catalog/Details?id={}", id);
        CatalogItem catalogItem = catalogService.findCatalogItem(id);
        if (catalogItem == null) {
            return "error";
        }
        addUriPlaceholder(catalogItem);
        model.addAttribute("catalogItem", catalogItem);
        return "catalog/details";
    }

    @GetMapping("/catalog/create")
    public String createForm(Model model) {
        log.info("Now loading... /Catalog/Create");
        model.addAttribute("catalogItem", new CatalogItem());
        model.addAttribute("brands", catalogService.getCatalogBrands());
        model.addAttribute("types", catalogService.getCatalogTypes());
        return "catalog/create";
    }

    @PostMapping("/catalog/create")
    public String create(@Valid @ModelAttribute("catalogItem") CatalogItem catalogItem,
                         BindingResult result, Model model) {
        log.info("Now processing... /Catalog/Create?catalogItemName={}", catalogItem.getName());
        if (result.hasErrors()) {
            model.addAttribute("brands", catalogService.getCatalogBrands());
            model.addAttribute("types", catalogService.getCatalogTypes());
            return "catalog/create";
        }
        catalogService.createCatalogItem(catalogItem);
        return "redirect:/";
    }

    @GetMapping("/catalog/edit/{id}")
    public String editForm(@PathVariable Integer id, Model model) {
        log.info("Now loading... /Catalog/Edit?id={}", id);
        CatalogItem catalogItem = catalogService.findCatalogItem(id);
        if (catalogItem == null) {
            return "error";
        }
        addUriPlaceholder(catalogItem);
        model.addAttribute("catalogItem", catalogItem);
        model.addAttribute("brands", catalogService.getCatalogBrands());
        model.addAttribute("types", catalogService.getCatalogTypes());
        return "catalog/edit";
    }

    @PostMapping("/catalog/edit/{id}")
    public String edit(@PathVariable Integer id,
                       @Valid @ModelAttribute("catalogItem") CatalogItem catalogItem,
                       BindingResult result, Model model) {
        log.info("Now processing... /Catalog/Edit?id={}", catalogItem.getId());
        if (result.hasErrors()) {
            addUriPlaceholder(catalogItem);
            model.addAttribute("brands", catalogService.getCatalogBrands());
            model.addAttribute("types", catalogService.getCatalogTypes());
            return "catalog/edit";
        }
        catalogService.updateCatalogItem(catalogItem);
        return "redirect:/";
    }

    @GetMapping("/catalog/delete/{id}")
    public String deleteForm(@PathVariable Integer id, Model model) {
        log.info("Now loading... /Catalog/Delete?id={}", id);
        CatalogItem catalogItem = catalogService.findCatalogItem(id);
        if (catalogItem == null) {
            return "error";
        }
        addUriPlaceholder(catalogItem);
        model.addAttribute("catalogItem", catalogItem);
        return "catalog/delete";
    }

    @PostMapping("/catalog/delete/{id}")
    public String deleteConfirmed(@PathVariable Integer id) {
        log.info("Now processing... /Catalog/DeleteConfirmed?id={}", id);
        CatalogItem catalogItem = catalogService.findCatalogItem(id);
        if (catalogItem != null) {
            catalogService.removeCatalogItem(catalogItem);
        }
        return "redirect:/";
    }

    private void changeUriPlaceholder(PaginatedItemsDto<CatalogItem> paginatedItems) {
        for (CatalogItem item : paginatedItems.getData()) {
            addUriPlaceholder(item);
        }
    }

    private void addUriPlaceholder(CatalogItem item) {
        item.setPictureUri(
                ServletUriComponentsBuilder.fromCurrentContextPath()
                        .path("/items/{catalogItemId}/pic")
                        .buildAndExpand(item.getId())
                        .toUriString());
    }
}
