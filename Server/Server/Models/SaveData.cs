using System.Collections.Generic;
using Newtonsoft.Json;

namespace Server.Models
{
    /// <summary>
    /// 클라이언트-서버 간 동기화용 SaveData (간소화된 버전)
    /// 실제 데이터는 정규화된 테이블에 저장되고, 이 클래스는 캐싱/동기화용으로만 사용
    /// 클라이언트 측 SaveData와 완전히 호환
    /// </summary>
    public class SaveData
    {
        // 기본 유저 정보
        [JsonProperty("gold")]
        public int gold { get; set; }
        
        [JsonProperty("gems")]
        public int gems { get; set; }
        
        [JsonProperty("selectedCharacter")]
        public string selectedCharacter { get; set; } = "Default";
        
        [JsonProperty("items")]
        public List<InventoryItemDto> items { get; set; } = new List<InventoryItemDto>();
        
        [JsonProperty("tutorialDisplayed")]
        public bool tutorialDisplayed { get; set; }
        
        // 스테이지 관련 (클라이언트 캐싱용)
        [JsonProperty("stageClears")]
        public List<bool> stageClears { get; set; } = new List<bool>();
        
        [JsonProperty("stageFlagPositions")]
        public List<SerializableVector3Dto> stageFlagPositions { get; set; } = new List<SerializableVector3Dto>();
        
        [JsonProperty("bestGemRewards")]
        public List<int> bestGemRewards { get; set; } = new List<int>();
        
        [JsonProperty("bestClearTimes")]
        public List<float> bestClearTimes { get; set; } = new List<float>();
        
        [JsonProperty("bestDeathCounts")]
        public List<int> bestDeathCounts { get; set; } = new List<int>();
        
        [JsonProperty("currentPlayTimes")]
        public List<float> currentPlayTimes { get; set; } = new List<float>();
        
        [JsonProperty("currentDeathCounts")]
        public List<int> currentDeathCounts { get; set; } = new List<int>();
        
        // 업적 관련 (클라이언트 캐싱용)
        [JsonProperty("achievementProgress")]
        public AchievementProgressDto achievementProgress { get; set; } = new AchievementProgressDto();
        
        [JsonProperty("achievementRewards")]
        public Dictionary<string, bool> achievementRewards { get; set; } = new Dictionary<string, bool>();
        
        [JsonProperty("achievementUnlocked")]
        public Dictionary<string, bool> achievementUnlocked { get; set; } = new Dictionary<string, bool>();
        
        [JsonProperty("version")]
        public int version { get; set; } = 5;  // 데이터베이스 구조 간소화
    }
    
    /// <summary>
    /// 업적 진행률 DTO (클라이언트 캐싱용)
    /// 클라이언트 측 AchievementProgressDto와 완전히 호환
    /// </summary>
    public class AchievementProgressDto
    {
        [JsonProperty("stagesCompleted")]
        public int stagesCompleted { get; set; } = 0;
        
        [JsonProperty("perfectClears")]
        public int perfectClears { get; set; } = 0;
        
        [JsonProperty("speedClears")]
        public int speedClears { get; set; } = 0;
        
        [JsonProperty("itemsPurchased")]
        public int itemsPurchased { get; set; } = 0;
        
        [JsonProperty("unlockedCharacters")]
        public List<string> unlockedCharacters { get; set; } = new List<string>();
        
        [JsonProperty("itemTypesUsed")]
        public List<string> itemTypesUsed { get; set; } = new List<string>();
        
        [JsonProperty("chapter1PerfectStages")]
        public int chapter1PerfectStages { get; set; } = 0;
        
        [JsonProperty("deathsInCurrentStage")]
        public int deathsInCurrentStage { get; set; } = 0;
        
        [JsonProperty("usedItemInCurrentStage")]
        public bool usedItemInCurrentStage { get; set; } = false;
    }
}
