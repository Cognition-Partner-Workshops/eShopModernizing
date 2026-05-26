# Azure VM Deployment Guide — eShop Catalog (Spring Boot)

This guide covers deploying the eShop Catalog Spring Boot application to an Azure VM using Docker containers.

---

## Prerequisites

| Tool | Purpose | Install Guide |
|------|---------|---------------|
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) | Provision Azure resources | `curl -sL https://aka.ms/InstallAzureCLIDeb \| sudo bash` |
| [Docker](https://docs.docker.com/engine/install/) | Build and run containers | Installed via `setup-vm.sh` |
| SSH client | Connect to the VM | Pre-installed on most systems |
| GitHub account | Access GHCR container registry | With a Personal Access Token (PAT) |

---

## 1. Azure VM Creation

### 1.1 Login and Set Subscription

```bash
az login
az account set --subscription "${AZURE_SUBSCRIPTION_ID}"
```

### 1.2 Create Resource Group

```bash
az group create \
  --name rg-eshop-catalog \
  --location eastus
```

### 1.3 Create the VM

```bash
az vm create \
  --resource-group rg-eshop-catalog \
  --name vm-eshop-catalog \
  --image Ubuntu2404 \
  --size Standard_B2s \
  --admin-username azureuser \
  --generate-ssh-keys \
  --public-ip-sku Standard \
  --output json
```

> Save the `publicIpAddress` from the output — you will need it for SSH and DNS.

---

## 2. Network Security Group Configuration

Open the required ports for the application, SSH, and HTTPS:

```bash
az vm open-port \
  --resource-group rg-eshop-catalog \
  --name vm-eshop-catalog \
  --port 22 \
  --priority 1000

az vm open-port \
  --resource-group rg-eshop-catalog \
  --name vm-eshop-catalog \
  --port 8080 \
  --priority 1010

az vm open-port \
  --resource-group rg-eshop-catalog \
  --name vm-eshop-catalog \
  --port 443 \
  --priority 1020
```

---

## 3. VM Setup

### 3.1 SSH Into the VM

```bash
ssh azureuser@<VM_PUBLIC_IP>
```

### 3.2 Run the Setup Script

Copy `scripts/setup-vm.sh` to the VM and execute it:

```bash
scp scripts/setup-vm.sh azureuser@<VM_PUBLIC_IP>:/tmp/setup-vm.sh
ssh azureuser@<VM_PUBLIC_IP> 'chmod +x /tmp/setup-vm.sh && sudo /tmp/setup-vm.sh'
```

This installs Docker, creates the application directory at `/opt/app`, and generates an environment file template.

> **Note:** Log out and back in after setup so Docker group membership takes effect.

---

## 4. Container Registry Setup (GHCR)

### 4.1 Create a GitHub Personal Access Token

1. Go to **GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)**
2. Create a token with `read:packages` scope
3. Save the token securely

### 4.2 Authenticate on the VM

```bash
echo "${GHCR_TOKEN}" | docker login ghcr.io -u "${GITHUB_USERNAME}" --password-stdin
```

---

## 5. Environment Variables

Edit the production environment file on the VM:

```bash
sudo nano /opt/app/production.env
```

### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `SPRING_PROFILES_ACTIVE` | Spring profile to activate | `prod` |
| `DB_URL` | JDBC connection string | `jdbc:sqlserver://db-host:1433;databaseName=CatalogDb;encrypt=true;trustServerCertificate=true` |
| `DB_USERNAME` | Database username | `catalog_user` |
| `DB_PASSWORD` | Database password | *(set securely)* |
| `SERVER_PORT` | Application port (optional) | `8080` |

### Example `production.env`

```env
SPRING_PROFILES_ACTIVE=prod
DB_URL=jdbc:sqlserver://${DB_HOST}:1433;databaseName=${DB_NAME};encrypt=true;trustServerCertificate=true
DB_USERNAME=${DB_USER}
DB_PASSWORD=${DB_PASSWORD}
SERVER_PORT=8080
```

