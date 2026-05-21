# eShop Catalog Service - Cutover Runbook

This runbook documents the procedure for cutting over from the legacy ASP.NET MVC application to the Java Spring Boot application.

---

## 1. Pre-Cutover Checklist

- [ ] Java 21 and Maven 3.9+ installed on build/deploy environment
- [ ] `mvn clean package` completes successfully with all tests passing
- [ ] `application-prod.yml` environment variables configured:
  - `SPRING_DATASOURCE_URL`
  - `SPRING_DATASOURCE_USERNAME`
  - `SPRING_DATASOURCE_PASSWORD`
- [ ] Docker image builds successfully: `docker build -t eshop-catalog .`
- [ ] Docker image pushed to container registry
- [ ] Flyway migrations reviewed and match existing EF6 schema
- [ ] Smoke test passes against staging environment
- [ ] Monitoring dashboards configured (Prometheus/Grafana)
- [ ] Alerting rules configured for error rates and latency
- [ ] Rollback procedure reviewed with operations team
- [ ] Stakeholders notified of cutover window

---

## 2. Blue-Green Deployment Steps

### 2.1 Prepare Green (Java) Environment

```bash
# Build and tag the Docker image
docker build -t eshop-catalog:v1.0.0 .

# Push to container registry
docker tag eshop-catalog:v1.0.0 <registry>/eshop-catalog:v1.0.0
docker push <registry>/eshop-catalog:v1.0.0

# Deploy green instance (not receiving production traffic)
docker run -d \
  --name eshop-catalog-green \
  -p 8081:8080 \
  -e SPRING_PROFILES_ACTIVE=prod \
  -e SPRING_DATASOURCE_URL="jdbc:sqlserver://<db-host>:1433;databaseName=Microsoft.eShopOnContainers.Services.CatalogDb;encrypt=true;trustServerCertificate=false" \
  -e SPRING_DATASOURCE_USERNAME=<username> \
  -e SPRING_DATASOURCE_PASSWORD=<password> \
  -e APP_USE_MOCK_DATA=false \
  eshop-catalog:v1.0.0
```

### 2.2 Verify Green Instance

```bash
# Health check
curl -f http://green-host:8081/actuator/health

# Verify metrics endpoint
curl -f http://green-host:8081/actuator/prometheus
```

### 2.3 Blue (ASP.NET) Remains Active

The existing ASP.NET MVC application continues serving production traffic on its current endpoint while the green instance is validated.

---

## 3. Smoke Tests Against Java Instance

Run the following smoke tests against the green (Java) instance before switching traffic:

```bash
BASE_URL="http://green-host:8081"

# 1. Health check
curl -f "$BASE_URL/actuator/health" | jq .status
# Expected: "UP"

# 2. Catalog listing
curl -f -o /dev/null -w "%{http_code}" "$BASE_URL/catalog"
# Expected: 200

# 3. Product detail (ID 1)
curl -f -o /dev/null -w "%{http_code}" "$BASE_URL/catalog/details/1"
# Expected: 200

# 4. Product image
curl -f -o /dev/null -w "%{http_code}" "$BASE_URL/items/1/pic"
# Expected: 200

# 5. REST API - brands
curl -f "$BASE_URL/api/brands" | jq length
# Expected: non-zero count

# 6. REST API - files
curl -f "$BASE_URL/api/files" | jq length
# Expected: non-zero count

# 7. Create form loads
curl -f -o /dev/null -w "%{http_code}" "$BASE_URL/catalog/create"
# Expected: 200

# 8. Metrics available
curl -f "$BASE_URL/actuator/prometheus" | head -5
# Expected: Prometheus-formatted metrics
```

---

## 4. Traffic Switching Procedure

### 4.1 Switch Traffic to Green (Java)

```bash
# Option A: DNS switch
# Update DNS A/CNAME record to point to green instance IP

# Option B: Load balancer switch
# Update load balancer target group to point to green instance
# Remove blue instance from target group

# Option C: Reverse proxy switch (nginx example)
# Update upstream in nginx.conf:
#   upstream eshop_backend {
#       server green-host:8081;
#   }
# Reload: sudo nginx -s reload
```

### 4.2 Verify Production Traffic

