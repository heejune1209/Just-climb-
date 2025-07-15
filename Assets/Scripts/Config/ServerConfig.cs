using UnityEngine;
using System.Net;
using System.Net.Security;

namespace JustClimb.Config
{
    public static class ServerConfig
    {
#if UNITY_EDITOR
        public const string BASE_URL = "https://localhost:5001"; // 개발용
        public const bool IS_DEVELOPMENT = true;
#elif DEVELOPMENT_BUILD
        public const string BASE_URL = "https://api.justclimb.com"; // AWS 개발 서버
        public const bool IS_DEVELOPMENT = true;
#else
        public const string BASE_URL = "https://api.justclimb.com"; // AWS 프로덕션 서버
        public const bool IS_DEVELOPMENT = false;
#endif

        public const string API_VERSION = "v1";
        public static string ApiUrl => $"{BASE_URL}/api/{API_VERSION}";
        
        // API 엔드포인트들
        public static class Endpoints
        {
            public static string Rankings => $"{ApiUrl}/rankings";
            public static string Achievements => $"{ApiUrl}/achievements";
            public static string Items => $"{ApiUrl}/items";
            public static string Users => $"{ApiUrl}/users";
            public static string Auth => $"{ApiUrl}/auth";
            public static string Health => $"{ApiUrl}/health";
        }
        
        // 요청 타임아웃 설정
        public const int REQUEST_TIMEOUT_SECONDS = 30;
        public const int CONNECTION_TIMEOUT_SECONDS = 10;
        
        // SSL 설정
        public static bool ValidateSSL => !IS_DEVELOPMENT;
        
        /// <summary>
        /// SSL 인증서 검증 설정 초기화
        /// 게임 시작 시 한 번 호출해야 함
        /// </summary>
        public static void InitializeSSL()
        {
            if (IS_DEVELOPMENT)
            {
                // 개발 환경에서는 SSL 검증 우회 (자체 서명 인증서 허용)
                ServicePointManager.ServerCertificateValidationCallback = 
                    (sender, certificate, chain, sslPolicyErrors) => true;
                Debug.LogWarning("[ServerConfig] Development mode: SSL validation disabled");
            }
            else
            {
                // 프로덕션에서는 엄격한 SSL 검증
                ServicePointManager.ServerCertificateValidationCallback = null;
                Debug.Log("[ServerConfig] Production mode: SSL validation enabled");
            }
            
            // TLS 버전 설정
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }
        
        /// <summary>
        /// 서버 설정 정보 로그 출력
        /// </summary>
        public static void LogServerInfo()
        {
            Debug.Log($"[ServerConfig] Environment: {(IS_DEVELOPMENT ? "Development" : "Production")}");
            Debug.Log($"[ServerConfig] Base URL: {BASE_URL}");
            Debug.Log($"[ServerConfig] API URL: {ApiUrl}");
            Debug.Log($"[ServerConfig] SSL Validation: {ValidateSSL}");
            Debug.Log($"[ServerConfig] Request Timeout: {REQUEST_TIMEOUT_SECONDS}s");
        }
        
        /// <summary>
        /// URL 빌더 헬퍼 메서드
        /// </summary>
        /// <param name="endpoint">API 엔드포인트</param>
        /// <param name="parameters">쿼리 파라미터</param>
        /// <returns>완성된 URL</returns>
        public static string BuildUrl(string endpoint, params (string key, string value)[] parameters)
        {
            var url = $"{ApiUrl}/{endpoint.TrimStart('/')}";
            
            if (parameters != null && parameters.Length > 0)
            {
                url += "?";
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) url += "&";
                    url += $"{parameters[i].key}={UnityEngine.Networking.UnityWebRequest.EscapeURL(parameters[i].value)}";
                }
            }
            
            return url;
        }
    }
} 