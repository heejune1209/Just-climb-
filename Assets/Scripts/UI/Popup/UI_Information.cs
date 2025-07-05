using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class UI_Information : UI_Popup
{
    // 1) Hierarchy 상의 GameObject 이름과 일치시킬 enum
    enum GameObjects
    {
        KeyTab,     // GameObject 이름: "KeyTab"
        ItemTab,    // GameObject 이름: "ItemTab"
        SpeedWG     // GameObject 이름: "SpeedWG"
    }

    // 2) 버튼 이름도 enum 으로
    enum Buttons
    {
        KeyButton,  // Button 이름: "KeyButton"
        ItemButton  // Button 이름: "ItemButton"
    }

    enum Texts
    {
        KeyButtonLabel,   // GameObject 이름: "KeyButtonLabel"
        ItemButtonLabel   // GameObject 이름: "ItemButtonLabel"
    }


    // 3) 자동 바인딩한 레퍼런스
    private GameObject _keyTab, _itemTab, _speedWG;
    private Button _keyButton, _itemButton;
    private TMP_Text _keyLabel, _itemLabel;

    // 4) Inspector 에만 남겨둘 것들 (이미지·색상 같은 설정값)
    [Header("버튼 선택 시 사용할 스프라이트 / 색상")]
    [SerializeField] private Sprite keyButtonSelectedImage;
    [SerializeField] private Sprite itemButtonSelectedImage;
    [SerializeField] private Sprite defaultButtonImage;
    private Color selectedTextColor = new Color(0, 0, 0, 1);  // Color.black
    private Color defaultTextColor = new Color(1, 1, 1, 1);  // Color.white

    private void Start()
    {
        Init();  // Awake 단계에서 바인딩이 되면 Update 때 null 방지
    }

    public override void Init()
    {
        base.Init();  // UI_Popup.Init(): SetCanvas + ESC 자동 처리

        // 5) 자동 바인딩
        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _keyTab = GetGameObject((int)GameObjects.KeyTab);
        _itemTab = GetGameObject((int)GameObjects.ItemTab);
        _speedWG = GetGameObject((int)GameObjects.SpeedWG);

        _keyButton = GetButton((int)Buttons.KeyButton);
        _itemButton = GetButton((int)Buttons.ItemButton);

        // 6) 버튼 이벤트 연결
        _keyButton.onClick.AddListener(ShowKeyTab);
        _itemButton.onClick.AddListener(ShowItemTab);

        // 7) 최초에는 KeyTab 보여주기
        ShowKeyTab();

        
    }

    private void ShowKeyTab()
    {
        _keyTab.SetActive(true);
        _itemTab.SetActive(false);

        // 버튼 외형 갱신
        _keyButton.image.sprite = keyButtonSelectedImage;
        if (_keyLabel != null) _keyLabel.color = selectedTextColor;

        _itemButton.image.sprite = defaultButtonImage;
        if (_itemLabel != null) _itemLabel.color = defaultTextColor;
    }

    private void ShowItemTab()
    {
        _keyTab.SetActive(false);
        _itemTab.SetActive(true);

        _keyButton.image.sprite = defaultButtonImage;
        if (_keyLabel != null) _keyLabel.color = defaultTextColor;

        _itemButton.image.sprite = itemButtonSelectedImage;
        if (_itemLabel != null) _itemLabel.color = selectedTextColor;
    }

    void Update()
    {
        HandleEscape();

        // SpeedWG가 활성화됐을 때만 화살표로 전환
        if (_speedWG.activeSelf)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                ShowKeyTab();
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                ShowItemTab();
        }
    }
}
