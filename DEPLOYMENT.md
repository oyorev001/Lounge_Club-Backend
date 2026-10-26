# Deployment — Oyo State Lounge & Clubs Compliance API

Recommended zero-cost stack:

| Piece | Service | Free tier |
|---|---|---|
| API | Koyeb (nano instance, no sleep) | free |
| Database | Neon Postgres | 0.5 GB free |
| File storage | Cloudflare R2 | 10 GB free |
| Frontend | Netlify | free |
| Uptime monitor | UptimeRobot → `GET /health` every 5 min | free |
| Email | Brevo / Gmail SMTP | 300/day free |

## Environment variables (Koyeb/Render/etc.)

```bash
# Database — Neon
ConnectionStrings__sqlConnection=Host=<neon-host>;Database=loungeclubs;Username=<user>;Password=<pass>;Ssl Mode=Require
DatabaseSettings__DBProvider=postgresql   # also supports: mssql, mysql

# JWT
JwtSettings__Secret=<64+ char random secret>
JwtSettings__ValidIssuer=loungeclubs-api
JwtSettings__ValidAudience=loungeclubs-portal

# Paystack (https://dashboard.paystack.com → Settings → API Keys & Webhooks)
Paystack__SecretKey=sk_live_...     # test: sk_test_...
Paystack__PublicKey=pk_live_...     # optional, hosted checkout doesn't need it server-side

# File storage — Cloudflare R2 (bucket private by default; URLs are presigned)
Storage__Provider=s3
Storage__S3__Endpoint=https://<account-id>.r2.cloudflarestorage.com
Storage__S3__Bucket=<bucket-name>
Storage__S3__AccessKey=<r2-access-key>
Storage__S3__SecretKey=<r2-secret-key>
# Storage__S3__PublicBaseUrl=  # only if bucket is behind a public/custom domain

# Email (Brevo: smtp-relay.brevo.com:587, login = account email)
MailSettings__Mail=<smtp-login>
MailSettings__Password=<smtp-key>
MailSettings__Host=smtp-relay.brevo.com
MailSettings__Port=587

# App
AppSettings__PortalUrl=https://<your-netlify-site>.netlify.app
AppSettings__AppUrl=https://<api-host>   # used for CORS origin allowlist
ASPNETCORE_ENVIRONMENT=Production

# REQUIRED in production — the app refuses to start without it.
# Without this env var the seeded admin would use the known dev password.
SeedAdmin__Password=<strong-admin-password>

# REQUIRED behind any reverse proxy (Koyeb/nginx/Cloudflare).
# List the proxy's egress IP(s) — without it, all rate limiting sees one
# shared IP (10 req/min for everyone) and forwarded https:// links break.
Proxy__TrustedIps=<proxy-ip-1>,<proxy-ip-2>

# SMS notifications (optional — outbox accumulates silently if unset)
SmsSettings__Enabled=true
SmsSettings__ApiKey=<termii-or-provider-key>
SmsSettings__SenderId=<sender-name>
```

## Paystack webhook

In Paystack dashboard → Settings → Webhooks:

```
https://<api-host>/api/v1/webhooks/paystack
```

The status endpoint (`GET /api/v1/payments/{reference}`) also verifies with
Paystack on every poll, so payments settle even if a webhook never arrives.

## Local development

```bash
# Postgres (any version >= 13); local trust-auth dev setup:
#   psql -c "CREATE DATABASE loungeclubs"
# appsettings.Development.json already points at Host=localhost ... Database=loungeclubs
dotnet run   # migrations + seeding run automatically on startup
```

Seed admin: `admin@oyolc.gov.ng` / `P@ssword123!` — **change before production**.

## Notes

- Migrations (`InitialPostgres` + `Migrations/TickerQ`) apply automatically on startup.
- TickerQ scheduled jobs (levy reminders, certificate expiry, payment timeouts) run in-process.
- Document uploads go to `wwwroot/uploads` when `Storage:Provider=local` (ephemeral on
  most PaaS — always use `s3` in production).
