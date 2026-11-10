# HomeRun

[![Deploy](https://github.com/tzer0m/HomeRun/actions/workflows/deploy.yml/badge.svg)](https://github.com/tzer0m/HomeRun/actions/workflows/deploy.yml) [![Dependabot](https://github.com/tzer0m/HomeRun/actions/workflows/dependabot/dependabot-updates/badge.svg)](https://github.com/tzer0m/HomeRun/actions/workflows/dependabot/dependabot-updates)

A simple home workout site: a daily checklist with a fasting countdown, a timed session runner, and the 8-week schedule with progression applied.

## Plan

The plan lives in `HomeRun/plan.json` and is edited by hand. It holds the start date, the fasting window, the daily stretching routine, the workouts, the weekly schedule and the 8-week progression. It is copied to the output folder on build.

## Development

Requires .NET 10 and PostgreSQL.

Set the connection string in user secrets, then create the database tables:

```
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>;Port=<port>;Database=homerun;Username=homerun;Password=<password>"
dotnet ef database update -- --environment Development
```

## Authentication

Every page requires sign-in with Pocket ID (cookie and OpenID Connect). Set the client in user secrets, and register `https://<host>/signin-oidc` as a redirect URI for it, for both `https://hr.tzer0m.co.uk` and the local development URL:

```
dotnet user-secrets set "Oidc:ClientId" "<client id>"
dotnet user-secrets set "Oidc:ClientSecret" "<client secret>"
```

The `/health` endpoint stays anonymous so the deploy check and Uptime Kuma work without signing in.

## Deployment

Pushes to `master` are deployed to Tyrion by GitHub Actions on the self-hosted runner, running as a systemd service behind a Cloudflare Tunnel at https://hr.tzer0m.co.uk. The workflow publishes the app, restarts the service, then checks the anonymous `/health` endpoint, which is also what Uptime Kuma monitors.

Dependabot checks NuGet packages weekly on Wednesdays, ignoring major versions and grouping minor and patch updates into one pull request.

## Icons

[Home run icons created by Magnific - Flaticon](https://www.flaticon.com/free-icons/home-run)
