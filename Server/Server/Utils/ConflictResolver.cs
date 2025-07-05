using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Utils
{
    /// <summary>
    /// 델타 이벤트를 User 엔티티에 적용하는 충돌 해결 로직을 담당합니다.
    /// </summary>
    public class ConflictResolver
    {
        /// <summary>
        /// 델타 이벤트를 User 엔티티에 병합합니다.
        /// </summary>
        public void Resolve(User userState, DeltaEventDto delta)
        {
            switch (delta.Key)
            {
                case "gold":
                    userState.Gold = ParseInt(delta.Value, userState.Gold);
                    break;

                case "gems":
                    userState.Gems = ParseInt(delta.Value, userState.Gems);
                    break;

                case "selectedCharacter":
                    userState.SelectedCharacter = ParseString(delta.Value, userState.SelectedCharacter);
                    break;

                case "tutorialDisplayed":
                    userState.TutorialDisplayed = ParseBool(delta.Value, userState.TutorialDisplayed);
                    break;

                case "items":
                    ResolveItems(userState, delta.Value);
                    break;

                case "stageClears":
                    userState.StageClearsJson = delta.Value;
                    break;

                case "stageFlagPositions":
                    userState.StageFlagPositionsJson = delta.Value;
                    break;

                case "bestGemRewards":
                    userState.BestGemRewardsJson = delta.Value;
                    break;

                case "bestClearTimes":
                    userState.BestClearTimesJson = delta.Value;
                    break;

                case "bestDeathCounts":
                    userState.BestDeathCountsJson = delta.Value;
                    break;

                case "currentPlayTimes":
                    userState.CurrentPlayTimesJson = delta.Value;
                    break;

                case "currentDeathCounts":
                    userState.CurrentDeathCountsJson = delta.Value;
                    break;

                case "version":
                    userState.Version = ParseInt(delta.Value, userState.Version);
                    break;

                default:
                    // 스테이지별 개별 델타 처리 (예: "lastGemRewards_1", "bestClearTimes_2" 등)
                    ResolveStageSpecificDelta(userState, delta);
                    break;
            }
        }

        /// <summary>
        /// 아이템 목록 델타를 처리합니다.
        /// 클라이언트에서 ItemType enum이 문자열로 직렬화되어 전송됩니다.
        /// </summary>
        private void ResolveItems(User userState, string deltaValue)
        {
            try
            {
                var items = JsonConvert.DeserializeObject<List<InventoryItemDto>>(deltaValue);
                if (items == null) return;

                // 기존 아이템들을 모두 제거하고 새로운 리스트로 교체
                userState.Items.Clear();
                
                foreach (var item in items)
                {
                    userState.Items.Add(new UserItem
                    {
                        UserId = userState.Id,
                        ItemId = item.itemId, // 이제 문자열을 그대로 저장 (ItemType enum 문자열)
                        Count = item.count
                    });
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ConflictResolver] 아이템 델타 파싱 실패: {ex.Message}");
                Console.WriteLine($"[ConflictResolver] 델타 값: {deltaValue}");
            }
        }

        /// <summary>
        /// 스테이지별 개별 델타를 처리합니다. (예: "lastGemRewards_1", "bestClearTimes_2")
        /// </summary>
        private void ResolveStageSpecificDelta(User userState, DeltaEventDto delta)
        {
            var keyParts = delta.Key.Split('_');
            if (keyParts.Length != 2) return;

            var fieldName = keyParts[0];
            if (!int.TryParse(keyParts[1], out int stageNum)) return;

            var index = stageNum - 1; // 1-based to 0-based

            switch (fieldName)
            {
                case "bestGemRewards":
                    userState.BestGemRewardsJson = UpdateListField(userState.BestGemRewardsJson, index, ParseInt(delta.Value, 0));
                    break;

                case "bestClearTimes":
                    userState.BestClearTimesJson = UpdateListField(userState.BestClearTimesJson, index, ParseFloat(delta.Value, 0f));
                    break;

                case "bestDeathCounts":
                    userState.BestDeathCountsJson = UpdateListField(userState.BestDeathCountsJson, index, ParseInt(delta.Value, 0));
                    break;

                case "currentPlayTimes":
                    userState.CurrentPlayTimesJson = UpdateListField(userState.CurrentPlayTimesJson, index, ParseFloat(delta.Value, 0f));
                    break;

                case "currentDeathCounts":
                    userState.CurrentDeathCountsJson = UpdateListField(userState.CurrentDeathCountsJson, index, ParseInt(delta.Value, 0));
                    break;

                case "stageFlagPositions":
                    // Vector3Dto로 파싱하여 리스트 업데이트
                    try
                    {
                        var vector3 = JsonConvert.DeserializeObject<SerializableVector3Dto>(delta.Value);
                        if (vector3 != null)
                        {
                            userState.StageFlagPositionsJson = UpdateListField(userState.StageFlagPositionsJson, index, vector3);
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[ConflictResolver] Vector3 델타 파싱 실패: {ex.Message}");
                    }
                    break;
            }
        }

        /// <summary>
        /// JSON 직렬화된 리스트 필드의 특정 인덱스를 업데이트하고 결과를 반환합니다.
        /// </summary>
        private string UpdateListField<T>(string jsonField, int index, T value)
        {
            List<T> list;
            
            try
            {
                list = string.IsNullOrEmpty(jsonField) 
                    ? new List<T>() 
                    : JsonConvert.DeserializeObject<List<T>>(jsonField) ?? new List<T>();
            }
            catch (JsonException)
            {
                list = new List<T>();
            }

            // 리스트 크기 확장
            while (list.Count <= index)
            {
                list.Add(default(T));
            }

            list[index] = value;
            return JsonConvert.SerializeObject(list);
        }

        // 파싱 헬퍼 메서드들
        private int ParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private float ParseFloat(string value, float defaultValue)
        {
            return float.TryParse(value, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
        }

        private bool ParseBool(string value, bool defaultValue)
        {
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        private string ParseString(string value, string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}
