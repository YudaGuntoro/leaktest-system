FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LeakTestWorker.csproj", "."]
RUN dotnet restore "LeakTestWorker.csproj"
COPY . .
RUN dotnet publish "LeakTestWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LeakTestWorker.dll"]
