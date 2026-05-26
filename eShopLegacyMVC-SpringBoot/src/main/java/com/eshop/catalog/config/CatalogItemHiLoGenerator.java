package com.eshop.catalog.config;

public class CatalogItemHiLoGenerator {

    private final HiLoSequenceGenerator sequenceGenerator;

    public CatalogItemHiLoGenerator(HiLoSequenceGenerator sequenceGenerator) {
        this.sequenceGenerator = sequenceGenerator;
    }

    public int getNextSequenceValue() {
        return sequenceGenerator.getNextValue("catalog_hilo");
    }
}
