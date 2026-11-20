# syntax=docker/dockerfile:1
# Builds, tests and packages ChefBot. Nothing needs to be installed on the host except Podman (or Docker).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first so this layer is reused until a project file changes.
COPY Directory.Build.props Directory.Packages.props ChefBot.slnx global.json ./
COPY src/ChefBot.Core/ChefBot.Core.csproj src/ChefBot.Core/
COPY src/ChefBot.Infrastructure/ChefBot.Infrastructure.csproj src/ChefBot.Infrastructure/
COPY src/ChefBot.Api/ChefBot.Api.csproj src/ChefBot.Api/
COPY tests/ChefBot.Core.Tests/ChefBot.Core.Tests.csproj tests/ChefBot.Core.Tests/
RUN dotnet restore ChefBot.slnx

COPY . .
RUN dotnet build ChefBot.slnx --configuration Release --no-restore

# podman build --target test .
FROM build AS test
RUN dotnet run --project tests/ChefBot.Core.Tests --configuration Release --no-build

FROM build AS publish
RUN dotnet publish src/ChefBot.Api/ChefBot.Api.csproj --configuration Release --no-build --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app ./
# Session keys, the HTTPS certificate and backups live in these folders. They are created with the
# app's user so mounted volumes are writable.
RUN mkdir -p /app/keys /app/backups && chown $APP_UID /app/keys /app/backups
USER $APP_UID
# ChefBot sets its own ports (Https:HttpPort and Https:Port), so the image's default is cleared.
ENV ASPNETCORE_HTTP_PORTS=
EXPOSE 8080 8443
ENTRYPOINT ["dotnet", "ChefBot.Api.dll"]
