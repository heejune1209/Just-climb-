#!/bin/bash

# AWS 환경에서 서버 실행
export ASPNETCORE_ENVIRONMENT=AWS

echo "Starting Just Climb Server in AWS environment..."
echo "PostgreSQL: justclimb-postgres.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com:5432"
echo "Redis: justclimb-redis-kp9dum.serverless.apn2.cache.amazonaws.com:6379"

# 패키지 복원
dotnet restore

# 데이터베이스 마이그레이션
dotnet ef database update --environment AWS

# 서버 실행
dotnet run --environment AWS 