using UnityEngine;
namespace JustClimb.Data
{
    /// <summary>
    /// 서버 연결 설정을 관리하는 ScriptableObject입니다.
    /// Resources 폴더에 배치하여 런타임에 로드합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ServerConfig", menuName = "JustClimb/Server Config")]
    public class ServerConfig : ScriptableObject
    {
        [Header("서버 URL 설정")]
        [Tooltip("개발 환경에서 사용할 서버 URL")]
        public string developmentServerUrl = "http://localhost:5259";  // 로컬 개발용 (HTTP)
        
        [Tooltip("운영 환경에서 사용할 서버 URL - AWS EC2 게임서버")]
        public string productionServerUrl = "http://54.180.97.179:5000";
        
        [Header("API 엔드포인트")]
        [Tooltip("사용자 상태 조회/저장 API 경로")]
        public string userStateApiPath = "/api/users";
        
        [Tooltip("델타 동기화 API 경로 (userId는 자동으로 삽입됨)")]
        public string deltaApiPath = "/api/users/{0}/state/delta";
        
        [Header("연결 설정")]
        [Tooltip("HTTP 요청 타임아웃 (초)")]
        public int timeoutSeconds = 30;
        
        [Tooltip("재시도 횟수")]
        public int retryCount = 3;
        
        /// <summary>
        /// 현재 환경에 맞는 기본 서버 URL을 반환합니다.
        /// </summary>
        public string GetBaseUrl()
        {
            // 🧪 AWS 서버 테스트용 - 임시로 Production URL 사용
            return productionServerUrl;
            
            // 원래 코드 (테스트 완료 후 복구)
            /*
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
                return developmentServerUrl;
            #else
                return productionServerUrl;
            #endif
            */
        }
        
        /// <summary>
        /// 사용자 상태 API의 전체 URL을 반환합니다.
        /// </summary>
        public string GetUserStateApiUrl()
        {
            return GetBaseUrl() + userStateApiPath;
        }
        
        /// <summary>
        /// 델타 동기화 API의 URL 형식을 반환합니다.
        /// </summary>
        public string GetDeltaApiUrlFormat()
        {
            return GetBaseUrl() + deltaApiPath;
        }
    }
} 