#!/bin/bash

# 데이터베이스 연결 테스트 스크립트
export ASPNETCORE_ENVIRONMENT=AWS

echo "=== Just Climb Database Connection Test ==="
echo "PostgreSQL: justclimb-postgres.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com:5432"
echo "Redis: justclimb-redis-kp9dum.serverless.apn2.cache.amazonaws.com:6379"
echo ""

# PostgreSQL 연결 테스트
echo "1. Testing PostgreSQL connection..."
dotnet run --project . --environment AWS --urls "http://localhost:5000" &
SERVER_PID=$!

# 서버 시작 대기
sleep 10

# Health Check 엔드포인트 테스트
echo "2. Testing Health Check endpoint..."
curl -s "http://localhost:5000/health" || echo "Health check failed"

# 서버 종료
kill $SERVER_PID

echo ""
echo "=== Test completed ==="
echo "다음 단계: appsettings.AWS.json에서 USERNAME/PASSWORD 설정" 