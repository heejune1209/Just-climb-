using UnityEngine;
using Zenject;
using JustClimb.Manager;
using JustClimb.Data;

namespace JustClimb.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("ProjectInstaller: InstallBindings 실행");
            
            // Configuration Layer - 가장 먼저 바인딩
            Container.Bind<ServerConfig>().FromScriptableObjectResource("ServerConfig").AsSingle().NonLazy();
            
            // UserId를 동적으로 관리하기 위한 UserIdProvider 바인딩
            Container.Bind<UserIdProvider>().AsSingle().NonLazy();
            Container.Bind<string>().WithId("UserId").FromMethod(GetUserId).AsSingle();
            
            // Persistence Layer - 가장 먼저 초기화 (MonoBehaviour이므로 FromNewComponentOnNewGameObject 사용)
            Container.BindInterfacesAndSelfTo<DataManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            // Infrastructure Layer - 다른 매니저들이 의존하는 기본 서비스들
            Container.BindInterfacesAndSelfTo<PoolManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            Container.Bind<IResourceManager>().To<ResourceManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ItemDatabase>().AsSingle().NonLazy();
            
            // Steam Auth Manager - Steam 연동을 위한 인증 매니저
            Container.BindInterfacesAndSelfTo<SteamAuthManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            // Domain Layer - DataManager 이후 초기화
            Container.BindInterfacesAndSelfTo<CurrencyManager>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<StageManager>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<RankingManager>().AsSingle().NonLazy();
            
            // MonoBehaviour 매니저들
            Container.BindInterfacesAndSelfTo<ItemManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<GameManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            Container.Bind<ISceneManagerEx>().To<SceneManagerEx>().AsSingle().NonLazy();
            
            // UI Layer - ResourceManager와 GameManager 이후 초기화
            Container.BindInterfacesAndSelfTo<UIManager>().AsSingle().NonLazy();
            
            // Audio Layer - 프리팹 기반 SoundManager
            Container.BindInterfacesAndSelfTo<SoundManager>().FromComponentInNewPrefabResource("Managers/SoundManager").AsSingle().NonLazy();
            
            // Data Sync Layer - DataManager 이후 초기화
            Container.BindInterfacesAndSelfTo<DataSyncManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();

            // OfflineCacheManager
            Container.BindInterfacesAndSelfTo<OfflineCacheManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<SaveManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            // Achievement System - Steam 업적 관리
            Container.BindInterfacesAndSelfTo<AchievementManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        }
        
        /// <summary>
        /// 동적으로 UserId를 가져오는 메서드
        /// </summary>
        private string GetUserId(InjectContext context)
        {
            var userIdProvider = context.Container.Resolve<UserIdProvider>();
            return userIdProvider.GetCurrentUserId();
        }
    }
    
    /// <summary>
    /// UserId를 동적으로 관리하는 Provider 클래스
    /// </summary>
    public class UserIdProvider
    {
        private string _currentUserId;
        private SteamAuthManager _steamAuthManager;
        
        public UserIdProvider()
        {
            _currentUserId = GenerateInitialUserId();
        }
        
        /// <summary>
        /// SteamAuthManager 의존성 주입 (늦은 바인딩)
        /// </summary>
        [Inject]
        public void Initialize(SteamAuthManager steamAuthManager)
        {
            _steamAuthManager = steamAuthManager;
            
            // Steam 인증 성공 시 UserId 업데이트
            _steamAuthManager.OnAuthenticationSuccess += OnSteamAuthSuccess;
        }
        
        /// <summary>
        /// 현재 사용자 ID 반환
        /// </summary>
        public string GetCurrentUserId()
        {
            return _currentUserId;
        }
        
        /// <summary>
        /// Steam 인증 성공 시 호출되는 콜백
        /// </summary>
        private void OnSteamAuthSuccess(string jwtToken)
        {
            if (_steamAuthManager != null && !string.IsNullOrEmpty(_steamAuthManager.SteamId))
            {
                string steamId = _steamAuthManager.SteamId;
                Debug.Log($"[UserIdProvider] Steam 인증 성공 - UserId 업데이트: {_currentUserId} → {steamId}");
                _currentUserId = steamId;
                
                // PlayerPrefs에도 저장
                PlayerPrefs.SetString("SteamUserId", steamId);
                PlayerPrefs.Save();
            }
        }
        
        /// <summary>
        /// 초기 UserId 생성
        /// </summary>
        private string GenerateInitialUserId()
        {
            // Steam ID가 있는 경우 Steam ID 사용 (이전 세션에서 저장된 것)
            string steamId = PlayerPrefs.GetString("SteamUserId", "");
            if (!string.IsNullOrEmpty(steamId))
            {
                Debug.Log($"[UserIdProvider] 저장된 Steam ID 사용: {steamId}");
                return steamId;
            }
            
            // PlayerPrefs에서 기존 임시 ID 확인
            string existingId = PlayerPrefs.GetString("TestUserId", "");
            if (!string.IsNullOrEmpty(existingId))
            {
                Debug.Log($"[UserIdProvider] 기존 임시 ID 사용: {existingId}");
                return existingId;
            }
            
            // 새 임시 ID 생성 (Steam 초기화 전)
            string newId = $"TestUser_{System.Guid.NewGuid().ToString("N")[..8]}";
            PlayerPrefs.SetString("TestUserId", newId);
            PlayerPrefs.Save();
            
            Debug.Log($"[UserIdProvider] 새 임시 사용자 ID 생성: {newId} (Steam 인증 후 Steam ID로 교체됨)");
            return newId;
        }
    }
} 