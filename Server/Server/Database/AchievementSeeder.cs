using Microsoft.EntityFrameworkCore;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Database
{
    /// <summary>
    /// 업적 초기 데이터를 시드하는 클래스
    /// 정규화된 Achievement 테이블에 22개 업적 정의를 추가
    /// </summary>
    public static class AchievementSeeder
    {
        /// <summary>
        /// 업적 초기 데이터 시드
        /// </summary>
        public static async Task SeedAsync(JustClimbDbContext context)
        {
            // 이미 업적이 있으면 건너뛰기
            if (await context.Achievements.AnyAsync())
            {
                return;
            }

            var achievements = new List<Achievement>
            {
                // ===== STAGE ACHIEVEMENTS =====
                new Achievement
                {
                    Code = "Novice_Climber",
                    Name = "초보 등반가",
                    Description = "첫 번째 스테이지를 클리어하세요",
                    RewardAmount = 50,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 1
                },
                new Achievement
                {
                    Code = "Intermediate_Climber", 
                    Name = "중급 등반가",
                    Description = "5개 스테이지를 클리어하세요",
                    RewardAmount = 100,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 2
                },
                new Achievement
                {
                    Code = "Advanced_Climber",
                    Name = "고급 등반가", 
                    Description = "10개 스테이지를 클리어하세요",
                    RewardAmount = 200,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 3
                },
                new Achievement
                {
                    Code = "CHAPTER_1_MASTER",
                    Name = "챕터 1 마스터",
                    Description = "챕터 1을 완주하세요",
                    RewardAmount = 150,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 4
                },
                new Achievement
                {
                    Code = "CHAPTER_2_MASTER",
                    Name = "챕터 2 마스터",
                    Description = "챕터 2를 완주하세요",
                    RewardAmount = 200,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 5
                },
                new Achievement
                {
                    Code = "CHAPTER_3_MASTER",
                    Name = "챕터 3 마스터",
                    Description = "챕터 3을 완주하세요",
                    RewardAmount = 250,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 6
                },
                new Achievement
                {
                    Code = "CHAPTER_4_MASTER",
                    Name = "챕터 4 마스터",
                    Description = "챕터 4를 완주하세요",
                    RewardAmount = 300,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 7
                },
                new Achievement
                {
                    Code = "CHAPTER_5_MASTER",
                    Name = "챕터 5 마스터",
                    Description = "챕터 5를 완주하세요",
                    RewardAmount = 400,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 8
                },
                new Achievement
                {
                    Code = "Mountain_god",
                    Name = "산신",
                    Description = "모든 챕터를 완주하세요",
                    RewardAmount = 1000,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 9
                },
                new Achievement
                {
                    Code = "Speed_Climber",
                    Name = "암벽을 평지처럼",
                    Description = "30초 이내에 스테이지를 클리어하세요",
                    RewardAmount = 100,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 10
                },
                new Achievement
                {
                    Code = "PERFECTIONIST",
                    Name = "완벽주의자",
                    Description = "사망하지 않고 스테이지를 클리어하세요",
                    RewardAmount = 75,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 11
                },
                new Achievement
                {
                    Code = "FLAWLESS_Climb",
                    Name = "완벽한 등반",
                    Description = "5개 스테이지를 완벽하게 클리어하세요",
                    RewardAmount = 200,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 12
                },
                new Achievement
                {
                    Code = "UNTOUCHABLE",
                    Name = "언터처블",
                    Description = "챕터 1의 모든 스테이지를 완벽하게 클리어하세요",
                    RewardAmount = 300,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 13
                },
                new Achievement
                {
                    Code = "Zombie",
                    Name = "좀비",
                    Description = "한 스테이지에서 100번 이상 사망한 후 클리어하세요",
                    RewardAmount = 150,
                    RewardType = "gems",
                    Category = "stage",
                    SortOrder = 14
                },

                // ===== CHARACTER ACHIEVEMENTS =====
                new Achievement
                {
                    Code = "Unlock_Braden",
                    Name = "브레이든 해제",
                    Description = "브레이든 캐릭터를 해제하세요",
                    RewardAmount = 100,
                    RewardType = "gems",
                    Category = "character",
                    SortOrder = 1
                },
                new Achievement
                {
                    Code = "Unlock_Lina",
                    Name = "리나 해제",
                    Description = "리나 캐릭터를 해제하세요",
                    RewardAmount = 100,
                    RewardType = "gems",
                    Category = "character",
                    SortOrder = 2
                },
                new Achievement
                {
                    Code = "Unlock_Elliott",
                    Name = "엘리엇 해제",
                    Description = "엘리엇 캐릭터를 해제하세요",
                    RewardAmount = 100,
                    RewardType = "gems",
                    Category = "character",
                    SortOrder = 3
                },

                // ===== ITEM ACHIEVEMENTS =====
                new Achievement
                {
                    Code = "FIRST_PURCHASE",
                    Name = "첫 구매",
                    Description = "처음으로 아이템을 구매하세요",
                    RewardAmount = 50,
                    RewardType = "gems",
                    Category = "item",
                    SortOrder = 1
                },
                new Achievement
                {
                    Code = "COLLECTOR",
                    Name = "수집가",
                    Description = "20개 이상의 아이템을 구매하세요",
                    RewardAmount = 300,
                    RewardType = "gems",
                    Category = "item",
                    SortOrder = 2
                },
                new Achievement
                {
                    Code = "Shop_VIP",
                    Name = "VIP 고객",
                    Description = "10개 이상의 아이템을 구매하세요",
                    RewardAmount = 150,
                    RewardType = "gems",
                    Category = "item",
                    SortOrder = 3
                },
                new Achievement
                {
                    Code = "NATURAL_CLIMBER",
                    Name = "맨손 등반가",
                    Description = "아이템을 사용하지 않고 스테이지를 클리어하세요",
                    RewardAmount = 100,
                    RewardType = "gems",
                    Category = "item",
                    SortOrder = 4
                },
                new Achievement
                {
                    Code = "TOOL_MASTER",
                    Name = "도구 마스터",
                    Description = "모든 종류의 아이템을 사용해보세요",
                    RewardAmount = 200,
                    RewardType = "gems",
                    Category = "item",
                    SortOrder = 5
                }
            };

            // 업적 추가
            await context.Achievements.AddRangeAsync(achievements);
            await context.SaveChangesAsync();

            Console.WriteLine($"[AchievementSeeder] {achievements.Count}개 업적을 시드했습니다.");
        }
    }
} 