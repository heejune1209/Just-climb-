using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class UI_Lobby : UI_Scene
{
    // 1) 바인딩할 텍스트
    enum Texts
    {
        PromptText
    }

    private TMP_Text _promptText;
    private string _currentArea;

    public override void Init()
    {
        base.Init();

        // 텍스트 자동 바인딩
        Bind<TextMeshProUGUI>(typeof(Texts));
        _promptText = GetText((int)Texts.PromptText);

        // 시작할 땐 숨김
        _promptText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Init()을 보장
        Init();
        // 혹시몰라서 초기화 
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // 1) 플레이어가 영역 안에 있고 E 키를 눌렀다면, 해당 팝업이 열려 있지 않은 경우에만
        if (!string.IsNullOrEmpty(_currentArea) &&
            !Managers.UI.IsPopupOpen<UI_Shop>() &&
            !Managers.UI.IsPopupOpen<UI_Worldview>() &&
            !Managers.UI.IsPopupOpen<UI_SelectChapter>() &&
            !Managers.UI.IsPopupOpen<UI_Ranking>() &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            switch (_currentArea)
            {
                case "Shop":
                    Managers.UI.ShowPopupUI<UI_Shop>("UI_Shop");
                    break;
                case "WorldView":
                    Managers.UI.ShowPopupUI<UI_Worldview>("UI_Worldview");
                    break;
                case "SelectStage":
                    Managers.UI.ShowPopupUI<UI_SelectChapter>("UI_SelectChapter");
                    break;
                case "Ranking":
                    Managers.UI.ShowPopupUI<UI_Ranking>("UI_Ranking");
                    break;
            }
            _promptText.gameObject.SetActive(false);
        }

        // 2) ESC 키: 다른 팝업(월드뷰, 상점, 스테이지, 랭킹 등)이 전부 닫혀 있을 때만
        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            Managers.UI.GetTopPopup() == null)
        {
            Managers.UI.ShowPopupUI<UI_Warning>("UI_Warning");
        }
    }

    // 트리거에 들어왔을 때 호출
    public void ShowAreaPrompt(string areaName)
    {
        _currentArea = areaName;
        _promptText.text = $"<color=yellow>E</color>  -  {areaName}";
        _promptText.gameObject.SetActive(true);
    }

    // 트리거에서 나갔을 때 호출
    public void HideAreaPrompt()
    {
        _currentArea = null;
        _promptText.gameObject.SetActive(false);
    }
}
