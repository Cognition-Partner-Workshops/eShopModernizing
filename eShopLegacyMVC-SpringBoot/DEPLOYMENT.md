# eShop Catalog — Deployment Guide

This document describes the deployment architecture and procedures for the Spring Boot eShop Catalog application on Azure VMs using Docker containers.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Pipeline Flow](#pipeline-flow)
3. [GitHub Environments and Secrets](#github-environments-and-secrets)
4. [Azure VM Prerequisites](#azure-vm-prerequisites)
5. [Environment Variable Configuration](#environment-variable-configuration)
6. [Local Development with Docker Compose](#local-development-with-docker-compose)
7. [DNS Configuration](#dns-configuration)
8. [Monitoring and Health Checks](#monitoring-and-health-checks)

---

## Architecture Overview

```
┌──────────────┐     ┌──────────────┐     ┌──────────────────────┐
│  Developer   │────▶│   GitHub     │────▶│  GitHub Container    │
│  Push/PR     │     │   Actions    │     │  Registry (GHCR)     │
└──────────────┘     └──────┬───────┘     └──────────┬───────────┘
                            │                        │
                    ┌───────▼────────┐       ┌───────▼────────┐
                    │  Staging VM    │       │ Production VM  │
                    │  (Docker)      │       │  (Docker)      │
                    │  Auto-deploy   │       │  Manual gate   │
                    └───────┬────────┘       └───────┬────────┘
                            │                        │
                    ┌───────▼────────┐       ┌───────▼────────┐
                    │  SQL Server    │       │  SQL Server    │
                    │  (Staging DB)  │       │ (Production DB)│
                    └────────────────┘       └────────────────┘
```

The application is packaged as a Docker image, pushed to GitHub Container Registry (GHCR), and deployed to Azure VMs via SSH using GitHub Actions.

---

## Pipeline Flow

The CI/CD pipeline is defined in `.github/workflows/deploy.yml` and runs on push to `main` or `migration/complete-java-migration`:

| Stage | Description | Trigger |
|-------|-------------|---------|
| **Build & Test** | Compiles with JDK 21, runs `./mvnw clean verify`, checks formatting with Spotless, publishes test results via `dorny/test-reporter` | Every push and PR |
| **Docker Build & Push** | Builds multi-stage Docker image, pushes to GHCR with SHA and branch tags | Push to branch only |
| **Deploy to Staging** | SSHs into staging VM, pulls new image, stops old container, starts new one | After Docker push |
| **Health Check** | Polls `/actuator/health` for `{"status":"UP"}` with retry logic | After staging deploy |
| **Deploy to Production** | Same SSH-based deployment; requires manual approval via GitHub Environment protection rules | After health check passes |

---

## GitHub Environments and Secrets

### Environments

Configure these in **Settings > Environments** in the GitHub repository:

| Environment | Protection Rules |
|-------------|-----------------|
| `staging` | Auto-deploy (no approval required) |
| `production` | Required reviewers (manual approval gate) |

### Required Secrets

Configure these in **Settings > Secrets and variables > Actions**:

| Secret | Description | Example |
|--------|-------------|---------|
| `STAGING_VM_HOST` | Staging VM public IP or hostname | `10.0.1.10` |
| `PROD_VM_HOST` | Production VM public IP or hostname | `10.0.2.10` |
| `VM_USERNAME` | SSH username for both VMs | `azureuser` |
| `VM_SSH_KEY` | SSH private key for VM access | PEM-encoded private key |
| `STAGING_URL` | Staging application base URL | `http://staging.example.com:8080` |
| `PROD_URL` | Production application base URL | `https://eshop.example.com` |

---

## Azure VM Prerequisites

Each VM (staging and production) must meet the following requirements:

- **OS**: Ubuntu 22.04 LTS
- **Docker**: Installed and running (see `scripts/setup-vm.sh`)
- **Network Security Group (NSG)** rules:

| Priority | Direction | Port | Protocol | Source | Purpose |
|----------|-----------|------|----------|--------|---------|
| 100 | Inbound | 22 | TCP | GitHub Actions IPs | SSH for deployments |
| 110 | Inbound | 8080 | TCP | Any (or restricted) | Application HTTP traffic |
| 120 | Inbound | 443 | TCP | Any | HTTPS (if reverse proxy configured) |

### VM Setup

Run the idempotent setup script on each fresh VM:

```bash
# Copy and run on the VM
scp scripts/setup-vm.sh azureuser@<vm-ip>:~/
ssh azureuser@<vm-ip> 'chmod +x ~/setup-vm.sh && sudo ~/setup-vm.sh'
```

---

## Environment Variable Configuration

The application reads configuration from environment files on each VM:

### Staging: `/opt/app/staging.env`

```env
SPRING_PROFILES_ACTIVE=prod
DB_URL=jdbc:sqlserver://<staging-db-host>:1433;databaseName=CatalogDb;encrypt=true;trustServerCertificate=true
DB_USERNAME=<staging-db-user>
DB_PASSWORD=<staging-db-password>
DB_DRIVER=com.microsoft.sqlserver.jdbc.SQLServerDriver
USE_MOCK_DATA=false
```

### Production: `/opt/app/production.env`

```env
SPRING_PROFILES_ACTIVE=prod
DB_URL=jdbc:sqlserver://<prod-db-host>:1433;databaseName=CatalogDb;encrypt=true;trustServerCertificate=false
DB_USERNAME=<prod-db-user>
DB_PASSWORD=<prod-db-password>
DB_DRIVER=com.microsoft.sqlserver.jdbc.SQLServerDriver
USE_MOCK_DATA=false
```

The setup script (`scripts/setup-vm.sh`) generates a template env file — fill in actual values before the first deployment.

---

## Local Development with Docker Compose

Use `docker-compose.yml` for local development with the application and a SQL Server instance:

```bash
# Start with mock data (default)
docker compose up --build

# Start with SQL Server database
USE_MOCK_DATA=false docker compose up --build

# Stop and clean up
docker compose down -v
```

The Compose file starts:
- **app**: The Spring Boot application on port 8080
- **sqlserver**: SQL Server 2022 on port 1433

---

## DNS Configuration

For production deployments, configure DNS to point to the VM:

1. **A Record**: Point your domain (e.g., `eshop-catalog.example.com`) to the production VM's public IP.
2. **TTL**: Set a low TTL (60–300 seconds) before cutover to allow fast rollback via DNS.
3. **Reverse Proxy** (optional): Use Nginx or Caddy on the VM to terminate TLS and proxy to port 8080.

Example Nginx configuration:

```nginx
server {
    listen 443 ssl;
    server_name eshop-catalog.example.com;

    ssl_certificate /etc/ssl/certs/eshop.crt;
    ssl_certificate_key /etc/ssl/private/eshop.key;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## Monitoring and Health Checks

The application exposes Spring Boot Actuator endpoints:

| Endpoint | Purpose |
|----------|---------|
| `/actuator/health` | Application health status |
| `/actuator/info` | Application metadata |
| `/actuator/metrics` | Micrometer metrics |
| `/actuator/prometheus` | Prometheus-format metrics export |

### Docker Health Check

The Dockerfile includes a built-in health check:

```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/actuator/health || exit 1
```

### Prometheus Integration

To scrape metrics, add the VM as a Prometheus target:

```yaml
scrape_configs:
  - job_name: 'eshop-catalog'
    metrics_path: '/actuator/prometheus'
    static_configs:
      - targets: ['<vm-ip>:8080']
```
