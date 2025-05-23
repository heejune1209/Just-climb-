using System;
using UnityEngine;

// SaveData는 게임 전반의 모든 지속 데이터를 담는 루트 컨테이너
// JsonUtility로 파일 ↔ 메모리 변환 시 최상위 루트로 사용되는 직렬화 모델

[Serializable]
public struct SerializableVector3
{
    public float x, y, z;
    public SerializableVector3(float x, float y, float z)
    {
        this.x = x; this.y = y; this.z = z;
    }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

// JSON 키 이름이 SaveData 클래스(혹은 Serializable 타입)의
// public 필드 이름과 정확히 일치해야만 직렬화,역직렬화가 제대로 됨.
[Serializable]
public class SaveData
{
    // 플레이어 보유 재화
    public int gold = 0;
    public int gems = 0;

    // 선택된 캐릭터 이름
    public string selectedCharacter = "Default";

    // 인벤토리 아이템 배열
    public InventoryItem[] items = new InventoryItem[0];

    public bool[] stageClears = new bool[0];    // 초기 크기 0
    public SerializableVector3[] stageFlagPositions = new SerializableVector3[0];   

    public int[] stageRewards = new int[0];     // 획득 보석 개수
    public float[] stageTimes = new float[0];   // 최단 클리어 타임 (초)
    public float[] stagePlayTimes = new float[0];   // 현재 플레이 시간 (초)
}
