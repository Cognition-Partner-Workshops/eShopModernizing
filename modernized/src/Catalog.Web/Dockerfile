# Multi-stage build for the Catalog Razor Pages web app (ASP.NET Core 8).
# Build context is the repository root so sibling projects are available.

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore against only the projects this app needs (better layer caching).
COPY modernized/src/Catalog.Web/Catalog.Web.csproj modernized/src/Catalog.Web/
COPY modernized/src/Catalog.Domain/Catalog.Domain.csproj modernized/src/Catalog.Domain/
COPY modernized/src/Catalog.Infrastructure/Catalog.Infrastructure.csproj modernized/src/Catalog.Infrastructure/
COPY modernized/src/Catalog.Shared/Catalog.Shared.csproj modernized/src/Catalog.Shared/
RUN dotnet restore modernized/src/Catalog.Web/Catalog.Web.csproj

# Copy the remaining sources and publish a release build.
COPY modernized/src/Catalog.Web/ modernized/src/Catalog.Web/
COPY modernized/src/Catalog.Domain/ modernized/src/Catalog.Domain/
COPY modernized/src/Catalog.Infrastructure/ modernized/src/Catalog.Infrastructure/
COPY modernized/src/Catalog.Shared/ modernized/src/Catalog.Shared/
RUN dotnet publish modernized/src/Catalog.Web/Catalog.Web.csproj -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is required by the container HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Catalog.Web.dll"]
