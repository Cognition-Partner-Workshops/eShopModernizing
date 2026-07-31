# Cutover Procedure — eShop Catalog (.NET to Java Migration)

**Version:** 1.0  
**Last Updated:** _YYYY-MM-DD_  
**Owner:** _Migration Team Lead_

---

## Overview

This document defines the step-by-step cutover procedure for migrating the eShop Catalog application from .NET Framework (ASP.NET MVC 5) to Java (Spring Boot 3.x). The cutover transitions production traffic from the legacy .NET application to the new Java/Spring Boot application while maintaining data integrity and minimizing downtime.

---

## Pre-Cutover Checklist

All items must be confirmed before scheduling the cutover window.

### Migration Readiness

- [ ] All migration epics completed and tested (Epics 1–8)
- [ ] All unit tests passing in Java application
- [ ] Integration tests validated against staging database
- [ ] Code review completed for all migration PRs

### Infrastructure

- [ ] Database migration scripts verified (Flyway migrations applied cleanly)
- [ ] Docker image built and tested (`eshop-catalog-java:latest`)
- [ ] Docker image pushed to container registry
- [ ] Staging environment validated with production-like data
- [ ] Load testing completed (results within acceptable thresholds)
- [ ] SSL/TLS certificates configured for Java application endpoints

### Operational Readiness

- [ ] Rollback plan reviewed by team (see [ROLLBACK.md](./ROLLBACK.md))
- [ ] Monitoring and alerting configured (health checks, error rates, latency)
- [ ] Log aggregation configured for Java application
- [ ] On-call schedule confirmed for cutover window
- [ ] Communication sent to stakeholders with maintenance window details

### Data

- [ ] Database backup strategy confirmed
- [ ] Data validation queries prepared
- [ ] CatalogDb schema compatibility verified between .NET and Java apps

---

## Cutover Steps

### Step 1: Schedule Maintenance Window

- Select a low-traffic period (recommended: weekday off-peak hours)
- Notify stakeholders at least 48 hours in advance
- Confirm on-call engineering team availability
- Duration: 2 hours (1 hour execution + 1 hour buffer)

### Step 2: Take Database Backup

```bash
# Full backup of CatalogDb
pg_dump -h <db-host> -U <db-user> -d CatalogDb -F c -f /backups/catalogdb_pre_cutover_$(date +%Y%m%d_%H%M%S).dump

# Verify backup integrity
pg_restore --list /backups/catalogdb_pre_cutover_*.dump | head -20
```

> **Checkpoint:** Confirm backup completed successfully before proceeding.

### Step 3: Deploy Java Application to Staging

```bash
# Pull the verified Docker image
docker pull <registry>/eshop-catalog-java:<release-tag>

# Deploy to staging environment
docker run -d \
  --name eshop-catalog-java-staging \
  --network eshop-network \
  -p 8081:8080 \
  -e "SPRING_PROFILES_ACTIVE=staging" \
  -e "SPRING_DATASOURCE_URL=jdbc:sqlserver://<db-host>:1433;databaseName=CatalogDb" \
  <registry>/eshop-catalog-java:<release-tag>

# Kubernetes deployment
kubectl apply -f k8s/staging/deployment.yaml
kubectl rollout status deployment/eshop-catalog-java -n eshop-staging
```

### Step 4: Run Smoke Tests on Staging

```bash
# Health check
curl -f http://staging:8081/actuator/health

# Catalog listing
curl -s http://staging:8081/api/catalog/items | jq '.totalItems'

# CRUD operations
# Read
curl -s http://staging:8081/api/catalog/items/1

# Create
curl -X POST http://staging:8081/api/catalog/items \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Item","price":9.99,"catalogTypeId":1,"catalogBrandId":1}'

# Update
curl -X PUT http://staging:8081/api/catalog/items/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Item","price":19.99,"catalogTypeId":1,"catalogBrandId":1}'

# Pagination
curl -s "http://staging:8081/api/catalog/items?pageIndex=0&pageSize=10"
```

> **Checkpoint:** All smoke tests must pass before proceeding.

### Step 5: Switch DNS/Load Balancer to Staging

```bash
# Drain connections from .NET application (30-second drain)
# Update load balancer target group

# Option A: Blue/Green via load balancer
# - Register Java staging targets in production target group
# - Wait for health checks to pass
# - Deregister .NET targets

# Option B: DNS switch
# - Update DNS A/CNAME record to point to Java application endpoint
# - TTL should have been lowered to 60s at least 24 hours prior
```

1. Enable traffic to Java application targets
2. Verify health checks pass on new targets
3. Disable traffic to .NET application targets
4. Confirm zero traffic flowing to .NET application

