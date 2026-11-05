# syntax=docker/dockerfile:1

####################################
# Cratis Identity
# Build: docker build -f Docker/Dockerfile -t cratis/identity .
####################################

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig .globalconfig ./
COPY Source/Identity/Identity.csproj Source/Identity/
COPY Source/Identity.Contracts/Identity.Contracts.csproj Source/Identity.Contracts/
COPY Source/Identity.Host/Identity.Host.csproj Source/Identity.Host/
RUN dotnet restore Source/Identity.Host/Identity.Host.csproj

COPY Source/Identity/ Source/Identity/
COPY Source/Identity.Contracts/ Source/Identity.Contracts/
COPY Source/Identity.Host/ Source/Identity.Host/
RUN dotnet publish Source/Identity.Host/Identity.Host.csproj -c Release -o /app --no-restore -p:CratisProxiesOutputPath=

# Chiseled: no shell or package manager, runs as a non-root user by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "/app/Cratis.Identity.Host.dll"]
