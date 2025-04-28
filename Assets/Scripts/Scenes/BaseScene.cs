using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BaseScene : MonoBehaviour
{
    // 이 씬은 어떤 타입의 씬인지를 알려줄 정보 (from 📜Define의 Scene enum)
    // 자식 씬들에게 상속
    // get 은 ScreenType 프로퍼티의 접근지정자 따라 public 하게, set 은 protected 한 프로퍼티로 설정
    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown; // 디폴트로 Unknow 이라고 초기화

    // Awake는 오브젝트가 비활성화 되어있어도 호출됨
    // 그리고 UI 시리즈를 만들 때는 최상위 부모에서 이런 Start나 Awake를 안 넣어줬는데
    // 얘를 만약에 최상희 부모한테 이렇게 넣어줄 경우에는 혹시라도
    // 이 GameScene에서 Start나 Awake를 까먹었다 하더라도 각 들고 있는 부모님이 Awake를 대신 실행해 주기 때문에 조금 더 편리하게 작성을 할 수 있습니다
    void Awake()
    {
        Init();
    }

    // UI는 반드시 EventSystem이 필요하기 때문에 꼭! 만들어주어야 한다. EventSystem을 만들어주는 작업.
    // EventSystem도 그냥 프리팹으로 만들어버리고 이를 생성시키기
    protected virtual void Init()
    {
        Object obj = FindAnyObjectByType(typeof(EventSystem));
        if (obj == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";
    }

    public abstract void Clear();
}