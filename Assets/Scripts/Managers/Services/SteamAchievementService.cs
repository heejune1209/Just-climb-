using System;
using UnityEngine;
using Steamworks;
using Zenject;

namespace JustClimb.Services
{
    /// <summary>
    /// Steam API 전담 서비스
    /// 초기화, 통계 관리, 업적 해제 처리를 담당
    /// </summary>
    public class SteamAchievementService
    {
        private bool _steamInitialized = false;
        private bool _steamStatsReady = false;
        private bool _useRealSteamInEditor = false;

        public bool IsSteamInitialized => _steamInitialized;
        public bool IsSteamStatsReady => _steamStatsReady;

        /// <summary>
        /// Steam 초기화
        /// </summary>
        public void InitializeSteam()
        {
            try
            {
                #if UNITY_EDITOR
                // 에디터에서는 UI 설정에 따라 동작 결정
                if (_useRealSteamInEditor)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log("[SteamAchievementService] 에디터에서 실제 Steam API 사용 모드");
#endif
                    if (!SteamAPI.IsSteamRunning())
                    {
                        Debug.LogWarning("[SteamAchievementService] Steam이 실행되지 않았습니다. 개발 모드로 전환합니다.");
                        _steamInitialized = false;
                        return;
                    }

                    if (SteamManager.Initialized)
                    {
                        _steamInitialized = true;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        Debug.Log("[SteamAchievementService] Steam initialized successfully (Editor + Real Steam)");
#endif
                    }
                    else
                    {
                        _steamInitialized = false;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        Debug.Log("[SteamAchievementService] Steam not initialized. 개발 모드로 전환합니다.");
#endif
                    }
                }
                else
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log("[SteamAchievementService] 개발 환경: Steam API 체크 건너뜀, 업적 시스템은 로그 모드로 동작");
#endif
                    _steamInitialized = true; // 개발 환경에서는 항상 초기화된 것으로 처리
                }
                return;
                #endif

                // Steam API가 사용 가능한지 확인
                if (!SteamAPI.IsSteamRunning())
                {
                    Debug.Log("[SteamAchievementService] Steam이 실행되지 않았습니다. 업적 시스템이 비활성화됩니다.");
                    _steamInitialized = false;
                    return;
                }

