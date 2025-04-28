using UnityEngine;

public class UI_Inven : UI_Scene
{
    // UI_Inven (UI_Inven.cs)
    // UI_Scene 상속 → 씬 UI로 띄워짐
    // GridPanel 자식 슬롯 모두 삭제 후, 인벤토리 아이템(8개) 생성
    // MakeSubItem<T>로 슬롯 프리팹 인스턴스화 ​
    enum GameObjects
    {
        GridPanel
    }

    void Start()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();

        // 1) GridPanel 바인딩
        Bind<GameObject>(typeof(GameObjects)); // GridPanel 오브젝트 바인딩

        GameObject gridPanel = Get<GameObject>((int)GameObjects.GridPanel);
        // 2) 기존 슬롯 모두 제거
        foreach (Transform child in gridPanel.transform) // 모든 아이템 슬롯들 파괴 (미리 파괴하고 시작)
            Managers.Resource.Destroy(child.gameObject);

        // 3) 인벤토리 슬롯(8개) 생성
        for (int i = 0; i < 8; i++)
        {
            GameObject item = Managers.UI.MakeSubItem<UI_Inven_Item>(gridPanel.transform).gameObject; // GridPanel의 자식으로하여 인벤트리 슬롯들 프리팹 생성
            // UI_Inven_Item invenItem = Util.GetOrAddComponent<UI_Inven_Item>(item); -> Extension 메서드를 안쓰면 이렇게 해야함                                                                                               
            UI_Inven_Item invenItem = item.GetOrAddComponent<UI_Inven_Item>(); // 확장 메서드로 컴포넌트 보장
            // 이때 프리펩에 UI_Inven_Item 컴포넌트가 추가되었기 때문에 UI_Inven_Item의 Init()이 실행됨
            // 각 슬롯에 UI_Inven_Item 컴포넌트 보장 후 SetInfo 실행
            invenItem.SetInfo($"Sword{i}");
        }
    }
}