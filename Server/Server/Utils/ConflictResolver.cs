using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Utils
{
    /// <summary>
    /// 기본 User 필드들에 대한 델타 이벤트 처리 (새로운 정규화된 구조용)
    /// </summary>
    public class ConflictResolver
    {
        /// <summary>
        /// 기본 User 필드들에 대한 델타 이벤트를 User 엔티티에 병합합니다.
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

                default:
                    // 기타 필드들은 UserStateService에서 별도 처리
                    Console.WriteLine($"[ConflictResolver] 처리되지 않은 델타 키 (다른 서비스에서 처리): {delta.Key}");
                    break;
            }
            
            // 업데이트 시간 갱신
            userState.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 아이템 목록 델타를 처리합니다.
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
                        ItemId = item.itemId,
                        Count = item.count
                    });
                }
                
                Console.WriteLine($"[ConflictResolver] 아이템 목록 업데이트 완료 - UserId: {userState.Id}, 아이템 수: {items.Count}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ConflictResolver] 아이템 델타 파싱 실패: {ex.Message}");
                Console.WriteLine($"[ConflictResolver] 델타 값: {deltaValue}");
            }
        }

        // 파싱 헬퍼 메서드들
        private int ParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
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
