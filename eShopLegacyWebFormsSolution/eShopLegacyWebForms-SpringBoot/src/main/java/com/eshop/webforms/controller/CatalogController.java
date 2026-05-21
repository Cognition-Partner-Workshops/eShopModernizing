package com.eshop.webforms.controller;

import com.eshop.webforms.dto.PaginatedItemsViewModel;
import com.eshop.webforms.model.CatalogItem;
import com.eshop.webforms.service.CatalogService;
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

@Controller
public class CatalogController {

    private static final Logger log = LoggerFactory.getLogger(CatalogController.class);
    private static final int DEFAULT_PAGE_SIZE = 10;
    private static final int DEFAULT_PAGE_INDEX = 0;

    private final CatalogService catalogService;

    public CatalogController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping("/")
    public String index(@RequestParam(defaultValue = "0") int pageIndex,
                        @RequestParam(defaultValue = "10") int pageSize,
                        Model model) {
        log.info("Now loading... /?pageSize={}&pageIndex={}", pageSize, pageIndex);
        PaginatedItemsViewModel<CatalogItem> paginatedItems =
                catalogService.getCatalogItemsPaginated(pageSize, pageIndex);
        model.addAttribute("model", paginatedItems);
        return "catalog/index";
    }

    @GetMapping("/catalog/details/{id}")
    public String details(@PathVariable int id, Model model) {
        log.info("Now loading... /catalog/details/{}", id);
        CatalogItem item = catalogService.findCatalogItem(id);
        model.addAttribute("item", item);
        return "catalog/details";
    }

    @GetMapping("/catalog/create")
    public String createForm(Model model) {
        log.info("Now loading... /catalog/create");
        model.addAttribute("catalogItem", new CatalogItem());
        model.addAttribute("brands", catalogService.getCatalogBrands());
        model.addAttribute("types", catalogService.getCatalogTypes());
        return "catalog/create";
    }

    @PostMapping("/catalog/create")
    public String create(@Valid @ModelAttribute CatalogItem catalogItem,
                         BindingResult result, Model model) {
        if (result.hasErrors()) {
            model.addAttribute("brands", catalogService.getCatalogBrands());
            model.addAttribute("types", catalogService.getCatalogTypes());
            return "catalog/create";
        }
        catalogService.createCatalogItem(catalogItem);
        return "redirect:/";
    }

    @GetMapping("/catalog/edit/{id}")
    public String editForm(@PathVariable int id, Model model) {
        log.info("Now loading... /catalog/edit/{}", id);
        CatalogItem item = catalogService.findCatalogItem(id);
        model.addAttribute("catalogItem", item);
        model.addAttribute("brands", catalogService.getCatalogBrands());
        model.addAttribute("types", catalogService.getCatalogTypes());
        return "catalog/edit";
    }

    @PostMapping("/catalog/edit/{id}")
    public String edit(@PathVariable int id,
                       @Valid @ModelAttribute CatalogItem catalogItem,
                       BindingResult result, Model model) {
        if (result.hasErrors()) {
            model.addAttribute("brands", catalogService.getCatalogBrands());
            model.addAttribute("types", catalogService.getCatalogTypes());
            return "catalog/edit";
        }
        catalogItem.setId(id);
        catalogService.updateCatalogItem(catalogItem);
        return "redirect:/";
    }

    @GetMapping("/catalog/delete/{id}")
    public String deleteForm(@PathVariable int id, Model model) {
        log.info("Now loading... /catalog/delete/{}", id);
        CatalogItem item = catalogService.findCatalogItem(id);
        model.addAttribute("item", item);
        return "catalog/delete";
    }

    @PostMapping("/catalog/delete/{id}")
    public String delete(@PathVariable int id) {
        log.info("Now deleting... /catalog/delete/{}", id);
        CatalogItem item = catalogService.findCatalogItem(id);
        catalogService.removeCatalogItem(item);
        return "redirect:/";
    }
}