> **Security:** Never commit `production.env` with real credentials. Use Azure Key Vault or environment-specific secrets management for sensitive values.

---

## 6. Deploying the Application

### 6.1 Copy and Run the Deploy Script

```bash
scp scripts/deploy.sh azureuser@<VM_PUBLIC_IP>:/opt/app/deploy.sh
ssh azureuser@<VM_PUBLIC_IP> 'chmod +x /opt/app/deploy.sh'
```

### 6.2 Deploy

```bash
ssh azureuser@<VM_PUBLIC_IP> '/opt/app/deploy.sh latest'
```

Or to deploy a specific tag:

```bash
ssh azureuser@<VM_PUBLIC_IP> '/opt/app/deploy.sh v1.2.3'
```

---

## 7. Health Check Verification

After deployment, verify the application is running:

```bash
curl -f http://<VM_PUBLIC_IP>:8080/actuator/health
```

Expected response:

```json
{"status":"UP"}
```

You can also check additional endpoints:

```bash
# Application info
curl http://<VM_PUBLIC_IP>:8080/actuator/info

# Prometheus metrics
curl http://<VM_PUBLIC_IP>:8080/actuator/prometheus
```

---

## 8. SSL/TLS Configuration

For production, terminate TLS using a reverse proxy. The recommended approach uses Nginx with Let's Encrypt.

### 8.1 Install Nginx and Certbot

```bash
sudo apt-get install -y nginx certbot python3-certbot-nginx
```

### 8.2 Configure Nginx as Reverse Proxy

Create `/etc/nginx/sites-available/eshop-catalog`:

```nginx
server {
    listen 80;
    server_name ${YOUR_DOMAIN};

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/eshop-catalog /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

### 8.3 Obtain SSL Certificate

```bash
sudo certbot --nginx -d ${YOUR_DOMAIN} --non-interactive --agree-tos -m ${YOUR_EMAIL}
```

Certbot automatically configures Nginx for HTTPS and sets up auto-renewal.

### 8.4 Verify Auto-Renewal

```bash
sudo certbot renew --dry-run
```

---

## 9. Monitoring and Logging

### 9.1 Container Logs

```bash
# Follow live logs
docker logs -f eshop-catalog

# Last 100 lines
docker logs --tail 100 eshop-catalog
```

### 9.2 Spring Boot Actuator Endpoints

The application exposes the following actuator endpoints:

| Endpoint | URL | Description |
|----------|-----|-------------|
| Health | `/actuator/health` | Application health status |
| Info | `/actuator/info` | Application metadata |
| Metrics | `/actuator/metrics` | Application metrics |
| Prometheus | `/actuator/prometheus` | Prometheus-format metrics |

### 9.3 Docker Resource Monitoring

```bash
# Container resource usage
docker stats eshop-catalog

# Container inspect
docker inspect eshop-catalog
```

### 9.4 System Monitoring

```bash
# Disk usage
df -h

# Memory usage
free -m

# Running processes
htop
```

### 9.5 Log Rotation

Configure Docker log rotation in `/etc/docker/daemon.json`:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

Restart Docker after changes:

```bash
sudo systemctl restart docker
```

### 9.6 Optional: Azure Monitor Integration

For centralized logging, install the Azure Monitor Agent:

```bash
az vm extension set \
  --resource-group rg-eshop-catalog \
  --vm-name vm-eshop-catalog \
  --name AzureMonitorLinuxAgent \
  --publisher Microsoft.Azure.Monitor \
  --version 1.0
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Container won't start | Check logs: `docker logs eshop-catalog` |
| Health check fails | Verify env vars in `/opt/app/production.env` |
| Cannot connect to DB | Check NSG rules and DB firewall settings |
| Port 8080 not accessible | Verify NSG rules: `az network nsg rule list --resource-group rg-eshop-catalog --nsg-name vm-eshop-catalogNSG` |
| Docker permission denied | Run `sudo usermod -aG docker $USER` and re-login |
| SSL certificate issues | Run `sudo certbot renew` and check Nginx config |
