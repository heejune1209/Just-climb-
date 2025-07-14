# Just Climb! Railway 무료 배포 가이드 🚂

## 🎯 5분 안에 무료 HTTPS 서버 완성하기

Railway는 가장 간단하고 빠른 무료 호스팅 서비스입니다. GitHub 연동만으로 자동 배포가 가능합니다!

---

## 🚀 1단계: Railway 계정 생성 (1분)

### A. 웹사이트 가입
```bash
# 1. Railway 웹사이트 방문
https://railway.app

# 2. "Deploy Now" 클릭
# 3. GitHub 계정으로 로그인
# 4. Railway 권한 승인
```

### B. CLI 설치 (선택사항)
```bash
# Node.js 필요
npm install -g @railway/cli

# 로그인
railway login
```

---

## 🏗️ 2단계: 프로젝트 설정 (2분)

### A. Railway 프로젝트 생성
```bash
# 방법 1: 웹 인터페이스
1. Railway 대시보드 → "New Project"
2. "Deploy from GitHub repo" 선택
3. Just_Climb 레포지토리 선택

# 방법 2: CLI (프로젝트 폴더에서)
cd /c/Users/user/Just_Climb
railway init
railway link  # 기존 프로젝트가 있는 경우
```

### B. 데이터베이스 추가
```bash
# 웹 인터페이스에서:
1. 프로젝트 대시보드
2. "+ New" 클릭
3. "Database" → "PostgreSQL" 선택

# CLI로:
railway add postgresql
railway add redis  # 선택사항 (Redis 캐시)
```

---

## 🔧 3단계: 환경 변수 설정 (1분)

### A. Railway 대시보드에서 설정
```bash
# 프로젝트 → Variables 탭에서 추가:

ASPNETCORE_ENVIRONMENT=Railway
JWT_SECRET_KEY=your-super-secure-jwt-key-here-32chars-minimum
STEAM_APP_ID=your-steam-app-id
STEAM_API_KEY=your-steam-api-key

# 자동으로 설정되는 변수들:
DATABASE_URL=postgresql://...  # PostgreSQL 연결 문자열
REDIS_URL=redis://...          # Redis 연결 문자열 (추가한 경우)
PORT=3000                      # 자동 할당 포트
```

### B. CLI로 설정 (선택사항)
```bash
railway variables set ASPNETCORE_ENVIRONMENT=Railway
railway variables set JWT_SECRET_KEY="your-secret-key"
railway variables set STEAM_APP_ID="your-app-id"
railway variables set STEAM_API_KEY="your-api-key"
```

---

## 📁 4단계: 배포 설정 파일 추가

### A. Railway 설정 파일 생성
```toml
# railway.toml
[build]
builder = "dockerfile"

[deploy]
startCommand = "dotnet Server.dll"
healthcheckPath = "/api/v1/health"
healthcheckTimeout = 300
restartPolicyType = "never"

[[services]]
name = "server"
source = "Server"
```

### B. 이미 생성된 파일 확인
```bash
# 이미 생성된 파일들:
✅ Server/Server/appsettings.Railway.json  # Railway 전용 설정
✅ Server/Dockerfile                       # Docker 설정
```

---

## 🚀 5단계: 배포 실행 (1분)

### A. 웹 인터페이스로 배포
```bash
1. Railway 대시보드
2. "Deploy" 버튼 클릭
3. 빌드 로그 확인
4. 배포 완료 대기 (2-3분)
```

### B. CLI로 배포
```bash
cd Server
railway up

# 또는 특정 환경으로
railway up --environment production
```

### C. 도메인 확인
```bash
# 배포 완료 후 자동 할당된 도메인 확인
https://your-app-name.railway.app

# 커스텀 도메인 설정 (선택사항)
# Railway 대시보드 → Settings → Domains
```

---

## 🎮 6단계: Unity 클라이언트 설정

### A. ServerConfig 업데이트
```csharp
// Assets/Scripts/Config/ServerConfig.cs
#if UNITY_EDITOR
    public const string BASE_URL = "https://localhost:5001";
#elif DEVELOPMENT_BUILD
    public const string BASE_URL = "https://your-app-name.railway.app";
#else
    public const string BASE_URL = "https://your-app-name.railway.app";
#endif
```

### B. GameManager에 SSL 초기화 추가
```csharp
// Assets/Scripts/Managers/GameManager.cs
using JustClimb.Config;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // SSL 설정 초기화
        ServerConfig.InitializeSSL();
        ServerConfig.LogServerInfo();
        
        // 기존 초기화 코드...
    }
}
```

---

## ✅ 7단계: 테스트 및 검증

### A. API 엔드포인트 테스트
```bash
# Health Check
curl https://your-app-name.railway.app/api/v1/health

# 응답 예시:
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "environment": "Railway"
}
```

