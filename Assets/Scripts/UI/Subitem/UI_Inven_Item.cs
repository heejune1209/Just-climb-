using TMPro;
using UnityEngine;

public class UI_Inven_Item : UI_Base
{
    // UI_Inven_Item (UI_Inven_Item.cs)
    // UI_Base 상속 → 단일 슬롯 항목
    // 아이콘·텍스트 바인딩, 클릭 이벤트로 로그 출력
    enum GameObjects // 구성 UI 오브젝트가 2개 뿐이라 그냥 GameObjects 한 곳에 묶음
    {
        ItemIcon,
        ItemNameText,
    }

    string _name;

    void Start()
    {
        Init();
    }

    public override void Init()
    {
        // 1) 아이콘·텍스트 바인딩
        Bind<GameObject>(typeof(GameObjects)); // ItemIcon, ItemNameText 오브젝트 바인딩하여 상속받은 Dictionary _objects에 저장.
        // 2) 텍스트 설정
        Get<GameObject>((int)GameObjects.ItemNameText).GetComponent<TextMeshProUGUI>().text = _name; // ItemNameText 텍스트 UI의 텍스트를 _name 으로 변경

        // 3) 아이콘 클릭 이벤트
        // 확장 메소드 BindEvent 사용. ItemIcon 클릭시 해당 람다함수 실행하게 이벤트 바인딩
        Get<GameObject>((int)GameObjects.ItemIcon).BindEvent((PointerEventData) => { Debug.Log($"아이템 클릭! {_name}"); }); 
    }

    // 외부에서 아이템명 설정
    public void SetInfo(string name)
    {
        _name = name;
    }
}