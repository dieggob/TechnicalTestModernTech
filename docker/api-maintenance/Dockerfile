# Build context: apps/api-maintenance (see docker/docker-compose.yml).
# Stage 1 restores and publishes the API with the .NET 8 SDK; stage 2 runs it on the ASP.NET runtime image.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project or the central package file changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Maintenance.Domain/Maintenance.Domain.csproj src/Maintenance.Domain/
COPY src/Maintenance.Application/Maintenance.Application.csproj src/Maintenance.Application/
COPY src/Maintenance.Infrastructure/Maintenance.Infrastructure.csproj src/Maintenance.Infrastructure/
COPY src/Maintenance.Api/Maintenance.Api.csproj src/Maintenance.Api/
RUN dotnet restore src/Maintenance.Api/Maintenance.Api.csproj

COPY src/ src/
RUN dotnet publish src/Maintenance.Api/Maintenance.Api.csproj -c Release -o /app/publish --no-restore --nologo

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# The SQLite file lives on /data, a named volume in compose, owned by the image's non-root user.
RUN mkdir /data && chown app:app /data
USER app
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/maintenance.db"
EXPOSE 8080

ENTRYPOINT ["dotnet", "Maintenance.Api.dll"]
