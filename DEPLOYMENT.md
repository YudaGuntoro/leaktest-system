# On-Premise Deployment

This project is intended to run against the on-premise MySQL database.

## Requirements

- Windows Server or Windows workstation
- .NET 8 Runtime or SDK
- Node.js 20+
- MySQL 8 at `127.0.0.1:3306`

## Database

Connection string:

```text
Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;
```

Apply the schema:

```powershell
mysql -h 127.0.0.1 -P 3306 -u root -pYOUR_PASSWORD < Backend\database\production_control_monitoring.sql
```

## API

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;"
dotnet run --project Backend\Web.API\Web.API.csproj --urls http://localhost:5241
```

Swagger:

```text
http://localhost:5241/swagger
```

## Frontend

```powershell
Set-Location Frontend
npm install
$env:NEXT_PUBLIC_API_BASE_URL="http://localhost:5241"
npm run dev
```

Open:

```text
http://localhost:3000
```
