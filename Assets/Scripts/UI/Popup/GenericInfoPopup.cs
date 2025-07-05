using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class GenericInfoPopup : UI_Popup
{
    [Header("텍스트 필드들 (순서대로 Title / Content / Price)")]
    [SerializeField] private TMP_Text[] textFields;  // 인스펙터에서 3개 할당
    // 사용법
    // textFields 크기를 3 으로 설정하고,
    // Element 0 → TitleText (TMP_Text) 
    // Element 1 → ContentText (TMP_Text) 
    // Element 2 → PriceText (TMP_Text)

    private Button closeButton;

    /// <summary>
    /// Zenject가 주입을 끝낸 뒤 Start() 시점에 호출.
    /// </summary>
    void Start()
    {
        Init();
    }

    /// <summary>
    /// Init 단계에서
    /// 1) base.Init()으로 캔버스를 세팅하고,
    /// 2) CloseButton 자동 바인딩 및 리스너 연결
    /// </summary>
    public override void Init()
    {
        base.Init();  // UI_Popup.Init() → _uiManager.SetCanvas(gameObject, true)

        // "CloseButton" 이라는 자식 오브젝트를 찾아서 Button 컴포넌트 가져오기
        var go = Util.FindChild(gameObject, "CloseButton", true);
        if (go != null)
            closeButton = go.GetComponent<Button>();

        // 버튼이 있으면 클릭 시 팝업 닫기
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopupUI);
    }


    // 넘겨주는 만큼만 보이고, 나머지는 자동 숨김.
    // ex) Setup("타이틀", "본문", "100")  
    public void Setup(params string[] texts)
    {
        for (int i = 0; i < textFields.Length; i++)
        {
            if (i < texts.Length && !string.IsNullOrEmpty(texts[i]))
            {
                textFields[i].text = texts[i];
                textFields[i].gameObject.SetActive(true);
            }
            else
            {
                textFields[i].gameObject.SetActive(false);
            }
        }
    }
}
