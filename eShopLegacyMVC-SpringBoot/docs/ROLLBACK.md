# Rollback Plan — eShop Catalog (.NET to Java Migration)

**Version:** 1.0  
**Last Updated:** _YYYY-MM-DD_  
**Owner:** _Migration Team Lead_

---

## Overview

This document defines the rollback procedure for reverting the eShop Catalog application from the Java/Spring Boot implementation back to the original .NET application. The rollback plan ensures business continuity in the event of critical issues discovered after the cutover to the Java application.

---

## Rollback Triggers

Initiate rollback when any of the following conditions are observed:

| Trigger | Threshold |
|---------|-----------|
| Application health check failures | Health endpoint returns non-200 for > 2 consecutive minutes |
| Error rate exceeding thresholds | HTTP 5xx rate > 5% of total requests over a 5-minute window |
| Database connectivity issues | Connection pool exhaustion or repeated timeout errors (> 30s) |
| Performance degradation | P95 response time > 3x baseline or sustained CPU > 90% for 5 minutes |
| Data integrity issues | Any evidence of data corruption or incorrect catalog operations |

---

## Prerequisites

Before initiating rollback, verify:

- [ ] .NET application artifacts (Docker image or deployment package) are available and accessible
- [ ] .NET application configuration (Web.config, connection strings) is preserved
- [ ] DNS/load balancer access credentials are available
- [ ] Database has not undergone irreversible schema changes
- [ ] Communication channels are open (Slack, PagerDuty, email distribution list)

---

## Rollback Steps

### Step 1: Stop the Java/Spring Boot Container

```bash
# Docker deployment
docker stop eshop-catalog-java
docker rm eshop-catalog-java

# Kubernetes deployment
kubectl scale deployment eshop-catalog-java --replicas=0 -n eshop
```

### Step 2: Verify .NET Application Artifacts Are Available

```bash
# Verify Docker image exists
docker images eshop-catalog-dotnet:latest

# Or verify deployment package
ls -la /opt/deployments/eshop-dotnet/
```

Confirm the last known-good .NET image tag or build artifact is accessible.

### Step 3: Re-deploy the .NET Application

```bash
# Docker deployment
docker run -d \
  --name eshop-catalog-dotnet \
  --network eshop-network \
  -p 80:80 \
  -e "ConnectionString=<connection-string>" \
  -e "UseMockData=false" \
  eshop-catalog-dotnet:latest

# Kubernetes deployment
kubectl set image deployment/eshop-catalog \
  eshop-catalog=eshop-catalog-dotnet:latest -n eshop
kubectl scale deployment eshop-catalog --replicas=3 -n eshop
```

### Step 4: Verify Database Compatibility

Both the .NET and Java applications use the same `CatalogDb` schema. Verify:

```sql
-- Confirm schema version is compatible
SELECT * FROM __MigrationHistory ORDER BY MigrationId DESC LIMIT 1;

-- Verify core tables are intact
SELECT COUNT(*) FROM Catalog;
SELECT COUNT(*) FROM CatalogBrand;
SELECT COUNT(*) FROM CatalogType;
```

> **Note:** The Java application uses Flyway for migrations. Ensure no Flyway-specific schema changes conflict with Entity Framework migration history.

### Step 5: Update DNS/Load Balancer

```bash
# Update load balancer target group to point to .NET application instances
# Example for cloud load balancer:
# - Remove Java application targets
# - Add .NET application targets
# - Verify health checks pass on new targets before enabling traffic
```

Switch traffic routing:

1. Drain active connections from Java application (allow 30-second drain period)
2. Point traffic to .NET application endpoint
3. Confirm routing change propagation

### Step 6: Verify .NET Application Health

```bash
# Health check
curl -f http://<dotnet-app-host>/health

# Smoke test - list catalog items
curl -s http://<dotnet-app-host>/api/catalog/items | jq '.count'

# Verify CRUD operations
curl -s http://<dotnet-app-host>/api/catalog/items/1
```

---

## Post-Rollback Verification Checklist

- [ ] Health check endpoint returns HTTP 200
- [ ] Catalog listing page loads correctly
- [ ] Create, Read, Update, Delete operations verified
- [ ] No elevated error rates in monitoring dashboard
- [ ] Response times within acceptable baseline (< 500ms P95)
- [ ] Database connections stable (pool utilization < 70%)
- [ ] Application logs show no critical errors
- [ ] External integrations (if any) functioning correctly
- [ ] Load balancer shows all targets healthy

---

## Communication Plan

### Notification Sequence

| Step | Audience | Channel | Timing |
|------|----------|---------|--------|
| 1 | On-call engineering team | PagerDuty / Slack #incidents | Immediately on trigger |
| 2 | Engineering leadership | Slack #eng-leadership | Within 5 minutes |
| 3 | Product/stakeholders | Email distribution list | Within 15 minutes |
| 4 | Affected end users (if applicable) | Status page update | Within 30 minutes |
| 5 | Post-rollback all-clear | All channels | After verification complete |

### Communication Template

```
SUBJECT: [ROLLBACK] eShop Catalog — Reverted to .NET Application

Status: Rollback {initiated | in progress | complete}
Trigger: {describe the triggering condition}
Impact: {describe user-facing impact}
ETA to resolution: {estimated time}
Next update: {time of next update}
```

---

## Rollback Timeline Expectations

| Phase | Expected Duration |
|-------|-------------------|
| Detection and decision to rollback | 5–10 minutes |
| Stop Java application | 1–2 minutes |
| Deploy .NET application | 3–5 minutes |
| Database verification | 2–3 minutes |
| DNS/load balancer switch | 2–5 minutes (plus propagation) |
| Post-rollback verification | 5–10 minutes |
| **Total estimated rollback time** | **20–35 minutes** |

---

## Post-Rollback Actions

1. **Incident report**: Create an incident ticket documenting the rollback trigger, timeline, and impact
2. **Root cause analysis**: Schedule RCA within 24 hours to identify the Java application issue
3. **Fix and re-test**: Address the root cause before attempting another cutover
4. **Update runbook**: Incorporate lessons learned into this rollback plan
5. **Stakeholder debrief**: Brief stakeholders on findings and updated migration timeline
