FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Micros.Core/Micros.Core.csproj src/Micros.Core/
COPY src/Micros.Scheduler/Micros.Scheduler.csproj src/Micros.Scheduler/

RUN dotnet restore src/Micros.Scheduler/Micros.Scheduler.csproj

COPY src/Micros.Core/ src/Micros.Core/
COPY src/Micros.Scheduler/ src/Micros.Scheduler/

RUN dotnet publish src/Micros.Scheduler/Micros.Scheduler.csproj \
    -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Micros.Scheduler.dll"]
