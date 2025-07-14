# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# Copy published app
COPY --from=build /app/out .

# Expose port
EXPOSE $PORT

# Run the application
ENTRYPOINT ["dotnet", "Server.dll"] 