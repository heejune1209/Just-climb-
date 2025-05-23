using UnityEngine;

public class LobbyScene : BaseScene
{
    protected override void Init()
    {
        base.Init();
        SceneType = Define.Scene.Lobby;
        // UI_Lobby 프리팹을 @LobbyScene 루트 아래에 표시
        Managers.Instance.UI.ShowSceneUI<UI_Lobby>("UI_Lobby");
    }

    public override void Clear()
    {
        // Scene 전환 직전 기존 UI 정리
        Managers.Clear();
    }
}