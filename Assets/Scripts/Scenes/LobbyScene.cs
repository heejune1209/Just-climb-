using UnityEngine;

public class LobbyScene : BaseScene
{
    // 정리된 호출 흐름
    // BaseScene.Awake()
    // ↳ virtual Init() 호출 → LobbyScene.Init()
    //   ↳ base.Init() 호출 → BaseScene.Init() (기본 초기화)
    //   ↳ 나머지 LobbyScene.Init() 로직 실행
    protected override void Init()
    {
        base.Init();
        SceneType = Define.Scene.Lobby;
        // UI_Lobby 프리팹을 @LobbyScene 루트 아래에 표시
        _uiManager.ShowSceneUI<UI_Lobby>("UI_Lobby");
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