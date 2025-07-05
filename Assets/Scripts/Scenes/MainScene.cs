using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScene : BaseScene
{
    protected override void Init()   // Awake()에서 자동으로 호출
    {
        Debug.Log("MainScene: Init 호출");
        base.Init();                 // EventSystem 세팅 등 공통 초기화
        SceneType = Define.Scene.Main;
        // 로고 팝업으로 표시
        //_uiManager.ShowPopupUI<StartLogo>("UI_Logo");
        // UI_Main 프리팹을 띄워 바인딩/초기화
        _uiManager.ShowSceneUI<UI_Main>("UI_Main");
    }

    public override void Clear()
    {
        // Scene 전환 직전 기존 UI 정리
        // Managers.Clear()는 더 이상 사용하지 않음 (DI 패턴에서는 불필요)
        base.Clear();
    }

    // 메모리 누수 방지
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}