package com.eshop.catalog;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.core.env.Environment;

@SpringBootApplication
public class CatalogApplication {

    private static final Logger log = LoggerFactory.getLogger(CatalogApplication.class);

    private final Environment environment;

    public CatalogApplication(Environment environment) {
        this.environment = environment;
    }

    public static void main(String[] args) {
        SpringApplication.run(CatalogApplication.class, args);
    }

    @EventListener(ApplicationReadyEvent.class)
    public void logApplicationStartup() {
        String datasourceUrl = environment.getProperty("spring.datasource.url");
        String activeProfiles = String.join(", ", environment.getActiveProfiles());
        log.info("Application started with active profiles: {}", activeProfiles.isEmpty() ? "default" : activeProfiles);
        log.info("Datasource URL: {}", datasourceUrl);
        log.info("app.use-mock-data: {}", environment.getProperty("app.use-mock-data"));
        log.info("app.use-customization-data: {}", environment.getProperty("app.use-customization-data"));
        log.info("app.pics-path: {}", environment.getProperty("app.pics-path"));
    }
}
