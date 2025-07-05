using UnityEngine;
using JustClimb.Manager;
using Zenject;

namespace JustClimb.Items
{
    [CreateAssetMenu(fileName = "FlagUse", menuName = "Game/ItemUse/FlagUse")]
    public class FlagUse : ScriptableObject, IItemUse
    {
        [Header("깃발 이펙트 Prefab")]
        public GameObject flagPrefab;

        [Header("Item Data")]
        [Tooltip("지속시간 등 메타데이터를 가진 SO를 할당")]
        public ItemData data;

        [Inject] private IResourceManager _rm;
        [Inject] private ISoundManager _sm;

        public void Use(GameObject user)
        {
            if (user == null)
            {
                Debug.LogWarning("FlagUse.Use 호출 시 user가 null 입니다.");
                return;
            }

            // ScriptableObject는 DI를 직접 지원하지 않으므로 
            // ProjectContext에서 GameManager를 찾아서 사용
            var gameManager = ProjectContext.Instance.Container.Resolve<IGameManager>();

            // 현재 위치를 바로 체크포인트로 저장
            Vector3 savePos = user.transform.position;
            gameManager.SaveFlagPosition(savePos);

            // 이펙트는 플레이어 머리 위에 띄우기 (옵션)
            if (flagPrefab != null)
            {
                Vector3 effectPos = savePos + Vector3.forward * 2f;
                _rm.Instantiate($"Prefabs/Items/{flagPrefab.name}", 
                    effectPos, Quaternion.identity, null, data._initialpoolcount);
                _sm.PlaySFX(4);
            }
        }
    }
}
