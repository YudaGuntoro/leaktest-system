# Production Control Monitoring System

Basic production control system for **PT YKK AP Indonesia**, adapted from the
Production Control domain.

## Included modules

- JWT login with the existing authentication flow
- Production dashboard and daily achievement
- PIC ID card verification
- Cutting list master data
- Production work order control
- Start, output update, and complete workflow
- Production activity log
- MySQL schema and demo data

## Default demo access

```text
Username: admin
Password: admin123
```

Demo PIC card:

```text
YKK-PIC-0001
```

## Database

MySQL 8 is required. From the repository root, run:

```powershell
mysql -u root -p -e "source database/production_control_monitoring.sql"
```

The script creates `db_production_control`, the login user, production tables,
and starter records. The production-only migration is also available at:

```text
ProductionControl/ProductionControl.Persistence/Migrations/20260707_001_production_control_monitoring.sql
```

## Run locally

API:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=db_production_control;SslMode=None;AllowPublicKeyRetrieval=True;"
dotnet run --project ProductionControl.WebAPI\ProductionControl.WebAPI.csproj
```

Frontend, in another terminal:

```powershell
Set-Location Frontend
npm install
$env:NEXT_PUBLIC_API_BASE_URL="http://localhost:5241"
npm run dev
```

Open `http://localhost:3000`.

## Core API

Core endpoints:

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
