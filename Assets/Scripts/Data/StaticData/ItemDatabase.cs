using System.Collections.Generic;
using UnityEngine;
using JustClimb.Items;  

// 정적 밸런스 정보는 전부 ItemDatabase가 책임
public class ItemDatabase
{
    // 키 타입을 string → ItemType enum 으로 변경
    Dictionary<ItemType, ItemData> _dict;

    // Resources/ScriptableObjects/Items 폴더에서 모든 ItemData.asset을 로드하여
    // ItemType을 키로 하는 _dict에 담아둡니다.
    public void Init()
    {
        // SO 전부 불러오기
        var datas = Resources.LoadAll<ItemData>("ScriptableObjects/Items");

        // 변경: _dict 초기화 시 key 타입도 ItemType으로
        _dict = new Dictionary<ItemType, ItemData>();

        foreach (var d in datas)
        {
            // 변경: d.itemId 필드가 이제 ItemType enum
            var key = d.itemid;

            // 중복 검사
            if (_dict.ContainsKey(key))
            {
                Debug.LogWarning($"[ItemDatabase] Duplicate ItemType detected: {key}");
            }
            else
            {
                _dict.Add(key, d);
            }
        }
    }

    // 단일 아이템 정의 조회
    // 변경: 파라미터 타입을 string id → ItemType id 로 변경
    public ItemData Get(ItemType id)
    {
        if (_dict.TryGetValue(id, out var data))
            return data;
        Debug.LogError($"[ItemDatabase] ItemData not found for ItemType: {id}");
        return null;
    }

    // 모든 아이템 정의를 ItemType→ItemData 사전으로 반환
    // 변경: 반환 사전의 키 타입도 ItemType으로
    public Dictionary<ItemType, ItemData> GetAllItemDefinitions()
    {
        return new Dictionary<ItemType, ItemData>(_dict);
    }
}
