package com.eshop.catalog.model;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Component;

@Component
public class CatalogItemHiLoGenerator {

    private static final int HI_LO_INCREMENT = 10;

    private int sequenceId = -1;
    private int remainingLoIds = 0;

    private final JdbcTemplate jdbcTemplate;

    public CatalogItemHiLoGenerator(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    public synchronized int getNextSequenceValue() {
        if (remainingLoIds == 0) {
            sequenceId = jdbcTemplate.queryForObject(
                    "SELECT NEXT VALUE FOR catalog_hilo", Long.class).intValue();
            remainingLoIds = HI_LO_INCREMENT - 1;
            return sequenceId;
        } else {
            remainingLoIds--;
            return ++sequenceId;
        }
    }
}
