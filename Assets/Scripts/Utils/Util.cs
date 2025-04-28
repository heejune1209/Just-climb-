using Unity.VisualScripting;
using UnityEngine;

public class Util
{
    // Util (Util.cs)
    // 계층 탐색용 FindChild<T>, FindChild(GameObject)
    // 컴포넌트 보장 GetOrAddComponent<T>

    // 주어진 GameObject의 자식들 중에서, Transform 컴포넌트를 이름으로 검색한 뒤, 해당 오브젝트(GameObject)를 반환합니다.
    // 이름이 null이거나 빈 문자열이면, 첫 번째 자식 오브젝트를 그냥 반환합니다.
    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        // 제네릭 버전 FindChild<Transform>을 호출해 Transform을 찾고,
        Transform transform = FindChild<Transform>(go, name, recursive);
        // 결과가 null이면 null 반환,
        if (transform == null)
            return null;
        // 그렇지 않으면 transform.gameObject를 돌려줍니다.
        // transform.gameObject → 그 컴포넌트가 붙어 있는 GameObject
        // 트랜스폼은 위치(Position)·회전(Rotation)·크기(Scale)뿐만 아니라 부모‑자식 계층(Parent‑Child Hierarchy)의 역할도 한다.
        return transform.gameObject;
    }

    // 자식 계층 전체에서 <T> 컴포넌트를 검색
    // go : 검색을 시작할 루트 오브젝트
    // 일치시킬 자식 오브젝트의 이름 (생략 시 첫 번째 발견되는 것)
    // recursive : false면 직계 자식만, true면 하위 계층 전체에서 검색
    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : Object
    {
        // go가 null이면 즉시 null 반환.
        if (go == null)
            return null;

        if (recursive == false)
        {
            // 1. 직계 자식만 순회
            // 직계 자식 순회
            // - childCount 만큼 반복하며
            // - (이름 미지정 OR 이름 일치) && GetComponent<T>() 결과가 null이 아닐 때 반환
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else
        {
            // 2. 하위 계층 전체 검색
            // GetComponentsInChildren<T>() 로 해당 타입의 모든 컴포넌트 배열을 가져와
            // 이름 조건에 맞으면 첫 번째 발견된 것을 반환
            // 수정된 코드: inactive 자식까지 모두 검색
            foreach (T component in go.GetComponentsInChildren<T>(true))
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }
        // 검색에 실패하면 null.
        return null;
    }

    // GetComponent<T>()가 없으면 AddComponent<T>()로 새로 붙임
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();

        if (component == null)
            component = go.AddComponent<T>();

        return component;
    }
}
