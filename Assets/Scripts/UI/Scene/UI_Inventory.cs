using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JustClimb.Manager;
using JustClimb.Items;

public class UI_Inventory : UI_Scene
{
    // 인스펙터에서 슬롯 아이콘 배열을 설정 (Feather, Wing, Lamp, Flag 순서)
    [Header("인스펙터에서 할당")]
    [Tooltip("아이템 슬롯 아이콘 배열 (Feather, Wing, Lamp, Flag 순서)")]
    public Image[] slotIcons;

    // 인스펙터에서 슬롯 개수 텍스트 배열을 설정
    [Tooltip("아이템 개수 텍스트 배열")]
    public TMP_Text[] slotCountTexts;

    // 인스펙터에서 슬롯 쿨타임 오버레이 이미지 배열을 설정 (Fill Method 사용)
    [Tooltip("쿨타임 오버레이 이미지 배열 (Fill Method 사용)")]
    public Image[] slotCooldownOverlays;

    [Header("버프 지속시간 오버레이")]
    public Image[] slotBuffOverlays;

    // UI에 표시할 아이템 ID 순서 정의
    private readonly ItemType[] _itemTypes =
        { ItemType.Feather, ItemType.Wing, ItemType.Lamp, ItemType.Flag };

    public override void Init()
    {
        base.Init();  // 부모 클래스(UI_Scene)의 Init 호출

        // ItemDatabase에서 아이콘만 로드
        var defs = Managers.Instance.ItemDB.GetAllItemDefinitions();
        for (int i = 0; i < _itemTypes.Length && i < slotIcons.Length; i++)
        {
            var type = _itemTypes[i];
            if (defs.TryGetValue(type, out var data))
                slotIcons[i].sprite = data.icon;
        }
    }

    private void OnEnable()
    {
        // 아이템 수량 변경 이벤트 구독
        Managers.Instance.Item.OnItemCountChanged += OnItemCountChanged;
            
        // 현재 상태를 즉시 반영
        for (int i = 0; i < _itemTypes.Length; i++)
        {
            var id = _itemTypes[i];
            slotCountTexts[i].text = Managers.Instance.Item.GetItemCount(id).ToString();
            UpdateCooldownOverlay(i, id);
            UpdateBuffOverlay(i, id);  // 버프 오버레이 초기 반영
        }
    }


    private void Update()
    {
        // 매 프레임마다 모든 슬롯의 쿨타임 오버레이를 갱신
        for (int i = 0; i < _itemTypes.Length; i++)
        {
            UpdateCooldownOverlay(i, _itemTypes[i]);
            UpdateBuffOverlay(i, _itemTypes[i]);
        }
    }

    // 슬롯 인덱스와 아이템 ID를 받아 해당 슬롯의 오버레이를 설정
    private void UpdateCooldownOverlay(int slotIndex, ItemType itemId)
    {
        // 남은 쿨타임 시간(초) 조회
        float remaining = Managers.Instance.Item.GetCooldownRemaining(itemId);
        // 총 쿨타임 길이(초) 조회
        float duration = Managers.Instance.Item.GetCooldownDuration(itemId);

        if (remaining > 0f)
        {
            // 남은 쿨타임이 있으면 오버레이를 활성화하고 fillAmount 설정
            slotCooldownOverlays[slotIndex].gameObject.SetActive(true);
            slotCooldownOverlays[slotIndex].fillAmount = Mathf.Clamp01(remaining / duration);
        }
        else
        {
            // 쿨타임이 끝났으면 오버레이 비활성화
            slotCooldownOverlays[slotIndex].gameObject.SetActive(false);
        }
    }

    // 슬롯 인덱스와 아이템 ID를 받아 해당 슬롯의 버프 오버레이를 설정
    private void UpdateBuffOverlay(int slotIndex, ItemType itemId)
    {
        float remaining = Managers.Instance.Item.GetBuffRemaining(itemId);
        float duration = Managers.Instance.Item.GetBuffDuration(itemId);

        if (remaining > 0f && duration > 0f)
        {
            slotBuffOverlays[slotIndex].gameObject.SetActive(true);
            slotBuffOverlays[slotIndex].fillAmount = Mathf.Clamp01(remaining / duration);
        }
        else
        {
            slotBuffOverlays[slotIndex].gameObject.SetActive(false);
        }
    }

    // ItemManager에서 수량 변경 이벤트가 발생할 때 호출됨
    private void OnItemCountChanged(ItemType itemId, int newCount)
    {
        // 변경된 아이템 ID에 해당하는 슬롯 인덱스를 찾아서 텍스트만 업데이트
        for (int i = 0; i < _itemTypes.Length; i++)
        {
            if (_itemTypes[i] == itemId)
            {
                slotCountTexts[i].text = newCount.ToString();
                break;  // 찾으면 루프 종료
            }
        }
    }

    private void OnDisable()
    {
        // 🔹 이벤트 구독 해제 (메모리 누수 방지)
        if (Managers.Instance.Item != null)
            Managers.Instance.Item.OnItemCountChanged -= OnItemCountChanged;
    }
}