### B. Unity 연결 테스트
```bash
# Unity 에디터에서:
1. 게임 실행
2. Console 로그 확인:
   [ServerConfig] Environment: Development
   [ServerConfig] Base URL: https://your-app-name.railway.app
   [ServerConfig] SSL Validation: True

3. 서버 통신 기능 테스트:
   - Steam 로그인
   - 랭킹 조회
   - 업적 확인
```

### C. 데이터베이스 연결 확인
```bash
# Railway 대시보드에서:
1. PostgreSQL 서비스 클릭
2. "Connect" 탭
3. 데이터베이스 접속 정보 확인

# 또는 CLI로:
railway connect postgres
```

---

## 📊 무료 사용량 모니터링

### A. 사용량 확인
```bash
# 웹 대시보드에서:
Railway 프로젝트 → Usage 탭

# CLI로:
railway usage
```

### B. 무료 한도
```bash
📊 Railway 무료 플랜:
- 실행 시간: 500시간/월
- 메모리: 512MB
- CPU: 공유 vCPU
- 네트워크: 100GB/월
- 스토리지: 1GB
- 크레딧: $5/월

💡 일반적인 사용량:
- 소규모 게임: ~100시간/월
- 중간 규모: ~300시간/월
- 크레딧 초과 시에만 요금 발생
```

---

## 🔄 자동 배포 설정 (GitHub Actions)

### A. GitHub Actions 워크플로우
```yaml
# .github/workflows/railway-deploy.yml
name: Deploy to Railway

on:
  push:
    branches: [main]
    paths: ['Server/**']

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Railway CLI
        run: npm install -g @railway/cli
        
      - name: Deploy to Railway
        env:
          RAILWAY_TOKEN: ${{ secrets.RAILWAY_TOKEN }}
        run: |
          cd Server
          railway up --service server
```

### B. Railway Token 설정
```bash
# 1. Railway 대시보드 → Account Settings → Tokens
# 2. "Create Token" 클릭
# 3. GitHub → Settings → Secrets → Actions
# 4. RAILWAY_TOKEN 추가
```

---

## 🛠️ 운영 및 모니터링

### A. 로그 확인
```bash
# 실시간 로그 보기
railway logs --follow

# 특정 서비스 로그
railway logs --service server

# 웹 대시보드에서도 확인 가능
```

### B. 성능 모니터링
```bash
# Railway 대시보드에서 확인:
- CPU 사용량
- 메모리 사용량
- 네트워크 트래픽
- 응답 시간
```

### C. 데이터베이스 관리
```bash
# 데이터베이스 접속
railway connect postgres

# 백업 (Railway Pro 플랜)
railway db backup

# 마이그레이션 실행
railway run dotnet ef database update
```

---

## 💡 최적화 팁

### A. 비용 절약
```csharp
// 1. 불필요한 로그 최소화
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// 2. 캐시 활용
services.AddMemoryCache();

// 3. 배치 처리
public class BatchProcessor 
{
    public async Task ProcessInBatches<T>(IEnumerable<T> items, int batchSize = 50)
    {
        // 대량 데이터 처리 시 배치로 나누기
    }
}
```

### B. 성능 향상
```csharp
// 1. 연결 풀링
services.AddDbContext<JustClimbDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
    });
});

// 2. 응답 압축
services.AddResponseCompression();
```

---

## 🎉 완료!

### ✅ 성공 확인사항
- [ ] Railway 프로젝트 생성 완료
- [ ] PostgreSQL 데이터베이스 연결 확인
- [ ] HTTPS 도메인 할당 확인
- [ ] API 엔드포인트 응답 확인
- [ ] Unity 클라이언트 연결 확인
- [ ] Steam 인증 연동 확인

### 🔗 최종 결과
```bash
🌐 서버 URL: https://your-app-name.railway.app
🔒 SSL 인증서: 자동 제공
💰 비용: 무료 (월 $5 크레딧)
⚡ 배포 시간: 5분
🔄 자동 배포: GitHub 연동
```

### 🚀 다음 단계
1. **Unity 빌드**: Steam 배포 준비
2. **도메인 연결**: 커스텀 도메인 설정 (선택사항)
3. **모니터링**: 사용량 및 성능 확인
4. **확장**: 필요 시 Railway Pro 플랜으로 업그레이드

**축하합니다! Just Climb이 Railway에서 무료로 실행되고 있습니다!** 🎉

---

## 🆘 문제 해결

### 일반적인 오류
1. **빌드 실패**: `railway logs` 로그 확인
2. **데이터베이스 연결 실패**: `DATABASE_URL` 환경 변수 확인
3. **포트 오류**: Railway가 자동 할당하는 `PORT` 사용
4. **SSL 인증서**: Railway가 자동 제공, 별도 설정 불필요

### 지원
- Railway 문서: https://docs.railway.app
- Discord 커뮤니티: https://discord.gg/railway
- GitHub 이슈: Repository Issues 