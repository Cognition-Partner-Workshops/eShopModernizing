# Observability Guide — eShop Catalog (Spring Boot)

## Built-in Actuator Endpoints

Spring Boot Actuator exposes the following endpoints out of the box:

| Endpoint | URL | Description |
|---|---|---|
| Health | `/actuator/health` | Application health status with component details |
| Metrics | `/actuator/metrics` | JVM, HTTP, and custom application metrics |
| Prometheus | `/actuator/prometheus` | Metrics in Prometheus scrape format |
| Info | `/actuator/info` | Application information |

### Health Endpoint

```
GET /actuator/health
```

Returns aggregated health status including:
- **diskSpace** — available disk space check
- **db** — database connectivity check
- **catalog** — custom `CatalogHealthIndicator` reporting service status

Example response:
```json
{
  "status": "UP",
  "components": {
    "catalog": {
      "status": "UP",
      "details": {
        "service": "eshop-catalog",
        "description": "eShop Catalog Service"
      }
    },
    "db": {
      "status": "UP"
    },
    "diskSpace": {
      "status": "UP"
    }
  }
}
```

### Metrics Endpoint

```
GET /actuator/metrics
```

Lists all available metric names. Drill into a specific metric:

```
GET /actuator/metrics/jvm.memory.used
GET /actuator/metrics/http.server.requests
```

All metrics are tagged with `application=eshop-catalog` via `ActuatorConfig`.

### Prometheus Endpoint

```
GET /actuator/prometheus
```

Returns all metrics in Prometheus exposition format, ready for scraping by a Prometheus server or compatible collector.

---

## Production Distributed Tracing

### OpenTelemetry Java Agent

Attach the [OpenTelemetry Java Agent](https://github.com/open-telemetry/opentelemetry-java-instrumentation) at startup for automatic instrumentation of HTTP, JDBC, JVM metrics, and distributed traces:

```bash
java -javaagent:opentelemetry-javaagent.jar \
     -Dotel.service.name=eshop-catalog \
     -Dotel.exporter.otlp.endpoint=http://otel-collector:4317 \
     -jar app.jar
```

Environment variable configuration is also supported:

```bash
export OTEL_SERVICE_NAME=eshop-catalog
export OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
java -javaagent:opentelemetry-javaagent.jar -jar app.jar
```

### Azure Application Insights 3.x Agent

Attach the [Azure Application Insights Java Agent](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=java) for Azure Monitor integration:

```bash
java -javaagent:applicationinsights-agent-3.x.jar -jar app.jar
```

Configure via `applicationinsights.json` in the same directory as the agent JAR:

```json
{
  "connectionString": "InstrumentationKey=<your-key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/",
  "role": {
    "name": "eshop-catalog"
  }
}
```

---

## .NET Application Insights to Java Migration Mapping

The original .NET eShopLegacyMVC application used 7 Application Insights NuGet packages. The table below maps each to its Java/Spring Boot equivalent:

| .NET NuGet Package | Java Equivalent | Notes |
|---|---|---|
| `Microsoft.ApplicationInsights` | Azure Application Insights Java Agent 3.x | Core SDK replaced by auto-instrumentation agent |
| `Microsoft.ApplicationInsights.Agent.Intercept` | Azure Application Insights Java Agent 3.x | Bytecode interception handled by the Java agent |
| `Microsoft.ApplicationInsights.DependencyCollector` | Azure Application Insights Java Agent 3.x | Automatic dependency tracking (JDBC, HTTP) via agent |
| `Microsoft.ApplicationInsights.PerfCounterCollector` | Micrometer + `spring-boot-starter-actuator` | JVM metrics (memory, GC, threads) via Micrometer |
| `Microsoft.ApplicationInsights.Web` | Azure Application Insights Java Agent 3.x | HTTP request tracking handled by the agent |
| `Microsoft.ApplicationInsights.WindowsServer` | Azure Application Insights Java Agent 3.x | Server telemetry (heartbeat, SDK version) via agent |
| `Microsoft.AspNet.TelemetryCorrelation` | OpenTelemetry context propagation (built into agent) | W3C Trace Context propagation is automatic |

### Key Differences

- **No SDK code changes required**: The Java agent attaches at the JVM level and auto-instruments frameworks (Spring, JDBC, HTTP clients) without code modifications.
- **Micrometer replaces PerfCounterCollector**: JVM and application metrics are exposed via Micrometer and the `/actuator/prometheus` endpoint.
- **OpenTelemetry compatibility**: The Azure Application Insights 3.x agent is built on OpenTelemetry, enabling vendor-neutral instrumentation and the ability to switch to other backends (Jaeger, Zipkin, Grafana) by swapping the agent.
