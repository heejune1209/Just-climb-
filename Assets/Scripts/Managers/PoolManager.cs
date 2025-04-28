using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

public class PoolManager : MonoBehaviour
{
    // PoolManager: 여러개의 Pool 객체들을 관리. 즉 여러개의 풀 관리.
    // Manager로 부터 사용
    // ResourceManager 를 보조 하는 역할.
    // @Pool_Root 👉 전체 풀 관리
    // 여러개의 풀
    // 각각의 풀에 속한 오브젝트들(재활용 대상)
    #region Pool

    // PoolManager 는 여러개의 Pool 들을 가지고 있다.
    // - @Pool_Root          👉 전체의 풀들을 한데 모음
    // - UnityChan_Root      👉 UnityChan_Root 원본 프리팹을 통해 만든 대기중인 UnityChan_Root 오브젝트들 모아둔 부모오브젝트, 풀
    // - Bird_Root 👉 Bird_Root 원본 프리팹을 통해 만든 대기중인 Bird_Root 들 모아둔 부모오브젝트, 풀
    // UnityChan_Root, Bird_Root 같은게 각각 하나의 Pool 객체가 된다.
    class Pool
    {
        public GameObject Original { get; private set; } // 원본 프리팹
        public Transform Root { get; set; } // 풀 이름 ex. UnityChan_Root, Bird_Root

        Stack<Poolable> _poolStack = new Stack<Poolable>(); // 풀에 모여 있는 오브젝트(📜Poolable 붙어있는 상태)들 스택으로 관리

        // 하나의 풀 초기화 (원본 프리팹 original, 풀링할 오브젝트 개수 count)
        // Original 원본프리팹
        // 풀링에 사용할 오브젝트들을 Root (ex. UnityChan_Root) 오브젝트 산하에 둘 것
        // count개수의 오브젝트를 생성하고 풀링하기 위해 스택에 넣어주기. 밑에 Push 참고
        public void Init(GameObject original, int count = 5)
        {
            Original = original;
            // UnityChan_Root 빈 오브젝트 생성. 
            Root = new GameObject().transform;
            // 위의 코드는 
            // GameObject tmp = new GameObject();  // 빈 게임오브젝트 생성
            // Root = tmp.transform;               // 그 오브젝트의 Transform을 Root에 할당
            // 위 두줄과 똑같은 의미다.
            // 즉, new GameObject()가 반환한 오브젝트의 Transform 컴포넌트를 가져옵니다.
            Root.name = $"{original.name}_Root";

            // count 개수의 오브젝트들을 UnityChan_Root의 자식으로. 이 5 개를 재활용할 것 👉 오브젝트 풀링 
            for (int i = 0; i < count; i++)
                Push(Create());
        }

        // 원본 프리팹으로부터 풀링에 사용할 오브젝트를 생성한다. 그리고 이 오브젝트를 📜Poolable로서 리턴.
        // 이름은 원본 프리팹과 이름 같게.
        Poolable Create()
        {
            GameObject go = Object.Instantiate<GameObject>(Original);
            go.name = Original.name; // 뒤에 붙는 (Clone) 없앰. 원본 프리팹과 이름 같게.
            return go.GetOrAddComponent<Poolable>();
        }
        
        // 풀에 넣어준다는 것은 곧 오브젝트를 비활성화 해놓고 사용될 때까지 대기한다는 것이다. (마치 Destroy 하는 효과)
        // 풀에서 대기중인 오브젝트는 Root의 자식이어야 함
        // 풀에서 대기중일땐 UnityChan_Root 의 자식이다가 진짜 활성화되어 사용될 떈 풀에서 빠져나와 게임 중에서의 원래 부모의 자식으로 부모 바꿔 설정할 것
        // 부모는 Root로, 비활성화, 스택에 넣어 대기시키기
        public void Push(Poolable poolable) // 풀에 넣어주기 (오브젝트 비활성화)
        {
            if (poolable == null)
                return;

            poolable.transform.parent = Root;
            poolable.gameObject.SetActive(false);
            poolable.IsUsing = false;

            _poolStack.Push(poolable);
        }

        // parent 👉 대기 상태가 아닌 활성화 상태로 풀 밖에서 게임 안에서 사용될 때의 부모. 원래 부모.
        // 풀에 빼낸다는 것은 곧 오브젝트를 활성화 해서 사용하는 것이다.생성되는 것 같은효과.
        // poolable 에다가 오브젝트 받고 리턴
        // 스택이 비어있지 않다면 재활용할 수 있는 대기 상태인 오브젝트가 있다는 것이니 그것을 사용하도록 한다.스택에서 빼서 사용
        // 스택이 비어있다면 새로 만들어야한다.Instantiate 필요.Create 호출.
        // 활성화 (poolable.gameObject로 접근해서 활성화)
        // 풀에서 대기 중일때의 부모로부터 원래 게임에서의 부모로 설정.
        public Poolable Pop(Transform parent) // 풀로부터 꺼내오기 (오브젝트 활성화)
        {
            Poolable poolable;

            if (_poolStack.Count > 0) // 스택(대기상태)이 빈 크기 X 즉 하나라도 재활용 할 수 있는 애가 있다면 
                poolable = _poolStack.Pop();
            else // 스택(대기상태)이 지금 비었다면 재활용 할 수 있는 애가 없으므로 새로 만들어야
                poolable = Create();

            poolable.gameObject.SetActive(true);  // 활성화 (poolable.gameObject로 접근해서 활성화)

            // DontDestroyOnLoad 해제 용도
            if (parent == null)
                poolable.transform.parent = Managers.Scene.CurrentScene.transform;

            // poolable 👉 풀에서 꺼낸 오브젝트의 Poolable
            poolable.transform.parent = parent; // 파라미터로 받은 parent 를 부모로 설정
            poolable.IsUsing = true;

            return poolable;
        }
    }
    #endregion

