# escape=`
# ---------------------------------------------------------------------------
# eShopLegacyMVC (ASP.NET MVC 5, .NET Framework 4.7.2) — self-contained image.
# Build context is the solution root so NuGet restores into .\packages, which
# is where the project's <HintPath> references (..\..\packages) resolve.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/framework/sdk:4.7.2 AS build
WORKDIR C:\src
COPY . .
RUN nuget restore eShopLegacyMVC.sln
RUN msbuild src\eShopLegacyMVC\eShopLegacyMVC.csproj `
    /p:Configuration=Release `
    /p:DeployOnBuild=true `
    /p:WebPublishMethod=FileSystem `
    /p:DeleteExistingFiles=True `
    /p:publishUrl=C:\publish

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.7.2 AS runtime
WORKDIR /inetpub/wwwroot
COPY --from=build C:\publish .\
EXPOSE 80

# Liveness probe against the /health endpoint exposed by the app.
HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=3 `
    CMD powershell -NoProfile -Command `
        "try { if ((Invoke-WebRequest -UseBasicParsing http://localhost/health).StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"
