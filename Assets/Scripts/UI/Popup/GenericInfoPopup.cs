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

    void Awake()
    {
        base.Init();  // Canvas 세팅 & ESC 처리
        
        // Hierarchy에서 CloseButton 이름으로 자동 바인딩
        var go = Util.FindChild(gameObject, "CloseButton", true);
        if (go != null)
            closeButton = go.GetComponent<Button>();

        // 바인딩 됐으면 리스너 추가
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
