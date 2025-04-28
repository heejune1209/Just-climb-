using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UI_Button : UI_Popup
{
    // UI_Button (UI_Button.cs)
    // UI_Popup 상속 → 팝업으로 띄워지고 닫을 수 있음
    // 버튼 · 텍스트 · 이미지 · 빈 오브젝트를 Enum 기반 바인딩
    // 클릭 시 카운터 증가, 드래그 시 아이콘 이동 ​

    // UI 요소를 Enum으로 정의하여 Bind 메서드에서 사용
    // Enum을 사용하면 코드 가독성이 높아지고, 실수를 줄일 수 있음
    enum Buttons
    {
        PointButton
    }

    enum Texts
    {
        PointText,
        ScoreText,
    }

    enum GameObjects
    {
        TestObject,
    }

    enum Images
    {
        ItemIcon,
    }

    private void Start()
    {
        Init();
    }

    public override void Init()
    {
        // 팝업 캔버스 세팅
        base.Init(); // 📜UI_Button 의 부모인 📜UI_PopUp 의 Init() 호출

        // 1) 자식 오브젝트 바인딩
        // 각 Enum 이름에 해당하는 자식 오브젝트를 찾아 _objects 딕셔너리에 저장 ​
        Bind<Button>(typeof(Buttons)); // 버튼 오브젝트들 가져와 dictionary인 _objects에 바인딩. 
        Bind<TextMeshProUGUI>(typeof(Texts));  // 텍스트 오브젝트들 가져와 dictionary인 _objects에 바인딩. 
        Bind<GameObject>(typeof(GameObjects));  // 빈 오브젝트들 가져와 dictionary인 _objects에 바인딩. 
        Bind<Image>(typeof(Images));  // 이미지 오브젝트들 가져와 dictionary인 _objects에 바인딩. 


        // (확장 메서드) 버튼(go)에 UI_EventHandler를 붙이고 액션에 OnButtonClicked 함수를 OnClickHandler (디폴트)등록한다.
        // 즉, 확장 메서드는 다른 클래스(보통 static 클래스)에 있는 정적 메서드를,
        // 마치 원하는 타입(여기서는 GameObject)의 인스턴스 메서드인 것처럼 쓸 수 있게 해 주는 문법적 편의입니다.
        // 버튼 클릭 시 OnButtonClicked가 호출
        // 2) 클릭 이벤트 연결
        GetButton((int)Buttons.PointButton).gameObject.BindEvent(OnButtonClicked);
        // 즉, UI_Base에 바인딩해 둔 Button 컴포넌트를 꺼내서, 그 컴포넌트가 붙어 있는 GameObject를 꺼내고,
        // 그 GameObject에 BindEvent 메서드를 호출하여 OnButtonClicked를 연결합니다.
        // 근데 여기서 마치 GameObject의 인스턴스 메서드인 것처럼 보이지만,
        // — 실제로는 Extension 클래스에 정의된 확장 메서드를 호출합니다.
        // — 내부에선 UI_Base.BindEvent(go, OnButtonClicked)를 수행해서 클릭 이벤트를 연결한다.

        // 이미지(go)에 📜UI_EventHandler를 붙이고 파라미터로 넘긴 람다 함수를 OnDragHandler 액션에 등록한다.
        // 3) 드래그 이벤트 연결 (아이콘 따라다니기)
        GameObject go = GetImage((int)Images.ItemIcon).gameObject;
        // 이미지 드래그 시 해당 아이콘을 따라다니게 함
        BindEvent(go, (PointerEventData data) => { go.transform.position = data.position; }, Define.UIEvent.Drag);
    }

    int _score = 0;

    // 내부 _score를 증가시키고, ScoreText를 갱신하는 단순 클릭 카운터
    public void OnButtonClicked(PointerEventData data)
    {
        _score++;
        GetText((int)Texts.ScoreText).text = $"Score : {_score}";
    }


}



    