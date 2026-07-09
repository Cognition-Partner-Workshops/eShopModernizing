# Multi-stage build for the Catalog gRPC service (ASP.NET Core 8).
# Build context is the repository root so sibling projects are available.

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore against only the projects this app needs (better layer caching).
COPY modernized/src/Catalog.Service/Catalog.Service.csproj modernized/src/Catalog.Service/
COPY modernized/src/Catalog.Domain/Catalog.Domain.csproj modernized/src/Catalog.Domain/
COPY modernized/src/Catalog.Infrastructure/Catalog.Infrastructure.csproj modernized/src/Catalog.Infrastructure/
COPY modernized/src/Catalog.Shared/Catalog.Shared.csproj modernized/src/Catalog.Shared/
RUN dotnet restore modernized/src/Catalog.Service/Catalog.Service.csproj

# Copy the remaining sources and publish a release build.
COPY modernized/src/Catalog.Service/ modernized/src/Catalog.Service/
COPY modernized/src/Catalog.Domain/ modernized/src/Catalog.Domain/
COPY modernized/src/Catalog.Infrastructure/ modernized/src/Catalog.Infrastructure/
COPY modernized/src/Catalog.Shared/ modernized/src/Catalog.Shared/
RUN dotnet publish modernized/src/Catalog.Service/Catalog.Service.csproj -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is required by the container HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# gRPC requires HTTP/2. Kestrel's default cleartext endpoint negotiates both
# HTTP/1.1 (used by the curl HEALTHCHECK against /health) and HTTP/2 (used by
# gRPC clients via prior knowledge) on this single port.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Catalog.Service.dll"]
