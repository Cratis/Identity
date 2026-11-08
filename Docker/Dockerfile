# syntax=docker/dockerfile:1

####################################
# Cratis Identity
# Build: docker build -f Docker/Dockerfile -t cratis/identity .
####################################

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Keep the reference host as the default for local compose users.
ARG HOST=Identity.Host

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig .globalconfig ./
COPY Source/Identity/Identity.csproj Source/Identity/
COPY Source/Identity.Contracts/Identity.Contracts.csproj Source/Identity.Contracts/
COPY Source/Identity.Host/Identity.Host.csproj Source/Identity.Host/
COPY Source/Identity.CratisId/Identity.CratisId.csproj Source/Identity.CratisId/
RUN dotnet restore Source/${HOST}/${HOST}.csproj

COPY Source/Identity/ Source/Identity/
COPY Source/Identity.Contracts/ Source/Identity.Contracts/
COPY Source/Identity.Host/ Source/Identity.Host/
COPY Source/Identity.CratisId/ Source/Identity.CratisId/
RUN dotnet publish Source/${HOST}/${HOST}.csproj -c Release -o /app --no-restore -p:CratisProxiesOutputPath=

# Chiseled: no shell or package manager, runs as a non-root user by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM runtime AS cratis-id
ENTRYPOINT ["dotnet", "/app/Cratis.Id.Host.dll"]

# Last target remains the reference host so docker-compose.yml is unchanged.
FROM runtime AS reference
ENTRYPOINT ["dotnet", "/app/Cratis.Identity.Host.dll"]
