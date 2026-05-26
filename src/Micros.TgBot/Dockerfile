FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Micros.Core/Micros.Core.csproj src/Micros.Core/
COPY src/Micros.TgBot/Micros.TgBot.csproj src/Micros.TgBot/

RUN dotnet restore src/Micros.TgBot/Micros.TgBot.csproj

COPY src/Micros.Core/ src/Micros.Core/
COPY src/Micros.TgBot/ src/Micros.TgBot/

RUN dotnet publish src/Micros.TgBot/Micros.TgBot.csproj \
    -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false 

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Micros.TgBot.dll"]

