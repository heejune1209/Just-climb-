using JustClimb.Items;
using JustClimb.Manager;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace JustClimb.Input
{
    // 키 입력(1~4)으로 아이템 사용을 시도합니다.
    public class ItemInput : MonoBehaviour
    {
        // DI 주입받을 매니저
        [Inject] private IItemManager _itemManager;

        [Tooltip("아이템 사용 시 대상(대개 플레이어) 게임오브젝트")]
        [SerializeField] private GameObject player;

        void Update()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                TryUse(ItemType.Feather);
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                TryUse(ItemType.Wing);
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                TryUse(ItemType.Lamp);
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                TryUse(ItemType.Flag);
        }

        private void TryUse(ItemType itemType)
        {
            Debug.Log($"[ItemInput] {itemType} 키 눌림, player = {player.name}");

            // ItemManager를 통해 사용 시도
            bool used = _itemManager.UseItem(itemType, player);

            if (used)
            {
                // 사용 성공 시, ItemManager에서 남은 개수 조회
                int remaining = _itemManager.GetItemCount(itemType);
                Debug.Log($"[ItemInput] {itemType} 사용 성공. 남은 개수: {remaining}");
            }
            else
            {
                // 사용 실패 (미보유 또는 쿨타임)
                Debug.Log($"[ItemInput] {itemType} 사용 실패 (미보유 혹은 쿨타임 중)");
            }
        }

        // 메모리 누수 방지
        private void OnDestroy()
        {
            // 컴포넌트 참조 해제
            player = null;
            
            // 매니저 참조 해제
            _itemManager = null;
        }
    }
}
