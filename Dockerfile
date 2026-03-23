# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and csproj files first for better layer caching
COPY ProductApi.sln .
COPY ProductApi/ProductApi.csproj ProductApi/
COPY ProductApi.Tests/ProductApi.Tests.csproj ProductApi.Tests/
RUN dotnet restore

# Copy the rest of the code
COPY ProductApi/ ProductApi/
WORKDIR /src/ProductApi
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN useradd --create-home --shell /bin/bash appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ProductApi.dll"]
