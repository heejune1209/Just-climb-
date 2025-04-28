using System.Collections;
using UnityEngine;
using DiasGames.Abilities; // Locomotion 네임스페이스가 여기라 가정

namespace JustClimb.Items
{
    [CreateAssetMenu(fileName = "FeatherUse", menuName = "Game/ItemUse/FeatherUse")]
    public class FeatherUse : ScriptableObject, IItemUse
    {
        [Header("Feather Buff Settings")]
        [Tooltip("이동 속도 배수")]
        public float speedMultiplier = 1.5f;

        [Tooltip("버프 지속 시간 (초)")]
        public float duration = 10f;

        [Header("Feather Effect Prefab")]
        [Tooltip("활성화할 깃털 이펙트 프리팹")]
        public GameObject featherEffectPrefab;

        // 아이템 사용 시 호출될 메서드
        // 버프를 받을 게임 오브젝트(플레이어)
        public void Use(GameObject user)
        {
            var loco = user.GetComponent<Locomotion>();
            if (loco != null)
            {
                // 코루틴 실행을 위한 MonoBehaviour 참조
                var mb = user.GetComponent<MonoBehaviour>();

                // 효과 이펙트 인스턴스 생성
                GameObject effectInstance = null;
                if (featherEffectPrefab != null)
                {
                    effectInstance = Instantiate(featherEffectPrefab, user.transform.position, Quaternion.identity);
                    effectInstance.transform.SetParent(user.transform);
                }

                // 버프와 이펙트 제거를 동시에 처리
                mb.StartCoroutine(ApplyFeatherBuff(loco, effectInstance));
            }
            else
            {
                Debug.LogWarning("FeatherUse: Locomotion 컴포넌트를 찾을 수 없습니다.");
            }
        }

        private IEnumerator ApplyFeatherBuff(Locomotion loco, GameObject effectInstance)
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

            // 이펙트가 생성되었으면 제거
            if (effectInstance != null)
                Destroy(effectInstance);
        }
    }
}