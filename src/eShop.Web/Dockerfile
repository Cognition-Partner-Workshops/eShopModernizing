# Catalog MVC front end (NET-69). Build context is the repository root:
#   docker build -f src/eShop.Web/Dockerfile -t eshop-web .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source

# Restore layer: only the files that determine the package graph, so editing source code does not
# invalidate the (slow) restore.
COPY global.json Directory.Build.props Directory.Packages.props BannedSymbols.txt ./
COPY src/eShop.Web/eShop.Web.csproj src/eShop.Web/
COPY src/eShop.Catalog.Data/eShop.Catalog.Data.csproj src/eShop.Catalog.Data/
COPY src/eShop.Catalog.Domain/eShop.Catalog.Domain.csproj src/eShop.Catalog.Domain/
COPY src/eShop.Shared/eShop.Shared.csproj src/eShop.Shared/
RUN dotnet restore src/eShop.Web/eShop.Web.csproj

COPY src/eShop.Web/ src/eShop.Web/
COPY src/eShop.Catalog.Data/ src/eShop.Catalog.Data/
COPY src/eShop.Catalog.Domain/ src/eShop.Catalog.Domain/
COPY src/eShop.Shared/ src/eShop.Shared/
RUN dotnet publish src/eShop.Web/eShop.Web.csproj \
    --configuration $BUILD_CONFIGURATION --no-restore --output /app


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
# curl is the HEALTHCHECK client; it is the only addition to the runtime image.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# $APP_UID is the non-root user (uid 1654) shipped in the .NET 8 runtime images.
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl --fail --silent --show-error http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "eShop.Web.dll"]
