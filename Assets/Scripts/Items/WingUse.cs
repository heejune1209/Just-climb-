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
        [SerializeField] private float _duration = 10f;

        [Header("Wing Effect Prefab")]
        [Tooltip("활성화할 날개 이펙트 프리팹")]
        [SerializeField] private GameObject _wingEffectPrefab;

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
                ability.UseJumpBoost(_boostMultiplier, _duration);

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
                    mb.StartCoroutine(RemoveEffect(effectInstance));
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

        /// <summary>
        /// 일정 시간 후 사용 이펙트를 제거하는 코루틴
        /// </summary>
        private IEnumerator RemoveEffect(GameObject effect)
        {
            yield return new WaitForSeconds(_duration);
            if (effect != null)
                Destroy(effect);
        }
    }
}