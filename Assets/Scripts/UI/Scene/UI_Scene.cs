using UnityEngine;

public class UI_Scene : UI_Base
{
    // UI_Scene (UI_Scene.cs)
    // 고정적인 씬 UI용 베이스
    // Init()에서 SetCanvas(go, false) 호출:
    // 정렬 순서 0 (항상 아래 레이어)
    // 역할 구분: 씬 고정 UI용 → 팝업과 달리 닫기 필요 없음
    public override void Init()
    {
        // 씬 UI용 캔버스를 설정 (sort = false)
        // sortingOrder = 0 고정 → 항상 배경 레이어
        Managers.UI.SetCanvas(gameObject, false);
    }
}
