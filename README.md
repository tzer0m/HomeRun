# HomeRun

A simple home workout site: a daily checklist with a fasting countdown, a timed session runner, and the 4-week schedule with progression applied.

## Plan

The plan lives in `HomeRun/plan.json` and is edited by hand. It holds the start date, the fasting window, the daily stretching routine, the workouts, the weekly schedule and the 4-week progression. It is copied to the output folder on build.

## Development

Requires .NET 10 and PostgreSQL.

Set the connection string in user secrets, then create the database tables:

```
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>;Port=<port>;Database=homerun;Username=homerun;Password=<password>"
dotnet ef database update -- --environment Development
```

## Icons

[Home run icons created by Magnific - Flaticon](https://www.flaticon.com/free-icons/home-run)
