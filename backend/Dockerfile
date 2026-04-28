# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore (layer caching)
COPY src/SmreaderAPI.Domain/SmreaderAPI.Domain.csproj src/SmreaderAPI.Domain/
COPY src/SmreaderAPI.Application/SmreaderAPI.Application.csproj src/SmreaderAPI.Application/
COPY src/SmreaderAPI.Infrastructure/SmreaderAPI.Infrastructure.csproj src/SmreaderAPI.Infrastructure/
COPY src/SmreaderAPI.API/SmreaderAPI.API.csproj src/SmreaderAPI.API/
RUN dotnet restore src/SmreaderAPI.API/SmreaderAPI.API.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/SmreaderAPI.API/SmreaderAPI.API.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appuser /app
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SmreaderAPI.API.dll"]
