package com.eshop.catalog.config;

import org.springframework.jdbc.core.JdbcTemplate;

public class CatalogItemHiLoGenerator {

    private static final int HI_LO_INCREMENT = 10;

    private final JdbcTemplate jdbcTemplate;
    private final Object sequenceLock = new Object();
    private int sequenceId = -1;
    private int remainingLoIds = 0;

    public CatalogItemHiLoGenerator(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    public int getNextSequenceValue() {
        synchronized (sequenceLock) {
            if (remainingLoIds == 0) {
                Long nextVal = jdbcTemplate.queryForObject(
                        "SELECT NEXT VALUE FOR catalog_hilo", Long.class);
                if (nextVal == null) {
                    throw new IllegalStateException("catalog_hilo sequence returned null");
                }
                sequenceId = nextVal.intValue();
                remainingLoIds = HI_LO_INCREMENT - 1;
                return sequenceId;
            } else {
                remainingLoIds--;
                return ++sequenceId;
            }
        }
    }
}
