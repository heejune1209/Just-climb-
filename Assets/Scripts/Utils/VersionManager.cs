using UnityEngine;

namespace JustClimb.Utils
{
    /// <summary>
    /// 게임 버전 정보를 관리하는 클래스
    /// GitHub Actions에서 자동으로 업데이트됩니다.
    /// </summary>
    public static class VersionManager
    {
        // 자동 생성되는 버전 정보
        public const string VERSION = "1.0.0";
        public const string BUILD_NUMBER = "1";
        public const string BUILD_DATE = "2024-01-01";
        public const string COMMIT_HASH = "dev";
        
        // Steam 빌드 정보
        public const string STEAM_APP_ID = "3862880";
        public const string STEAM_BRANCH = "default";
        
        /// <summary>
        /// 전체 버전 문자열 반환
        /// </summary>
        public static string FullVersion => $"{VERSION}.{BUILD_NUMBER}";
        
        /// <summary>
        /// 버전 정보를 콘솔에 출력
        /// </summary>
        public static void LogVersionInfo()
        {
            Debug.Log($"[Version] Just Climb v{FullVersion}");
            Debug.Log($"[Version] Build Date: {BUILD_DATE}");
            Debug.Log($"[Version] Commit: {COMMIT_HASH}");
            Debug.Log($"[Version] Steam App ID: {STEAM_APP_ID}");
            Debug.Log($"[Version] Steam Branch: {STEAM_BRANCH}");
        }
        
        /// <summary>
        /// 개발 빌드 여부 확인
        /// </summary>
        public static bool IsDevelopmentBuild => 
            COMMIT_HASH == "dev" || 
            BUILD_DATE == "2024-01-01" ||
            Debug.isDebugBuild;
        
        /// <summary>
        /// Steam 업데이트 알림 표시
        /// </summary>
        public static void ShowUpdateNotification()
        {
            if (IsDevelopmentBuild)
            {
                Debug.LogWarning("[Version] Development build - 자동 업데이트 비활성화");
                return;
            }
            
            Debug.Log("[Version] Steam 자동 업데이트 확인 중...");
            // Steam API를 통한 업데이트 확인 로직
        }
    }
} 