    // _pool 풀들을 미리 로드해와 모아둘 그 ‘Pool’
    // 관련있는 오브젝트들을 모으는 것도 하나의 Pool 이다. (위의 Pool 클래스)
    // 풀도 여러개일 수 있다.
    // ex)
    // 무기 프리팹으로 생성되어 재활용할 무기 오브젝트들 모여있는 풀
    // 플레이어 프리팹으로 생성되어 재활용할 플레이어 오브젝트들 모여있는 풀
    // 나무 프리팹으로 생성되어 재활용할 나무 오브젝트들 모여있는 풀

    // 이들을 모아둔 Dictionary이므로 즉, 게임 내의 모든 전체 풀들을 _pool에서 관리.
    // Key는 원본 프리팹의 이름으로 쓸 것!
    // 풀들을 _root(@Pool_Root)의 자식으로 묶어 정리할 것이다.
    Dictionary<string, Pool> _pool = new Dictionary<string, Pool>();
    Transform _root;

    // 풀링 할 오브젝트들을 모아서 그룹화해 정리할 @Pool_Root 오브젝트를 만든다.
    // 풀링 오브젝트들은 이 오브젝트의 자식으로 묶일 것이며
    // 게임 내내 유지되도록 @Pool_Root 오브젝트를 DontDestroyOnLoad 처리 한다.
    public void Init()
    {
        if (_root == null)
        {
            _root = new GameObject { name = "@Pool_Root" }.transform;
            Object.DontDestroyOnLoad(_root);
        }
    }

    // Push 다 사용한 오브젝트 풀에 다시 넣어 대기 상태로 만들기
    public void Push(Poolable poolable)
    {
        string name = poolable.gameObject.name;
        if (_pool.ContainsKey(name) == false)
        {
            GameObject.Destroy(poolable.gameObject);
            return;
        }

        _pool[name].Push(poolable);

        // 그냥 _pool[name].Push(poolable)을 해주면 땡
        // 이름(Key)과 일치하는 해당 풀에 해당 오브젝트 poolable을 Push 함수 호출해 넣어줌
        // 풀링하지 않는 오브젝트는 파괴
    }

    // Pop 풀로부터 사용할 오브젝트 리턴
    // _pool Dictionary에서 보관 중인 original 프리팹 이름에 해당하는 Key의 Value인 풀을 리턴한다.
    // 리턴한 Pool에서 Pop 호출
    // 풀 Stack(풀 마다 본인의 오브젝트들 보관하는_poolStack)에서 가장 위에 있는 오브젝트를 pop하고(후입선출) 활성화하고 그 오브젝트의 부모를 parent로 한다.
    // CreatePool(original); 👉 디폴트로 5 개 생성
    public Poolable Pop(GameObject original, Transform parent = null)
    {
        if (_pool.ContainsKey(original.name) == false) // Key는 원본 프리팹 이름으로 저장되므로 해당 프리팹으로 만든 오브젝트풀이 있나 검색. 
            CreatePool(original); // 없다면 새로운 풀을 만든다. 

        return _pool[original.name].Pop(parent); // 풀이 없다면 여기서 런타임 에러 날 것이므로 위의 과정을 해주는 것. original.name인 풀이 아직 없다면 만들어주기.
    }

    // CreatePool 풀 만들기
    // 풀을 생성하고 풀의 Init 함수 호출
    // 풀들은 @Pool_Root(_root)의 자식이어야 한다.
    // _pool Dictionary에 추가해준다.
    // Key는 프리팹 이름인 original.name으로 풀을 추가해준다.
    public void CreatePool(GameObject original, int count = 5)
    {
        Pool pool = new Pool();
        pool.Init(original, count); // Init 을 통해 해당 Pool은 DontDestroyOnLoad가 된다.
        pool.Root.parent = _root;

        _pool.Add(original.name, pool);
    }

    // GetOriginal 프리팹 가져오기
    // ResourceManager 의 Load 함수에서 호출 시킬 것이다.
    // 그래서 public 이고
    // original.name을 사용하지 않고 그냥 name 매개 변수로 설정.
    // _pool Dictionary을 통해 Pool Value의 Original에 원본 프리팹 담고 있으니 이를 리턴해주면 된다.
    // Key가 없을 수도 있으니 위에 미리 체크.없다면 null 리턴.
    public GameObject GetOriginal(string name)
    {
        if (_pool.ContainsKey(name) == false)
            return null;
        return _pool[name].Original;
    }

    // 여러 가지의 Pool을 전부 날리자. Dictionary도 비우기.
    // 풀에 비활성화 상태로 대기 중인 오브젝트들은
    // _root(@Pool_Root) child(UnityChan_Root)의 자식들로 있는 상태일테니 이것들도 다 날라갈 것..
    // 다른 씬에서는 해당 풀에 있는 오브젝트들을 다신 안 쓰는 경우가 생기면 이렇게 풀을 다 날려 버리는 기능이 필요할 것이다.
    public void Clear()
    {
        foreach (Transform child in _root)
            GameObject.Destroy(child.gameObject);

        _pool.Clear();
    }
}
