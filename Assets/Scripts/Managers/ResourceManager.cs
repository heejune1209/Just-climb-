using UnityEngine;
using Zenject;
using static UnityEngine.UI.Image;

public class ResourceManager : IResourceManager
{
    // DI 주입받을 매니저
    [Inject] private IPoolManager _poolManager;
    [Inject] private DiContainer _container;

    // ResourceManager
    // 여러 스크립트 내에서 Resources.Load 함수를 사용하여 에셋을 불러오거나 아니면 직접 변수에 에셋을 할당하는 식으로 하면,
    // 여러 곳에서 불러오는 작업을 하게 되므로 잘못되도 추적이 어렵고 수정도 어려워 진다.
    // 오로지 에셋을 불러오는 작업만 하는 스크립트로 따로 만들었다.Input Manager 처럼!

    // 제네릭을 사용
    // Load 제네릭 사용자 지정 함수 정의
    // where T : Object 부모 클래스가 Object 인 타입만 받을 수 있도록 제약을 걸음(C# 문법)
    // Resources.Load<T>(path)
    // Resource 폴더를 시작 위치로 한 "path"에 해당하는 T 타입의 에셋 파일을 불러오고 이를 리턴한다.

    // 프리팹을 로드하는 것 또한 풀에 있으면 풀에서 가져온다. Instantiate을 줄이려고 하듯, 로드 또한 최대한 줄이기 위해!
    // 프리팹을 로드할 때 프리팹 또한 Pool에 있으면 로드하지 않고 거기서 가져 온다.
    // 이미 Pool 에 프리팹으로 생성한 오브젝트가 있다면 Pool의 Original에 저장되어 있을 것이기 때문에 GetOriginal 함수를 통해 가져올 수 있다.
    // 풀에 없는 프리팹이라면 힘겹게 로컬 폴더로부터 Resources.Load<T>(path)을 호출해 로딩.
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        if (typeof(T) == typeof(GameObject))
        {
            string name = path;
            int index = name.LastIndexOf('/'); // '/' 뒤의 이름 추출. 
            if (index >= 0)
                name = name.Substring(index + 1); // 이게 바로 프리팹의 이름.

            GameObject go = _poolManager.GetOriginal(name);
            if (go != null)
                return go as T;
        }

        // 풀에서 못 찾았다면 힘들게 로딩
        return Resources.Load<T>(path); // UnityEngine의 Resource.
    }

    public T[] LoadAll<T>(string path) where T : UnityEngine.Object
    {
        return Resources.LoadAll<T>(path);
    }

    // Instantiate 사용자 지정 함수 정의
    // Load 를 사용해 prefab에 path 에 해당하는 GameObject 타입의 에셋을 할당한다.
    // Resource의 Prefab 에서 찾아 온다. ($"Prefabs/{path}")
    // 성공적으로 찾았다면 Object.Instantiate(prefab, parent) 리턴
    // 그냥 Instantiate이 아닌 Object.Instantiate 이라고 명시해준 이유 
    // 그냥 Instantiate 라고 명시하면 지금 정의하고 있는 이 사용자 지정 함수 Instantiate 라고 인식되서 재귀호출 되므로.
    // 못 찾았다면 null 리턴
    public GameObject Instantiate(string path, Transform parent = null, int count = 5)
    {
        Debug.Log($"ResourceManager: Instantiate 호출 - path: {path}");
        // 이미 Prefabs/로 시작하면 그대로, 아니면 붙이기
        string loadPath = path.StartsWith("Prefabs/") ? path : $"Prefabs/{path}";
        GameObject original = Load<GameObject>(loadPath);
        if (original == null)
        {
            Debug.Log($"Failed to load prefab : {loadPath}");
            return null;
        }
        GameObject go;
        if (original.GetComponent<Poolable>() != null)
        {
            // 풀에서 꺼내기
            go = _poolManager.Pop(original, parent, count).gameObject;
        }
        else
        {
            // 아니면 일반 Instantiate
            go = Object.Instantiate(original, parent);
            go.name = original.name;
        }

        // 풀든 Instantiate든 DI 주입
        _container.InjectGameObject(go);
        return go;
    }

    /// <summary>
    /// 풀링 또는 일반 Instantiate 후, 
    /// 월드 위치·회전까지 한 번에 설정.
    /// </summary>
    public GameObject Instantiate(string path, Vector3 worldPos, Quaternion worldRot, Transform parent = null , int count = 5)
    {
        // 1) 기존 Instantiate 로 풀링/DI + parent 바인딩
        var go = Instantiate(path, parent, count);
        if (go == null) return null;

        // 2) 월드 위치·회전 설정
        go.transform.SetPositionAndRotation(worldPos, worldRot);
        return go;
    }

    // Destroy 사용자 지정 함수 정의
    // 마찬가지로 재귀를 막기 위해 Object.까지 붙여서 Object.Destroy 호출
    // 만약 풀링이 필요한 애라면 파괴하는게 아니라 풀링 매니저에게 위탁해서 단순 비활성화시키기
    public void Destroy(GameObject go)
    {
        if (go == null)
            return;

        Poolable poolable = go.GetComponent<Poolable>();
        if (poolable != null)
        {
            _poolManager.Push(poolable);
            return;
        }

        Object.Destroy(go);
    }
}