```bash
# Confirm traffic is flowing to Java instance
curl -f https://production-url/actuator/health
# Should return Spring Boot health response

# Check access logs on green instance
docker logs eshop-catalog-green --tail 20
```

---

## 5. Monitoring and Error Rate Checks

### 5.1 Key Metrics to Monitor (First 30 Minutes)

| Metric | Threshold | Action if Breached |
|---|---|---|
| HTTP 5xx rate | > 1% of requests | Initiate rollback |
| Response latency (p95) | > 2x baseline | Investigate, consider rollback |
| JVM heap usage | > 80% | Investigate memory settings |
| Database connection pool | > 80% utilized | Adjust pool size |
| Error log rate | > 10 errors/min | Investigate, consider rollback |

### 5.2 Prometheus Queries

```promql
# Error rate
rate(http_server_requests_seconds_count{status=~"5.."}[5m])
  / rate(http_server_requests_seconds_count[5m])

# P95 latency
histogram_quantile(0.95, rate(http_server_requests_seconds_bucket[5m]))

# JVM heap usage
jvm_memory_used_bytes{area="heap"} / jvm_memory_max_bytes{area="heap"}

# Active database connections
hikaricp_connections_active
```

### 5.3 Monitoring Checklist (Post-Switch)

- [ ] Error rate below 1% for 30 minutes
- [ ] P95 latency within 2x of baseline
- [ ] No database connection errors
- [ ] All smoke tests passing against production URL
- [ ] No unusual patterns in application logs
- [ ] Prometheus scraping metrics successfully

---

## 6. Rollback Procedure

If issues are detected after switching traffic:

### 6.1 Immediate Rollback (< 5 minutes)

```bash
# Switch traffic back to blue (ASP.NET) instance
# Use the reverse of the traffic switching method (Section 4.1)

# Option A: Revert DNS to blue instance IP
# Option B: Revert load balancer target group
# Option C: Revert nginx upstream to blue-host

# Verify blue instance is still healthy
curl -f http://blue-host/health
```

### 6.2 Post-Rollback Actions

1. Collect logs from green instance: `docker logs eshop-catalog-green > /tmp/green-logs.txt`
2. Capture thread dump if JVM issues: `docker exec eshop-catalog-green jcmd 1 Thread.print`
3. Capture heap dump if memory issues: `docker exec eshop-catalog-green jcmd 1 GC.heap_dump /tmp/heap.hprof`
4. Stop green instance: `docker stop eshop-catalog-green`
5. Analyze failure and create incident report
6. Schedule retry after fixes are applied

### 6.3 Rollback Decision Matrix

| Condition | Action |
|---|---|
| 5xx rate > 5% | Immediate rollback |
| 5xx rate 1-5% | Investigate for 10 min, rollback if not resolved |
| Data corruption detected | Immediate rollback + DB restore |
| Performance degradation only | Investigate, rollback if > 30 min |

---

## 7. Data Migration Notes

### 7.1 Database Compatibility

- **Same SQL Server database**: Both ASP.NET and Java applications connect to the same SQL Server database (`Microsoft.eShopOnContainers.Services.CatalogDb`)
- **No data migration required**: The Java application reads and writes the same schema
- **Flyway matches EF6 schema**: Flyway migration scripts were created to match the existing Entity Framework 6 schema exactly
- **Flyway baseline-on-migrate**: Enabled in configuration so Flyway will baseline against the existing database without attempting to re-run migrations

### 7.2 Schema Validation

- Hibernate is configured with `ddl-auto: validate` to verify the JPA entity mappings match the existing database schema at startup
- If schema validation fails on startup, the application will not start — this is a safety mechanism

### 7.3 Concurrent Access During Cutover

- Both ASP.NET (blue) and Java (green) instances can safely read from the same database simultaneously during the cutover window
- Write operations should be directed to only one instance at a time to avoid conflicts
- The traffic switch should be atomic (all-or-nothing) to prevent split-brain writes

### 7.4 Post-Cutover Database Tasks

- [ ] Verify Flyway schema history table (`flyway_schema_history`) is populated correctly
- [ ] Confirm no EF6 migration history conflicts (`__EFMigrationsHistory` table can be retained for reference)
- [ ] Monitor database performance for query plan differences between EF6 and Hibernate-generated SQL
- [ ] Review and optimize any slow queries identified via `spring.jpa.show-sql` or query logging
