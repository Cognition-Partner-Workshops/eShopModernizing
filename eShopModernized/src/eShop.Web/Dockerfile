# Catalog MVC UI. Build context is eShopModernized/ (the solution root), because central package
# management and the analyzer rule set live there:
#   docker build -f src/eShop.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore layer: only the files that affect package resolution, so a source-only change does not
# invalidate the restore cache. Every library csproj is copied, so the restore keeps working as
# eShop.Web takes on more project references.
COPY Directory.Build.props Directory.Packages.props BannedSymbols.txt ./
COPY src/eShop.Catalog.Domain/eShop.Catalog.Domain.csproj src/eShop.Catalog.Domain/
COPY src/eShop.Shared/eShop.Shared.csproj src/eShop.Shared/
COPY src/eShop.Catalog.Data/eShop.Catalog.Data.csproj src/eShop.Catalog.Data/
COPY src/eShop.Web/eShop.Web.csproj src/eShop.Web/
RUN dotnet restore src/eShop.Web/eShop.Web.csproj

COPY src/ src/
RUN dotnet publish src/eShop.Web/eShop.Web.csproj \
    -c "$BUILD_CONFIGURATION" --no-restore -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
# curl backs the HEALTHCHECK below; the SDK is deliberately absent from this stage.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish ./

# Pics/ holds the catalog item pictures the UI serves; logs/ is the Serilog file sink.
RUN mkdir -p /app/Pics /app/logs && chown -R app:app /app/Pics /app/logs

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

USER app
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=10 \
    CMD curl --fail --silent --show-error http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "eShop.Web.dll"]
