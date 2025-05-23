using JustClimb.Items;
using UnityEngine;

public class DataManagerTester : MonoBehaviour
{
    void Start()
    {
        // 1) 초기화
        Managers.Instance.Data.Init();

        // 2) 초기 값 로그
        Debug.Log("[Test] Initial Gold: " + Managers.Instance.Data.Current.gold);
        Debug.Log("[Test] Initial Gems: " + Managers.Instance.Data.Current.gems);
        //Debug.Log("[Test] Initial testItem Count: " + Managers.Data.GetItemCount("testItem"));

        // 3) 값 변경
        //Managers.Data.AddGold(100);
        //Managers.Data.AddGems(500);
        //Managers.Data.SetItemCount("testItem", 5);

        //Managers.Data.ClearAllItems();

        Managers.Instance.Data.Load();

        //// 4) 변경 후 값 로그
        //Debug.Log("[Test] After AddGold(100): " + Managers.Data.Current.gold);
        ////Debug.Log("[Test] After SetItemCount(\"testItem\", 5): "
        ////          + Managers.Data.GetItemCount("testItem"));
        //Debug.Log("[Test] After AddGems(500): " + Managers.Data.Current.gems);

        //// 5) 강제 리로드
        //Managers.Data.Load();

        //// 6) 로드 후 값 로그
        //Debug.Log("[Test] After Reload Gold: " + Managers.Data.Current.gold);
        ////Debug.Log("[Test] After Reload testItem Count: "
        ////          + Managers.Data.GetItemCount("testItem"));
        //Debug.Log("[Test] After Reload Gems: " + Managers.Data.Current.gems);

        //// 7) 파일 위치 안내
        //Debug.Log("[Test] save.json path: "
        //          + Application.persistentDataPath + "/save.json");
    }
}
