package com.eshop.catalog.controller;

import java.util.List;

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
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.server.ResponseStatusException;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.service.CatalogService;

import jakarta.validation.Valid;

@Controller
public class CatalogController {

    private static final Logger log = LoggerFactory.getLogger(CatalogController.class);

    private final CatalogService service;

    public CatalogController(CatalogService service) {
        this.service = service;
    }

    @GetMapping("/")
    public String index(@RequestParam(defaultValue = "10") int pageSize,
                        @RequestParam(defaultValue = "0") int pageIndex,
                        Model model) {
        log.info("Now loading... /Catalog/Index?pageSize={}&pageIndex={}", pageSize, pageIndex);
        PaginatedItemsDto<CatalogItem> paginatedItems = service.getCatalogItemsPaginated(pageSize, pageIndex);
        changeUriPlaceholder(paginatedItems.data());
        model.addAttribute("paginatedItems", paginatedItems);
        return "catalog/index";
    }

    @GetMapping("/catalog/details/{id}")
    public String details(@PathVariable int id, Model model) {
        log.info("Now loading... /Catalog/Details?id={}", id);
        CatalogItem catalogItem = service.findCatalogItem(id);
        if (catalogItem == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        addUriPlaceholder(catalogItem);
        model.addAttribute("catalogItem", catalogItem);
        return "catalog/details";
    }

    @GetMapping("/catalog/create")
    public String createForm(Model model) {
        log.info("Now loading... /Catalog/Create");
        model.addAttribute("catalogItem", new CatalogItem());
        addBrandAndTypeDropdowns(model);
        return "catalog/create";
    }

    @PostMapping("/catalog/create")
    public String create(@Valid @ModelAttribute("catalogItem") CatalogItem catalogItem,
                         BindingResult bindingResult,
                         Model model) {
        log.info("Now processing... /Catalog/Create?catalogItemName={}", catalogItem.getName());
        if (bindingResult.hasErrors()) {
            addBrandAndTypeDropdowns(model);
            return "catalog/create";
        }
        service.createCatalogItem(catalogItem);
        return "redirect:/";
    }

    @GetMapping("/catalog/edit/{id}")
    public String editForm(@PathVariable int id, Model model) {
        log.info("Now loading... /Catalog/Edit?id={}", id);
        CatalogItem catalogItem = service.findCatalogItem(id);
        if (catalogItem == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        addUriPlaceholder(catalogItem);
        model.addAttribute("catalogItem", catalogItem);
        addBrandAndTypeDropdowns(model);
        return "catalog/edit";
    }

    @PostMapping("/catalog/edit/{id}")
    public String edit(@PathVariable int id,
                       @Valid @ModelAttribute("catalogItem") CatalogItem catalogItem,
                       BindingResult bindingResult,
                       Model model) {
        log.info("Now processing... /Catalog/Edit?id={}", id);
        if (bindingResult.hasErrors()) {
            addBrandAndTypeDropdowns(model);
            return "catalog/edit";
        }
        catalogItem.setId(id);
        service.updateCatalogItem(catalogItem);
        return "redirect:/";
    }

    @GetMapping("/catalog/delete/{id}")
    public String deleteForm(@PathVariable int id, Model model) {
        log.info("Now loading... /Catalog/Delete?id={}", id);
        CatalogItem catalogItem = service.findCatalogItem(id);
        if (catalogItem == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        addUriPlaceholder(catalogItem);
        model.addAttribute("catalogItem", catalogItem);
        return "catalog/delete";
    }

    @PostMapping("/catalog/delete/{id}")
    public String deleteConfirmed(@PathVariable int id) {
        log.info("Now processing... /Catalog/DeleteConfirmed?id={}", id);
        CatalogItem catalogItem = service.findCatalogItem(id);
        if (catalogItem == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND);
        }
        service.removeCatalogItem(catalogItem);
        return "redirect:/";
    }

    private void addBrandAndTypeDropdowns(Model model) {
        List<CatalogBrand> brands = service.getCatalogBrands();
        List<CatalogType> types = service.getCatalogTypes();
        model.addAttribute("brands", brands);
        model.addAttribute("types", types);
    }

    private void changeUriPlaceholder(List<CatalogItem> items) {
        for (CatalogItem item : items) {
            addUriPlaceholder(item);
        }
    }

    private void addUriPlaceholder(CatalogItem item) {
        item.setPictureUri("/items/" + item.getId() + "/pic");
    }
}
