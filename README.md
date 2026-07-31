# Leaktester Work Record

Basic leak tester work record system for **PT. Yanmar Diesel Indonesia**.

## Included modules

- JWT login with the existing authentication flow
- Leak test dashboard and daily achievement
- PIC ID card verification
- Cutting list master data
- Leak test work order control
- Engine information and leak test work record table
- Engine model master data
- Start, output update, and complete workflow
- Work record activity log
- MySQL schema and demo data

## Project structure

- `Frontend` - JavaScript/Next.js frontend application
- `Backend` - C#/.NET API, domain/persistence projects, database script, and backend assets

## Default demo access

```text
Username: root
Password: root_native
```

Demo PIC card:

```text
LT-PIC-0001
```

## Database

MySQL 8 is required. From the repository root, run:

```powershell
mysql -u root -p -e "source Backend/database/production_control_monitoring.sql"
```

The script creates `yanmarleaktest`, the login user, work record tables,
and starter records. The production-only migration is also available at:

```text
Backend/Web.API.Persistence/Migrations/20260707_001_production_control_monitoring.sql
```

## Run locally

API:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;"
dotnet run --project Backend\Web.API\Web.API.csproj
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

- `GET /api/leaktester/dashboard`
- `GET|POST /api/leaktester/work-records`
- `GET|POST /api/leaktester/engine-models`
- `GET|POST /api/leaktester/work-orders`
- `POST /api/leaktester/work-orders/scan`
- `POST /api/leaktester/work-orders/{id}/scan-pic`
- `POST /api/leaktester/work-orders/{id}/operators/{operatorId}/remove`
- `POST /api/leaktester/work-orders/{id}/start`
- `POST /api/leaktester/work-orders/{id}/finish`
- `POST /api/leaktester/work-orders/{id}/cancel-finish`
- `GET|POST /api/leaktester/cutting-lists`
- `GET /api/leaktester/pic-cards`
- `GET /api/leaktester/activity-logs`
