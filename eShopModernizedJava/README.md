# eShop Modernized Java

This project is a migration of the **eShopLegacyMVC** ASP.NET MVC 5 (.NET Framework 4.7.2) application to **Java 21** with **Spring Boot 3.5.x**.

## Technology Stack

| .NET Component | Java Equivalent |
|---|---|
| ASP.NET MVC 5 | Spring MVC + Thymeleaf |
| Entity Framework 6 | Spring Data JPA (Hibernate) |
| Autofac DI | Spring Framework DI |
| Web API 2 | Spring REST Controllers |
| SQL Server + EF Migrations | SQL Server + Flyway |
| log4net | SLF4J + Logback |
| Application Insights | Micrometer + Prometheus + OpenTelemetry |
| `BinaryFormatter` (Serializing.cs) | JSON (Jackson) — security vulnerability eliminated |
| `HandleErrorAttribute` | `@ControllerAdvice` + `@ExceptionHandler` |
| `InProc` Session | Spring Boot `HttpSession` (in-memory) |
| Bundling & Minification | WebJars |

## Prerequisites

- Java 21+
- Maven 3.9+ (or use the included Maven Wrapper: `./mvnw`)
- SQL Server (for production) or H2 (for testing)
- Docker & Docker Compose (optional, for containerized deployment)

## Quick Start

### Mock Data Mode (no database required)

```bash
cd eShopModernizedJava
./mvnw spring-boot:run -Dspring-boot.run.arguments="--app.use-mock-data=true"
```

### With SQL Server (via Docker Compose)

```bash
cd eShopModernizedJava
docker-compose up
```

The application will be available at http://localhost:8080

## Project Structure

```
src/main/java/com/eshop/catalog/
├── CatalogApplication.java          # Main Spring Boot entry point
├── config/                          # Configuration classes
│   ├── AppConfig.java               # Custom app properties (replaces ApplicationModule.cs)
│   ├── SecurityConfig.java          # Spring Security (permit all, matching legacy)
│   ├── SessionConfig.java           # Session listener (replaces Global.asax Session_Start)
│   ├── WebMvcConfig.java            # MVC configuration + interceptors
│   └── RequestLoggingInterceptor.java # Request logging (replaces Application_BeginRequest)
├── controller/                      # MVC & REST controllers
│   ├── CatalogController.java       # Main CRUD controller (replaces CatalogController.cs)
│   ├── PicController.java           # Image serving (replaces PicController.cs)
│   ├── GlobalExceptionHandler.java  # Error handling (replaces FilterConfig.cs)
│   └── api/                         # REST API controllers
│       ├── BrandsRestController.java  # Brand API (replaces WebApi/BrandsController.cs)
│       ├── FilesRestController.java   # Files API — JSON instead of BinaryFormatter
│       └── CatalogRestController.java # Catalog API (replaces Api/CatalogController.cs)
├── dto/                             # Data Transfer Objects
│   ├── BrandDto.java                # Brand DTO for Files API
│   └── PaginatedItemsDto.java       # Pagination (replaces PaginatedItemsViewModel.cs)
├── infrastructure/                  # Data seeding
│   ├── CatalogDbInitializer.java    # DB seeder (replaces CatalogDBInitializer.cs)
│   └── PreconfiguredData.java       # Seed data (replaces PreconfiguredData.cs)
├── model/                           # JPA entities
│   ├── CatalogBrand.java
│   ├── CatalogItem.java
│   └── CatalogType.java
├── repository/                      # Spring Data JPA repositories
│   ├── CatalogBrandRepository.java
│   ├── CatalogItemRepository.java
│   └── CatalogTypeRepository.java
└── service/                         # Service layer
    ├── CatalogService.java          # Interface (replaces ICatalogService.cs)
    ├── CatalogServiceImpl.java      # JPA implementation (replaces CatalogService.cs)
    └── CatalogServiceMock.java      # In-memory mock (replaces CatalogServiceMock.cs)
```

## Configuration

Configuration is managed via Spring profiles:

| Profile | Description |
|---|---|
| (default) | Production-like settings, SQL Server, Flyway enabled |
| `dev` | Verbose logging, SQL output |
| `prod` | Minimal logging |

Key properties in `application.yml`:

| Property | Description | Default |
|---|---|---|
| `app.use-mock-data` | Use in-memory data instead of database | `false` |
| `app.use-customization-data` | Seed from CSV files instead of preconfigured data | `false` |
| `app.pics-directory` | Directory for product images | `classpath:static/pics/` |

## API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `/` | GET | Catalog list with pagination |
| `/catalog/details/{id}` | GET | Item details |
| `/catalog/create` | GET/POST | Create new item |
| `/catalog/edit/{id}` | GET/POST | Edit item |
| `/catalog/delete/{id}` | GET/POST | Delete item |
| `/items/{id}/pic` | GET | Product image |
| `/api/brands` | GET | All brands (JSON) |
| `/api/brands/{id}` | GET | Single brand |
| `/api/brands/{id}` | DELETE | Delete brand (demo, no-op) |
| `/api/files` | GET | Brand list as JSON (was binary in .NET) |
| `/api` | GET | Hello World |
| `/actuator/health` | GET | Health check |
| `/actuator/prometheus` | GET | Prometheus metrics |

## Breaking Changes

1. **FilesController** (`/api/files`) now returns JSON instead of binary-serialized data. The legacy `BinaryFormatter` in `eShopLegacy.Utilities/Serializing.cs` was a known security vulnerability and has been eliminated entirely.

2. The application title shows "Spring Boot" instead of "MVC" to distinguish from the legacy version.

3. Product image URLs use the pattern `/items/{id}/pic` (same as legacy).

## Building

```bash
./mvnw clean package
```

## Running Tests

```bash
./mvnw test
```

Tests use H2 in-memory database with mock data mode enabled.