### Step 6: Monitor for 30 Minutes

During the monitoring window, actively watch:

| Metric | Acceptable Range | Action if Exceeded |
|--------|-----------------|-------------------|
| HTTP 5xx rate | < 1% | Investigate; rollback if > 5% |
| P95 response time | < 500ms | Investigate; rollback if > 1500ms |
| CPU utilization | < 70% | Scale up if sustained > 80% |
| Memory utilization | < 80% | Investigate potential memory leaks |
| Database connection pool | < 70% utilized | Check for connection leaks |
| Active user sessions | Comparable to pre-cutover baseline | Verify no session loss |

```bash
# Continuous health monitoring
watch -n 10 'curl -s http://production:8080/actuator/health | jq .'

# Check error rates via logs
tail -f /var/log/eshop-catalog-java/application.log | grep -i error
```

> **Checkpoint:** If any rollback trigger is hit during this window, execute the [Rollback Plan](./ROLLBACK.md).

### Step 7: Promote Staging to Production

Once the 30-minute monitoring window passes without issues:

```bash
# Tag the release as production-verified
docker tag <registry>/eshop-catalog-java:<release-tag> \
  <registry>/eshop-catalog-java:production

# Scale to production capacity
kubectl scale deployment/eshop-catalog-java --replicas=3 -n eshop-production

# Remove staging label / finalize production deployment
kubectl label deployment/eshop-catalog-java environment=production -n eshop-production
```

### Step 8: Run Full Validation Suite

```bash
# Execute full integration test suite against production
./mvnw verify -P integration-tests -Dtest.target=production

# Run data integrity checks
curl -s http://production:8080/api/catalog/items?pageSize=100 | jq '.totalItems'
```

---

## Post-Cutover Validation

### Application Health

- [ ] Health check endpoints responding (`/actuator/health` returns UP)
- [ ] All Spring Boot Actuator endpoints accessible
- [ ] Application startup logs show no warnings or errors

### Functional Verification

- [ ] All CRUD operations working (Create, Read, Update, Delete catalog items)
- [ ] Pagination functioning correctly
- [ ] Search/filter operations returning expected results
- [ ] Image URLs resolving correctly

### Performance Metrics

- [ ] P50 response time within acceptable range (< 200ms)
- [ ] P95 response time within acceptable range (< 500ms)
- [ ] P99 response time within acceptable range (< 1000ms)
- [ ] Throughput comparable to .NET application baseline
- [ ] No memory leaks observed over monitoring period

### Error Monitoring

- [ ] No increase in error rates compared to .NET baseline
- [ ] No unhandled exceptions in application logs
- [ ] No 5xx responses beyond acceptable threshold (< 0.1%)

### Database Operations

- [ ] Connection pool stable and within limits
- [ ] Query performance consistent with expectations
- [ ] No deadlocks or long-running transactions
- [ ] Flyway migration history table intact

---

## Rollback Triggers

If any of the following conditions are observed during or after cutover, initiate the rollback procedure documented in [ROLLBACK.md](./ROLLBACK.md):

- Health check failures for > 2 consecutive minutes
- HTTP 5xx error rate exceeds 5% over a 5-minute window
- Database connectivity loss or repeated timeouts
- P95 response time > 3x baseline for sustained period (> 5 minutes)
- Data integrity issues (incorrect or missing catalog data)

---

## Sign-Off Procedure

The following sign-offs are required to consider the cutover complete:

| Role | Name | Sign-Off | Date |
|------|------|----------|------|
| Migration Lead | _________________ | ☐ | ________ |
| Engineering Manager | _________________ | ☐ | ________ |
| QA Lead | _________________ | ☐ | ________ |
| DevOps/SRE | _________________ | ☐ | ________ |
| Product Owner | _________________ | ☐ | ________ |

### Sign-Off Criteria

1. All post-cutover validation checks passed
2. 30-minute monitoring window completed without rollback triggers
3. No P1/P2 issues reported by end users
4. Performance metrics within acceptable range
5. Rollback plan confirmed still viable (artifacts retained for 7 days)

---

## Post-Cutover Housekeeping

After sign-off is complete:

1. **Retain .NET artifacts**: Keep .NET Docker images and deployment packages available for 7 days minimum
2. **Update documentation**: Mark migration as complete in project tracking
3. **Decommission timeline**: Schedule .NET infrastructure decommission (recommend 30-day retention)
4. **Retrospective**: Schedule migration retrospective within 1 week
5. **Update monitoring**: Adjust alerting baselines to reflect Java application norms
