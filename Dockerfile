# Just Climb Server - 간단한 Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Server 폴더만 복사
COPY Server/Server/ ./

# 복원, 빌드, 퍼블리시
RUN dotnet restore *.csproj
RUN dotnet publish *.csproj -c Release -o out

# 런타임 이미지
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# curl 설치
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 앱 복사
COPY --from=build /app/out .

# 포트 설정
EXPOSE $PORT

# 헬스체크
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:$PORT/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "Server.dll"] 