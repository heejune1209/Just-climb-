# Just Climb! SSL 초기화 가이드

## 🚀 Unity에서 SSL 설정 적용하기

### 1. **GameManager에서 SSL 초기화**

기존 `GameManager.cs` 파일의 `Awake()` 또는 `Start()` 메서드에 다음 코드를 추가하세요:

```csharp
using JustClimb.Config;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // SSL 설정 초기화 (게임 시작 시 한 번만)
        ServerConfig.InitializeSSL();
        ServerConfig.LogServerInfo();
        
        // 기존 초기화 코드...
    }
}
```

### 2. **매니저들에서 새로운 API URL 사용**

#### A. **RankingManager.cs 수정 예시**
```csharp
using JustClimb.Config;

public class RankingManager : MonoBehaviour
{
    private async Task<bool> SendRankingToServer(RankingData data)
    {
        string url = ServerConfig.Endpoints.Rankings;
        // 또는 파라미터가 있는 경우:
        // string url = ServerConfig.BuildUrl("rankings", 
        //     ("stage", data.stageId.ToString()), 
        //     ("mode", data.gameMode));
        
        // 기존 HTTP 요청 코드...
        using var request = UnityWebRequest.PostWwwForm(url, formData);
        request.timeout = ServerConfig.REQUEST_TIMEOUT_SECONDS;
        
        await request.SendWebRequest();
        return request.result == UnityWebRequest.Result.Success;
    }
}
```

#### B. **AchievementManager.cs 수정 예시**
```csharp
using JustClimb.Config;

public class AchievementManager : MonoBehaviour
{
    private async Task SyncAchievementsWithServer()
    {
        string url = ServerConfig.Endpoints.Achievements;
        
        using var request = UnityWebRequest.Get(url);
        request.timeout = ServerConfig.REQUEST_TIMEOUT_SECONDS;
        
        await request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"업적 동기화 성공: {url}");
        }
    }
}
```

#### C. **ItemManager.cs 수정 예시**
```csharp
using JustClimb.Config;

public class ItemManager : MonoBehaviour
{
    private async Task<bool> PurchaseItemFromServer(int itemId, int quantity)
    {
        string url = ServerConfig.BuildUrl("items/purchase", 
            ("itemId", itemId.ToString()), 
            ("quantity", quantity.ToString()));
        
        var purchaseData = new { itemId, quantity };
        string jsonData = JsonUtility.ToJson(purchaseData);
        
        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = ServerConfig.REQUEST_TIMEOUT_SECONDS;
        
        await request.SendWebRequest();
        return request.result == UnityWebRequest.Result.Success;
    }
}
```

### 3. **테스트 및 검증**

#### A. **Unity 에디터에서 로그 확인**
게임 실행 시 Console에서 다음과 같은 로그가 표시되어야 합니다:

```
[ServerConfig] Development mode: SSL validation disabled
[ServerConfig] Environment: Development  
[ServerConfig] Base URL: https://localhost:5001
[ServerConfig] API URL: https://localhost:5001/api/v1
[ServerConfig] SSL Validation: False
[ServerConfig] Request Timeout: 30s
```

#### B. **빌드된 게임에서 확인**
프로덕션 빌드에서는 다음과 같이 표시되어야 합니다:

```
[ServerConfig] Production mode: SSL validation enabled
[ServerConfig] Environment: Production
[ServerConfig] Base URL: https://api.justclimb.com  
[ServerConfig] API URL: https://api.justclimb.com/api/v1
[ServerConfig] SSL Validation: True
[ServerConfig] Request Timeout: 30s
```

### 4. **주의사항**

#### ⚠️ **SSL 인증서 문제 해결**

1. **개발 환경 문제**:
   ```csharp
   // 개발 서버 SSL 문제 시 임시 해결
   #if UNITY_EDITOR
   ServicePointManager.ServerCertificateValidationCallback = 
       (sender, certificate, chain, sslPolicyErrors) => true;
   #endif
   ```

2. **Unity 2022+ WebGL 빌드**:
   ```csharp
   #if UNITY_WEBGL && !UNITY_EDITOR
   // WebGL에서는 브라우저가 SSL 처리
   // 별도 SSL 설정 불필요
   #endif
   ```

3. **모바일 플랫폼**:
   ```csharp
   #if UNITY_ANDROID || UNITY_IOS
   // 모바일에서는 시스템 인증서 저장소 사용
   ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
   #endif
   ```

### 5. **배포 전 체크리스트**

- [ ] GameManager에서 `ServerConfig.InitializeSSL()` 호출 확인
- [ ] 모든 매니저에서 하드코딩된 URL을 `ServerConfig.Endpoints` 사용으로 변경  
- [ ] 개발/프로덕션 환경별 URL 정상 동작 확인
- [ ] SSL 인증서 검증 정상 동작 확인
- [ ] 타임아웃 설정 적용 확인
- [ ] 에러 핸들링 추가 (네트워크 오류, SSL 오류 등)

### 6. **완료!**

이제 Just Climb 게임이 환경별로 적절한 HTTPS 서버와 통신할 준비가 완료되었습니다! 🎉

- ✅ **개발**: `https://localhost:5001` (SSL 검증 우회)
- ✅ **스테이징**: `https://dev-api.justclimb.com` (SSL 검증 우회)  
- ✅ **프로덕션**: `https://api.justclimb.com` (완전한 SSL 검증)

서버를 Railway나 AWS에 배포한 후 Steam에 게임을 업로드하면 됩니다! 