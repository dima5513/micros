FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Micros.Core/Micros.Core.csproj src/Micros.Core/
COPY src/Micros.Api/Micros.Api.csproj src/Micros.Api/

RUN dotnet restore src/Micros.Api/Micros.Api.csproj

COPY src/Micros.Core/ src/Micros.Core/
COPY src/Micros.Api/ src/Micros.Api/

RUN dotnet publish src/Micros.Api/Micros.Api.csproj \
    -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false 

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV HTTP_PORTS=8445
USER $APP_UID
EXPOSE 8445
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Micros.Api.dll"]

