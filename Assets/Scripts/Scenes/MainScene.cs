using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScene : BaseScene
{
    protected override void Init()   // Awake()에서 자동으로 호출
    {
        base.Init();                 // EventSystem 세팅 등 공통 초기화
        SceneType = Define.Scene.Main;
        // UI_Main 프리팹을 띄워 바인딩/초기화
        Managers.UI.ShowSceneUI<UI_Main>("UI_Main");
    }

    public override void Clear()
    {
        // 씬 전환 전 정리 로직 필요하면 여기에
    }
}