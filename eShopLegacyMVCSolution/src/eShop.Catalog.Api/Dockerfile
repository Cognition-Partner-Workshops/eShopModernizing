# syntax=docker/dockerfile:1
# Build context: eShopLegacyMVCSolution/
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/eShop.Catalog.Api/eShop.Catalog.Api.csproj src/eShop.Catalog.Api/
RUN dotnet restore src/eShop.Catalog.Api/eShop.Catalog.Api.csproj
COPY src/eShop.Catalog.Api/ src/eShop.Catalog.Api/
RUN dotnet publish src/eShop.Catalog.Api/eShop.Catalog.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseMockData=true
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "eShop.Catalog.Api.dll"]
