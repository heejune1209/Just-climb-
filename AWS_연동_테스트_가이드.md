# AWS 연동 테스트 가이드

## 1. AWS CloudShell 접속

### CloudShell 실행
```
AWS Console → 상단 CloudShell 아이콘 클릭
→ 브라우저에서 Linux 터미널 실행
```

## 2. EC2 연결 테스트

### SSH 연결
```bash
# EC2 키페어 업로드 (CloudShell에서)
# 1. 로컬 .pem 파일을 CloudShell에 업로드
# 2. 권한 설정
chmod 400 keypair.pem

# EC2 SSH 연결
ssh -i keypair.pem ec2-user@13.125.227.110
```

### EC2 상태 확인
```bash
# EC2 내부에서 실행
# 시스템 정보 확인
uname -a
cat /etc/os-release

# 네트워크 확인
ping google.com
```

## 3. RDS 연결 테스트

### PostgreSQL 클라이언트 설치
```bash
# CloudShell에서 실행
sudo dnf install postgresql15 -y
```

### RDS 연결
```bash
# PostgreSQL 연결 테스트
psql -h just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com \
     -U heejune1209 \
     -d postgres

# 연결 성공 시 실행할 명령어
\l                    # 데이터베이스 목록
\dt                   # 테이블 목록
SELECT version();     # PostgreSQL 버전 확인
```

## 4. Redis 연결 테스트

### Redis CLI 설치
```bash
# CloudShell에서 실행
sudo dnf install redis -y
```

### Redis 연결
```bash
# Redis 연결 테스트
redis-cli -h just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com -p 6379 ping

# 기본 Redis 명령어 테스트
redis-cli -h just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com -p 6379
> SET test "Hello Just Climb"
> GET test
> DEL test
> EXIT
```

## 5. EC2에서 RDS/Redis 연결 테스트

### EC2에 접속 후 테스트
```bash
# EC2 SSH 연결
ssh -i keypair.pem ec2-user@13.125.227.110

# PostgreSQL 클라이언트 설치
sudo dnf install postgresql15 -y

# Redis 클라이언트 설치
sudo dnf install redis6 -y

# RDS 연결 테스트
psql -h just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com \
     -U heejune1209 \
     -d postgres

# Redis 연결 테스트
redis6-cli -h just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com -p 6379 ping
```

## 6. 네트워크 연결 진단

### 포트 연결 확인
```bash
# EC2에서 실행
# RDS 포트 확인
nc -zv just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com 5432

# Redis 포트 확인
nc -zv just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com 6379
```

### DNS 해상도 확인
```bash
# DNS 확인
nslookup just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com
nslookup just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com
```

## 7. 연결 문제 해결

### 보안 그룹 확인
```
AWS Console → EC2 → 보안 그룹
1. EC2 보안 그룹 아웃바운드 규칙 확인
2. RDS 보안 그룹 인바운드 규칙 확인
3. Redis 보안 그룹 인바운드 규칙 확인
```

### VPC 라우팅 확인
```
AWS Console → VPC → 라우팅 테이블
1. 퍼블릭 서브넷 라우팅 확인
2. 프라이빗 서브넷 라우팅 확인
3. 인터넷 게이트웨이 연결 확인
```

## 8. 성공 기준

### 연결 성공 시 예상 결과
```bash
# PostgreSQL 연결 성공
psql (15.x)
Type "help" for help.
postgres=>

# Redis 연결 성공
PONG

# 네트워크 연결 성공
Connection to [HOST] [PORT] port [tcp/*] succeeded!
```

## 9. 자주 발생하는 문제

### 1. 연결 타임아웃
```
원인: 보안 그룹 설정 오류
해결: 인바운드 규칙에서 올바른 포트와 소스 확인
```

### 2. 인증 실패
```
원인: 사용자명/암호 오류
해결: RDS 마스터 사용자 정보 재확인
```

### 3. DNS 해상도 실패
```
원인: VPC DNS 설정 문제
해결: VPC에서 DNS 호스트명 활성화 확인
```

## 10. 모니터링 및 로그

### CloudWatch 로그 확인
```
AWS Console → CloudWatch → 로그 그룹
- EC2 인스턴스 로그
- RDS 로그
- ElastiCache 로그
```

### 성능 모니터링
```
AWS Console → CloudWatch → 대시보드
- CPU 사용률
- 메모리 사용률
- 네트워크 트래픽
- 데이터베이스 연결 수
``` 