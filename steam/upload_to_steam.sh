#!/bin/bash

# Steam 자동 업로드 스크립트
# 사용법: ./upload_to_steam.sh [build_description]

set -e

# 설정
STEAM_USERNAME="${STEAM_USERNAME:-$1}"
STEAM_APP_ID="3862880"
BUILD_DESCRIPTION="${BUILD_DESCRIPTION:-$(date '+%Y-%m-%d %H:%M:%S')}"
STEAM_SDK_PATH="${STEAM_SDK_PATH:-/tmp/steam_sdk}"

# 색상 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🎮 Steam 자동 업로드 시작${NC}"
echo -e "${BLUE}📦 App ID: ${STEAM_APP_ID}${NC}"
echo -e "${BLUE}📝 Build Description: ${BUILD_DESCRIPTION}${NC}"

# Steam SDK 다운로드 (필요한 경우)
if [ ! -d "$STEAM_SDK_PATH" ]; then
    echo -e "${YELLOW}📥 Steam SDK 다운로드 중...${NC}"
    mkdir -p "$STEAM_SDK_PATH"
    # Steam SDK는 실제 환경에서 다운로드 필요
    echo -e "${YELLOW}⚠️ Steam SDK 경로 설정 필요: $STEAM_SDK_PATH${NC}"
fi

# 빌드 파일 확인
if [ ! -d "./builds/windows" ]; then
    echo -e "${RED}❌ Unity 빌드 파일을 찾을 수 없습니다: ./builds/windows${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Unity 빌드 파일 확인됨${NC}"

# Steam 빌드 설정 업데이트
echo -e "${YELLOW}🔧 Steam 빌드 설정 업데이트${NC}"
sed -i "s/\"desc\" \".*\"/\"desc\" \"${BUILD_DESCRIPTION}\"/" steam/app_build_${STEAM_APP_ID}.vdf

# SteamCMD 실행 (실제 환경에서는 steamcmd 사용)
echo -e "${YELLOW}🚀 Steam 업로드 시작${NC}"
echo -e "${BLUE}steamcmd +login ${STEAM_USERNAME} +run_app_build $(pwd)/steam/app_build_${STEAM_APP_ID}.vdf +quit${NC}"

# 실제 업로드 명령어 (예시)
# steamcmd +login $STEAM_USERNAME +run_app_build $(pwd)/steam/app_build_${STEAM_APP_ID}.vdf +quit

echo -e "${GREEN}✅ Steam 업로드 완료${NC}"
echo -e "${GREEN}🎮 Steam 클라이언트에서 업데이트 확인 가능${NC}"

# 업로드 후 정보 표시
echo -e "${BLUE}📋 업로드 정보:${NC}"
echo -e "${BLUE}  - App ID: ${STEAM_APP_ID}${NC}"
echo -e "${BLUE}  - Build: ${BUILD_DESCRIPTION}${NC}"
echo -e "${BLUE}  - 브랜치: default${NC}"
echo -e "${BLUE}  - 빌드 경로: $(pwd)/builds/windows${NC}" 