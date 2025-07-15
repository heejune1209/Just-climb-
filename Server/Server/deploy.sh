#!/bin/bash

# Just Climb Server 배포 스크립트
# 사용법: ./deploy.sh [IMAGE_TAG]

set -e

# 설정
REGISTRY="ghcr.io"
IMAGE_NAME="your-github-username/just_climb"
CONTAINER_NAME="just-climb-server"
APP_PORT="5000"
IMAGE_TAG="${1:-latest}"

# 색상 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Just Climb Server 배포 시작${NC}"
echo -e "${BLUE}📦 이미지: ${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}${NC}"

# 환경 변수 체크
echo -e "${YELLOW}🔍 환경 변수 확인${NC}"
required_vars=("DB_HOST" "DB_PORT" "DB_NAME" "DB_USER" "DB_PASSWORD" "REDIS_HOST" "REDIS_PORT" "JWT_SECRET_KEY" "STEAM_API_KEY")

for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo -e "${RED}❌ 환경 변수 $var 가 설정되지 않았습니다${NC}"
        exit 1
    fi
done

echo -e "${GREEN}✅ 모든 환경 변수가 설정되었습니다${NC}"

# 기존 컨테이너 중지 및 제거
echo -e "${YELLOW}🛑 기존 컨테이너 중지 및 제거${NC}"
if [ $(docker ps -q -f name=${CONTAINER_NAME}) ]; then
    docker stop ${CONTAINER_NAME}
    echo "기존 컨테이너 중지됨"
fi

if [ $(docker ps -aq -f name=${CONTAINER_NAME}) ]; then
    docker rm ${CONTAINER_NAME}
    echo "기존 컨테이너 제거됨"
fi

# Docker 이미지 pull
echo -e "${YELLOW}📥 Docker 이미지 다운로드${NC}"
docker pull ${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}

# 새 컨테이너 실행
echo -e "${YELLOW}🚀 새 컨테이너 실행${NC}"
docker run -d \
    --name ${CONTAINER_NAME} \
    --restart unless-stopped \
    -p ${APP_PORT}:5000 \
    -e ASPNETCORE_ENVIRONMENT=AWS \
    -e ASPNETCORE_URLS=http://+:5000 \
    -e DB_HOST=${DB_HOST} \
    -e DB_PORT=${DB_PORT} \
    -e DB_NAME=${DB_NAME} \
    -e DB_USER=${DB_USER} \
    -e DB_PASSWORD=${DB_PASSWORD} \
    -e REDIS_HOST=${REDIS_HOST} \
    -e REDIS_PORT=${REDIS_PORT} \
    -e JWT_SECRET_KEY=${JWT_SECRET_KEY} \
    -e STEAM_API_KEY=${STEAM_API_KEY} \
    ${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}

# 컨테이너 시작 대기
echo -e "${YELLOW}⏳ 컨테이너 시작 대기 (30초)${NC}"
sleep 30

# 헬스 체크
echo -e "${YELLOW}🏥 헬스 체크${NC}"
for i in {1..10}; do
    if curl -f http://localhost:${APP_PORT}/api/v1/health > /dev/null 2>&1; then
        echo -e "${GREEN}✅ 헬스 체크 성공${NC}"
        break
    else
        echo -e "${YELLOW}❌ 헬스 체크 실패, 재시도 중... ($i/10)${NC}"
        sleep 10
    fi
    
    if [ $i -eq 10 ]; then
        echo -e "${RED}❌ 헬스 체크 실패${NC}"
        echo -e "${RED}컨테이너 로그:${NC}"
        docker logs ${CONTAINER_NAME} --tail=50
        exit 1
    fi
done

# 데이터베이스 마이그레이션
echo -e "${YELLOW}🗄️ 데이터베이스 마이그레이션${NC}"
docker exec ${CONTAINER_NAME} \
    dotnet ef database update \
    --project /app \
    --environment AWS \
    --verbose || echo -e "${YELLOW}⚠️ 마이그레이션 실패 또는 이미 최신 상태${NC}"

# 배포 완료
echo -e "${GREEN}✅ Just Climb Server 배포 완료!${NC}"
echo -e "${GREEN}🌐 애플리케이션 URL: http://localhost:${APP_PORT}${NC}"
echo -e "${GREEN}🏥 헬스 체크: http://localhost:${APP_PORT}/api/v1/health${NC}"
echo -e "${GREEN}📊 컨테이너 상태: docker ps -f name=${CONTAINER_NAME}${NC}"
echo -e "${GREEN}📜 컨테이너 로그: docker logs -f ${CONTAINER_NAME}${NC}" 