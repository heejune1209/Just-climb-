# AWS 연동 완료 가이드

## 🎯 연동 완료 확인 체크리스트

### 1. 인프라 연결 확인
- ✅ EC2 인스턴스: 13.125.227.110
- ✅ RDS PostgreSQL: just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com
- ✅ ElastiCache Redis: just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com

### 2. 네트워크 연결 테스트
```bash
# EC2에서 RDS 연결 테스트
nc -zv just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com 5432

# EC2에서 Redis 연결 테스트  
nc -zv just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com 6379
```

### 3. 데이터베이스 연결 테스트
```bash
# PostgreSQL 연결
psql -h just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com -U heejune1209 -d postgres

# Redis 연결
redis6-cli -h just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com -p 6379 ping
```

## 🚀 배포 프로세스

### 1. 로컬 개발 환경 설정
```bash
# 프로젝트 디렉토리로 이동
cd /c/Users/user/Just_Climb

# 환경별 설정 파일 확인
Server/Server/appsettings.json           # 로컬 개발용
Server/Server/appsettings.Production.json # AWS 배포용
```

### 2. 로컬에서 빌드 테스트
```bash
cd Server/Server
dotnet build
dotnet publish -c Release -o ../publish
```

### 3. AWS 배포 실행
```bash
# Windows에서 실행
deploy-scripts/local-deploy.bat

# 또는 수동으로 파일 전송
scp -i keypair.pem -r Server/publish/* ec2-user@13.125.227.110:/tmp/justclimb-deploy/
```

## 🔧 서비스 관리 명령어

### EC2에서 서비스 제어
```bash
# 서비스 상태 확인
sudo systemctl status justclimb

# 서비스 시작/중지/재시작
sudo systemctl start justclimb
sudo systemctl stop justclimb
sudo systemctl restart justclimb

# 로그 확인
sudo journalctl -u justclimb -f
```

### 실시간 모니터링
```bash
# 시스템 리소스 모니터링
top
htop

# 네트워크 연결 상태
netstat -tlnp | grep :5000

# 애플리케이션 로그
tail -f /var/log/justclimb/app.log
```

## 🌐 API 엔드포인트 테스트

### 서버 상태 확인
```bash
# 서버 응답 확인
curl http://13.125.227.110:5000/api/health

# Redis 연결 테스트
curl http://13.125.227.110:5000/api/redistest/ping

# 데이터베이스 연결 테스트
curl http://13.125.227.110:5000/api/database/test
```

### Unity 게임에서 연결 테스트
```csharp
// Unity에서 서버 URL 변경
public string serverUrl = "http://13.125.227.110:5000";

// API 호출 테스트
StartCoroutine(TestServerConnection());
```

## 📊 성능 모니터링

### AWS CloudWatch 메트릭
```
EC2 인스턴스:
- CPU 사용률
- 메모리 사용률
- 네트워크 I/O

RDS:
- 데이터베이스 연결 수
- 쿼리 응답 시간
- 스토리지 사용량

ElastiCache:
- Redis 연결 수
- 캐시 히트율
- 메모리 사용률
```

### 애플리케이션 성능 확인
```bash
# 서버 응답 시간 측정
curl -w "@curl-format.txt" -o /dev/null -s http://13.125.227.110:5000/api/health

# 동시 접속자 수 테스트
ab -n 1000 -c 10 http://13.125.227.110:5000/api/health
```

## 🔐 보안 설정 확인

### 보안 그룹 규칙
```
EC2 보안 그룹:
- 인바운드: 22(SSH), 5000(HTTP)
- 아웃바운드: 모든 트래픽

RDS 보안 그룹:
- 인바운드: 5432(PostgreSQL) ← EC2 보안 그룹만

Redis 보안 그룹:
- 인바운드: 6379(Redis) ← EC2 보안 그룹만
```

### SSL/TLS 설정 (선택사항)
```bash
# Let's Encrypt SSL 인증서 설치
sudo dnf install certbot -y
sudo certbot certonly --standalone -d yourdomain.com

# Nginx 프록시 설정
sudo dnf install nginx -y
```

## 🐛 문제 해결

### 일반적인 문제들
```bash
# 1. 서비스 시작 실패
sudo systemctl status justclimb
sudo journalctl -u justclimb --since "1 hour ago"

# 2. 데이터베이스 연결 실패
psql -h just-climb-db.c7keagac6fmv.ap-northeast-2.rds.amazonaws.com -U heejune1209 -d postgres

# 3. Redis 연결 실패
redis6-cli -h just-climb-redis-kp9dum.serverless.apn2.cache.amazonaws.com -p 6379 ping

# 4. 포트 접근 불가
sudo firewall-cmd --list-all
sudo firewall-cmd --permanent --add-port=5000/tcp
sudo firewall-cmd --reload
```

### 로그 파일 위치
```
애플리케이션 로그: sudo journalctl -u justclimb
시스템 로그: /var/log/messages
웹 서버 로그: /var/log/httpd/ (Apache) 또는 /var/log/nginx/ (Nginx)
```

## 🎮 Unity 클라이언트 연결 설정

### DataManager.cs 수정
```csharp
public class DataManager : MonoBehaviour
{
    // 개발 환경
    private const string DEV_SERVER_URL = "http://localhost:5000";
    
    // 프로덕션 환경 (AWS)
    private const string PROD_SERVER_URL = "http://13.125.227.110:5000";
    
    public string GetServerUrl()
    {
        #if UNITY_EDITOR
            return DEV_SERVER_URL;
        #else
            return PROD_SERVER_URL;
        #endif
    }
}
```

## 🎯 배포 완료 후 확인사항

### 1. 기본 기능 테스트
- [ ] 서버 시작 및 응답 확인
- [ ] 데이터베이스 연결 확인
- [ ] Redis 캐시 동작 확인
- [ ] Unity 클라이언트 연결 확인

### 2. 성능 테스트
- [ ] 동시 접속자 테스트
- [ ] 응답 시간 측정
- [ ] 메모리/CPU 사용량 확인

### 3. 보안 테스트
- [ ] 불필요한 포트 차단 확인
- [ ] 데이터베이스 직접 접근 차단 확인
- [ ] SSL/TLS 설정 (선택사항)

## 📞 지원 및 문의

### 유용한 명령어 모음
```bash
# 전체 서비스 상태 확인
sudo systemctl list-units --type=service --state=running

# 디스크 사용량 확인
df -h

# 메모리 사용량 확인
free -h

# 네트워크 연결 상태
ss -tulpn
```

### AWS 리소스 모니터링
```
AWS Console → CloudWatch → 대시보드
→ EC2, RDS, ElastiCache 메트릭 확인
```

---

🎉 **축하합니다!** Just Climb 게임이 AWS 클라우드에서 성공적으로 실행되고 있습니다! 