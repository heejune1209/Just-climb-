# appsettings 설정 파일 설명

## 파일 구조 및 역할

### 1. appsettings.json (로컬 개발환경)
- **사용 시점**: `ASPNETCORE_ENVIRONMENT=Development` 일 때 사용
- **역할**: 로컬 개발 환경에서 SQL Server Express와 로컬 Redis 사용

### 2. appsettings.Production.json (Production 환경)
- **사용 시점**: `ASPNETCORE_ENVIRONMENT=Production` 일 때 사용
- **역할**: 실제 운영 환경에서 AWS RDS PostgreSQL과 ElastiCache Redis 사용
- **특징**: 환경 변수 플레이스홀더 사용

### 3. appsettings.AWS.json (AWS 환경)
- **사용 시점**: `ASPNETCORE_ENVIRONMENT=AWS` 일 때 사용
- **역할**: AWS 배포 환경에서 실제 값들이 직접 설정됨
- **특징**: 모든 값이 하드코딩되어 있음

---

## 각 설정 항목 설명

### ConnectionStrings
```json
"ConnectionStrings": {
  "DefaultConnection": "연결문자열"
}
```
- **바인딩**: `Program.cs`의 `AddDbContext<GameDbContext>()`와 연결
- **역할**: 데이터베이스 연결 설정
- **로컬**: SQL Server Express 사용
- **AWS**: PostgreSQL RDS 사용

### Redis
```json
"Redis": {
  "ConnectionString": "Redis서버:포트"
}
```
- **바인딩**: `Program.cs`의 Redis 설정 부분, `IConnectionMultiplexer` 인터페이스
- **역할**: 캐싱 및 세션 관리
- **로컬**: `localhost:6379` 사용
- **AWS**: ElastiCache Redis 엔드포인트 사용

### RedisSyncConfig
```json
"RedisSyncConfig": {
  "CacheDurationHours": 24
}
```
- **바인딩**: `RedisCacheService` 클래스에서 사용
- **역할**: 캐시 데이터 만료 시간 설정 (24시간)

### SteamSettings
```json
"SteamSettings": {
  "AppId": "3862880",
  "WebApiKey": "A9C492944CB953C66EC8C24EFB725737"
}
```
- **바인딩**: `SteamService` 클래스, Steam API 호출 시 사용
- **역할**: Steam 업적, 통계 등 Steam 기능 연동
- **AppId**: Steam에서 할당받은 게임 ID
- **WebApiKey**: Steam Web API 키

### JwtSettings
```json
"JwtSettings": {
  "SecretKey": "토큰서명용비밀키",
  "Issuer": "JustClimbServer",
  "Audience": "JustClimbUsers",
  "ExpirationHours": 24
}
```
- **바인딩**: `Program.cs`의 JWT 인증 설정, `JwtSecurityTokenHandler` 클래스
- **역할**: 사용자 인증 토큰 생성/검증
- **SecretKey**: 토큰 서명용 비밀 키
- **Issuer**: 토큰 발급자 식별자
- **Audience**: 토큰 수신자 식별자
- **ExpirationHours**: 토큰 만료 시간 (24시간)

### RedisSettings
```json
"RedisSettings": {
  "ConnectionString": "Redis서버:포트",
  "InstanceName": "JustClimb"
}
```
- **바인딩**: `RedisCacheService` 클래스, `IDistributedCache` 인터페이스
- **역할**: Redis 캐시 서비스 상세 설정
- **ConnectionString**: Redis 서버 연결 정보
- **InstanceName**: Redis 인스턴스 이름 (키 prefix로 사용)

### Logging
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```
- **바인딩**: `ILogger<T>` 인터페이스
- **역할**: 로깅 레벨 설정
- **Default**: 기본 로그 레벨
- **Microsoft.AspNetCore**: ASP.NET Core 로그 레벨

### AllowedHosts
```json
"AllowedHosts": "*"
```
- **바인딩**: ASP.NET Core의 Host Filtering 미들웨어
- **역할**: 허용할 호스트 설정
- **"*"**: 모든 호스트에서 접근 허용

---

## Program.cs와의 연결 예시

```csharp
// ConnectionStrings 읽기
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JwtSettings 읽기
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

// RedisSettings 읽기
var redisConnection = builder.Configuration.GetSection("RedisSettings")["ConnectionString"];

// SteamSettings 읽기
var steamAppId = builder.Configuration.GetSection("SteamSettings")["AppId"];
```

---

## 환경별 설정 파일 우선순위

1. **appsettings.json** (기본)
2. **appsettings.{Environment}.json** (환경별)
3. **환경 변수** (최우선)

예: `ASPNETCORE_ENVIRONMENT=AWS`일 때
1. `appsettings.json` 로드
2. `appsettings.AWS.json` 로드 (기본 설정을 오버라이드)
3. 환경 변수가 있다면 최종 오버라이드

---

## 보안 고려사항

- **Production 환경**: 민감한 정보는 환경 변수로 관리
- **AWS 환경**: 실제 값들이 하드코딩되어 있어 보안 취약
- **권장**: AWS Systems Manager Parameter Store 또는 AWS Secrets Manager 사용 