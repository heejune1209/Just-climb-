using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

// 이 클래스는 객체 생성을 금지하고, 반드시 상속되어야 하는 클래스임을 표현하기 위해 abstract을 붙임.
public abstract class BaseScene : MonoBehaviour, IInitializable
{
    // DI 주입받을 매니저들
    [Inject] protected IResourceManager _resourceManager;
    [Inject] protected IUIManager _uiManager;

    // 이 씬은 어떤 타입의 씬인지를 알려줄 정보 (from Define의 Scene enum)
    // 자식 씬들에게 상속
    // get 은 ScreenType 프로퍼티의 접근지정자 따라 public 하게, set 은 protected 한 프로퍼티로 설정
    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown; // 디폴트로 Unknow 이라고 초기화
    
    // Awake는 오브젝트가 비활성화 되어있어도 호출됨
    // 그리고 UI 시리즈를 만들 때는 최상위 부모에서 이런 Start나 Awake를 안 넣어줬는데
    // 얘를 만약에 최상위 부모한테 이렇게 넣어줄 경우에는 혹시라도
    // 이 BaseScene을 상속받은 자식 클래스에서 Start나 Awake를 까먹었다 하더라도
    // 각 들고 있는 부모님이 Awake를 대신 실행해 주기 때문에 조금 더 편리하게 작성을 할 수 있습니다
    // 이렇게 하면 "자식이 Awake를 깜빡 잊어도" BaseScene.Awake → virtual Init() 으로 자식 Init이 보장 호출
    // IInitializable 구현
    public void Initialize()
    {
        Init();
    }

    // UI는 반드시 EventSystem이 필요하기 때문에 꼭! 만들어주어야 한다. EventSystem을 만들어주는 작업.
    // EventSystem도 그냥 프리팹으로 만들어버리고 이를 생성시키기
    protected virtual void Init()
    {
        Object obj = FindAnyObjectByType(typeof(EventSystem));
        if (obj == null)
            _resourceManager.Instantiate("UI/EventSystem").name = "@EventSystem";
    }

    public virtual void Clear()
    {
        // UI Scene Root 파괴
        _uiManager.ClearSceneUI();
        // (필요하다면) 씬 전용 오브젝트 추가 정리
    }


    // MonoBehaviour 기준으로 “자식 클래스가 Awake를 깜빡해도” Init()이 항상 호출되도록 보장
    // 주입 타이밍: Zenject의 SceneContext가 컴포넌트를 스캔해서[Inject] 필드를 채운 뒤(Injection)에,
    // Unity가 각 MonoBehaviour의 Awake()를 호출해 줍니다.
    // 따라서 BaseScene.Awake 안에서 Initialize() → Init() 을 호출해도,
    // 이미 _resourceManager, _uiManager 같은 [Inject] 매니저가 값이 들어간 상태라 안전합니다BaseScene.
    // 결론: Scene 로드시 필요한 초기화(예: EventSystem 인스턴스화)를 놓치지 않으려면, Awake override를 유지.
    protected virtual void Awake()
    {
        Initialize();
    }

    // 메모리 누수 방지
    protected virtual void OnDestroy()
    {
        // 매니저 참조 해제
        _resourceManager = null;
        _uiManager = null;
    }
}