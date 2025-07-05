using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneManagerEx : ISceneManagerEx
{
    // DI 주입받을 매니저들 (선택적)
    [Inject(Optional = true)] private IUIManager _uiManager;
    [Inject] private IPoolManager _poolManager;

    public BaseScene CurrentScene { get { return GameObject.FindAnyObjectByType<BaseScene>(); } }

    // GetSceneName는 Define.Scene enum을 string으로 변환하는 함수
    // 즉, 이 함수는 씬의 이름을 가져오는 역할을 한다.
    public string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type); // C#의 Reflection. Scene enum의 
        return name;
    }

    public void LoadScene(Define.Scene type)
    {
        // 1) 씬 UI만 지우기 (UIManager가 있을 때만)
        _uiManager?.ClearSceneUI();

        // 2) 팝업만 지우기 + 풀 등 전역 정리
        _uiManager?.ClearPopupUI();
        _poolManager.Clear();

        SceneManager.LoadScene(GetSceneName(type)); // SceneManager는 UnityEngine의 SceneManager
    }
}