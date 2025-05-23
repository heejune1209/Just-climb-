using UnityEngine;
using JustClimb.Manager;

namespace JustClimb.Items
{
    [CreateAssetMenu(fileName = "FlagUse", menuName = "Game/ItemUse/FlagUse")]
    public class FlagUse : ScriptableObject, IItemUse
    {
        [Header("깃발 이펙트 Prefab")]
        public GameObject flagPrefab;

        public void Use(GameObject user)
        {
            if (user == null)
            {
                Debug.LogWarning("FlagUse.Use 호출 시 user가 null 입니다.");
                return;
            }

            // 현재 위치를 바로 체크포인트로 저장
            Vector3 savePos = user.transform.position;
            Managers.Instance.Game.SaveFlagPosition(savePos);

            // 이펙트는 플레이어 머리 위에 띄우기 (옵션)
            if (flagPrefab != null)
            {
                Vector3 effectPos = savePos + Vector3.forward * 2f;
                Instantiate(flagPrefab, effectPos, Quaternion.identity);
            }
        }
    }
}