                if (SteamManager.Initialized)
                {
                    _steamInitialized = true;
                    Debug.Log("[SteamAchievementService] Steam initialized successfully");
                }
                else
                {
                    _steamInitialized = false;
                    Debug.Log("[SteamAchievementService] Steam not initialized. 업적 시스템이 비활성화됩니다.");
                }
            }
            catch (System.Exception e)
            {
                _steamInitialized = false;
                _steamStatsReady = false;
                Debug.LogError($"[SteamAchievementService] Steam 초기화 중 예외 발생: {e.Message}");
            }
        }

        /// <summary>
        /// Steam 테스트 설정 변경
        /// </summary>
        public void SetUseRealSteamInEditor(bool useRealSteam)
        {
            _useRealSteamInEditor = useRealSteam;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[SteamAchievementService] Steam 테스트 설정 변경: useRealSteamInEditor = {_useRealSteamInEditor}");
            
            if (_useRealSteamInEditor)
            {
                Debug.Log("[SteamAchievementService] 에디터에서 실제 Steam API 사용 모드로 변경");
            }
            else
            {
                Debug.Log("[SteamAchievementService] 에디터에서 개발 모드(로그만)로 변경");
            }
#endif
        }

        /// <summary>
        /// Steam 통계를 안전하게 요청하는 메서드 (캐싱 지원)
        /// </summary>
        public bool EnsureSteamStatsReady()
        {
            // 이미 준비된 경우 캐시된 결과 반환
            if (_steamStatsReady)
            {
                return true;
            }

            if (!_steamInitialized || !SteamManager.Initialized)
            {
                return false;
            }

            // Steam API가 실행 중인지 확인
            if (!SteamAPI.IsSteamRunning())
            {
                return false;
            }

            try
            {
                #if UNITY_EDITOR
                if (_useRealSteamInEditor)
                {
                    // 에디터에서 실제 Steam API 사용
                    bool result = SteamUserStats.RequestCurrentStats();
                    if (result)
                    {
                        _steamStatsReady = true;
                        Debug.Log("[SteamAchievementService] Steam stats 준비 완료 (Editor + Real Steam)");
                    }
                    else
                    {
                        Debug.Log("[SteamAchievementService] Steam stats 요청 실패 (Editor + Real Steam)");
                    }
                    return result;
                }
                else
                {
                    // 개발 환경에서는 Steam 통계 요청을 건너뛰고 바로 성공으로 처리
                    _steamStatsReady = true;
                    Debug.Log("[SteamAchievementService] 개발 환경: Steam stats 요청 건너뜀");
                    return true;
                }
                #else
                // 프로덕션에서만 실제 Steam 통계 요청
                bool result = SteamUserStats.RequestCurrentStats();
                if (result)
                {
                    _steamStatsReady = true;
                    Debug.Log("[SteamAchievementService] Steam stats 준비 완료");
                }
                else
                {
                    Debug.Log("[SteamAchievementService] Steam stats 요청 실패");
                }
                return result;
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[SteamAchievementService] Steam stats 요청 중 예외: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Steam 업적 해제 처리
        /// </summary>
        public bool UnlockSteamAchievement(string achievementID)
        {
            // 기본 유효성 검사
            if (!_steamInitialized || !SteamManager.Initialized || string.IsNullOrEmpty(achievementID))
            {
                Debug.Log($"[SteamAchievementService] Steam 조건 불만족, 업적 건너뜀: {achievementID}");
                return false;
            }

            // Steam 통계 준비 확인
            if (!EnsureSteamStatsReady())
            {
                Debug.Log($"[SteamAchievementService] Steam stats not ready, skipping achievement: {achievementID}");
                return false;
            }

            try
            {
                #if UNITY_EDITOR
                if (_useRealSteamInEditor)
                {
                    // 에디터에서 실제 Steam API 사용
                    bool success = SteamUserStats.SetAchievement(achievementID);
                    if (success)
                    {
                        bool storeResult = SteamUserStats.StoreStats();
                        if (storeResult)
                        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                            Debug.Log($"[SteamAchievementService] [실제 Steam API] Achievement Unlocked: {achievementID}");
#endif
                            return true;
                        }
                        else
                        {
                            Debug.LogError($"[SteamAchievementService] [실제 Steam API] Failed to store stats for achievement: {achievementID}");
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[SteamAchievementService] [실제 Steam API] Failed to unlock achievement: {achievementID}");
                        return false;
                    }
                }
                else
                {
                    // 개발 환경에서는 로그만 출력
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"[SteamAchievementService] [개발모드] Achievement Unlocked: {achievementID}");
#endif
                    return true;
                }
                #else
                // 프로덕션에서만 실제 Steam API 호출
                bool success = SteamUserStats.SetAchievement(achievementID);
                if (success)
                {
                    bool storeResult = SteamUserStats.StoreStats();
                    if (storeResult)
                    {
                        Debug.Log($"[SteamAchievementService] Achievement Unlocked: {achievementID}");
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"[SteamAchievementService] Failed to store stats for achievement: {achievementID}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError($"[SteamAchievementService] Failed to unlock achievement: {achievementID}");
                    return false;
                }
                #endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SteamAchievementService] Exception while unlocking achievement {achievementID}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Steam에서 업적 상태 체크
        /// </summary>
        public bool IsSteamAchievementUnlocked(string achievementID)
        {
            if (!_steamInitialized || !SteamManager.Initialized || string.IsNullOrEmpty(achievementID))
            {
                return false;
            }

            if (!EnsureSteamStatsReady())
            {
                return false;
            }

            #if UNITY_EDITOR
            if (!_useRealSteamInEditor)
            {
                // 개발 환경에서는 Steam 체크 건너뛰기
                return false;
            }
            #endif

            try
            {
                bool achieved = false;
                bool success = SteamUserStats.GetAchievement(achievementID, out achieved);
                return success && achieved;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SteamAchievementService] Exception while checking achievement {achievementID}: {e.Message}");
                return false;
            }
        }
    }
} 