# syntax=docker/dockerfile:1
# Builds, tests and packages ChefBot. Nothing needs to be installed on the host except Podman (or Docker).

# The web page (src/ChefBot.Web, Svelte) is built here into dist/. Only this stage needs Node, and nothing but the
# final image uses it, so the page and the .NET build run side by side.
FROM docker.io/library/node:24-alpine AS web
WORKDIR /source/src/ChefBot.Web
COPY src/ChefBot.Web/package.json src/ChefBot.Web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/ChefBot.Web/ ./
RUN npm run build

# The documentation (docs/, MkDocs Material) is built here for /help, so it is there on a home network with no
# internet. SITE_URL is empty so every address in it stays relative to /help; the privacy plugin downloads the few
# outside assets (Mermaid) now, so the pages make no outside requests later.
FROM docker.io/library/python:3.13-slim AS docs
WORKDIR /source
COPY docs/requirements.txt docs/
RUN pip install --no-cache-dir -r docs/requirements.txt
COPY mkdocs.yml ./
COPY docs/ docs/
RUN SITE_URL= mkdocs build --strict --site-dir /site

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first so this layer is reused until a project file changes. Debug's packages are fetched too, for the
# code generation step below; Release is restored last because a build uses whichever restore ran last.
COPY Directory.Build.props Directory.Packages.props ChefBot.slnx global.json ./
COPY src/ChefBot.Core/ChefBot.Core.csproj src/ChefBot.Core/
COPY src/ChefBot.Api/ChefBot.Api.csproj src/ChefBot.Api/
COPY tests/ChefBot.Core.Tests/ChefBot.Core.Tests.csproj tests/ChefBot.Core.Tests/
RUN dotnet restore ChefBot.slnx -p:Configuration=Debug && dotnet restore ChefBot.slnx -p:Configuration=Release

COPY . .
# Wolverine's code for calling the endpoints and handlers is generated from this source and compiled into the Release
# build, which never compiles code while running and ships without Roslyn (see CONTRIBUTING.md). This starts a Debug
# build only far enough to write the code; it doesn't connect to the database.
RUN rm -rf src/ChefBot.Api/Internal/Generated \
    && dotnet run --project src/ChefBot.Api -- codegen write \
    && dotnet restore ChefBot.slnx -p:Configuration=Release
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
      org.opencontainers.image.description="Self-hosted meal planner: weekly meal plans, a smart shopping list and a pantry tracker that helps you waste less food." \
      org.opencontainers.image.documentation="https://jamiethenextkevinbaconhamm.github.io/chef-bot/" \
      org.opencontainers.image.url="https://github.com/Jamiethenextkevinbaconhamm/chef-bot" \
      org.opencontainers.image.licenses="GPL-3.0-only"
WORKDIR /app
COPY --from=publish /app ./
# The app serves the page from wwwroot next to it (WebPage.Folder).
COPY --from=web /source/src/ChefBot.Web/dist/ ./wwwroot/
# The documentation, served at /help without sign-in (WebPage.MapChefBotWebPage).
COPY --from=docs /site/ ./wwwroot/help/
# Session keys, the HTTPS certificate and backups live in these folders. They are created with the
# app's user so mounted volumes are writable. Declared as volumes so they survive even when nobody mounts
# them; give them named volumes (docs/getting-started/install-docker-compose.md) so they also survive replacing the
# container.
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
