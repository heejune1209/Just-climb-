using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using JustClimb.Items;
using JustClimb.Data;

/// <summary>
/// 메인 게임 데이터 저장 클래스
/// 서버 측 SaveData와 완전히 호환되는 구조
/// </summary>
[Serializable]
public class SaveData
{
    // 골드 개수
    [JsonProperty("gold")]
    public int gold = 0;

    // 보석 개수
    [JsonProperty("gems")]
    public int gems = 0;

    // 현재 선택된 캐릭터의 이름
    [JsonProperty("selectedCharacter")]
    public string selectedCharacter = "Default";

    // 보유 중인 아이템 리스트 (서버 호환용 DTO 사용)
    [JsonProperty("items")]
    public List<InventoryItemDto> items = new List<InventoryItemDto>();

    // 스테이지 씬에서 튜토리얼을 이미 띄웠는지 여부
    [JsonProperty("tutorialDisplayed")]
    public bool tutorialDisplayed = false;

    // 스테이지별 클리어 여부 (true = 클리어)
    [JsonProperty("stageClears")]
    public List<bool> stageClears = new List<bool>();

    // 스테이지별 깃발(체크포인트) 위치 저장 (서버 호환용 DTO 사용)
    [JsonProperty("stageFlagPositions")]
    public List<SerializableVector3Dto> stageFlagPositions = new List<SerializableVector3Dto>();

    // 스테이지별 최고 보상(획득 보석 개수)
    [JsonProperty("bestGemRewards")]
    public List<int> bestGemRewards = new List<int>();

    // 스테이지별 최단 클리어 타임(초) - 개인 기록용
    [JsonProperty("bestClearTimes")]
    public List<float> bestClearTimes = new List<float>();

    // 스테이지별 최소 사망 횟수 - 개인 기록용
    [JsonProperty("bestDeathCounts")]
    public List<int> bestDeathCounts = new List<int>();

    // 유지되는 필드들 (플레이 중 임시 저장용)
    // 중간 저장된 진행 시간(일시정지 등)
    [JsonProperty("currentPlayTimes")]
    public List<float> currentPlayTimes = new List<float>();
    // 중간 저장된 사망 횟수(일시정지 등)
    [JsonProperty("currentDeathCounts")]
    public List<int> currentDeathCounts = new List<int>();

    // 업적 관련 데이터 (클라이언트 캐싱용)
    [JsonProperty("achievementProgress")]
    public AchievementProgressDto achievementProgress = new AchievementProgressDto();

    // 업적 보상 수령 상태 (클라이언트 캐싱용, 서버는 별도 테이블)
    [JsonProperty("achievementRewards")]
    public Dictionary<string, bool> achievementRewards = new Dictionary<string, bool>();

    // Steam 업적 달성 여부 (클라이언트 캐싱용, 서버는 별도 테이블)
    [JsonProperty("achievementUnlocked")]
    public Dictionary<string, bool> achievementUnlocked = new Dictionary<string, bool>();

    // SaveData 구조 변경 시 버전 관리용 (서버와 맞춤)
    [JsonProperty("version")]
    public int version = 5;  // 데이터베이스 구조 간소화 버전
    
    #region Legacy Support Methods
    
    /// <summary>
    /// 기존 InventoryItem 리스트와 호환성을 위한 변환 메서드
    /// </summary>
    public List<InventoryItem> GetLegacyItems()
    {
        var legacyItems = new List<InventoryItem>();
        foreach (var dto in items)
        {
            legacyItems.Add(dto.ToInventoryItem());
        }
        return legacyItems;
    }
    
    /// <summary>
    /// 기존 InventoryItem 리스트를 DTO로 변환하여 설정
    /// </summary>
    public void SetLegacyItems(List<InventoryItem> legacyItems)
    {
        items.Clear();
        foreach (var item in legacyItems)
        {
            items.Add(new InventoryItemDto(item));
        }
    }
    
    /// <summary>
    /// 기존 SerializableVector3 리스트와 호환성을 위한 변환 메서드
    /// </summary>
    public List<SerializableVector3> GetLegacyFlagPositions()
    {
        var legacyPositions = new List<SerializableVector3>();
        foreach (var dto in stageFlagPositions)
        {
            legacyPositions.Add(dto.ToSerializableVector3());
        }
        return legacyPositions;
    }
    
    /// <summary>
    /// 기존 SerializableVector3 리스트를 DTO로 변환하여 설정
    /// </summary>
    public void SetLegacyFlagPositions(List<SerializableVector3> legacyPositions)
    {
        stageFlagPositions.Clear();
        foreach (var pos in legacyPositions)
        {
            stageFlagPositions.Add(new SerializableVector3Dto(pos));
        }
    }
    
    #endregion
}
