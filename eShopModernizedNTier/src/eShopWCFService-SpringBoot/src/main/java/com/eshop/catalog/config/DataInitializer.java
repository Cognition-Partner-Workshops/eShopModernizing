package com.eshop.catalog.config;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;

import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogItemsStock;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.model.DiscountItem;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogItemsStockRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.repository.DiscountItemRepository;

@Component
@Profile("mock")
public class DataInitializer implements ApplicationRunner {

    private final CatalogBrandRepository brandRepository;
    private final CatalogTypeRepository typeRepository;
    private final CatalogItemRepository itemRepository;
    private final CatalogItemsStockRepository stockRepository;
    private final DiscountItemRepository discountRepository;

    public DataInitializer(CatalogBrandRepository brandRepository,
                           CatalogTypeRepository typeRepository,
                           CatalogItemRepository itemRepository,
                           CatalogItemsStockRepository stockRepository,
                           DiscountItemRepository discountRepository) {
        this.brandRepository = brandRepository;
        this.typeRepository = typeRepository;
        this.itemRepository = itemRepository;
        this.stockRepository = stockRepository;
        this.discountRepository = discountRepository;
    }

    @Override
    public void run(ApplicationArguments args) {
        List<CatalogBrand> brands = seedBrands();
        List<CatalogType> types = seedTypes();
        seedCatalogItems(brands, types);
        seedStock();
        seedDiscounts();
    }

    private List<CatalogBrand> seedBrands() {
        List<CatalogBrand> brands = List.of(
                new CatalogBrand(null, "Azure"),
                new CatalogBrand(null, ".NET"),
                new CatalogBrand(null, "Visual Studio"),
                new CatalogBrand(null, "SQL Server"),
                new CatalogBrand(null, "Other")
        );
        return brandRepository.saveAll(brands);
    }

    private List<CatalogType> seedTypes() {
        List<CatalogType> types = List.of(
                new CatalogType(null, "Mug"),
                new CatalogType(null, "T-Shirt"),
                new CatalogType(null, "Sheet"),
                new CatalogType(null, "USB Memory Stick")
        );
        return typeRepository.saveAll(types);
    }

    private void seedCatalogItems(List<CatalogBrand> brands, List<CatalogType> types) {
        CatalogBrand dotNet = brands.get(1);
        CatalogBrand other = brands.get(4);
        CatalogType mug = types.get(0);
        CatalogType tShirt = types.get(1);
        CatalogType sheet = types.get(2);

        List<CatalogItem> items = List.of(
                new CatalogItem(null, ".NET Bot Black Hoodie", ".NET Bot Black Hoodie", new BigDecimal("19.5000"), "2.png", dotNet, tShirt),
                new CatalogItem(null, ".NET Black & White Mug", ".NET Black & White Mug", new BigDecimal("8.5000"), "11.png", dotNet, mug),
                new CatalogItem(null, "Prism White T-Shirt", "Prism White T-Shirt", new BigDecimal("12.0000"), "7.png", other, tShirt),
                new CatalogItem(null, ".NET Foundation T-shirt", ".NET Foundation T-shirt", new BigDecimal("12.0000"), "5.png", dotNet, tShirt),
                new CatalogItem(null, "Roslyn Red Sheet", "Roslyn Red Sheet", new BigDecimal("8.5000"), "9.png", other, sheet),
                new CatalogItem(null, ".NET Blue Hoodie", ".NET Blue Hoodie", new BigDecimal("12.0000"), "1.png", dotNet, tShirt),
                new CatalogItem(null, "Roslyn Red T-Shirt", "Roslyn Red T-Shirt", new BigDecimal("12.0000"), "6.png", other, tShirt),
                new CatalogItem(null, "Kudu Purple Hoodie", "Kudu Purple Hoodie", new BigDecimal("8.5000"), "3.png", other, tShirt),
                new CatalogItem(null, "Cup<T> White Mug", "Cup<T> White Mug", new BigDecimal("12.0000"), "12.png", other, mug),
                new CatalogItem(null, ".NET Foundation Sheet", ".NET Foundation Sheet", new BigDecimal("12.0000"), "8.png", dotNet, sheet),
                new CatalogItem(null, "Cup<T> Sheet", "Cup<T> Sheet", new BigDecimal("8.5000"), "10.png", dotNet, sheet),
                new CatalogItem(null, "Cup<T> TShirt", "Cup<T> TShirt", new BigDecimal("12.0000"), "4.png", other, tShirt)
        );
        itemRepository.saveAll(items);
    }

    private void seedStock() {
        List<CatalogItemsStock> stocks = List.of(
                new CatalogItemsStock(null, LocalDate.of(2017, 9, 20), 1, 100),
                new CatalogItemsStock(null, LocalDate.of(2017, 9, 21), 1, 120),
                new CatalogItemsStock(null, LocalDate.of(2017, 9, 22), 1, 80),
                new CatalogItemsStock(null, LocalDate.of(2017, 9, 20), 2, 45),
                new CatalogItemsStock(null, LocalDate.of(2017, 9, 25), 4, 65),
                new CatalogItemsStock(null, LocalDate.of(2017, 9, 28), 5, 22)
        );
        stockRepository.saveAll(stocks);
    }

    private void seedDiscounts() {
        List<DiscountItem> discounts = List.of(
                new DiscountItem(null, 0.3, LocalDate.of(2017, 9, 18), LocalDate.of(2017, 9, 21)),
                new DiscountItem(null, 0.25, LocalDate.of(2017, 9, 22), LocalDate.of(2017, 9, 26)),
                new DiscountItem(null, 0.1, LocalDate.of(2017, 9, 27), LocalDate.of(2017, 9, 30)),
                new DiscountItem(null, 0.5, LocalDate.of(2017, 10, 5), LocalDate.of(2017, 10, 20)),
                new DiscountItem(null, 0.3, LocalDate.of(2017, 11, 13), LocalDate.of(2017, 11, 25)),
                new DiscountItem(null, 0.25, LocalDate.of(2017, 12, 20), LocalDate.of(2017, 12, 25))
        );
        discountRepository.saveAll(discounts);
    }
}
