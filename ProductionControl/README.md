# ProductionControl Backend

Layered .NET 8 backend for the PT YKK AP Indonesia Production Control system.

## Projects

- `ProductionControl.Domain`: authentication, response, and production domain models.
- `ProductionControl.Persistence`: EF Core context, auth service, and production database mappings.
- `../ProductionControl.WebAPI`: ASP.NET Core Web API.

## Database

Use the on-premise MySQL database:

```text
Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=db_production_control;SslMode=None;AllowPublicKeyRetrieval=True;
```

Apply the production schema from:

```text
ProductionControl.Persistence/Migrations/20260707_001_production_control_monitoring.sql
ProductionControl.Persistence/Migrations/20260707_002_production_work_order_operators.sql
ProductionControl.Persistence/Migrations/20260708_001_shift_master_and_no_target.sql
```

## API Modules

- `POST /api/auth/login`
- `GET /api/production/dashboard`
- `GET|POST /api/production/work-orders`
- `POST /api/production/work-orders/scan`
- `POST /api/production/work-orders/{id}/scan-pic`
- `POST /api/production/work-orders/{id}/operators/{operatorId}/remove`
- `POST /api/production/work-orders/{id}/start`
- `POST /api/production/work-orders/{id}/finish`
- `POST /api/production/work-orders/{id}/cancel-finish`
- `GET|POST /api/production/cutting-lists`
- `GET /api/production/pic-cards`
- `GET /api/production/activity-logs`

## Run

```powershell
$env:ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=db_production_control;SslMode=None;AllowPublicKeyRetrieval=True;"
dotnet run --project ..\ProductionControl.WebAPI\ProductionControl.WebAPI.csproj --urls http://localhost:5241
```
