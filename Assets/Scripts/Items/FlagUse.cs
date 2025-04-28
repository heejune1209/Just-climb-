using UnityEngine;

namespace JustClimb.Items
{
    /// <summary>
    /// 체크포인트 깃발 아이템 사용 시 현재 위치를 저장하거나 저장된 위치로 복귀시키는 클래스
    /// </summary>
    [CreateAssetMenu(fileName = "FlagUse", menuName = "Game/ItemUse/FlagUse", order = 100)]
    public class FlagUse : ScriptableObject, IItemUse
    {
        [Header("깃발 이펙트 Prefab")]
        [Tooltip("위치 저장 시 소환할 깃발 프리팹")]
        public GameObject flagPrefab;

        // PlayerPrefs에 저장할 키
        private const string KeyX = "FlagX";
        private const string KeyY = "FlagY";
        private const string KeyZ = "FlagZ";

        /// <summary>
        /// IItemUse 인터페이스 구현: 아이템 사용 시 호출됩니다.
        /// 저장된 위치가 없으면 현재 위치를 저장하고 깃발 이펙트를 소환,
        /// 이미 저장된 위치가 있으면 해당 위치로 즉시 복귀시킵니다.
        /// </summary>
        /// <param name="user">아이템을 사용하는 GameObject (플레이어)</param>
        public void Use(GameObject user)
        {
            if (user == null)
            {
                Debug.LogWarning("FlagUse.Use 호출 시 user가 null 입니다.");
                return;
            }

            // 저장된 위치가 있는지 확인
            if (!PlayerPrefs.HasKey(KeyX) || !PlayerPrefs.HasKey(KeyY) || !PlayerPrefs.HasKey(KeyZ))
            {
                // 현재 위치 저장
                Vector3 pos = user.transform.position;
                PlayerPrefs.SetFloat(KeyX, pos.x);
                PlayerPrefs.SetFloat(KeyY, pos.y);
                PlayerPrefs.SetFloat(KeyZ, pos.z);
                PlayerPrefs.Save();

                // 깃발 이펙트 소환
                if (flagPrefab != null)
                {
                    Instantiate(flagPrefab, pos, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("FlagUse: flagPrefab이 설정되지 않았습니다.");
                }

                Debug.Log($"FlagUse: 위치 저장 완료 ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
            }
            else
            {
                // 저장된 위치 불러오기
                float x = PlayerPrefs.GetFloat(KeyX);
                float y = PlayerPrefs.GetFloat(KeyY);
                float z = PlayerPrefs.GetFloat(KeyZ);
                Vector3 savedPos = new Vector3(x, y, z);

                // 플레이어 위치 복귀
                user.transform.position = savedPos;
                Debug.Log($"FlagUse: 저장된 위치로 복귀 ({savedPos.x:F2}, {savedPos.y:F2}, {savedPos.z:F2})");
            }
        }
    }
}
