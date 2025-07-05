// CurrencyManager.cs
using System;
using JustClimb.Manager;
using JustClimb.Data;
using Zenject;

namespace JustClimb.Manager
{
    /// <summary>
    /// 재화(골드,보석) 관리 전담.
    /// DataManager의 이벤트를 구독하고, UI나 다른 로직에 OnGoldChanged/OnGemsChanged만 노출.
    /// </summary>
    public class CurrencyManager : ICurrencyManager, IInitializable, IDisposable
    {
        public event Action<int> OnGoldChanged;
        public event Action<int> OnGemsChanged;

        /// <summary>현재 골드</summary>
        public int Gold { get; private set; }
        /// <summary>현재 보석</summary>
        public int Gems { get; private set; }

        private readonly IDataManager _dataManager;

        // 생성자 주입
        public CurrencyManager(IDataManager dataManager)
        {
            _dataManager = dataManager;
        }

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
            // 1) 데이터 로드 직후(OnLoaded)와 저장 직후(OnSaved)에 값 갱신
            _dataManager.OnLoaded += UpdateCurrencies;
            _dataManager.OnSaved += UpdateCurrencies;

            // Current가 세팅된 경우에만 최초 갱신
            if (_dataManager.Current != null)
                UpdateCurrencies(_dataManager.Current);

            // 2) 로컬 캐시 → (온라인 시 서버 GET) 흐름 트리거
            _dataManager.Load();
        }

        /// <summary>
        /// DataManager.Current를 보고 _gold/_gems 갱신 및 이벤트 발행
        /// </summary>
        void UpdateCurrencies(SaveData save)
        {
            Gold = save.gold;
            OnGoldChanged?.Invoke(Gold);

            Gems = save.gems;
            OnGemsChanged?.Invoke(Gems);
        }

        // 외부 API
        public int GetGold() { return Gold; }
        public int GetGems() { return Gems; }

        // 골드 추가. DataManager.Current.gold 변경 후 Save().
        public void AddGold(int amount)
        {
            _dataManager.Current.gold += amount;
            // 로컬 JSON 저장만 (풀-덤프 델타는 발생하지 않음)
            _dataManager.SaveLocal();

            // 필드 단위 델타 생성 (키: "gold", 값: 변경된 골드)
            _dataManager.GenerateDelta("gold", _dataManager.Current.gold);
        }

        // 골드 사용. 충분히 있으면 차감 후 Save() 하고 true, 아니면 false.
        public bool SpendGold(int amount)
        {
            if (Gold < amount)
                return false;

            _dataManager.Current.gold -= amount;
            _dataManager.SaveLocal();

            _dataManager.GenerateDelta("gold", _dataManager.Current.gold);
            return true;
        }

        // 젬 추가. DataManager.Current.gems 변경 후 Save().
        public void AddGems(int amount)
        {
            _dataManager.Current.gems += amount;
            _dataManager.SaveLocal();

            // 필드 단위 델타 생성 (키: "gems", 값: 변경된 젬)
            _dataManager.GenerateDelta("gems", _dataManager.Current.gems);
        }

        // 젬 사용. 충분히 있으면 차감 후 Save() 하고 true, 아니면 false.
        public bool SpendGems(int amount)
        {
            if (Gems < amount)
                return false;

            _dataManager.Current.gems -= amount;
            _dataManager.SaveLocal();

            _dataManager.GenerateDelta("gems", _dataManager.Current.gems);
            return true;
        }

        // 메모리 누수 방지
        public void Dispose()
        {
            _dataManager.OnLoaded -= UpdateCurrencies;
            _dataManager.OnSaved -= UpdateCurrencies;
        }
    }
}
