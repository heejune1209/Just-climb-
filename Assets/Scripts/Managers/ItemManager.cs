using System;
using UnityEngine;
using System.Collections.Generic;
using JustClimb.Items;

namespace JustClimb.Manager
{
    /// <summary>
    /// 아이템 정의·사용 로직·카운트 관리와
    /// 보석·골드 같은 재화 관리까지 담당하는 싱글톤 매니저
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        // Lazy 싱글톤
        private static ItemManager _instance;
        public static ItemManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var existing = FindObjectOfType<ItemManager>();
                    if (existing != null)
                        _instance = existing;
                    else
                    {
                        var go = new GameObject { name = "@ItemManager" };
                        _instance = go.AddComponent<ItemManager>();
                    }
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        // --- 아이템 관리용 ---
        Dictionary<string, ItemData> _itemDataDict;
        Dictionary<string, IItemUse> _itemUseDict;
        Dictionary<string, int> _itemCountDict;

        // 아이템 수량 변경 시 발행 (itemId, newCount)
        public event Action<string, int> OnItemCountChanged;

        // --- 재화 관리용 ---
        int _gems;
        int _gold;

        /// <summary>재화 변경 시 발행 (\"Gem\" 또는 \"Gold\", newCount)</summary>
        public event Action<string, int> OnCurrencyChanged;

        // ItemManager 필드에 추가
        private Dictionary<string, float> _cooldownDurations = new Dictionary<string, float>
        {
            {"Feather",  10f},  // 10초
            {"Wing",     10f},  // 10초
            {"Lamp",     20f},  // 20초
            {"Flag",    120f},  // 2분 = 120초
        };
        // 다음 사용 가능 시간 기록
        private Dictionary<string, float> _nextAvailableTime = new Dictionary<string, float>();

        void Awake()
        {
            // 중복 생성 방지
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 1) 아이템 로드
            LoadItemDefinitions();
            LoadItemUses();
            LoadItemCounts();

            // 2) 재화 로드
            LoadCurrency();
        }

        // =====================
        // 아이템 데이터 로드
        // =====================
        void LoadItemDefinitions()
        {
            _itemDataDict = new Dictionary<string, ItemData>();
            var datas = Managers.Resource.LoadAll<ItemData>("ScriptableObjects/Items");
            foreach (var d in datas)
                if (!string.IsNullOrEmpty(d.itemId) && !_itemDataDict.ContainsKey(d.itemId))
                    _itemDataDict.Add(d.itemId, d);
        }

        // 아이템 사용 로직 로드
        void LoadItemUses()
        {
            _itemUseDict = new Dictionary<string, IItemUse>();
            var sos = Managers.Resource.LoadAll<ScriptableObject>("Game/ItemUse");
            foreach (var so in sos)
                if (so is IItemUse useLogic)
                {
                    var key = so.name.Replace("Use", "");
                    if (!_itemUseDict.ContainsKey(key))
                        _itemUseDict.Add(key, useLogic);
                }
        }

        // 저장된 수량 불러오기 & 초기 이벤트 발행
        void LoadItemCounts()
{
    _itemCountDict = new Dictionary<string, int>();
    foreach (var kv in _itemDataDict)
    {
        // defaultCount 대신 0을 기본값으로 사용
        int saved = PlayerPrefs.GetInt(kv.Key, 0);
        _itemCountDict.Add(kv.Key, saved);
    }
    // 구독자에게 초기 상태 알림
    foreach (var kv in _itemCountDict)
        OnItemCountChanged?.Invoke(kv.Key, kv.Value);
}


        // 재화 데이터 로드
        void LoadCurrency()
        {
            _gems = PlayerPrefs.GetInt("Gem", 0);
            _gold = PlayerPrefs.GetInt("Gold", 0);

            // 초기 이벤트
            OnCurrencyChanged?.Invoke("Gem", _gems);
            OnCurrencyChanged?.Invoke("Gold", _gold);
        }

        // =====================
        // 공개 API

        // 아이템 수량 조회
        public int GetItemCount(string itemId)
        {
            return _itemCountDict.TryGetValue(itemId, out var c) ? c : 0;
        }

        // 아이템 보유 여부
        public bool HasItem(string itemId)
        {
            return _itemCountDict.TryGetValue(itemId, out var c) && (c > 0);
        }

        // 아이템 획득
        public void AddItem(string itemId, int amount = 1)
        {
            if (!_itemCountDict.ContainsKey(itemId))
                _itemCountDict[itemId] = 0;
            _itemCountDict[itemId] += amount;
            SaveItemCount(itemId);
        }

        // 아이템 사용
        public bool UseItem(string itemId, GameObject user)
        {
            // ❶ 쿨타임 남았으면 사용 불가
            if (_nextAvailableTime.TryGetValue(itemId, out var ready) && Time.time < ready)
                return false;

            // ❷ 기존 사용 로직 실행
            if (!HasItem(itemId) || !_itemUseDict.TryGetValue(itemId, out var logic))
                return false;

            logic.Use(user);
            RemoveItem(itemId, 1);

            // ❸ 쿨타임 시작
            if (_cooldownDurations.TryGetValue(itemId, out var cd))
                _nextAvailableTime[itemId] = Time.time + cd;

            return true;
        }

        // 아이템 제거
        public void RemoveItem(string itemId, int amount = 1)
        {
            if (!_itemCountDict.ContainsKey(itemId))
                return;
            _itemCountDict[itemId] = Mathf.Max(0, _itemCountDict[itemId] - amount);
            SaveItemCount(itemId);
        }

        // 보석 조회
        public int GetGems() => _gems;
        // 골드 조회
        public int GetGold() => _gold;

        // 보석 획득
        public void AddGems(int amount)
        {
            _gems += amount;
            SaveCurrency("Gem", _gems);
        }

        // 골드 획득
        public void AddGold(int amount)
        {
            _gold += amount;
            SaveCurrency("Gold", _gold);
        }

        // 골드 사용 시도
        public bool SpendGold(int amount)
        {
            if (_gold < amount) return false;
            _gold -= amount;
            SaveCurrency("Gold", _gold);
            return true;
        }

        // =====================
        // 내부 저장 & 이벤트

        void SaveItemCount(string itemId)
        {
            PlayerPrefs.SetInt(itemId, _itemCountDict[itemId]);
            PlayerPrefs.Save();
            OnItemCountChanged?.Invoke(itemId, _itemCountDict[itemId]);
        }

        void SaveCurrency(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            OnCurrencyChanged?.Invoke(key, value);
        }

        // 남은 쿨타임(초)
        public float GetCooldownRemaining(string itemId)
        {
            if (_nextAvailableTime.TryGetValue(itemId, out var ready))
                return Mathf.Max(0f, ready - Time.time);
            return 0f;
        }

        // 총 쿨타임 길이(초)
        public float GetCooldownDuration(string itemId)
        {
            if (_cooldownDurations.TryGetValue(itemId, out var cd))
                return cd;
            return 0f;
        }

        // 모든 아이템 ID 목록
        public IEnumerable<string> GetAllItemIds()
        {
            return _itemDataDict.Keys;
        }
        // 또는 ScriptableObject 정보까지 필요하면
        public Dictionary<string, ItemData> GetAllItemDefinitions()
        {
            // 복사해서 반환
            return new Dictionary<string, ItemData>(_itemDataDict);
        }
    }
}
