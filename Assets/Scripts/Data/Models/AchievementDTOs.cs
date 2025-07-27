using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JustClimb.Data
{
    /// <summary>
    /// 업적 진행률 데이터 (서버 호환용)
    /// 서버 측 AchievementProgressDto와 완전히 동일한 구조
    /// </summary>
    [Serializable]
    public class AchievementProgressDto
    {
        [JsonProperty("stagesCompleted")]
        public int stagesCompleted = 0;
        
        [JsonProperty("perfectClears")]
        public int perfectClears = 0;
        
        [JsonProperty("speedClears")]
        public int speedClears = 0;
        
        [JsonProperty("deathsInCurrentStage")]
        public int deathsInCurrentStage = 0;
        
        [JsonProperty("usedItemInCurrentStage")]
        public bool usedItemInCurrentStage = false;
        
        [JsonProperty("unlockedCharacters")]
        public List<string> unlockedCharacters = new List<string>();  // 🔧 서버에 맞춰 List<string>으로 수정
        
        [JsonProperty("itemsPurchased")]
        public int itemsPurchased = 0;
        
        [JsonProperty("itemTypesUsed")]
        public List<string> itemTypesUsed = new List<string>();
        
        [JsonProperty("chapter1PerfectStages")]
        public int chapter1PerfectStages = 0;  // 🔧 서버에 맞춰 int로 수정
        
        [JsonProperty("totalDeaths")]
        public int totalDeaths = 0;
        
        [JsonProperty("totalGemsCollected")]
        public int totalGemsCollected = 0;
    }

    /// <summary>
    /// 기존 클래스명 호환성을 위한 별칭 (레거시 지원)
    /// </summary>
    [System.Obsolete("Use AchievementProgressDto instead")]
    public class AchievementProgressData : AchievementProgressDto
    {
        // 기존 코드 호환성을 위한 별칭
    }
} 