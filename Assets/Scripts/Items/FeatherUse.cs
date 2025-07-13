using DiasGames.Abilities; // Locomotion 네임스페이스가 여기라 가정
using System.Collections;
using UnityEngine;
using Zenject;

namespace JustClimb.Items
{
    [CreateAssetMenu(fileName = "FeatherUse", menuName = "Game/ItemUse/FeatherUse")]
    public class FeatherUse : ScriptableObject, IItemUse
    {
        [Inject] private IResourceManager _resourceManager;
        [Inject] private ISoundManager _soundmanager;

        [Header("Feather Buff Settings")]
        [Tooltip("이동 속도 배수")]
        public float speedMultiplier = 1.5f;

        [Header("Item Data")]
        [Tooltip("지속시간 등 메타데이터를 가진 SO를 할당")]
        public ItemData data;

        [Header("Feather Effect Prefab")]
        [Tooltip("활성화할 깃털 이펙트 프리팹")]
        public GameObject featherEffectPrefab;

        // 아이템 사용 시 호출될 메서드
        // 버프를 받을 게임 오브젝트(플레이어)
        public void Use(GameObject user)
        {
            var loco = user.GetComponent<Locomotion>();
            if (loco == null)
            {
                Debug.LogWarning("FeatherUse: Locomotion 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            // 지속시간을 SO에서 가져옴
            float duration = data.buffDuration;
            if (duration <= 0f)
            {
                Debug.LogWarning($"FeatherUse: data.buffDuration이 0 이하입니다. ({data.buffDuration})");
                return;
            }

            // MonoBehaviour 코루틴 실행용
            var mb = user.GetComponent<MonoBehaviour>();
            if (mb == null)
            {
                Debug.LogWarning("FeatherUse: 코루틴 실행을 위한 MonoBehaviour를 찾을 수 없습니다.");
                return;
            }

            // 이펙트 인스턴스화
            GameObject effectInstance = null;
            if (featherEffectPrefab != null)
            {
                effectInstance = _resourceManager.Instantiate(
                    $"Prefabs/Items/{featherEffectPrefab.name}",
                    user.transform.position,
                    Quaternion.identity, null, data._initialpoolcount
                );
                
                if (effectInstance != null)
                {
                effectInstance.transform.SetParent(user.transform);
                _soundmanager.PlaySFX(2);
                }
                else
                {
                    Debug.LogWarning($"[FeatherUse] 이펙트 인스턴스화에 실패했습니다: {featherEffectPrefab.name}");
                }
            }

            // 버프 적용과 종료 처리를 코루틴으로
            mb.StartCoroutine(ApplyFeatherBuff(loco, effectInstance, duration));
        }


        private IEnumerator ApplyFeatherBuff(Locomotion loco, GameObject effectInstance, float duration)
        {
            // 원래 속도 저장
            float originalWalk = loco.WalkSpeed;
            float originalSprint = loco.SprintSpeed;

            // 속도 버프 적용
            loco.WalkSpeed = originalWalk * speedMultiplier;
            loco.SprintSpeed = originalSprint * speedMultiplier;

            // 버프 지속 시간 대기
            yield return new WaitForSeconds(duration);

            // 원래 속도로 복원
            loco.WalkSpeed = originalWalk;
            loco.SprintSpeed = originalSprint;

            // 이펙트 제거
            if (effectInstance != null)
                Destroy(effectInstance);
        }
    }
}