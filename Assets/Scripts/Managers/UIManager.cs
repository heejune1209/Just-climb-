using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager
{
    // UI 자동화는 “프리팹 루트만 준비해 두면, 코드가 거기에 스크립트와 Canvas를 자동으로 붙여 주고,
    // 자식 오브젝트는 이름과 최소한의 컴포넌트만 프리팹에 담아 두면, 인스펙터에서 수동으로 연결하지 않아도 런타임에 전부 바인딩 및 초기화됩니다.”

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

    // SetCanvas() : 캔버스 세팅
    public void SetCanvas(GameObject go, bool sort = true)
    {
        // go에 Canvas 컴포넌트가 없으면 추가, 있으면 그대로 사용
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        // 오버레이 모드로 고정하고, 부모 캔버스 영향을 받지 않도록 강제 정렬 모드 사용.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true; // 캔버스 안에 캔버스 중첩 경우 (자식 캔버스가 있더라도 부모 캔버스가 어떤 값을 가지던 나는 내 오더값을 가지려 할때)

        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else // soring 요청 X 라는 소리는 팝업이 아닌 일반 고정 UI
        {
            canvas.sortingOrder = 0;
        }
    }

    public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
    {
        // 프리팹 경로 자동 계산
        // name이 비어 있으면 타입명(typeof(T).Name)을 사용해
        // "UI/SubItem/{name}" 경로의 프리팹을 인스턴스화
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        // 경로에 이미 에디터 상에 만들어 둔 Prefab이 있어야 한다
        GameObject go = Managers.Resource.Instantiate($"UI/SubItem/{name}");

        // parent 지정
        // 지정된 Transform 아래에 자식으로 배치
        // 예) 인벤토리 슬롯(GridPanel) 안에 각 아이템 프리팹 추가
        if (parent != null)
            go.transform.SetParent(parent);

        // 컴포넌트 보장
        // UI_Base를 상속한 T 컴포넌트를 가져오거나 새로 붙여서 반환
        return Util.GetOrAddComponent<T>(go);
    }

    // 역할: 씬 UI를 동적으로 불러와 화면에 표시
    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        // name이 비어 있으면 타입명(typeof(T).Name)을 사용해
        // 프리팹 "UI/Scene/{name}" 인스턴스화
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        // 경로에 이미 에디터 상에 만들어 둔 Prefab이 있어야 한다
        GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");
        // UI_Scene 컴포넌트 가져오기/추가 → _sceneUI에 저장
        T sceneUI = Util.GetOrAddComponent<T>(go); // 이때 씬에 새 GameObject가 생성되고, 그 위에 UI_Inven 컴포넌트가 붙어 있다.
        // 오브젝트가 생성되면서 오브젝트의 UI_Inven.Start()를 호출
        _sceneUI = sceneUI;

        // Root 밑에 부모 설정(SetParent(@UI_Root))
        go.transform.SetParent(Root.transform);

        return sceneUI;
    }

    // 팝업 UI를 띄우고 스택에 추가
    public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        // name이 비어 있으면 타입명(typeof(T).Name)을 사용해
        // UI/Popup/{name}" 프리팹 인스턴스화
        if (string.IsNullOrEmpty(name)) // 이름을 안받았다면 T로 ㄱㄱ
            name = typeof(T).Name;

        // 경로에 이미 에디터 상에 만들어 둔 Prefab이 있어야 한다
        GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");

        // UI_Popup 컴포넌트 가져오기/추가
        T popup = Util.GetOrAddComponent<T>(go);

        string scene = SceneManager.GetActiveScene().name;
        bool isStageScene = scene.Contains("Stage");

        // **시간 멈추기:** 팝업 스택이 비어 있었으면 처음 열리는 팝업
        if (_popupStack.Count == 0)
        {
            Time.timeScale = 0f;
            if (isStageScene)
                Managers.Game.IsTimerPaused = true;
        }

        // **커서 보이기**: 메인 씬이 아니면
        if (scene != "Main")
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }


        // _popupStack.Push(popup) → 나중에 닫을 때 LIFO 보장
        _popupStack.Push(popup);

        _openPopupTypes.Add(typeof(T));
        // Root 밑에 배치 → Canvas.sortingOrder는 UI_Popup.Init()에서 설정
        go.transform.SetParent(Root.transform);

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
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject { name = "@UI_Root" };
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
        _openPopupTypes.Remove(popup.GetType());  // ← 추가
        Managers.Resource.Destroy(popup.gameObject);
        popup = null;
        _order--; // order 줄이기

        // **시간 재개:** 팝업을 모두 닫은 순간
        if (_popupStack.Count == 0)
        {
            Time.timeScale = 1f;
            string scene = SceneManager.GetActiveScene().name;
            if (scene.Contains("Stage"))
                Managers.Game.IsTimerPaused = false;

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

    public void Clear()
    {
        CloseAllPopupUI();
        _sceneUI = null;
    }

}
