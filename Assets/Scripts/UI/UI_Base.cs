using System;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

abstract public class UI_Base : MonoBehaviour
{
    // UI_Base (UI_Base.cs, 이전 설명)
    // 모든 UI 클래스의 공통 베이스
    // 자식 오브젝트 자동 바인딩(Bind<T>)
    // 클릭·드래그 이벤트 연결(BindEvent)
    // 조회 편의 메서드(GetButton, GetText 등)

    // 타입별 오브젝트 배열 저장
    Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();

    // 이름(Enum) 기반으로 자식 오브젝트 검색·저장
    // Enum 순서와 같은 인덱스 구조로, 원하는 UI 요소를 이름으로 한 번에 바인딩할 수 있습니다.

    public abstract void Init();

    // Bind<T>(Type type) 메서드 호출 시, Enum 타입을 인자로 전달합니다.
    protected void Bind<T>(Type type) where T : UnityEngine.Object
    {
        // 이전에 Bind<T> 된 바인딩을 제거
        if (_objects.ContainsKey(typeof(T)))
            _objects.Remove(typeof(T));

        // type으로 전달된 enum의 이름 배열을 가져옵니다.
        // 예: enum Texts { PointText, ScoreText } → ["PointText", "ScoreText"]
        string[] names = Enum.GetNames(type);

        // 배열 생성 & 사전 등록
        UnityEngine.Object[] objects = new UnityEngine.Object[names.Length];
        _objects.Add(typeof(T), objects);

        // 반복문: 이름 ↔ 오브젝트 매핑
        // T 에 속하는 오브젝트들을 Dictionary의 Value인 objects 배열의 원소들에 하나하나 추가
        for (int i = 0; i < names.Length; i++)
        {
            // GameObject 타입이면 Util.FindChild(gameObject, names[i], true) 호출
            // 그 외 컴포넌트(Button, TextMeshProUGUI, Image 등)면 Util.FindChild<T>(...) 호출
            if (typeof(T) == typeof(GameObject))
                objects[i] = Util.FindChild(gameObject, names[i], true);
            else
                objects[i] = Util.FindChild<T>(gameObject, names[i], true);
            // recursive=true 로 하위 계층 전체에서 검색

            // 검색 결과가 null이면 Debug.LogError로 어느 이름이 누락됐는지 알립니다. ​
            if (objects[i] == null)
            {
                Debug.LogError($"Bind Error! {names[i]} is null");
            }

        }
    }

    // 바인딩된 오브젝트 꺼내기
    // 목적: _objects[typeof(T)][index]에 저장된 요소를 꺼내 T로 캐스팅해 반환
    protected T Get<T>(int index) where T : UnityEngine.Object
    {
        UnityEngine.Object[] objects = null;

        // 키가 존재하면 true와 함께 배열 반환
        // 키가 없으면 null 반환
        if (!_objects.TryGetValue(typeof(T), out objects))
            return null;

        return objects[index] as T;
    }

    // Get<T>를 직접 쓰지 않고, 각 UI 타입에 맞는 이름으로 호출할 수 있어 가독성 향상
    protected TextMeshProUGUI GetText(int index) { return Get<TextMeshProUGUI>(index); }
    protected Button GetButton(int index) { return Get<Button>(index); }
    protected Image GetImage(int index) { return Get<Image>(index); }
    protected GameObject GetGameObject(int index) { return Get<GameObject>(index); }


    // BindEvent(): 클릭/드래그 이벤트 연결
    public static void BindEvent(GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    {
        // go에 UI_EventHandler가 없으면 추가, 있으면 기존 컴포넌트 반환 ​
        UI_EventHandler evt = Util.GetOrAddComponent<UI_EventHandler>(go);

        // Click → OnClickHandler에 델리게이트 언바인드 후 바인드
        // Drag → OnDragHandler에 동일 처리
        //-= 로 기존에 등록된 동일 액션을 제거해 중복 실행 방지
        switch (type)
        {
            case Define.UIEvent.Click:
                evt.OnClickHandler -= action; // 혹시나 이미 있을까봐 빼줌
                evt.OnClickHandler += action;
                break;
            case Define.UIEvent.Drag:
                evt.OnDragHandler -= action; // 혹시나 이미 있을까봐 빼줌
                evt.OnDragHandler += action;
                break;
        }
    }
    // 특정 키 입력이 들어왔을 때 callback을 실행하도록 바인딩합니다.
    public static void BindKeyEvent(GameObject go, Key key, Action callback)
    {
        var handler = Util.GetOrAddComponent<UI_KeyEventhandler>(go);
        handler.AddKeyBinding(key, callback);
    }


}
