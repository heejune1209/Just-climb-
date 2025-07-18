# AWS 서버/DB 배포 가이드 (2024)

## 개요
이 가이드는 Just Climb 게임의 서버와 데이터베이스를 AWS에 배포하는 방법을 설명합니다.

## 사전 준비사항
- AWS 계정 및 결제 정보 등록
- AWS CLI 설치
- .NET 9.0 SDK 설치
- Docker 설치 (선택적)

## 1. AWS 리소스 생성

### 1.1 RDS (PostgreSQL) 생성
1. AWS Console > RDS 서비스 이동
2. "Create database" 클릭
3. 설정값:
   - Engine: PostgreSQL
   - Version: 16.6 (최신)
   - Template: Free tier
   - Instance class: db.t4g.micro
   - Storage: 20GB
   - DB instance identifier: just-climb-db
   - Master username: postgres
   - Master password: [안전한 비밀번호]
   - VPC: Default VPC
   - Security group: 새로 생성 (just-climb-db-sg)
   - Port: 5432
4. "Create database" 클릭

### 1.2 ElastiCache (Redis) 생성
1. AWS Console > ElastiCache 서비스 이동
2. "Create" 클릭
3. 설정값:
   - Engine: Redis
   - Location: AWS Cloud
   - Cluster mode: Disabled
   - Name: just-climb-redis
   - Description: Just Climb Redis Cache
   - Engine version: 7.0 (최신)
   - Port: 6379
   - Parameter group: default.redis7.x
   - Node type: cache.t3.micro (Free tier eligible)
   - Number of replicas: 0
   - Multi-AZ: Disabled (Free tier)
   - Subnet group: Default
   - Security group: 새로 생성 (just-climb-redis-sg)
4. "Create" 클릭

### 1.3 EC2 인스턴스 생성
1. AWS Console > EC2 서비스 이동
2. "Launch instances" 클릭
3. 설정값:
   - Name: just-climb-server
   - AMI: Amazon Linux 2023
   - Instance type: t3.micro (Free tier)
   - Key pair: 새로 생성 또는 기존 키 사용
   - Security group: 새로 생성
     - HTTP (80) - 0.0.0.0/0
     - HTTPS (443) - 0.0.0.0/0
     - SSH (22) - 내 IP
     - Custom TCP (5000) - 0.0.0.0/0
   - Storage: 8GB gp3
4. "Launch instance" 클릭

### 1.4 보안 그룹 설정 (3-Tier 분리)

**⚠️ 중요: 각 서비스별로 별도의 보안 그룹 사용 (보안 모범 사례)**

#### **EC2 보안 그룹** (just-climb-web-sg)
```
인바운드:
- HTTP (80) ← 0.0.0.0/0
- HTTPS (443) ← 0.0.0.0/0  
- SSH (22) ← 내 IP만
- Custom TCP (5000) ← 0.0.0.0/0
```

#### **RDS 보안 그룹** (just-climb-db-sg)
```
인바운드:
- PostgreSQL (5432) ← EC2 보안 그룹 ID만
외부에서 직접 접근 차단!
```

#### **ElastiCache 보안 그룹** (just-climb-redis-sg)
```
인바운드:
- 사용자 지정 TCP (6379) ← EC2 보안 그룹 ID만
외부에서 직접 접근 차단!
```

**보안 원칙**: EC2 → RDS/Redis 접근만 허용, 외부에서 DB/Redis 직접 접근 금지

## 2. 서버 설정

### 2.1 EC2 인스턴스 접속

#### Linux/macOS에서 SSH 접속
```bash
ssh -i your-key.pem ec2-user@[EC2_PUBLIC_IP]
```

#### Windows에서 PuTTY 사용 접속
1. **PuTTY 다운로드 및 설치**
   - https://www.putty.org/ 에서 다운로드
   - PuTTY와 PuTTYgen 모두 설치

2. **키 파일 변환 (PEM → PPK)**
   ```
   1. PuTTYgen 실행
   2. "Load" 클릭하여 .pem 키 파일 선택
   3. "Save private key" 클릭하여 .ppk 파일로 저장
   ```

3. **PuTTY로 접속**
   ```
   1. PuTTY 실행
   2. Host Name: ec2-user@[EC2_PUBLIC_IP]
   3. Port: 22
   4. Connection Type: SSH
   5. SSH > Auth > Private key file에서 .ppk 파일 선택
   6. "Open" 클릭하여 접속
   ```

#### Windows PowerShell/CMD SSH 접속 (Windows 10+)
```bash
# OpenSSH가 설치된 경우
ssh -i your-key.pem ec2-user@[EC2_PUBLIC_IP]
```

### 2.2 필수 소프트웨어 설치
```bash
# 시스템 업데이트
sudo yum update -y

# .NET 9.0 설치
sudo yum install -y dotnet-sdk-9.0

# Git 설치
sudo yum install -y git

# Nginx 설치 (리버스 프록시용)
sudo yum install -y nginx

# Redis CLI 설치 (테스트용)
sudo yum install -y redis6

# PM2 설치 (프로세스 관리용)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
source ~/.bashrc
nvm install node
npm install -g pm2
```

### 2.3 애플리케이션 배포

#### 방법 1: Git 직접 클론 (간단한 방법)
```bash
# 프로젝트 클론
git clone https://github.com/[YOUR_USERNAME]/Just_Climb.git
cd Just_Climb/Server/Server

# 프로덕션 설정 파일 수정
sudo nano appsettings.Production.json
```

