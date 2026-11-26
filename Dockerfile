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

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
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

# PostgreSQL, for the database ChefBot runs itself (src/ChefBot.Api/Database). It comes from PostgreSQL's own apt
# repository, because the base image is Ubuntu 24.04, which only has 16; its key is checked against this checksum.
# The package is installed here and only the server is copied into the image: its dependencies (LLVM for JIT, Perl
# and systemd for the cluster tools) would double the image's size, and ChefBot uses none of them.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS postgres
ARG POSTGRES_MAJOR=17
ADD --checksum=sha256:0144068502a1eddd2a0280ede10ef607d1ec592ce819940991203941564e8e76 \
    https://www.postgresql.org/media/keys/ACCC4CF8.asc /usr/share/keyrings/postgresql.asc
RUN chmod 644 /usr/share/keyrings/postgresql.asc \
    && . /etc/os-release \
    && echo "deb [signed-by=/usr/share/keyrings/postgresql.asc] https://apt.postgresql.org/pub/repos/apt ${VERSION_CODENAME}-pgdg main" \
        > /etc/apt/sources.list.d/pgdg.list \
    && mkdir -p /etc/postgresql-common/createcluster.d \
    && echo "create_main_cluster = false" > /etc/postgresql-common/createcluster.d/chefbot.conf \
    && apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends "postgresql-${POSTGRES_MAJOR}" \
    && rm -rf "/usr/lib/postgresql/${POSTGRES_MAJOR}/lib/bitcode" "/usr/lib/postgresql/${POSTGRES_MAJOR}/lib/llvmjit"* \
    && mkdir "/usr/lib/postgresql/${POSTGRES_MAJOR}/libpq" \
    && cp -P /usr/lib/*-linux-gnu/libpq.so.5* "/usr/lib/postgresql/${POSTGRES_MAJOR}/libpq/"

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Links the published image to its repository on the GitHub Container Registry.
LABEL org.opencontainers.image.source="https://github.com/FreeReins/chef-bot" \
      org.opencontainers.image.title="ChefBot" \
      org.opencontainers.image.description="Self-hosted meal planner: weekly meal plans, a smart shopping list and a pantry tracker that helps you waste less food." \
      org.opencontainers.image.documentation="https://freereins.github.io/chef-bot/" \
      org.opencontainers.image.url="https://github.com/FreeReins/chef-bot" \
      org.opencontainers.image.licenses="GPL-3.0-only"

# The bundled PostgreSQL, at the paths it was built for, and the libraries it needs that the base image doesn't
# have (readline is for psql, for looking at the database by hand). tini is process 1, so it passes on the stop
# signal to ChefBot, which stops PostgreSQL cleanly, and reaps anything left behind. No database is made here:
# ChefBot creates its own in /data on first start.
ARG POSTGRES_MAJOR=17
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        tini libxml2 libxslt1.1 libldap2 libgssapi-krb5-2 libpam0g libsystemd0 libselinux1 libedit2 \
        liblz4-1 libzstd1 libuuid1 libreadline8t64 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=postgres /usr/lib/postgresql/${POSTGRES_MAJOR}/ /usr/lib/postgresql/${POSTGRES_MAJOR}/
COPY --from=postgres /usr/share/postgresql/${POSTGRES_MAJOR}/ /usr/share/postgresql/${POSTGRES_MAJOR}/
# Its own libpq, which its tools need (Ubuntu's is older).
RUN echo "/usr/lib/postgresql/${POSTGRES_MAJOR}/libpq" > /etc/ld.so.conf.d/postgresql.conf && ldconfig

WORKDIR /app
COPY --from=publish /app ./
# The app serves the page from wwwroot next to it (WebPage.Folder).
COPY --from=web /source/src/ChefBot.Web/dist/ ./wwwroot/
# The documentation, served at /help without sign-in (WebPage.MapChefBotWebPage).
COPY --from=docs /site/ ./wwwroot/help/
# Everything ChefBot keeps lives in /data: the database, sign-in keys, the HTTPS certificate, backups and an
# optional settings.json. It is created with the app's user so a mounted volume is writable, and declared as a
# volume so it survives even when nobody mounts one; give it a named volume so it also survives replacing the
# container.
RUN mkdir -p /data && chown $APP_UID /data
VOLUME ["/data"]
USER $APP_UID
ENV Data__Root=/data \
    Database__Binaries=/usr/lib/postgresql/${POSTGRES_MAJOR}/bin
# ChefBot sets its own ports (Https:HttpPort and Https:Port), so the image's default is cleared.
ENV ASPNETCORE_HTTP_PORTS=
# Secure out of the box: ChefBot makes its own certificate. Behind a reverse proxy that handles HTTPS, set
# Https__Mode=Off; to use your own certificate, set Https__Mode=Certificate.
ENV Https__Mode=SelfSigned
EXPOSE 8080 8443
ENTRYPOINT ["tini", "--", "dotnet", "ChefBot.Api.dll"]
