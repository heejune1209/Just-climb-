using UnityEngine;

public class LobbyScene : BaseScene
{
    protected override void Init()
    {
        base.Init();
        SceneType = Define.Scene.Lobby;
        // UI_Lobby 프리팹을 @LobbyScene 루트 아래에 표시
        Managers.UI.ShowSceneUI<UI_Lobby>("UI_Lobby");
    }

    public override void Clear()
    {
        // 씬 종료 시 필요한 정리 로직
    }
}