# Transgraphier 2.4.1 Self Hosting

This app is meant to run as one published ASP.NET server. The server serves the web UI, handles uploads, runs the engine, and stores job data.

## 1. Publish

From the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File WEB\Publish-Web.ps1
```

This creates:

```text
WEB\publish
```

## 2. Run Locally In Production Mode

```powershell
powershell -ExecutionPolicy Bypass -File WEB\Start-Published.ps1 `
  -SharedPassword "choose-a-password"
```

Open:

```text
http://localhost:5188
```

By default, job data is stored in:

```text
WEB\data
```

You can choose a different data folder:

```powershell
powershell -ExecutionPolicy Bypass -File WEB\Start-Published.ps1 `
  -SharedPassword "choose-a-password" `
  -WorkspaceRoot "D:\DigitC2\Data"
```

## 3. Auto Start On The PC

After publishing, install a Windows scheduled task:

```powershell
powershell -ExecutionPolicy Bypass -File WEB\Install-StartupTask.ps1 `
  -SharedPassword "choose-a-password" `
  -WorkspaceRoot "D:\DigitC2\Data"
```

Start it immediately:

```powershell
Start-ScheduledTask -TaskName "Transgraphier 2.4.1"
```

Stop it:

```powershell
Stop-ScheduledTask -TaskName "Transgraphier 2.4.1"
```

Remove it:

```powershell
Unregister-ScheduledTask -TaskName "Transgraphier 2.4.1" -Confirm:$false
```

The installer writes a local runner script under `WEB\LocalHost`. That file contains the shared password and is ignored by git.

## 4. Recommended First Internet Exposure: Cloudflare Tunnel

Use this when you do not know whether your ISP gives you a public inbound IP, or you do not want to open router ports.

High-level steps:

1. Create or use a Cloudflare account.
2. Add a domain to Cloudflare, or use an existing Cloudflare-managed domain.
3. In Cloudflare Zero Trust, create a tunnel for this PC.
4. Install `cloudflared` on the PC.
5. Point the tunnel public hostname to:

```text
http://localhost:5188
```

6. Share the public HTTPS URL and the Transgraphier shared password with colleagues.

Cloudflare's current tunnel docs are here:

```text
https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/get-started/create-remote-tunnel/
```

The app already has its own shared-password login. You can later add Cloudflare Access on top if you want email-based access rules.

## 5. Synology / Router Path

Use this if you have a reachable public IP or working Synology DDNS.

Recommended shape:

```text
Internet HTTPS
  -> Synology reverse proxy
  -> http://PC-LAN-IP:5188
```

Keep the ASP.NET app listening on the LAN/PC:

```text
http://0.0.0.0:5188
```

Then configure Synology reverse proxy to terminate HTTPS and forward to the PC.

## 6. Production Notes

- Keep `DigitC2:SharedPassword` non-empty when exposed to the internet.
- Keep the PC awake and prevent sleep.
- Put `WorkspaceRoot` on a disk with enough free space.
- Default upload limit is 100 MB.
- Default job cleanup deletes jobs older than 14 days.
- After code changes, rerun `WEB\Publish-Web.ps1` and restart the hosted process.
