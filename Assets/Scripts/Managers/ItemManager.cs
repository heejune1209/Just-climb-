using System;
using System.Collections.Generic;
using UnityEngine;
using JustClimb.Items;

namespace JustClimb.Manager
{
    /// <summary>
    /// 아이템 정의·사용 로직·카운트 관리 전담.
    /// JSON 저장·로드는 DataManager, 도메인 로직은 여기서 처리.
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        // --- SO 기반 사용 로직 매핑 ---
        Dictionary<ItemType, IItemUse> _itemUseDict;

        // --- 버프·쿨다운 관리 ---
        Dictionary<ItemType, float> _buffEndTimes = new Dictionary<ItemType, float>();
        Dictionary<ItemType, float> _nextAvailableTime = new Dictionary<ItemType, float>();

        // 아이템 수량 변경 이벤트 (itemId, newCount)
        public event Action<ItemType, int> OnItemCountChanged;

        /// <summary>
        /// Managers.Awake()에서 호출합니다.
        /// </summary>
        public void Init()
        {
            // 1) SO 로직 로드
            LoadItemUses();

            // 2) 초기 상태 발행
            foreach (var type in Managers.Instance.ItemDB.GetAllItemDefinitions().Keys)
                OnItemCountChanged?.Invoke(type, GetItemCount(type));
        }

        // =====================
        // IItemUse 로직 로드
        // =====================
        void LoadItemUses()
        {
            _itemUseDict = new Dictionary<ItemType, IItemUse>();
            var sos = Managers.Instance.Resource.LoadAll<ScriptableObject>("Game/ItemUse");
            foreach (var so in sos)
                if (so is IItemUse logic)
                {
                    var name = so.name.Replace("Use", "");
                    if (Enum.TryParse<ItemType>(name, out var type)
                        && !_itemUseDict.ContainsKey(type))
                    {
                        _itemUseDict.Add(type, logic);
                    }
                }
        }

        // =====================
        // 공개 API
        // =====================

        // 현재 아이템 보유 개수 조회
        public int GetItemCount(ItemType itemId)
        {
            return GetItemCountInternal(itemId);
        }

        // 아이템 획득
        public void AddItem(ItemType itemId, int amount = 1)
        {
            int newCount = GetItemCount(itemId) + amount;
            SetItemCountInternal(itemId, newCount);
            Managers.Instance.Data.Save();
            OnItemCountChanged?.Invoke(itemId, newCount);
        }

        // 아이템 사용 시도
        public bool UseItem(ItemType itemId, GameObject user)
        {
            var data = Managers.Instance.ItemDB.Get(itemId);

            // 쿨다운 체크
            if (_nextAvailableTime.TryGetValue(itemId, out var ready)
                && Time.time < ready)
                return false;

            // 소지 & 사용 로직
            if (GetItemCount(itemId) <= 0
                || !_itemUseDict.TryGetValue(itemId, out var logic))
                return false;

            logic.Use(user);  // LampUse, WingUse, FeatherUse, FlagUse 등 

            // 개수 차감 & 이벤트
            RemoveItem(itemId, 1);

            // 버프 적용 기록
            if (data.buffDuration > 0f)
                _buffEndTimes[itemId] = Time.time + data.buffDuration;

            // 쿨다운 기록
            if (data.cooldownDuration > 0f)
                _nextAvailableTime[itemId] = Time.time + data.cooldownDuration;

            return true;
        }

        // 아이템 제거
        public void RemoveItem(ItemType itemId, int amount = 1)
        {
            int newCount = Mathf.Max(0, GetItemCount(itemId) - amount);
            SetItemCountInternal(itemId, newCount);
            Managers.Instance.Data.Save();
            OnItemCountChanged?.Invoke(itemId, newCount);
        }

        // 버프 남은 시간 조회
        public float GetBuffRemaining(ItemType itemId)
        {
            return _buffEndTimes.TryGetValue(itemId, out var end)
               ? Mathf.Max(0f, end - Time.time) : 0f;
        }

        // 버프 총 지속 시간 조회
        public float GetBuffDuration(ItemType itemId)
        {
            return Managers.Instance.ItemDB.Get(itemId).buffDuration;
        }

        // 쿨다운 남은 시간 조회
        public float GetCooldownRemaining(ItemType itemId)
        {
            return _nextAvailableTime.TryGetValue(itemId, out var cd)
               ? Mathf.Max(0f, cd - Time.time) : 0f;
        }

        // 총 쿨다운 길이 조회
        public float GetCooldownDuration(ItemType itemId)
        {
            return Managers.Instance.ItemDB.Get(itemId).cooldownDuration;
        }

        // 모든 아이템 ID 목록
        public IEnumerable<ItemType> GetAllItemIds()
        {
            return Managers.Instance.ItemDB.GetAllItemDefinitions().Keys;
        }

        // =====================
        // 내부 저장·로드 헬퍼
        // =====================

        // DataManager.Current.items에서 해당 아이템 개수 읽기
        int GetItemCountInternal(ItemType itemId)
        {
            var arr = Managers.Instance.Data.Current.items;
            var inv = Array.Find(arr, x => x.itemId == itemId);
            return inv != null ? inv.count : 0;
        }

        // DataManager.Current.items에 해당 아이템 개수 쓰기
        void SetItemCountInternal(ItemType itemId, int count)
        {
            var dm = Managers.Instance.Data;
            var list = new List<InventoryItem>(dm.Current.items);
            int idx = list.FindIndex(x => x.itemId == itemId);
            if (idx < 0)
            {
                if (count > 0)
                    list.Add(new InventoryItem(itemId, count));
            }
            else
            {
                if (count > 0) list[idx].count = count;
                else list.RemoveAt(idx);
            }

            dm.Current.items = list.ToArray();
        }
    }
}
