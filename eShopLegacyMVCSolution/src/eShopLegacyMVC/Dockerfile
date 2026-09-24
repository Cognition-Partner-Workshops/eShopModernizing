# syntax=docker/dockerfile:1
# Build context: eShopLegacyMVCSolution/
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY eShopLegacy.Utilities/eShopLegacy.Utilities.csproj eShopLegacy.Utilities/
COPY src/eShopLegacyMVC/eShopLegacyMVC.csproj src/eShopLegacyMVC/
RUN dotnet restore src/eShopLegacyMVC/eShopLegacyMVC.csproj
COPY eShopLegacy.Utilities/ eShopLegacy.Utilities/
COPY src/eShopLegacyMVC/ src/eShopLegacyMVC/
RUN dotnet publish src/eShopLegacyMVC/eShopLegacyMVC.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# log4net writes to ./logFiles; make it writable for the non-root user
RUN mkdir -p /app/logFiles && chown -R app:app /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseMockData=true
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "eShopLegacyMVC.dll"]
