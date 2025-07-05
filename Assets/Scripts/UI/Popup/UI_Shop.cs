using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using JustClimb.Manager;   // ItemManager 네임스페이스
using JustClimb.Items;
using Zenject;

public class UI_Shop : UI_Popup
{
    // DI 주입받을 매니저들
    [Inject] private ICurrencyManager _currencyManager;
    [Inject] private IItemManager _itemManager;
    [Inject] private ItemDatabase _itemDatabase;

    // 자동 바인딩할 텍스트 요소를 식별하는 enum
    enum Texts { GemText, GoldText, FeatherCount, WingCount, LampCount, FlagCount }

    // 자동 바인딩할 버튼을 식별하는 enum
    enum Buttons
    {
        BuyFeather, BuyWing, BuyLamp, BuyFlag,     // 구매 버튼
        DescFeather, DescWing, DescLamp, DescFlag, // 아이템 설명 팝업 버튼
        OpenConversion, CloseConversion,            // 환전 패널 열기/닫기 버튼
        Exchange1, Exchange10, Exchange100,         // 환전 옵션 버튼
        Exchange1000, Exchange10000, Exchange100000
    }

    // 자동 바인딩할 패널(환전소)을 식별하는 enum
    enum Panels { ConversionPanel }

    // 보유 보석 수를 보여주는 텍스트
    TMP_Text _gemText;
    // 보유 골드 수를 보여주는 텍스트
    TMP_Text _goldText;
    // 깃털 아이템 개수를 보여주는 텍스트
    TMP_Text _featherCount;
    // 날개 아이템 개수를 보여주는 텍스트
    TMP_Text _wingCount;
    // 램프 아이템 개수를 보여주는 텍스트
    TMP_Text _lampCount;
    // 깃발 아이템 개수를 보여주는 텍스트
    TMP_Text _flagCount;

    // 깃털 구매 버튼
    Button _btnBuyFeather;
    // 날개 구매 버튼
    Button _btnBuyWing;
    // 램프 구매 버튼
    Button _btnBuyLamp;
    // 깃발 구매 버튼
    Button _btnBuyFlag;

    // 깃털 설명 팝업 버튼
    Button _btnDescFeather;
    // 날개 설명 팝업 버튼
    Button _btnDescWing;
    // 램프 설명 팝업 버튼
    Button _btnDescLamp;
    // 깃발 설명 팝업 버튼
    Button _btnDescFlag;

    // 환전소 패널 전체 GameObject
    GameObject _conversionPanel;
    // 환전소 열기 버튼
    Button _btnOpenConversion;
    // 환전소 닫기 버튼
    Button _btnCloseConversion;
    // 환전 옵션(1,10,100,...) 버튼 배열
    Button[] _btnExchange = new Button[6];
    // 각 환전 옵션에 해당하는 보석 개수 배열
    readonly int[] _exchangeAmounts = { 1, 10, 100, 1000, 10000, 100000 };
    
    // 보석→골드 환전 비율 상수
    const int GEM_TO_GOLD_RATIO = 400;   

    // UI 요소 바인딩 및 버튼 이벤트 연결
    public override void Init()
    {
        base.Init();

        // 1) UI 컴포넌트 바인딩
        Bind<TextMeshProUGUI>(typeof(Texts));    // 텍스트
        Bind<Button>(typeof(Buttons)); // 버튼
        Bind<GameObject>(typeof(Panels));  // 패널

        // 2) 바인딩된 오브젝트 가져오기
        _gemText = GetText((int)Texts.GemText);
        _goldText = GetText((int)Texts.GoldText);
        _featherCount = GetText((int)Texts.FeatherCount);
        _wingCount = GetText((int)Texts.WingCount);
        _lampCount = GetText((int)Texts.LampCount);
        _flagCount = GetText((int)Texts.FlagCount);

        _btnBuyFeather = GetButton((int)Buttons.BuyFeather);
        _btnBuyWing = GetButton((int)Buttons.BuyWing);
        _btnBuyLamp = GetButton((int)Buttons.BuyLamp);
        _btnBuyFlag = GetButton((int)Buttons.BuyFlag);

        // --------- 아이템 설명 팝업 띄우기 ---------
        _btnDescFeather = GetButton((int)Buttons.DescFeather);
        _btnDescWing = GetButton((int)Buttons.DescWing);
        _btnDescLamp = GetButton((int)Buttons.DescLamp);
        _btnDescFlag = GetButton((int)Buttons.DescFlag);

        // 설명 팝업 리스너
        _btnDescFeather.onClick.AddListener(() => ShowItemInfo(ItemType.Feather));
        _btnDescWing.onClick.AddListener(() => ShowItemInfo(ItemType.Wing));
        _btnDescLamp.onClick.AddListener(() => ShowItemInfo(ItemType.Lamp));
        _btnDescFlag.onClick.AddListener(() => ShowItemInfo(ItemType.Flag));

        // 환전소 패널 및 버튼 바인딩
        _conversionPanel = GetGameObject((int)Panels.ConversionPanel);
        _btnOpenConversion = GetButton((int)Buttons.OpenConversion);
        _btnCloseConversion = GetButton((int)Buttons.CloseConversion);
        for (int i = 0; i < _btnExchange.Length; i++)
            _btnExchange[i] = GetButton((int)Buttons.Exchange1 + i);

        // 구매 버튼
        _btnBuyFeather.onClick.AddListener(() => BuyItem(ItemType.Feather));
        _btnBuyWing.onClick.AddListener(() => BuyItem(ItemType.Wing));
        _btnBuyLamp.onClick.AddListener(() => BuyItem(ItemType.Lamp));
        _btnBuyFlag.onClick.AddListener(() => BuyItem(ItemType.Flag));

        // 환전 버튼 이벤트 연결
        _btnOpenConversion.onClick.AddListener(() => _conversionPanel.SetActive(true));
        _btnCloseConversion.onClick.AddListener(() => _conversionPanel.SetActive(false));
        for (int i = 0; i < _exchangeAmounts.Length; i++)
        {
            int amt = _exchangeAmounts[i];
            _btnExchange[i].onClick.AddListener(() => TryExchange(amt));
        }

        // 4) 매니저 이벤트 구독 (DI가 완료된 시점)
        _currencyManager.OnGoldChanged += HandleGoldChanged;
        _currencyManager.OnGemsChanged += HandleGemsChanged;
        _itemManager.OnItemCountChanged += OnItemCountChanged;

        // 5) 초기 UI 반영
        HandleGemsChanged(_currencyManager.Gems);
        HandleGoldChanged(_currencyManager.Gold);

        foreach (var kv in _itemDatabase.GetAllItemDefinitions())
            OnItemCountChanged(kv.Key, _itemManager.GetItemCount(kv.Key));
    }

