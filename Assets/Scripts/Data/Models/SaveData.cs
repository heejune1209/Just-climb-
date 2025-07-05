using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using JustClimb.Items;

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

[Serializable]
public class SaveData
{
    // 골드 개수
    [JsonProperty("gold")]
    public int gold = 0;

    // 보석 개수
    [JsonProperty("gems")]
    public int gems = 0;

    // // 현재 선택된 캐릭터의 이름
    [JsonProperty("selectedCharacter")]
    public string selectedCharacter = "Default";

    // 보유 중인 아이템 리스트
    [JsonProperty("items")]
    public List<InventoryItem> items = new List<InventoryItem>();

    // 스테이지 씬에서 튜토리얼을 이미 띄웠는지 여부
    [JsonProperty("tutorialDisplayed")]
    public bool tutorialDisplayed = false;

    // 스테이지별 클리어 여부 (true = 클리어)
    [JsonProperty("stageClears")]
    public List<bool> stageClears = new List<bool>();

    // 스테이지별 깃발(체크포인트) 위치 저장
    [JsonProperty("stageFlagPositions")]
    public List<SerializableVector3> stageFlagPositions = new List<SerializableVector3>();

    // 스테이지별 최고 보상(획득 보석 개수)
    [JsonProperty("bestGemRewards")]
    public List<int> bestGemRewards = new List<int>();

    // 스테이지별 최단 클리어 타임(초) - 개인 기록용
    [JsonProperty("bestClearTimes")]
    public List<float> bestClearTimes = new List<float>();

    // 스테이지별 최소 사망 횟수 - 개인 기록용
    [JsonProperty("bestDeathCounts")]
    public List<int> bestDeathCounts = new List<int>();

    // ✅ 유지되는 필드들 (플레이 중 임시 저장용)
    // 중간 저장된 진행 시간(일시정지 등)
    [JsonProperty("currentPlayTimes")]
    public List<float> currentPlayTimes = new List<float>();
    // 중간 저장된 사망 횟수(일시정지 등)
    [JsonProperty("currentDeathCounts")]
    public List<int> currentDeathCounts = new List<int>();

    // SaveData 구조 변경 시 버전 관리용
    [JsonProperty("version")]
    public int version = 2;  // 버전 업데이트
}
