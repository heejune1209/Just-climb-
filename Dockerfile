# Just Climb Server 배포용 Dockerfile  
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# 전체 소스 복사
COPY . .

# Server 프로젝트로 이동하여 빌드
WORKDIR "/src/Server/Server"
RUN dotnet restore
RUN dotnet build "Server.csproj" -c Release -o /app/build

# 퍼블리시
FROM build AS publish
WORKDIR "/src/Server/Server"
RUN dotnet publish "Server.csproj" -c Release -o /app/publish

# 런타임
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# curl 설치 (헬스체크용)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Railway 포트 설정
EXPOSE $PORT

# 헬스체크
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:$PORT/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "Server.dll"] 