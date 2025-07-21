using UnityEngine;
using JustClimb.Data;

namespace JustClimb.Utils
{
    /// <summary>
    /// Unity 클라이언트용 설정 관리 공통 헬퍼 클래스
    /// ServerConfig 로드 중복을 제거합니다.
    /// </summary>
    public static class ConfigHelper
    {
        private static ServerConfig _cachedConfig;

        /// <summary>
        /// ServerConfig를 캐시된 상태로 로드 (싱글톤 패턴)
        /// </summary>
        public static ServerConfig GetServerConfig()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = Resources.Load<ServerConfig>("ServerConfig");
                
                if (_cachedConfig == null)
                {
                    Debug.LogError("[ConfigHelper] ServerConfig를 찾을 수 없습니다! Resources/ServerConfig.asset을 생성하세요.");
                    
                    // 런타임에 기본값으로 생성 (에러 방지)
                    _cachedConfig = ScriptableObject.CreateInstance<ServerConfig>();
                }
            }
            
            return _cachedConfig;
        }

        /// <summary>
        /// 서버 베이스 URL 반환
        /// </summary>
        public static string GetBaseUrl()
        {
            return GetServerConfig().GetBaseUrl();
        }

        /// <summary>
        /// 사용자 상태 API URL 반환
        /// </summary>
        public static string GetUserStateApiUrl()
        {
            return GetServerConfig().GetUserStateApiUrl();
        }

        /// <summary>
        /// 델타 API URL 포맷 반환
        /// </summary>
        public static string GetDeltaApiUrlFormat()
        {
            return GetServerConfig().GetDeltaApiUrlFormat();
        }

        /// <summary>
        /// 랭킹 API URL 반환
        /// </summary>
        public static string GetRankingApiUrl()
        {
            return $"{GetBaseUrl()}/api/ranking";
        }

        /// <summary>
        /// Steam 인증 API URL 반환
        /// </summary>
        public static string GetSteamAuthApiUrl()
        {
            return $"{GetBaseUrl()}/api/auth/steam";
        }

        /// <summary>
        /// HTTP 타임아웃(초) 반환
        /// </summary>
        public static int GetTimeoutSeconds()
        {
            return GetServerConfig().timeoutSeconds;
        }

        /// <summary>
        /// 재시도 횟수 반환
        /// </summary>
        public static int GetRetryCount()
        {
            return GetServerConfig().retryCount;
        }

        /// <summary>
        /// 캐시 초기화 (테스트용)
        /// </summary>
        public static void ClearCache()
        {
            _cachedConfig = null;
        }

        /// <summary>
        /// 특정 사용자의 기록 업데이트 URL 생성
        /// </summary>
        public static string GetUserRecordApiUrl(string userId)
        {
            return $"{GetRankingApiUrl()}/{userId}/record";
        }

        /// <summary>
        /// 특정 사용자의 상태 API URL 생성
        /// </summary>
        public static string GetUserStateApiUrl(string userId)
        {
            return $"{GetBaseUrl()}/api/users/{userId}/state";
        }

        /// <summary>
        /// 델타 API URL 생성 (특정 사용자)
        /// </summary>
        public static string GetDeltaApiUrl(string userId)
        {
            return string.Format(GetDeltaApiUrlFormat(), userId);
        }

        /// <summary>
        /// 업적 API URL 반환
        /// </summary>
        public static string GetAchievementsApiUrl()
        {
            return $"{GetBaseUrl()}/api/achievements";
        }

        /// <summary>
        /// 특정 사용자의 업적 API URL 생성
        /// </summary>
        public static string GetUserAchievementsApiUrl(string userId)
        {
            return $"{GetAchievementsApiUrl()}/{userId}";
        }

        /// <summary>
        /// 업적 보상 수령 API URL 생성
        /// </summary>
        public static string GetClaimAchievementApiUrl(string userId, string achievementId)
        {
            return $"{GetUserAchievementsApiUrl(userId)}/{achievementId}/claim";
        }
    }
} 