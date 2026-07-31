# eShop Catalog — Cutover & Rollback Plan

This document describes the procedure for cutting over from the legacy .NET MVC application to the new Spring Boot Java application on Azure VMs, and how to roll back if issues arise.

---

## Table of Contents

1. [Pre-Cutover Checklist](#pre-cutover-checklist)
2. [Database Migration](#database-migration)
3. [Cutover Procedure](#cutover-procedure)
4. [Rollback Plan](#rollback-plan)
5. [Monitoring Checklist](#monitoring-checklist)
6. [Communication Plan](#communication-plan)
7. [Success Criteria](#success-criteria)

---

## Pre-Cutover Checklist

### CI/CD Pipeline

- [ ] GitHub Actions pipeline passes: build, tests, Spotless formatting, JaCoCo coverage
- [ ] Docker image built and pushed to GHCR (`ghcr.io`)
- [ ] Staging deployment completed via GitHub Actions
- [ ] Health endpoint returns `{"status":"UP"}` on staging

### Infrastructure

- [ ] Production Azure VM provisioned (Ubuntu 22.04, Docker installed via `scripts/setup-vm.sh`)
- [ ] NSG rules configured: SSH (port 22), HTTP (port 8080)
- [ ] Environment file populated: `/opt/app/production.env`
- [ ] SSH key for GitHub Actions stored in repository secrets (`VM_SSH_KEY`)
- [ ] DNS records prepared with low TTL (60s) for fast rollback

### Application

- [ ] Flyway migrations validated against production database schema
- [ ] Database backup taken before any schema changes
- [ ] Smoke tests passed on staging (catalog browse, search, CRUD)
- [ ] Load testing completed — response times within SLA
- [ ] Legacy .NET application snapshot/backup taken

### Organizational

- [ ] Rollback procedure reviewed and rehearsed by the team
- [ ] Communication sent to stakeholders with cutover window
- [ ] On-call team aware of the cutover schedule

---

## Database Migration

### Compatibility

Both the .NET and Java applications connect to the same SQL Server database (`CatalogDb`). The Java application uses Flyway for schema migrations and `ddl-auto: validate` — Hibernate will not alter the schema.

### Steps

1. **Create a database backup** before any schema changes:
   ```sql
   BACKUP DATABASE CatalogDb TO DISK = '/backups/CatalogDb_pre_cutover.bak'
   ```

2. **Preview Flyway migrations** in dry-run mode:
   ```bash
   ./mvnw flyway:info -Dspring.profiles.active=prod
   ```

3. **Apply migrations** against the production database:
   ```bash
   ./mvnw flyway:migrate -Dspring.profiles.active=prod
   ```

4. **Verify data integrity** — spot-check row counts:
   ```sql
   SELECT COUNT(*) FROM Catalog;
   SELECT COUNT(*) FROM CatalogBrand;
   SELECT COUNT(*) FROM CatalogType;
   ```

### Database Rollback Strategy

If Flyway migrations introduced breaking schema changes:

- **Option A — Flyway undo migrations**: If undo migrations (`U1__*.sql`) are available, run:
  ```bash
  ./mvnw flyway:undo -Dspring.profiles.active=prod
  ```
- **Option B — Backup restore**: Restore from the pre-cutover backup:
  ```sql
  RESTORE DATABASE CatalogDb FROM DISK = '/backups/CatalogDb_pre_cutover.bak' WITH REPLACE
  ```

Both options require restarting the legacy application afterward.

---

## Cutover Procedure

### Phase 1: Deploy to Production VM

1. Trigger the GitHub Actions pipeline by merging to `main` or confirm the production deployment job has the correct image tag.

2. The pipeline (with manual approval gate on the `production` environment) will:
   - SSH into the production VM
   - Pull the Docker image from GHCR
   - Stop the old container and start the new one

   Manual equivalent:
   ```bash
   ssh azureuser@<prod-vm-ip>
   docker pull ghcr.io/<org>/eshop-catalog:<commit-sha>
   docker stop eshop-catalog || true
   docker rm eshop-catalog || true
   docker run -d \
     --name eshop-catalog \
     --restart unless-stopped \
     -p 8080:8080 \
     --env-file /opt/app/production.env \
     ghcr.io/<org>/eshop-catalog:<commit-sha>
   ```

3. Verify the application is healthy:
   ```bash
   curl -sf http://<prod-vm-ip>:8080/actuator/health
   # Expected: {"status":"UP"}
   ```

### Phase 2: Traffic Switching

**Single-VM approach (stop old, start new):**

1. Stop the legacy .NET application on the production VM (or separate legacy VM).
2. Update DNS A record to point to the Java application's VM IP.
3. Verify DNS propagation:
   ```bash
   dig eshop-catalog.example.com
   curl -sf https://eshop-catalog.example.com/actuator/health
   ```

**Blue-green approach (two VMs):**

1. Keep the legacy application running on VM-A.
2. Deploy the Java application to VM-B.
3. Switch DNS from VM-A to VM-B.
4. Monitor for 30–60 minutes.
5. If stable, decommission VM-A after the soak period.

### Phase 3: Decommission Legacy

After a soak period (24–48 hours with no issues):

1. Stop the legacy .NET application container or service.
2. Archive the legacy application artifacts.
3. Keep the legacy Docker image and VM snapshot available for at least 30 days.

---

## Rollback Plan

### Immediate Rollback (< 5 minutes)

If critical issues are detected during or immediately after cutover:

1. **Stop the Java application** on the production VM:
   ```bash
   ssh azureuser@<prod-vm-ip> 'docker stop eshop-catalog'
   ```

2. **Restart the legacy .NET application**:
   ```bash
   # If legacy runs on the same VM:
   ssh azureuser@<prod-vm-ip> 'docker start eshop-legacy'

   # If legacy runs on a separate VM (blue-green):
   # Switch DNS back to the legacy VM IP
   ```

3. **Revert DNS** to point to the legacy application's IP:
   ```bash
   # Update DNS A record back to legacy VM IP
   dig eshop-catalog.example.com  # Verify propagation
   ```

4. **Verify** the legacy application is serving traffic:
   ```bash
   curl -sf https://eshop-catalog.example.com/
   ```

### Database Rollback

See [Database Rollback Strategy](#database-rollback-strategy) above.

### Post-Rollback Actions

- Document the issue that triggered the rollback.
- Create a fix branch, apply the fix, re-run the full CI/CD pipeline.
- Re-test on staging before attempting another cutover.
- Notify stakeholders of the rollback and revised timeline.

---

## Monitoring Checklist

### Key Metrics to Watch During Cutover

| Metric | Source | Threshold |
|--------|--------|-----------|
| HTTP 5xx error rate | Application logs / Prometheus | < 0.1% |
| P95 response latency | Micrometer / Prometheus | < 500ms |
| JVM heap usage | `/actuator/prometheus` | < 80% of limit |
| Database connection pool | HikariCP metrics | No pool exhaustion |
| Container restart count | `docker inspect` | 0 restarts |
| Flyway migration status | `/actuator/flyway` | All migrations applied |

### Actuator Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/actuator/health` | Application health status |
| `/actuator/metrics` | Micrometer metrics |
| `/actuator/prometheus` | Prometheus-format metrics export |
| `/actuator/flyway` | Flyway migration status |

### Prometheus Integration

If Prometheus is configured, add the VM as a scrape target:

```yaml
scrape_configs:
  - job_name: 'eshop-catalog'
    metrics_path: '/actuator/prometheus'
    static_configs:
      - targets: ['<vm-ip>:8080']
```

### First 24 Hours

- Check dashboards every 30 minutes for the first 4 hours.
- Validate end-to-end functionality (catalog browse, search, CRUD operations).
- Compare response times and error rates against the legacy application baseline.

---

## Communication Plan

### Before Cutover

| When | Who | What |
|------|-----|------|
| T-5 days | All stakeholders | Announce cutover window, link to this document |
| T-1 day | Engineering + Ops | Final go/no-go decision based on staging results |
| T-1 hour | On-call team | Confirm readiness, verify monitoring dashboards |

### During Cutover

| When | Who | What |
|------|-----|------|
| T+0 | Engineering | Begin cutover, post status to team channel |
| T+15 min | Engineering | Report initial health check results |
| T+1 hour | Engineering | Report stability metrics, confirm or escalate |

### After Cutover

| When | Who | What |
|------|-----|------|
| T+4 hours | Engineering | Summary of first 4 hours to stakeholders |
| T+24 hours | Engineering | Confirm soak period passed, declare success or plan remediation |
| T+48 hours | Product Owner | Final sign-off and legacy decommission approval |

### Rollback Communication

If rollback is triggered:

1. Immediately notify the on-call channel: "Rolling back eShop Catalog to legacy .NET — [reason]"
2. Post a follow-up within 1 hour with root cause analysis.
3. Schedule a retrospective within 48 hours.

---

## Success Criteria

The cutover is considered successful when all of the following are met:

1. **Functional parity** — all catalog operations (list, search, create, edit, delete) work identically to the legacy application.
2. **Performance** — P95 latency is within 10% of the legacy application's baseline.
3. **Availability** — zero unplanned downtime in the first 48 hours post-cutover.
4. **Error rate** — HTTP 5xx rate stays below 0.1% for 48 hours.
5. **Data integrity** — no data loss or corruption; row counts match pre-cutover values.
6. **Health probes** — `/actuator/health` returns `UP` continuously.
7. **Stakeholder sign-off** — product owner confirms acceptance after the soak period.
