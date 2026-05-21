# eShop Catalog Service (Spring Boot)

Migrated from ASP.NET MVC (.NET Framework 4.7.2) to Java 21 / Spring Boot 3.5.0.

## Quick Start

### Prerequisites
- Java 21
- Maven 3.9+
- SQL Server (or use mock profile)

### Run with Mock Data (no database required)
```bash
mvn spring-boot:run -Dspring-boot.run.profiles=mock
```

### Run with Database
```bash
export SPRING_DATASOURCE_PASSWORD=your_password
mvn spring-boot:run -Dspring-boot.run.profiles=dev
```

### Build and Run
```bash
mvn clean package
java -jar target/eshop-legacy-mvc-1.0.0-SNAPSHOT.jar --spring.profiles.active=mock
```

### Docker
```bash
docker build -t eshop-catalog .
docker run -p 8080:8080 -e APP_USE_MOCK_DATA=true eshop-catalog
```

## Environment Variables

| Variable | Required | Default | Description |
|---|---|---|---|
| SPRING_DATASOURCE_URL | Yes (prod) | jdbc:sqlserver://localhost:1433;... | SQL Server connection URL |
| SPRING_DATASOURCE_USERNAME | Yes (prod) | sa | Database username |
| SPRING_DATASOURCE_PASSWORD | Yes (prod) | - | Database password |
| APP_USE_MOCK_DATA | No | false | Use in-memory mock data |
| APP_PICS_PATH | No | classpath:static/Pics | Product images directory |
| SPRING_PROFILES_ACTIVE | No | - | Active profile (dev/prod/mock) |

## Endpoints

| Path | Method | Description |
|---|---|---|
| / | GET | Redirect to /catalog |
| /catalog | GET | Product listing (paginated) |
| /catalog/details/{id} | GET | Product detail |
| /catalog/create | GET/POST | Create product form |
| /catalog/edit/{id} | GET/POST | Edit product form |
| /catalog/delete/{id} | GET/POST | Delete product confirmation |
| /items/{id}/pic | GET | Product image |
| /api/brands | GET | REST: All brands (JSON) |
| /api/brands/{id} | GET | REST: Brand by ID (JSON) |
| /api/files | GET | REST: Brand DTOs (JSON) |
| /actuator/health | GET | Health check |
| /actuator/metrics | GET | Application metrics |
| /actuator/prometheus | GET | Prometheus metrics |

## Architecture

See OBSERVABILITY.md for monitoring and telemetry configuration.
