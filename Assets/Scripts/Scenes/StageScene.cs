using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageScene : BaseScene
{
    [Header("Inspector용: 이 씬에 대응하는 Define.Scene 이름 (예: Stage1, Stage2, …)")]
    [SerializeField] private string sceneString;
    protected override void Init()
    {
        base.Init();
        // 문자열을 Define.Scene enum으로 변환
        if (Enum.TryParse<Define.Scene>(sceneString, out var parsed))
        {
            SceneType = parsed;
        }
        else
        {
            Debug.LogError($"[StageScene] 잘못된 sceneString: '{sceneString}'. Define.Scene에 해당 이름이 없습니다.");
            SceneType = Define.Scene.Unknown;
        }
        Managers.UI.ShowSceneUI<UI_Stage>("UI_Stage");
    }
    
    public override void Clear()
    {
        // 씬 종료 시 필요한 정리 로직
    }
}
