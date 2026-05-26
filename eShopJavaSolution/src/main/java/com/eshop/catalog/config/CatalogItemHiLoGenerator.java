package com.eshop.catalog.config;

import java.util.concurrent.locks.ReentrantLock;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Component;

@Component
public class CatalogItemHiLoGenerator {

  private static final int HI_LO_INCREMENT = 10;

  private final JdbcTemplate jdbcTemplate;
  private final ReentrantLock lock = new ReentrantLock();

  private int sequenceId = -1;
  private int remainingLoIds = 0;

  public CatalogItemHiLoGenerator(JdbcTemplate jdbcTemplate) {
    this.jdbcTemplate = jdbcTemplate;
  }

  public int getNextSequenceValue() {
    lock.lock();
    try {
      if (remainingLoIds == 0) {
        Long nextValue =
            jdbcTemplate.queryForObject("SELECT NEXT VALUE FOR catalog_hilo", Long.class);
        sequenceId = nextValue.intValue();
        remainingLoIds = HI_LO_INCREMENT - 1;
        return sequenceId;
      } else {
        remainingLoIds--;
        return ++sequenceId;
      }
    } finally {
      lock.unlock();
    }
  }
}
