# Deployment runbook — single VPS (Hetzner CX22)

Deploys the whole stack with Docker Compose on one server, served over **HTTPS on a
domain**, with a **Caddy** reverse proxy that obtains and auto-renews a free Let's
Encrypt certificate.

```
Internet ──80/443──► Caddy (TLS, auto-renew) ──► client:8080 (Blazor)
                                                     │
                             ┌───────────────────────┼───────────────┐
                             ▼                        ▼
                       ml_service:8000          postgres:5432
                       (FastAPI + torch)        (2 databases)
                       internal only            internal only
```

Only Caddy is exposed to the internet (ports 80 + 443). The client, ML service, and
Postgres are reachable only on the internal Docker network.

Target box: **Hetzner CX22** — 2 vCPU / 4 GB RAM / 40 GB disk (or any ~4 GB VPS).

---

## 1. Provision the server

1. Create an Ubuntu 24.04 VPS (Hetzner Cloud → CX22).
2. In the Hetzner Cloud firewall (or `ufw`), allow inbound **22 (SSH)**, **80 (HTTP)**
   and **443 (HTTPS)**. Port 80 is required for the Let's Encrypt HTTP-01 challenge.
   Do **not** expose 5432 or 8000 — those stay on the internal Docker network.
3. SSH in as root (or a sudo user).

## 2. Point a domain at the server

You need a hostname that resolves to the server's public IP. A free
[DuckDNS](https://www.duckdns.org) subdomain works well:

1. Sign in to DuckDNS, create a sub-domain (e.g. `label-inspection`).
2. Set its IP to the VPS public IPv4.
3. Confirm it resolves: `dig +short label-inspection.duckdns.org` → your IP.

Any real domain works too — just create an `A` record pointing at the server.

## 3. Install Docker

```bash
curl -fsSL https://get.docker.com | sh
docker --version && docker compose version
```

## 4. Get the code

```bash
git clone <your-repo-url> label-inspection-system
cd label-inspection-system
```

## 5. Configure secrets

```bash
cp .env.prod.example .env.prod
openssl rand -hex 24   # run once per secret value below
nano .env.prod
```

Fill in every value in `.env.prod`:

| Variable | Notes |
|---|---|
| `DOMAIN` | The hostname Caddy serves HTTPS on (e.g. `label-inspection.duckdns.org`) |
| `SERVER_IP` | Public IPv4 of the VPS (e.g. `203.0.113.10`) |
| `POSTGRES_PASSWORD` | Postgres superuser password |
| `ML_DB_PASSWORD` | Password for the `ml_service_user` role |
| `INSPECTION_ML_API_KEY` | Shared secret, client → ML |
| `INSPECTION_WEBHOOK_SECRET` | Shared secret, ML → client webhook |

`.env.prod` is gitignored — keep it only on the server.

## 6. Build & launch

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

First build takes a few minutes (PyTorch CPU wheel + model weights bake into the ML
image). On startup Caddy contacts Let's Encrypt and issues the certificate for
`$DOMAIN` automatically — no manual cert steps.

## 7. Verify

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod ps   # all up; postgres healthy
docker logs -f lis-caddy       # "certificate obtained successfully"
docker logs -f lis-client      # EF migrations applied, "Hosting environment: Production"
docker logs -f lis-ml          # "loaded resnet model", workers started
```

Then from anywhere:

```bash
curl -I https://<DOMAIN>/                 # HTTP 200 with a valid cert
curl -I http://<DOMAIN>/                  # 308 redirect to https://
```

Open `https://<DOMAIN>/` in a browser, run one inspection end to end, and confirm the
ML result comes back via the webhook.

---

## Operations

```bash
# Update to latest code
git pull
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build

# Tail logs
docker compose -f docker-compose.prod.yml logs -f

# Stop / start
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d

# Backup the Postgres data
docker exec lis-postgres pg_dumpall -U postgres > backup_$(date +%F).sql
```

Persistent state lives in named volumes, all of which survive `down`/`up`:

| Volume | Contents |
|---|---|
| `postgres_data` | Database |
| `app_data` | Uploaded images (client `App_Data`) |
| `dataprotection_keys` | ASP.NET DataProtection keys (keeps users logged in across redeploys) |
| `caddy_data` | Let's Encrypt certificates + ACME account |
| `caddy_config` | Caddy autosave config |

A `down -v` **deletes** all of them (including the DB and issued certs).

---

## TLS / HTTPS notes

- **Certificate lifecycle is fully automatic.** Caddy issues on first start and
  renews well before expiry. Certs and the ACME account persist in `caddy_data`, so
  restarts don't re-issue and won't hit Let's Encrypt rate limits.
- **The client runs behind the proxy over plain HTTP** (`client:8080`). It's told the
  original request was HTTPS via `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` in the
  compose file, so redirects, secure cookies, and antiforgery work correctly.
- **Reverse-proxy config** lives in `docker/caddy/Caddyfile`; the domain is injected
  from the `DOMAIN` env var. To change domains: update `DOMAIN` in `.env.prod` and
  re-run the compose `up -d` command.
- **DuckDNS + static IP:** because the Hetzner IP is static, no dynamic-DNS updater is
  needed. If you ever move to a changing IP, run the DuckDNS updater on the box.

## Other notes

- **Memory.** PyTorch + the two CNNs use the most RAM. If the ML container gets
  OOM-killed on a smaller box, reduce the worker-thread count in
  `ml_service/image_inspection_service/main.py` (`for i in range(3)`).
- **DB init runs once.** The `docker/postgres/init-prod` scripts only execute when the
  `postgres_data` volume is first created. Changing them later requires a fresh volume
  (or manual `psql`).
