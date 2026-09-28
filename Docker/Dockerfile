# syntax=docker/dockerfile:1

####################################
# Cratis Identity
# Build: docker build -f Docker/Dockerfile -t cratis/identity .
####################################

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig .globalconfig ./
COPY Source/Identity/Identity.csproj Source/Identity/
RUN dotnet restore Source/Identity/Identity.csproj

COPY Source/Identity/ Source/Identity/
RUN dotnet publish Source/Identity/Identity.csproj -c Release -o /app --no-restore

# Chiseled: no shell or package manager, runs as a non-root user by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "/app/Cratis.Identity.dll"]
