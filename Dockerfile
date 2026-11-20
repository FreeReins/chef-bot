# syntax=docker/dockerfile:1
# Builds, tests and packages ChefBot. Nothing needs to be installed on the host except Podman (or Docker).

# The web page (src/ChefBot.Web, Svelte) is built here into ChefBot.Api's wwwroot. Only this stage needs Node.
FROM docker.io/library/node:24-alpine AS web
WORKDIR /source/src/ChefBot.Web
COPY src/ChefBot.Web/package.json src/ChefBot.Web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/ChefBot.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first so this layer is reused until a project file changes.
COPY Directory.Build.props Directory.Packages.props ChefBot.slnx global.json ./
COPY src/ChefBot.Core/ChefBot.Core.csproj src/ChefBot.Core/
COPY src/ChefBot.Api/ChefBot.Api.csproj src/ChefBot.Api/
COPY tests/ChefBot.Core.Tests/ChefBot.Core.Tests.csproj tests/ChefBot.Core.Tests/
RUN dotnet restore ChefBot.slnx

COPY . .
# Before the build, so the page is part of the app's static files when it is published.
COPY --from=web /source/src/ChefBot.Api/wwwroot/ src/ChefBot.Api/wwwroot/
RUN dotnet build ChefBot.slnx --configuration Release --no-restore

# podman build --target test .
FROM build AS test
RUN dotnet run --project tests/ChefBot.Core.Tests --configuration Release --no-build

FROM build AS publish
RUN dotnet publish src/ChefBot.Api/ChefBot.Api.csproj --configuration Release --no-build --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Links the published image to its repository on the GitHub Container Registry.
LABEL org.opencontainers.image.source="https://github.com/Jamiethenextkevinbaconhamm/chef-bot" \
      org.opencontainers.image.title="ChefBot" \
      org.opencontainers.image.description="Plans a week of meals around shared ingredients, the season and the pantry."
WORKDIR /app
COPY --from=publish /app ./
# Session keys, the HTTPS certificate and backups live in these folders. They are created with the
# app's user so mounted volumes are writable. Declared as volumes so they survive even when nobody mounts
# them; give them named volumes (see the README) so they also survive replacing the container.
RUN mkdir -p /app/keys /app/backups && chown $APP_UID /app/keys /app/backups
VOLUME ["/app/keys", "/app/backups"]
USER $APP_UID
# ChefBot sets its own ports (Https:HttpPort and Https:Port), so the image's default is cleared.
ENV ASPNETCORE_HTTP_PORTS=
# Secure out of the box: ChefBot makes its own certificate. Behind a reverse proxy that handles HTTPS, set
# Https__Mode=Off; to use your own certificate, set Https__Mode=Certificate.
ENV Https__Mode=SelfSigned
EXPOSE 8080 8443
ENTRYPOINT ["dotnet", "ChefBot.Api.dll"]
