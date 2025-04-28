using System;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.InputSystem;

// static 클래스로, 인스턴스를 만들 수 없습니다.
// 확장 메서드를 한곳에 모아두는 용도입니다.
public static class Extension
{
    // 확장 메서드
    // this GameObject go 부분이 핵심.
    // 컴파일러가 go.GetOrAddComponent<MyComp>()를 Extension.GetOrAddComponent(go)로 변환해 줍니다.
    // 내부에선 Util.GetOrAddComponent<T>(go)를 호출해, 이미 붙어 있으면 가져오고 없으면 새로 붙여 줍니다.
    // this 키워드가 없으면, 그냥 일반 메서드로 인식합니다.
    // this를 붙이면 확장 메서드로 인식해서 gameObject.으로 접근할수있다.
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return Util.GetOrAddComponent<T>(go);
    }

    // 확장메서드
    // 마찬가지로 go.BindEvent(...)처럼 쓰면, UI_Base.BindEvent(go, ...)를 호출하도록 해 줍니다.
    // Define.UIEvent.Click이 기본값이라 클릭 이벤트 바인딩을 더 간결하게 작성할 수 있습니다.
    public static void BindEvent(this GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    {
        UI_Base.BindEvent(go, action, type);
    }

    // 기존 BindEvent 옆에 추가
    public static void BindKeyEvent(this GameObject go, Key key, Action callback)
    {
        UI_Base.BindKeyEvent(go, key, callback);
    }
}