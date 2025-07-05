using System.Collections.Generic;

namespace Server.Models
{
    public class SaveData
    {
        public int gold { get; set; }
        public int gems { get; set; }
        public string selectedCharacter { get; set; } = "Default";
        public List<InventoryItemDto> items { get; set; } = new List<InventoryItemDto>();  // DTOs.cs의 InventoryItemDto 사용
        public bool tutorialDisplayed { get; set; }
        public List<bool> stageClears { get; set; } = new List<bool>();
        public List<SerializableVector3Dto> stageFlagPositions { get; set; } = new List<SerializableVector3Dto>();  // 클라이언트와 일치
        public List<int> bestGemRewards { get; set; } = new List<int>();
        public List<float> bestClearTimes { get; set; } = new List<float>();  // 개인 기록 동기화용
        public List<int> bestDeathCounts { get; set; } = new List<int>();     // 개인 기록 동기화용
        public List<float> currentPlayTimes { get; set; } = new List<float>();
        public List<int> currentDeathCounts { get; set; } = new List<int>();
        public int version { get; set; } = 2;
    }
}
