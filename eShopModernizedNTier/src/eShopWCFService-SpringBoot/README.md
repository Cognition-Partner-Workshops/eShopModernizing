# eShop Catalog Service — Spring Boot REST API

A Spring Boot REST API migrated from the legacy eShopWCFService (.NET Framework WCF SOAP service). This service manages the product catalog for the eShop application, providing RESTful endpoints for catalog items, brands, types, stock, and discounts.

## Prerequisites

- **Java 21** (Eclipse Temurin or equivalent)
- **Maven 3.9+** (included via Maven Wrapper — `./mvnw`)

## Quick Start

```bash
# Run with dev profile (H2 in-memory database)
./mvnw spring-boot:run -Dspring-boot.run.profiles=dev
```

The service starts on [http://localhost:8080](http://localhost:8080).

## Profiles

| Profile | Database | Description |
|---------|----------|-------------|
| `dev` | H2 (in-memory) | Development with H2 console at `/h2-console` |
| `mock` | H2 (in-memory) | Uses mock data, Flyway disabled, schema auto-created |
| `prod` | SQL Server | Production configuration with Flyway migrations |

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `SPRING_DATASOURCE_URL` | `jdbc:sqlserver://sql.data:1433;databaseName=eShopDatabase;encrypt=true;trustServerCertificate=true` | JDBC connection URL (prod) |
| `SPRING_DATASOURCE_USERNAME` | `sa` | Database username (prod) |
| `SPRING_DATASOURCE_PASSWORD` | _(empty)_ | Database password (prod) |

## REST API Endpoints

All endpoints are under the `/api` base path.

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/catalog/items` | List catalog items with optional brand/type filters |
| `GET` | `/api/catalog/items/{id}` | Get a single catalog item by ID |
| `POST` | `/api/catalog/items` | Create a new catalog item |
| `PUT` | `/api/catalog/items/{id}` | Update an existing catalog item |
| `DELETE` | `/api/catalog/items/{id}` | Remove a catalog item |
| `GET` | `/api/catalog/brands` | List all catalog brands |
| `GET` | `/api/catalog/types` | List all catalog types |
| `GET` | `/api/catalog/stock` | Get available stock for a given date and item |
| `POST` | `/api/catalog/stock` | Create a stock record |
| `GET` | `/api/catalog/discounts` | Get discount for a given date |

### Actuator Endpoints

| Path | Access |
|------|--------|
| `/actuator/health` | Public |
| `/actuator/info` | Public |
| `/actuator/metrics` | Authenticated |
| `/actuator/prometheus` | Authenticated |

## Docker

### Build the image

```bash
docker build -t eshop-catalog-service .
```

### Run the container

```bash
# Dev profile (H2 in-memory)
docker run -p 8080:8080 -e SPRING_PROFILES_ACTIVE=dev eshop-catalog-service

# Production with SQL Server
docker run -p 8080:8080 \
  -e SPRING_PROFILES_ACTIVE=prod \
  -e SPRING_DATASOURCE_URL="jdbc:sqlserver://your-sql-server:1433;databaseName=eShopDatabase;encrypt=true;trustServerCertificate=true" \
  -e SPRING_DATASOURCE_USERNAME=sa \
  -e SPRING_DATASOURCE_PASSWORD=your_password \
  eshop-catalog-service
```

## Original WCF Service Reference

This Spring Boot application replaces the `eShopWCFService` project, a .NET Framework 4.7.2 WCF SOAP service located at `eShopModernizedNTier/src/eShopWCFService/`. The original service exposed a `CatalogService.svc` endpoint using `basicHttpBinding` and Entity Framework 6 for data access against a SQL Server (LocalDB) database. All SOAP operations have been mapped to equivalent REST endpoints above.