    // Awake 시 Init() 자동 호출
    void Start() => Init();

    // OnDestroy 에서 구독 해제
    protected override void OnDestroy()
    {
        if (_currencyManager != null)
        {
            _currencyManager.OnGoldChanged -= HandleGoldChanged;
            _currencyManager.OnGemsChanged -= HandleGemsChanged;
        }
        if (_itemManager != null)
        {
            _itemManager.OnItemCountChanged -= OnItemCountChanged;
        }
    }

    // 래핑용 핸들러 (언급된 델리게이트와 언바인딩을 위해 메서드로 분리)
    private void HandleGoldChanged(int newGold)
    {
        OnCurrencyChanged("Gold", newGold);
    }
    private void HandleGemsChanged(int newGems)
    {
        OnCurrencyChanged("Gem", newGems);
    }


    // 재화 변경 시 텍스트 업데이트
    void OnCurrencyChanged(string key, int cnt)
    {
        if (key == "Gem") _gemText.text = $": {cnt}";
        if (key == "Gold") _goldText.text = $": {cnt}";
    }

    // 아이템 수량 변경 시 해당 슬롯 텍스트 업데이트
    void OnItemCountChanged(ItemType id, int cnt)
    {
        switch (id)
        {
            case ItemType.Feather: _featherCount.text = cnt.ToString(); break;
            case ItemType.Wing: _wingCount.text = cnt.ToString(); break;
            case ItemType.Lamp: _lampCount.text = cnt.ToString(); break;
            case ItemType.Flag: _flagCount.text = cnt.ToString(); break;
        }
    }

    // 아이템 설명 팝업 열기
    void ShowItemInfo(ItemType itemId)
    {
        var data = _itemDatabase.Get(itemId);
        _uiManager.ShowPopupUI<GenericInfoPopup>($"{itemId}Info")
            .Setup(data.displayName, data.description, $"Price: {data.price}");
    }

    // 아이템 구매 시도: 골드 지불 후 아이템 추가
    void BuyItem(ItemType itemId)
    {
        var data = _itemDatabase.Get(itemId);
        if (_currencyManager.SpendGold(data.price))
            _itemManager.AddItem(itemId);  // 이 key가 ScriptableObject.itemId와 100% 일치해야됨.
        else
            _uiManager.ShowPopupUI<GenericInfoPopup>("EmptyGoldPanel")
                .Setup("Warning!", "You can't buy items because you're short of Gold.");
    }    

    // 보석 → 골드 환전 시도
    void TryExchange(int gemAmt)
    {
        if (_currencyManager.SpendGems(gemAmt))
            _currencyManager.AddGold(gemAmt * GEM_TO_GOLD_RATIO);
        else
            _uiManager.ShowPopupUI<GenericInfoPopup>("EmptyGemPanel")
                .Setup("Warning!", "You don't have enough Gems.");
    }

    protected override void HandleEscape()
    {
        // ESC 키를 눌렀을 때만 동작
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 1) 환전 패널 열려 있으면 닫기
            if (_conversionPanel != null && _conversionPanel.activeSelf)
            {
                _conversionPanel.SetActive(false);
            }
            else
            {
                // 2) 아니면 팝업 자체 닫기
                base.HandleEscape();
            }
        }
    }
}
