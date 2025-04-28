using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UI_SelectChapter : UI_Popup
{
    // 6개 챕터 + 리턴 버튼
    enum Buttons
    {
        Chapter1,
        Chapter2,
        Chapter3,
        Chapter4,
        Chapter5,
        Chapter6,
        Return
    }

    private Button[] _chapterButtons = new Button[6];
    private Button _returnButton;

    public override void Init()
    {
        base.Init();

        // 버튼 자동 바인딩
        Bind<Button>(typeof(Buttons));

        // 챕터 버튼 연결
        for (int i = 0; i < 6; i++)
        {
            // enum 순서대로 GetButton(0)부터 Chapter1~Chapter6
            _chapterButtons[i] = GetButton(i);
            int chapterIndex = i + 1;  // 1~6
            _chapterButtons[i].onClick.AddListener(() =>
            {
                if (chapterIndex == 1)
                {
                    // 1번 챕터는 바로 스테이지 UI 호출
                    Managers.UI.ShowPopupUI<UI_SelectStage>("UI_SelectStage");
                }
                else
                {
                    // 2~6번 챕터는 업데이트 예정 팝업
                    Managers.UI
                        .ShowPopupUI<GenericInfoPopup>("UI_UpdatedLater")
                        .Setup
                        (
                            $" Chapter {chapterIndex} will be updated."
                        );
                }
            });
        }

        // Return 버튼 연결
        _returnButton = GetButton((int)Buttons.Return);
        _returnButton.onClick.AddListener(ClosePopupUI);

        // 항상 커서 보이기
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
        // ShowPopupUI 이후 Init이 보장되지 않을 수 있으므로
        Init();
    }

}
