# eShop Catalog — Cutover & Rollback Plan

This document describes the procedure for cutting over from the legacy .NET MVC application to the new Spring Boot Java application, and how to roll back if issues arise.

---

## Table of Contents

1. [Pre-Cutover Checklist](#pre-cutover-checklist)
2. [Data Migration](#data-migration)
3. [Cutover Procedure](#cutover-procedure)
4. [Rollback Procedure](#rollback-procedure)
5. [Post-Cutover Monitoring](#post-cutover-monitoring)
6. [Success Criteria](#success-criteria)

---

## Pre-Cutover Checklist

- [ ] All CI/CD pipelines pass (build, tests, spotless, JaCoCo coverage)
- [ ] Docker image built and pushed to container registry
- [ ] Kubernetes manifests reviewed and applied to staging
- [ ] Staging environment fully tested (smoke tests, integration tests)
- [ ] Health endpoints responding: `/actuator/health`, `/actuator/health/readiness`
- [ ] Prometheus metrics available at `/actuator/prometheus`
- [ ] Database migration scripts (Flyway) validated against production schema
- [ ] Secrets (DB credentials) provisioned in Kubernetes and verified
- [ ] TLS certificates provisioned for the Ingress host
- [ ] Load testing completed on staging (response times within SLA)
- [ ] Rollback procedure reviewed and rehearsed
- [ ] Communication sent to stakeholders with cutover window
- [ ] On-call team aware of the cutover schedule
- [ ] Legacy .NET application snapshot/backup taken

---

## Data Migration

### Database Compatibility

The Spring Boot application connects to the same SQL Server database (`CatalogDb`) used by the legacy .NET application. Flyway manages schema migrations.

### Steps

1. **Create a database backup** before any schema changes:
   ```sql
   BACKUP DATABASE CatalogDb TO DISK = '/backups/CatalogDb_pre_cutover.bak'
   ```

2. **Run Flyway migrations** in dry-run mode to preview changes:
   ```bash
   ./mvnw flyway:info -Dspring.profiles.active=prod
   ```

3. **Apply migrations** against the production database:
   ```bash
   ./mvnw flyway:migrate -Dspring.profiles.active=prod
   ```

4. **Verify data integrity** — spot-check row counts and key records:
   ```sql
   SELECT COUNT(*) FROM CatalogItems;
   SELECT COUNT(*) FROM CatalogBrands;
   SELECT COUNT(*) FROM CatalogTypes;
   ```

### Compatibility Notes

- Both the .NET and Java applications can read from the same database simultaneously during the transition period.
- The Java application uses `ddl-auto: validate`, so Hibernate will not alter the schema — only Flyway applies changes.

---

## Cutover Procedure

### Phase 1: Parallel Run (Canary)

1. Deploy the Spring Boot application to production alongside the legacy .NET application.
2. Route a small percentage of traffic (e.g., 10%) to the new Java service via Ingress weight annotations or a traffic-splitting mechanism:
   ```yaml
   nginx.ingress.kubernetes.io/canary: "true"
   nginx.ingress.kubernetes.io/canary-weight: "10"
   ```
3. Monitor error rates, latency, and logs for 30–60 minutes.
4. Gradually increase the traffic percentage (25% → 50% → 100%) as confidence grows.

### Phase 2: DNS / Load Balancer Switch

1. Once the canary phase is successful (100% traffic on Java), update the DNS or load balancer to point entirely to the new service:
   ```bash
   # Update DNS A record or CNAME to point to the new Ingress IP
   # Example: eshop-catalog.example.com → <new-ingress-external-ip>
   ```
2. Set TTL on DNS records to a low value (e.g., 60s) before the cutover to allow fast rollback.
3. Verify the DNS change has propagated:
   ```bash
   dig eshop-catalog.example.com
   curl -sf https://eshop-catalog.example.com/actuator/health
   ```

### Phase 3: Decommission Legacy

1. After a soak period (24–48 hours with no issues):
   - Stop the legacy .NET application containers.
   - Remove legacy deployment resources.
   - Archive the legacy application artifacts.
2. Keep the legacy Docker image and deployment manifests available for at least 30 days in case a late rollback is needed.

---

## Rollback Procedure

### Immediate Rollback (< 5 minutes)

If critical issues are detected during or immediately after cutover:

1. **Revert traffic routing** — remove canary annotations or switch Ingress back:
   ```bash
   kubectl apply -f k8s/ingress-legacy.yml
   ```
   Or revert DNS to the legacy application's IP/CNAME.

2. **Scale down the Java deployment**:
   ```bash
   kubectl scale deployment eshop-catalog --replicas=0
   ```

3. **Verify** the legacy application is serving traffic:
   ```bash
   curl -sf https://eshop-catalog.example.com/
   ```

### Database Rollback

If Flyway migrations introduced breaking schema changes:

1. Restore the database from the pre-cutover backup:
   ```sql
   RESTORE DATABASE CatalogDb FROM DISK = '/backups/CatalogDb_pre_cutover.bak' WITH REPLACE
   ```

2. Restart the legacy application to reconnect.

### Post-Rollback Actions

- Document the issue that triggered the rollback.
- Create a fix branch, apply the fix, and re-run the full CI/CD pipeline.
- Re-test on staging before attempting another cutover.
- Notify stakeholders of the rollback and revised timeline.

---

## Post-Cutover Monitoring

### Key Metrics to Watch

| Metric                       | Source                         | Threshold               |
|------------------------------|--------------------------------|--------------------------|
| HTTP 5xx error rate          | Prometheus / Ingress metrics   | < 0.1%                  |
| P95 response latency         | Prometheus                     | < 500ms                 |
| JVM heap usage               | `/actuator/prometheus`         | < 80% of limit          |
| Database connection pool     | HikariCP metrics               | No pool exhaustion       |
| Pod restart count            | `kubectl get pods`             | 0 restarts              |
| Flyway migration status      | `/actuator/flyway`             | All migrations applied   |

### Monitoring Checklist

- [ ] Grafana dashboards configured with Spring Boot and JVM panels
- [ ] Alerts configured for error rate spikes and high latency
- [ ] Log aggregation (e.g., Azure Monitor, ELK) collecting application logs
- [ ] On-call rotation aware of new service and its health endpoints
- [ ] Periodic health checks verified (`/actuator/health`)

### First 24 Hours

- Check dashboards every 30 minutes for the first 4 hours.
- Validate end-to-end functionality (catalog browse, search, CRUD operations).
- Compare response times and error rates against the legacy application baseline.

---

## Success Criteria

The cutover is considered successful when all of the following are met:

1. **Functional parity** — all catalog operations (list, search, create, edit, delete) work identically to the legacy application.
2. **Performance** — P95 latency is within 10% of the legacy application's baseline.
3. **Availability** — zero unplanned downtime in the first 48 hours post-cutover.
4. **Error rate** — HTTP 5xx rate stays below 0.1% for 48 hours.
5. **Data integrity** — no data loss or corruption; row counts match pre-cutover values.
6. **Health probes** — liveness and readiness probes pass continuously.
7. **Stakeholder sign-off** — product owner confirms acceptance after the soak period.
