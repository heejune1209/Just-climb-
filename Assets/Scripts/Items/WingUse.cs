using UnityEngine;
using System.Collections;
using DiasGames.Abilities;

namespace JustClimb.Items
{
    [CreateAssetMenu(fileName = "WingUse", menuName = "Game/ItemUse/WingUse", order = 100)]
    public class WingUse : ScriptableObject, IItemUse
    {
        [Header("날개 사용 설정")]
        [SerializeField] private float _boostMultiplier = 1.5f;

        [Header("Wing Effect Prefab")]
        [Tooltip("활성화할 날개 이펙트 프리팹")]
        [SerializeField] private GameObject _wingEffectPrefab;

        [Header("Item Data (buffDuration 포함)")]
        [Tooltip("지속시간 등 메타데이터를 가진 SO를 할당")]
        public ItemData data;

        /// <summary>
        /// IItemUse 인터페이스 구현: 아이템 사용 시 호출됩니다.
        /// </summary>
        /// <param name="user">아이템을 사용하는 GameObject (플레이어)</param>
        public void Use(GameObject user)
        {
            if (user == null)
            {
                Debug.LogWarning("WingUse.Use 호출 시 user가 null 입니다.");
                return;
            }

            // 점프 부스트 능력 적용
            var ability = user.GetComponent<AirControlAbility>();
            if (ability != null)
            {
                // 지속시간을 SO에서 가져옴
                float duration = data.buffDuration;
                if (duration <= 0f)
                {
                    Debug.LogWarning($"WingUse: data.buffDuration이 0 이하입니다. ({duration})");
                    return;
                }

                ability.UseJumpBoost(_boostMultiplier, duration);

                // 사용 이펙트 생성
                GameObject effectInstance = null;
                if (_wingEffectPrefab != null)
                {
                    effectInstance = Instantiate(_wingEffectPrefab, user.transform.position, Quaternion.identity);
                    effectInstance.transform.SetParent(user.transform);
                }

                // 이펙트 제거를 위한 코루틴 실행
                var mb = user.GetComponent<MonoBehaviour>();
                if (mb != null && effectInstance != null)
                {
                    mb.StartCoroutine(RemoveEffect(effectInstance, duration));
                }
                else if (effectInstance != null)
                {
                    Debug.LogWarning("WingUse: MonoBehaviour를 찾을 수 없어 이펙트 제거 코루틴을 시작하지 못했습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"WingUse: {user.name}에 AirControlAbility 컴포넌트가 없습니다.");
            }
        }

        // 일정 시간 후 사용 이펙트를 제거하는 코루틴
        private IEnumerator RemoveEffect(GameObject effect, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (effect != null)
                Destroy(effect);
        }
    }
}