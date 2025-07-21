using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using JustClimb.Data;
using JustClimb.Manager;

namespace JustClimb.Utils
{
    /// <summary>
    /// Unity 클라이언트용 네트워크 통신 공통 헬퍼 클래스
    /// JWT 토큰 관리 및 HTTP 통신을 통합합니다.
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// UnityWebRequest에 JWT 토큰 헤더 추가
        /// </summary>
        public static void AddAuthorizationHeader(UnityWebRequest request, SteamAuthManager steamAuthManager)
        {
            if (steamAuthManager != null && steamAuthManager.HasValidToken())
            {
                request.SetRequestHeader("Authorization", $"Bearer {steamAuthManager.JwtToken}");
            }
        }

        /// <summary>
        /// JSON 데이터로 POST 요청 생성
        /// </summary>
        public static UnityWebRequest CreatePostRequest(string url, object data, SteamAuthManager steamAuthManager = null)
        {
            string json = JsonHelper.SerializeObject(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };
            
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (steamAuthManager != null)
            {
                AddAuthorizationHeader(request, steamAuthManager);
            }

            // 개발 환경에서 SSL 우회 설정
            SetupDevelopmentSSL(request);

            return request;
        }

        /// <summary>
        /// JSON 데이터로 PUT 요청 생성
        /// </summary>
        public static UnityWebRequest CreatePutRequest(string url, object data, SteamAuthManager steamAuthManager = null)
        {
            string json = JsonHelper.SerializeObject(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(url, "PUT")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };
            
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (steamAuthManager != null)
            {
                AddAuthorizationHeader(request, steamAuthManager);
            }

            // 개발 환경에서 SSL 우회 설정
            SetupDevelopmentSSL(request);

            return request;
        }

        /// <summary>
        /// GET 요청 생성
        /// </summary>
        public static UnityWebRequest CreateGetRequest(string url, SteamAuthManager steamAuthManager = null)
        {
            var request = UnityWebRequest.Get(url);
            
            if (steamAuthManager != null)
            {
                AddAuthorizationHeader(request, steamAuthManager);
            }

            // 개발 환경에서 SSL 우회 설정
            SetupDevelopmentSSL(request);

            return request;
        }

        /// <summary>
        /// DELETE 요청 생성
        /// </summary>
        public static UnityWebRequest CreateDeleteRequest(string url, SteamAuthManager steamAuthManager = null)
        {
            var request = new UnityWebRequest(url, "DELETE")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            
            if (steamAuthManager != null)
            {
                AddAuthorizationHeader(request, steamAuthManager);
            }

            // 개발 환경에서 SSL 우회 설정
            SetupDevelopmentSSL(request);

            return request;
        }

        /// <summary>
        /// 응답 결과를 JSON으로 파싱
        /// </summary>
        public static T ParseResponse<T>(UnityWebRequest request, T defaultValue = default)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                return JsonHelper.DeserializeObject(request.downloadHandler.text, defaultValue);
            }
            
            Debug.LogError($"[NetworkHelper] 요청 실패: {request.error}");
            Debug.LogError($"[NetworkHelper] 응답 코드: {request.responseCode}");
            Debug.LogError($"[NetworkHelper] 응답 내용: {request.downloadHandler.text}");
            
            return defaultValue;
        }

        /// <summary>
        /// 네트워크 연결 상태 확인
        /// </summary>
        public static bool IsOnline()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        /// <summary>
        /// 개발 환경에서 SSL 인증서 검증 우회 설정
        /// </summary>
        public static void SetupDevelopmentSSL(UnityWebRequest request)
        {
#if UNITY_EDITOR
            request.certificateHandler = new AcceptAllCertificatesHandler();
#endif
        }

        /// <summary>
        /// 요청 성공 여부 확인
        /// </summary>
        public static bool IsSuccess(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.Success;
        }

        /// <summary>
        /// 상세 오류 로그 출력
        /// </summary>
        public static void LogDetailedError(string context, UnityWebRequest request)
        {
            Debug.LogError($"[{context}] 네트워크 요청 실패");
            Debug.LogError($"[{context}] URL: {request.url}");
            Debug.LogError($"[{context}] Method: {request.method}");
            Debug.LogError($"[{context}] Error: {request.error}");
            Debug.LogError($"[{context}] Response Code: {request.responseCode}");
            Debug.LogError($"[{context}] Response Text: {request.downloadHandler?.text}");
        }

        /// <summary>
        /// 재시도 로직이 포함된 요청 실행
        /// </summary>
        public static IEnumerator SendRequestWithRetry(UnityWebRequest request, int maxRetries = 3, float retryDelay = 1f)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    yield break; // 성공 시 종료
                }

                Debug.LogWarning($"[NetworkHelper] 요청 실패 (시도 {attempt + 1}/{maxRetries}): {request.error}");
                
                if (attempt < maxRetries - 1)
                {
                    // 재시도 전 대기
                    yield return new WaitForSeconds(retryDelay);
                    
                    // 요청 객체 재생성 (재사용 불가능한 경우가 있음)
                    // Note: 호출자가 새로운 request 객체를 제공해야 할 수도 있음
                }
            }
        }
    }

    /// <summary>
    /// 개발 환경에서 모든 SSL 인증서를 허용하는 핸들러
    /// </summary>
    public class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // 개발 환경에서 모든 인증서 허용
        }
    }
} 