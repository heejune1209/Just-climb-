// CurrencyManager.cs
using System;
using JustClimb.Manager;

namespace JustClimb.Manager
{
    /// <summary>
    /// 재화(골드,보석) 관리 전담.
    /// DataManager의 이벤트를 구독하고, UI나 다른 로직에 OnGoldChanged/OnGemsChanged만 노출.
    /// </summary>
    public class CurrencyManager
    {
        public event Action<int> OnGoldChanged;
        public event Action<int> OnGemsChanged;

        int _gold;
        int _gems;

        /// <summary>
        /// Managers.Awake()에서 호출.
        /// </summary>
        public void Init()
        {
            var data = Managers.Instance.Data;

            // 1) 데이터 로드 직후(OnLoaded)와 저장 직후(OnSaved)에 값 갱신
            data.OnLoaded += UpdateCurrencies;
            data.OnSaved += UpdateCurrencies;

            // 2) 현재 로드된 값으로 초기 발행
            UpdateCurrencies(data.Current);
        }

        /// <summary>
        /// DataManager.Current를 보고 _gold/_gems 갱신 및 이벤트 발행
        /// </summary>
        void UpdateCurrencies(SaveData save)
        {
            _gold = save.gold;
            OnGoldChanged?.Invoke(_gold);

            _gems = save.gems;
            OnGemsChanged?.Invoke(_gems);
        }

        // 외부 API
        public int GetGold() { return _gold; }
        public int GetGems() { return _gems; }

        // 골드 추가. DataManager.Current.gold 변경 후 Save().
        public void AddGold(int amount)
        {
            var data = Managers.Instance.Data;
            data.Current.gold += amount;
            data.Save();
        }

        // 골드 사용. 충분히 있으면 차감 후 Save() 하고 true, 아니면 false.
        public bool SpendGold(int amount)
        {
            if (_gold < amount)
                return false;

            var data = Managers.Instance.Data;
            data.Current.gold -= amount;
            data.Save();
            return true;
        }

        // 젬 추가. DataManager.Current.gems 변경 후 Save().
        public void AddGems(int amount)
        {
            var data = Managers.Instance.Data;
            data.Current.gems += amount;
            data.Save();
        }

        // 젬 사용. 충분히 있으면 차감 후 Save() 하고 true, 아니면 false.
        public bool SpendGems(int amount)
        {
            if (_gems < amount)
                return false;

            var data = Managers.Instance.Data;
            data.Current.gems -= amount;
            data.Save();
            return true;
        }
    }
}
