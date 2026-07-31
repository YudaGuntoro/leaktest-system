# Leaktester Work Record Backend

Layered .NET 8 backend for the PT. Yanmar Diesel Indonesia Leaktester Work Record system.

## Projects

- `Web.API`: ASP.NET Core Web API.
- `Web.API.Domain`: authentication, response, and work record domain models.
- `Web.API.Persistence`: EF Core context, auth service, and work record database mappings.

## Database

Use the on-premise MySQL database:

```text
Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;
```

Apply the work record schema from:

```text
Web.API.Persistence/Migrations/20260729_003_engine_models.sql
Web.API.Persistence/Migrations/20260729_001_leak_test_work_records.sql
Web.API.Persistence/Migrations/20260729_006_decimal_pressure_values.sql
Web.API.Persistence/Migrations/20260730_002_user_roles.sql
Web.API.Persistence/Migrations/20260730_003_user_is_active.sql
```

For an existing database that still has legacy production-control tables, run:

```text
Web.API.Persistence/Migrations/20260730_001_drop_unused_tables.sql
```

## API Modules

- `POST /api/auth/login`
- `GET|POST /api/leaktester/work-records`
- `GET|POST /api/leaktester/engine-models`
- `GET /api/leaktester/status`

## Run

```powershell
$env:ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;"
dotnet run --project Web.API\Web.API.csproj --urls http://localhost:5241
```
