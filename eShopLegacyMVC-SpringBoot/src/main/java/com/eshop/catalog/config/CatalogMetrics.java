package com.eshop.catalog.config;

import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.MeterRegistry;
import org.springframework.stereotype.Component;

@Component
public class CatalogMetrics {

  private final Counter itemsCreatedCounter;
  private final Counter itemsUpdatedCounter;
  private final Counter itemsDeletedCounter;
  private final Counter itemsViewedCounter;

  public CatalogMetrics(MeterRegistry meterRegistry) {
    this.itemsCreatedCounter =
        Counter.builder("catalog.items.created")
            .description("Number of catalog items created")
            .register(meterRegistry);
    this.itemsUpdatedCounter =
        Counter.builder("catalog.items.updated")
            .description("Number of catalog items updated")
            .register(meterRegistry);
    this.itemsDeletedCounter =
        Counter.builder("catalog.items.deleted")
            .description("Number of catalog items deleted")
            .register(meterRegistry);
    this.itemsViewedCounter =
        Counter.builder("catalog.items.viewed")
            .description("Number of catalog item detail views")
            .register(meterRegistry);
  }

  public void incrementItemsCreated() {
    itemsCreatedCounter.increment();
  }

  public void incrementItemsUpdated() {
    itemsUpdatedCounter.increment();
  }

  public void incrementItemsDeleted() {
    itemsDeletedCounter.increment();
  }

  public void incrementItemsViewed() {
    itemsViewedCounter.increment();
  }
}
