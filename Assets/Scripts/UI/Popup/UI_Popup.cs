using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UI_Popup : UI_Base
{
    // UI_Popup: UI_Base를 상속 → 바인딩·이벤트 기본 기능 상속

    // 팝업 UI용 베이스
    // Init()에서 SetCanvas(go, true) 호출:
    // 동적으로 증가하는 sortingOrder 적용 ​
    // ClosePopupUI()로 팝업 스택에서 자신을 제거
    public override void Init()
    {
        // 팝업용 캔버스를 설정 (sort = true)
        // 내부적으로 Canvas 컴포넌트 추가 → 오버레이 모드 → 동적 sortingOrder 적용
        Managers.Instance.UI.SetCanvas(gameObject, true);
    }

    // 자신을 UIManager의 팝업 스택에서 안전하게 제거
    // 팝업 전용 닫기 로직을 캡슐화
    public virtual void ClosePopupUI()  // 팝업이니까 고정 캔버스(Scene)과 다르게 닫는게 필요
    {
        Managers.Instance.UI.ClosePopupUI(this);
    }
    // UI_Scene을 상속받은 애들은 그 안에서 팝업을 띄웠다가 닫는다면 그 팝업을 닫을때 UI매니저의 ClosePopupUI를 사용.
    // UI_Popup을 상속받은 애들은 팝업을 닫을때 UI_Popup의 ClosePopupUI를 사용.
    // 근데 ShowPopupUI은 모두 UIManager의 ShowPopupUI를 사용.


    // Unity가 매 프레임 호출하는 Update
    void Update()
    {
        HandleEscape();
    }

    // ESC 키 누를 때, 내가 최상위 팝업일 때만 닫기
    protected virtual void HandleEscape()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            Managers.Instance.UI.GetTopPopup() == this)
        {
            ClosePopupUI();
        }
    }
}
