# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project files
COPY ["Server/Server.csproj", "Server/"]
RUN dotnet restore "Server/Server.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Server"
RUN dotnet build "Server.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Server.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN addgroup --system --gid 1001 justclimb && \
    adduser --system --uid 1001 --ingroup justclimb justclimb

# Copy published app
COPY --from=publish /app/publish .

# Change ownership
RUN chown -R justclimb:justclimb /app
USER justclimb

# Health check (Railway 호환)
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:${PORT:-5000}/api/v1/health || exit 1

# Railway는 동적 포트 할당
EXPOSE ${PORT:-5000}
ENTRYPOINT ["dotnet", "Server.dll"] 