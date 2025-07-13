using System;
using System.Collections.Generic;
using UnityEngine;
using JustClimb.Items;
using JustClimb.Data;
using Zenject;

namespace JustClimb.Manager
{
    /// <summary>
    /// 아이템 정의·사용 로직·카운트 관리 전담.
    /// JSON 저장·로드는 DataManager, 도메인 로직은 여기서 처리.
    /// </summary>
    public class ItemManager : MonoBehaviour, IItemManager, IInitializable
    {
        // DI 주입받을 매니저들
        [Inject] private IDataManager _dataManager;
        [Inject] private IResourceManager _resourceManager;
        [Inject] private ItemDatabase _itemDatabase;
        [Inject] private DiContainer _container;

        // --- SO 기반 사용 로직 매핑 ---
        Dictionary<ItemType, IItemUse> _itemUseDict;

        // --- 버프·쿨다운 관리 ---
        Dictionary<ItemType, float> _buffEndTimes = new Dictionary<ItemType, float>();
        Dictionary<ItemType, float> _nextAvailableTime = new Dictionary<ItemType, float>();

        // 아이템 수량 변경 이벤트 (itemId, newCount)
        public event Action<ItemType, int> OnItemCountChanged;

        // 메모리 누수 방지를 위해 핸들러를 필드로 보관
        private Action<SaveData> _onDataLoadedHandler;
        /// <summary>
        /// Zenject에서 자동으로 호출됨.
        /// </summary>
        public void Initialize()
        {
            Init();
        }

        /// <summary>
        /// 실제 초기화 로직.
        /// </summary>
        public void Init()
        {
            // 1) SO 로직 로드
            LoadItemUses();

            // 1) DataManager.OnLoaded 핸들러 생성 & 구독
            _onDataLoadedHandler = save =>
            {
                foreach (var type in _itemDatabase.GetAllItemDefinitions().Keys)
                    OnItemCountChanged?.Invoke(type, GetItemCount(type));
            };
            _dataManager.OnLoaded += _onDataLoadedHandler;

            // 2) 초기 상태 발행
            foreach (var type in _itemDatabase.GetAllItemDefinitions().Keys)
                OnItemCountChanged?.Invoke(type, GetItemCount(type));
        }

        // =====================
        // IItemUse 로직 로드
        // =====================
        void LoadItemUses()
        {
            _itemUseDict = new Dictionary<ItemType, IItemUse>();
            var sos = _resourceManager.LoadAll<ScriptableObject>("Game/ItemUse");
            foreach (var so in sos)
            {
                // 컨테이너에 이 SO에도 [Inject] 수행해 달라고 요청
                _container.Inject(so);

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
                
        }

        // =====================
        // 공개 API
        // =====================

        // 현재 아이템 보유 개수 조회
        public int GetItemCount(ItemType itemId)
        {
            if (_dataManager.Current == null)
                return 0;
            return GetItemCountInternal(itemId);
        }

        // 아이템 획득
        public void AddItem(ItemType itemId, int amount = 1)
        {
            int newCount = GetItemCount(itemId) + amount;
            SetItemCountInternal(itemId, newCount);
            _dataManager.SaveLocal();
            OnItemCountChanged?.Invoke(itemId, newCount);

            // 서버 호환 아이템 리스트 델타 생성
            GenerateItemsDelta();
            
            // 업적 시스템에 아이템 구매 알림 (첫 획득 시에만)
            if (GetItemCount(itemId) == amount) // 처음 획득한 경우
            {
                AchievementIntegration.OnItemPurchased(itemId.ToString());
            }
        }

        // 아이템 사용 시도
        public bool UseItem(ItemType itemId, GameObject user)
        {
            var data = _itemDatabase.Get(itemId);

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

            // 업적 시스템에 아이템 사용 알림
            AchievementIntegration.OnItemUsed(itemId.ToString());

            return true;
        }

        // 아이템 제거
        public void RemoveItem(ItemType itemId, int amount = 1)
        {
            int newCount = Mathf.Max(0, GetItemCount(itemId) - amount);
            SetItemCountInternal(itemId, newCount);
            _dataManager.SaveLocal();
            OnItemCountChanged?.Invoke(itemId, newCount);

            // 서버 호환 아이템 리스트 델타 생성
            GenerateItemsDelta();
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
            return _itemDatabase.Get(itemId).buffDuration;
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
            return _itemDatabase.Get(itemId).cooldownDuration;
        }

        // 모든 아이템 ID 목록
        public IEnumerable<ItemType> GetAllItemIds()
        {
            return _itemDatabase.GetAllItemDefinitions().Keys;
        }

        // =====================
        // 내부 저장·로드 헬퍼
        // =====================

        /// <summary>
        /// DataManager.Current.items에서 아이템 개수 조회
        /// SaveData의 InventoryItemDto를 InventoryItem으로 변환하여 사용
        /// </summary>
        int GetItemCountInternal(ItemType itemId)
        {
            // DataManager.Current가 null이면 빈 리스트 취급
            var saveData = _dataManager.Current;
            if (saveData?.items == null)
                return 0;

            // InventoryItemDto에서 itemId는 문자열이므로 enum을 문자열로 변환하여 비교
            var itemDto = saveData.items.Find(x => x.itemId == itemId.ToString());
            return itemDto?.count ?? 0;
        }

        /// <summary>
        /// DataManager.Current.items 리스트에 아이템 수량 쓰기
        /// InventoryItemDto 형태로 저장
        /// </summary>
        void SetItemCountInternal(ItemType itemId, int count)
        {
            var saveData = _dataManager.Current;
            var list = saveData.items;              // List<InventoryItemDto> 사용
            string itemIdStr = itemId.ToString();   // enum을 문자열로 변환
            
            int idx = list.FindIndex(x => x.itemId == itemIdStr);

            if (idx < 0)
            {
                // 새로 추가
                if (count > 0)
                    list.Add(new InventoryItemDto(itemIdStr, count));
            }
            else
            {
                if (count > 0)
                    list[idx].count = count;                   // 기존 개수 업데이트
                else
                    list.RemoveAt(idx);                        // 수량 0 → 제거
            }
        }

        /// <summary>
        /// 서버 호환 아이템 델타 생성
        /// 이미 InventoryItemDto 형태이므로 그대로 전송
        /// </summary>
        private void GenerateItemsDelta()
        {
            var items = _dataManager.Current.items; // 이미 InventoryItemDto 리스트
            _dataManager.GenerateDelta("items", items);
        }

        // 메모리 누수 방지: MonoBehaviour OnDestroy 에서 이벤트 해제
        private void OnDestroy()
        {
            if (_dataManager != null && _onDataLoadedHandler != null)
                _dataManager.OnLoaded -= _onDataLoadedHandler;

            // 외부 구독자들이 남아있을 수 있으니 이벤트 자체를 초기화
            OnItemCountChanged = null;
        }
    }
}
