using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public class UIManager : IUIManager, IInitializable
{
    // DI 주입받을 매니저들
    [Inject] private IResourceManager _resourceManager;
    [Inject(Optional = true)] private IGameManager _gameManager; // 순환 의존성 방지
    [Inject] private DiContainer _container; // Zenject container for runtime injection

    // UI 자동화는 "프리팹 루트만 준비해 두면, 코드가 거기에 스크립트와 Canvas를 자동으로 붙여 주고,
    // 자식 오브젝트는 이름과 최소한의 컴포넌트만 프리팹에 담아 두면, 인스펙터에서 수동으로 연결하지 않아도 런타임에 전부 바인딩 및 초기화됩니다."

    // UIManager: 전체 UI 흐름을 관장
    // SetCanvas 👉 go 오브젝트의 캔버스 컴포넌트 가져와(GetOrAddComponent를 통해 없다면 붙여서라도 가져옴) sort order값 세팅
    // Show~ 👉 캔버스 UI 프리팹 생성
    // Close~ 👉 캔버스 UI 오브젝트 파괴
    // Root 👉 @UI_Root이라는 이름의 빈 오브젝트를 만들어서라도 리턴해줌. UI 오브젝트들은 이 @UI_Root 빈 오브젝트 아래에 생성되게 그룹화할 것이라서 필요.
    // 각 UI에 Canvas 세팅 및 계층(@UI_Root) 관리

    int _order = 10; // 현재까지 최근에 사용한 오더
    Stack<UI_Popup> _popupStack = new Stack<UI_Popup>(); // 오브젝트 말고 컴포넌트를 담음. 팝업 캔버스 UI 들을 담는다.
    UI_Scene _sceneUI = null; // 현재의 고정 캔버스 UI

    // 열려 있는 팝업 타입을 기록
    HashSet<System.Type> _openPopupTypes = new HashSet<System.Type>();

    /// <summary>
    /// Zenject에서 자동으로 호출됨.
    /// </summary>
    public void Initialize()
    {
        Debug.Log("UIManager: Initialize");
        Init();
    }

    // SetCanvas() : 캔버스 세팅
    public void SetCanvas(GameObject go, bool sort = true)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = 0;
        }
        // Ensure UI scaling and raycasting components
        Util.GetOrAddComponent<CanvasScaler>(go);
        Util.GetOrAddComponent<GraphicRaycaster>(go);
    }
    /// <summary>
    /// 앱 시작 시 한 번만 호출해서
    /// - @UI_Root 오브젝트 생성·DontDestroyOnLoad
    /// </summary>
    void Init()
    {
        // 이제 이 한 줄로 충분합니다.
        var root = Root;
    }

    public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;
        GameObject go = _resourceManager.Instantiate($"Prefabs/UI/SubItem/{name}");
        if (parent != null)
            go.transform.SetParent(parent);
        return Util.GetOrAddComponent<T>(go);
    }

    // 역할: 씬 UI를 동적으로 불러와 화면에 표시
    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        Debug.Log($"UIManager: ShowSceneUI 호출 - name: {name}");
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;
        string prefabPath = name.Contains("/") ? name : $"Prefabs/UI/Scene/{name}";
        GameObject go = _resourceManager.Instantiate(prefabPath);
        // Inject dependencies into newly instantiated UI GameObject and its components
        _container.InjectGameObject(go);
        T sceneUI = Util.GetOrAddComponent<T>(go);
        _sceneUI = sceneUI;
        string scene = SceneManager.GetActiveScene().name;
        bool isStageScene = scene.Contains("Stage");
        if (scene == "Main")
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        go.transform.SetParent(Root.transform, false);
        return sceneUI;
    }

    // 팝업 UI를 띄우고 스택에 추가
    public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;
        GameObject go = _resourceManager.Instantiate($"Prefabs/UI/Popup/{name}");
        // Inject dependencies into newly instantiated Popup GameObject
        _container.InjectGameObject(go);

        T popup = Util.GetOrAddComponent<T>(go);        

        string scene = SceneManager.GetActiveScene().name;
        bool isStageScene = scene.Contains("Stage");
        if (_popupStack.Count == 0)
        {
            Time.timeScale = 0f;
            if (isStageScene && _gameManager != null)
                _gameManager.IsTimerPaused = true;
        }
        if (scene != "Main")
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        _popupStack.Push(popup);
        _openPopupTypes.Add(typeof(T));
        go.transform.SetParent(Root.transform, false);
        return popup;
    }
    public UI_Popup GetTopPopup()
    {
        // 팝업 스택에 하나라도 남아 있으면 그 최상위 팝업을, 그렇지 않으면 null을 리턴.
        return _popupStack.Count > 0 ? _popupStack.Peek() : null;
    }
    // 해당 팝업(T)이 현재 열려 있는지 여부를 리턴합니다.
    // 참이면 열려 있는 상태, 거짓이면 닫혀 있는 상태입니다.
    public bool IsPopupOpen<T>() where T : UI_Popup
    {
        return _openPopupTypes.Contains(typeof(T));
    }

    // @UI_Root라는 이름의 오브젝트를 없다면 만들어서라도 리턴해주는 프로퍼티 Root
    // 이게 필요한 이유는, Hierarchy 상의 오브젝트들도 마치 폴더 안에 있는것처럼 관련 있는 것들끼리 종류별로
    // 이름을 구분한 빈 오브젝트의 자식으로 넣어 정리할 것이기 때문이다. UI 오브젝트들은 이 @UI_Root 빈 오브젝트 아래에 생성되게 그룹화할 것이라서 필요.
    public GameObject Root
    {
        get
        {
            // 찾거나 생성
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject("@UI_Root");

            // 활성 보장
            if (!root.activeSelf)
                root.SetActive(true);

            // 씬 전환 시에도 파괴되지 않게
            Object.DontDestroyOnLoad(root);

            return root;
        }
    }

    // 스택 최상위 팝업이 일치해야만 닫을 수 있도록 안전장치
    public void ClosePopupUI(UI_Popup popup) // 안전 차원
    {
        if (_popupStack.Count == 0) // 비어있는 스택이라면 삭제 불가
            return;

        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!"); // 스택의 가장 위에있는 Peek() 것만 삭제할 수 잇기 때문에 popup이 Peek()가 아니면 삭제 못함
            return;
        }

        ClosePopupUI();
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Popup popup = _popupStack.Pop();
        _openPopupTypes.Remove(popup.GetType());
        _resourceManager.Destroy(popup.gameObject);
        popup = null;
        _order--; // order 줄이기

        // **시간 재개:** 팝업을 모두 닫은 순간
        if (_popupStack.Count == 0)
        {
            Time.timeScale = 1f;
            string scene = SceneManager.GetActiveScene().name;
            if (scene.Contains("Stage") && _gameManager != null)
                _gameManager.IsTimerPaused = false;

            // **커서 숨기기**: 메인 씬이 아니면
            if (scene != "Main")
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }
    // 기존 팝업을 모두 닫습니다.
    public void ClearPopupUI()
    {
        CloseAllPopupUI();
    }

    // 현재 띄워진 씬용 UI를 파괴하고 레퍼런스를 정리합니다.
    public void ClearSceneUI()
    {
        var root = Root.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child.GetComponent<UI_Scene>() != null)
                Object.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 전체 정리: 팝업 + 씬 UI 모두 지워야 할 때 호출
    /// </summary>
    public void ClearAllUI()
    {
        ClearPopupUI();
        ClearSceneUI();
    }
}
