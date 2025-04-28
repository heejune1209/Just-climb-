using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using JustClimb.Manager;   // ItemManager 네임스페이스

public class UI_Shop : UI_Popup
{
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

    // 아이템 정의 구조체: key는 아이템 ID, price는 가격
    struct Item { public string key; public int price; }
    // UI에서 사용할 아이템별 가격 정보 사전
    Dictionary<string, Item> _itemDefs = new Dictionary<string, Item>()
    {
        {"Feather", new Item{key="Feather",price=100}},
        {"Wing",    new Item{key="Wing",   price=100}},
        {"Lamp",    new Item{key="Lamp",   price=200}},
        {"Flag",    new Item{key="Flag",   price=300}},
    };

    /// <summary>
    /// UI 요소 바인딩 및 버튼 이벤트 연결
    /// </summary>
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

        // 설명 팝업 버튼에 리스너 추가
        _btnDescFeather.onClick.AddListener(() =>
            Managers.UI.ShowPopupUI<GenericInfoPopup>("FeatherInfo")
                .Setup("Feather",
                       "Lightweight & Low Air Resistance Feather.\r\nThe speed of movement increases when used.",
                       $"Price : {_itemDefs["Feather"].price}"));
        _btnDescWing.onClick.AddListener(() =>
            Managers.UI.ShowPopupUI<GenericInfoPopup>("WingInfo")
                .Setup("Wing",
                       "Strong leather climbing shoes.\r\nJumping power increases when used.",
                       $"Price : {_itemDefs["Wing"].price}"));
        _btnDescLamp.onClick.AddListener(() =>
            Managers.UI.ShowPopupUI<GenericInfoPopup>("LampInfo")
                .Setup("Lamp",
                       "A lamp that lights up your surroundings.\r\nWhen used, it finds transparent objects around it.",
                       $"Price : {_itemDefs["Lamp"].price}"));
        _btnDescFlag.onClick.AddListener(() =>
            Managers.UI.ShowPopupUI<GenericInfoPopup>("FlagInfo")
                .Setup("Flag",
                       "Flag that allows you to save and return your current location.\r\nSave/load the current location within the stage when used.",
                       $"Price : {_itemDefs["Flag"].price}"));

        // 환전소 패널 및 버튼 바인딩
        _conversionPanel = GetGameObject((int)Panels.ConversionPanel);
        _btnOpenConversion = GetButton((int)Buttons.OpenConversion);
        _btnCloseConversion = GetButton((int)Buttons.CloseConversion);
        for (int i = 0; i < _btnExchange.Length; i++)
            _btnExchange[i] = GetButton((int)Buttons.Exchange1 + i);

        // 구매 버튼 이벤트 연결
        _btnBuyFeather.onClick.AddListener(() => TryBuy("Feather"));
        _btnBuyWing.onClick.AddListener(() => TryBuy("Wing"));
        _btnBuyLamp.onClick.AddListener(() => TryBuy("Lamp"));
        _btnBuyFlag.onClick.AddListener(() => TryBuy("Flag"));

        // 환전 버튼 이벤트 연결
        _btnOpenConversion.onClick.AddListener(OpenConversion);
        _btnCloseConversion.onClick.AddListener(() => _conversionPanel.SetActive(false));
        for (int i = 0; i < _exchangeAmounts.Length; i++)
        {
            int amt = _exchangeAmounts[i];
            _btnExchange[i].onClick.AddListener(() => TryExchange(amt));
        }
    }

    // Awake 시 Init() 자동 호출
    void Awake() => Init();

    // 팝업 켜질 때 이벤트 구독 및 초기 UI 반영
    private void OnEnable()
    {
        var mgr = ItemManager.Instance;
        mgr.OnCurrencyChanged += OnCurrencyChanged;    // 재화 변경 이벤트
        mgr.OnItemCountChanged += OnItemCountChanged;   // 아이템 수량 변경 이벤트

        // 팝업 오픈 시점에 현재 값 UI에 반영
        OnCurrencyChanged("Gem", mgr.GetGems());
        OnCurrencyChanged("Gold", mgr.GetGold());
        OnItemCountChanged("Feather", mgr.GetItemCount("Feather"));
        OnItemCountChanged("Wing", mgr.GetItemCount("Wing"));
        OnItemCountChanged("Lamp", mgr.GetItemCount("Lamp"));
        OnItemCountChanged("Flag", mgr.GetItemCount("Flag"));
    }

    // 팝업 꺼질 때 이벤트 구독 해제
    private void OnDisable()
    {
        var mgr = ItemManager.Instance;
        mgr.OnCurrencyChanged -= OnCurrencyChanged;
        mgr.OnItemCountChanged -= OnItemCountChanged;
    }

    // 재화 변경 시 텍스트 업데이트
    void OnCurrencyChanged(string key, int cnt)
    {
        if (key == "Gem") _gemText.text = $": {cnt}";
        if (key == "Gold") _goldText.text = $": {cnt}";
    }

    // 아이템 수량 변경 시 해당 슬롯 텍스트 업데이트
    void OnItemCountChanged(string id, int cnt)
    {
        switch (id)
        {
            case "Feather": _featherCount.text = cnt.ToString(); break;
            case "Wing": _wingCount.text = cnt.ToString(); break;
            case "Lamp": _lampCount.text = cnt.ToString(); break;
            case "Flag": _flagCount.text = cnt.ToString(); break;
        }
    }

    // 아이템 구매 시도: 골드 지불 후 아이템 추가
    void TryBuy(string key)
    {
        var def = _itemDefs[key];
        if (ItemManager.Instance.SpendGold(def.price))
            ItemManager.Instance.AddItem(key);
        else
            Managers.UI.ShowPopupUI<GenericInfoPopup>("EmptyGoldPanel")
                       .Setup("Warning!", "You can't buy items because you're short of Gold.");
    }

    // 환전 패널 열기: 자식 텍스트에 옵션별 환전 정보 세팅
    void OpenConversion()
    {
        _conversionPanel.SetActive(true);
        var convTxts = _conversionPanel.GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < _exchangeAmounts.Length; i++)
            convTxts[i].text = $"Gem {_exchangeAmounts[i]} => Gold {_exchangeAmounts[i] * GEM_TO_GOLD_RATIO}";
    }

    // 보석 → 골드 환전 시도
    void TryExchange(int gemAmt)
    {
        if (ItemManager.Instance.GetGems() >= gemAmt)
        {
            ItemManager.Instance.AddGold(gemAmt * GEM_TO_GOLD_RATIO);
            ItemManager.Instance.AddGems(-gemAmt);
        }
        else
        {
            Managers.UI.ShowPopupUI<GenericInfoPopup>("EmptyGemPanel")
                       .Setup("Warning!", "You can't exchange Coins because you're short of Gem.");
        }
    }
}