#### 방법 2: S3를 통한 배포 (권장 방법)
```bash
# 1. 로컬에서 서버 빌드 및 압축
dotnet publish -c Release -o ./publish
tar -czf server-build.tar.gz -C ./publish .

# 2. S3에 업로드 (AWS CLI 사용)
aws s3 cp server-build.tar.gz s3://just-climb-deployments/

# 3. EC2에서 S3에서 다운로드 및 배포
aws s3 cp s3://just-climb-deployments/server-build.tar.gz ./
tar -xzf server-build.tar.gz -C /var/www/justclimb/
```

#### S3 버킷 생성 (S3 배포 방법 사용시)
```bash
# S3 버킷 생성
aws s3 mb s3://just-climb-deployments

# 버킷 정책 설정 (EC2에서만 접근 가능)
aws s3api put-bucket-policy --bucket just-climb-deployments --policy file://bucket-policy.json
```

appsettings.Production.json 예시:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=[RDS_ENDPOINT];Database=just_climb;Username=postgres;Password=[DB_PASSWORD]",
    "Redis": "[REDIS_ENDPOINT]:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "CorsSettings": {
    "AllowedOrigins": ["https://your-domain.com"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"]
  },
  "Redis": {
    "Configuration": "[REDIS_ENDPOINT]:6379",
    "InstanceName": "JustClimb"
  }
}
```

### 2.4 데이터베이스 마이그레이션
```bash
# 데이터베이스 마이그레이션 실행
dotnet ef database update --environment Production
```

### 2.5 애플리케이션 빌드 및 실행
```bash
# 릴리즈 빌드
dotnet publish -c Release -o /var/www/justclimb

# 실행 권한 설정
sudo chmod +x /var/www/justclimb/Server

# PM2로 프로세스 관리
pm2 start /var/www/justclimb/Server --name "just-climb-server" --env production
pm2 startup
pm2 save
```

### 2.6 Nginx 설정
```bash
sudo nano /etc/nginx/conf.d/justclimb.conf
```

Nginx 설정 예시:
```nginx
server {
    listen 80;
    server_name [your-domain.com];
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

```bash
# Nginx 시작
sudo systemctl start nginx
sudo systemctl enable nginx
```

## 3. 테스트 방법

### 3.1 서버 연결 테스트
```bash
# 서버 상태 확인
curl http://[EC2_PUBLIC_IP]:5000/health

# 또는 로컬에서
curl http://localhost:5000/health
```

### 3.2 데이터베이스 연결 테스트
```bash
# PostgreSQL 연결 테스트
dotnet run --project Server/Server --environment Production -- --test-db
```

### 3.3 Redis 연결 테스트
```bash
# Redis 연결 테스트 (서버 내에서)
redis-cli -h [REDIS_ENDPOINT] -p 6379 ping

# 또는 서버 애플리케이션에서 Redis 연결 확인
curl -X GET "http://localhost:5000/api/health/redis"
```

### 3.4 API 엔드포인트 테스트
```bash
# 랭킹 API 테스트
curl -X GET "http://[EC2_PUBLIC_IP]:5000/api/ranking"

# 업적 API 테스트
curl -X GET "http://[EC2_PUBLIC_IP]:5000/api/achievements"
```

### 3.5 로그 확인
```bash
# PM2 로그 확인
pm2 logs just-climb-server

# 시스템 로그 확인
sudo journalctl -u nginx -f
```

## 4. 보안 설정

### 4.1 SSL 인증서 설치 (Let's Encrypt)
```bash
# Certbot 설치
sudo yum install -y certbot python3-certbot-nginx

# SSL 인증서 발급
sudo certbot --nginx -d [your-domain.com]
```

### 4.2 방화벽 설정
```bash
# 방화벽 활성화
sudo systemctl start firewalld
sudo systemctl enable firewalld

# 필요한 포트만 열기
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https
sudo firewall-cmd --permanent --add-port=22/tcp
sudo firewall-cmd --reload
```

## 5. 모니터링 및 백업

### 5.1 CloudWatch 설정
1. AWS Console > CloudWatch
2. Log Groups 생성
3. EC2 인스턴스에 CloudWatch Agent 설치

### 5.2 데이터베이스 백업
```bash
# 자동 백업 스크립트
#!/bin/bash
BACKUP_DIR="/var/backups/postgresql"
mkdir -p $BACKUP_DIR

pg_dump -h [RDS_ENDPOINT] -U postgres -d just_climb > $BACKUP_DIR/backup_$(date +%Y%m%d_%H%M%S).sql
```

## 6. 트러블슈팅

### 6.1 일반적인 문제들
1. **502 Bad Gateway**: 서버가 실행되지 않음
   - `pm2 restart just-climb-server`
2. **데이터베이스 연결 오류**: 보안 그룹 설정 확인
3. **SSL 인증서 오류**: Certbot 재실행

### 6.2 성능 최적화
- EC2 인스턴스 유형 업그레이드
- RDS 인스턴스 클래스 조정
- CloudFront CDN 사용
- ElastiCache 추가

## 완료 체크리스트
- [ ] RDS PostgreSQL 인스턴스 생성
- [ ] ElastiCache Redis 인스턴스 생성
- [ ] EC2 인스턴스 생성 및 설정
- [ ] 보안 그룹 설정 (PostgreSQL, Redis, HTTP/HTTPS)
- [ ] 애플리케이션 배포
- [ ] 데이터베이스 마이그레이션
- [ ] Redis 연결 테스트
- [ ] Nginx 설정
- [ ] SSL 인증서 설치
- [ ] 모든 API 엔드포인트 테스트
- [ ] 로그 및 모니터링 설정
- [ ] 백업 스크립트 설정

배포 완료 후 Unity 클라이언트의 `ServerConfig.cs`에서 서버 URL을 업데이트하세요. 