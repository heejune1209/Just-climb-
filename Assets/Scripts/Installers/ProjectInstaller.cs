using UnityEngine;
using Zenject;
using JustClimb.Manager;

namespace JustClimb.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("ProjectInstaller: InstallBindings 실행");
            
            // UserId 바인딩 - 테스트용으로 임시 ID 생성 (실제로는 Steam ID나 사용자 계정 시스템에서 가져와야 함)
            var userId = GenerateUserId();
            Container.Bind<string>().WithId("UserId").FromInstance(userId).AsSingle();
            Debug.Log($"ProjectInstaller: UserId 바인딩 완료 - {userId}");
            
            // Persistence Layer - 가장 먼저 초기화
            Container.BindInterfacesAndSelfTo<DataManager>().AsSingle().NonLazy();
            
            // Infrastructure Layer - 다른 매니저들이 의존하는 기본 서비스들
            Container.BindInterfacesAndSelfTo<PoolManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            
            Container.Bind<IResourceManager>().To<ResourceManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ItemDatabase>().AsSingle().NonLazy();
            
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
        }
        
        /// <summary>
        /// 테스트용 사용자 ID 생성 - 실제 프로덕션에서는 Steam ID나 계정 시스템에서 가져와야 함
        /// </summary>
        private string GenerateUserId()
        {
            // PlayerPrefs에서 기존 ID 확인
            string existingId = PlayerPrefs.GetString("TestUserId", "");
            if (!string.IsNullOrEmpty(existingId))
            {
                return existingId;
            }
            
            // 새 ID 생성 (테스트용)
            string newId = $"TestUser_{System.Guid.NewGuid().ToString("N")[..8]}";
            PlayerPrefs.SetString("TestUserId", newId);
            PlayerPrefs.Save();
            
            Debug.Log($"새 테스트 사용자 ID 생성: {newId}");
            return newId;
        }
    }
} 