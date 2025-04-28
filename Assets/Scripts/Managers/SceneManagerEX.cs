using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{

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
        Managers.Clear(); // 씬을 로드하기 전에 현재 씬을 초기화

        SceneManager.LoadScene(GetSceneName(type)); // SceneManager는 UnityEngine의 SceneManager
    }

    public void Clear()
    {
        CurrentScene.Clear();
    }
}