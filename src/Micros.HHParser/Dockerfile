FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Micros.Core/Micros.Core.csproj src/Micros.Core/
COPY src/Micros.HHParser/Micros.HHParser.csproj src/Micros.HHParser/

RUN dotnet restore src/Micros.HHParser/Micros.HHParser.csproj

COPY src/Micros.Core/ src/Micros.Core/
COPY src/Micros.HHParser/ src/Micros.HHParser/

RUN dotnet publish src/Micros.HHParser/Micros.HHParser.csproj \
    -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false 

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Micros.HHParser.dll"]

