# escape=`
# ---------------------------------------------------------------------------
# eShopWCFService (WCF Service, .NET Framework 4.6.1) — self-contained image.
# Build context is the solution root. The project's <HintPath> references point
# to ..\packages (i.e. src\packages), so NuGet is restored there explicitly.
# Only the WCF service is containerized; eShopWinForms is a desktop client.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/framework/sdk:4.7.2 AS build
WORKDIR C:\src
COPY . .
RUN nuget restore eShopLegacyNTier.sln -PackagesDirectory src\packages
RUN msbuild src\eShopWCFService\eShopWCFService.csproj `
    /p:Configuration=Release `
    /p:DeployOnBuild=true `
    /p:WebPublishMethod=FileSystem `
    /p:DeleteExistingFiles=True `
    /p:publishUrl=C:\publish

FROM mcr.microsoft.com/dotnet/framework/wcf:4.7.2 AS runtime
WORKDIR /inetpub/wwwroot
COPY --from=build C:\publish .\
EXPOSE 80

# Liveness probe against the /health endpoint exposed by the service.
HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=3 `
    CMD powershell -NoProfile -Command `
        "try { if ((Invoke-WebRequest -UseBasicParsing http://localhost/health).StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
