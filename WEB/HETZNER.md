# Hetzner Deployment

The production container builds the React client, publishes the ASP.NET Core server in Release mode, and runs both as one web application. Job data is kept in `/opt/digitc/data` on the host and mounted into the container at `/data`.

## 1. Prepare The Server Checkout

These commands assume the repository is already checked out at `/opt/digitc/app`:

```bash
cd /opt/digitc/app
git pull
```

## 2. Configure The Shared Password

Create the local deployment environment file. It is ignored by git:

```bash
cd /opt/digitc/app/WEB/Deploy
cp .env.example .env
nano .env
```

Replace the example value with the password that will be shared with the users.

## 3. Build And Start

```bash
cd /opt/digitc/app/WEB/Deploy
docker compose build
docker compose up -d
```

The application is exposed on the Hetzner server's loopback interface and joins the existing external `digitc-net` Docker network so Caddy can reach it by container name:

```text
http://127.0.0.1:5188
```

Verify it on the server:

```bash
curl http://127.0.0.1:5188/health
docker compose logs --tail=100 transgraphier
```

The health endpoint should return:

```json
{"status":"ok"}
```

## 4. Connect Caddy

After the local health check succeeds, add the contents of `Caddyfile.example` to the server's Caddy configuration and replace `transgraphier.example.com` with the real hostname. The upstream is `transgraphier:8080`, resolved through `digitc-net`.

Validate and reload Caddy using the paths and service already configured on the server. A typical package installation uses:

```bash
sudo caddy validate --config /etc/caddy/Caddyfile
sudo systemctl reload caddy
```

Caddy terminates HTTPS and forwards requests to `127.0.0.1:5188`. The ASP.NET application supplies its own shared-password login.

## 5. Deploy Later Changes

```bash
cd /opt/digitc/app
git pull
cd WEB/Deploy
docker compose build
docker compose up -d
```

Docker Compose replaces the application container but preserves the host data under `/opt/digitc/data`.

## Operations

Show status:

```bash
docker compose ps
```

Follow logs:

```bash
docker compose logs -f transgraphier
```

Stop the application without deleting job data:

```bash
docker compose down
```

Start it again:

```bash
docker compose up -d
```

`docker compose down` does not delete the persistent job data under `/opt/digitc/data`.
