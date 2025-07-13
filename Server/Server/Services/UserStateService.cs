using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Server.Config;
using Server.Database;
using Server.Models;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 새로운 정규화된 데이터베이스 구조를 사용하는 사용자 상태 서비스
    /// </summary>
    public class UserStateService : IUserStateService
    {
        private readonly JustClimbDbContext _dbContext;
        private readonly ConflictResolver _conflictResolver;
        private readonly IDistributedCache _cache;
        private readonly RedisSyncConfig _redisConfig;
        private readonly ILogger<UserStateService> _logger;
        private readonly IAchievementService _achievementService;

        public UserStateService(
            JustClimbDbContext dbContext,
            ConflictResolver conflictResolver,
            IDistributedCache cache,
            IOptions<RedisSyncConfig> redisOptions,
            ILogger<UserStateService> logger,
            IAchievementService achievementService)
        {
            _dbContext = dbContext;
            _conflictResolver = conflictResolver;
            _cache = cache;
            _redisConfig = redisOptions.Value;
            _logger = logger;
            _achievementService = achievementService;
        }

        /// <summary>
        /// 정규화된 테이블들에서 데이터를 조회하여 SaveData로 매핑
        /// </summary>
        public async Task<SaveData> LoadStateAsync(string userId)
        {
            _logger.LogInformation("[UserStateService] LoadStateAsync 시작 - UserId: {UserId}", userId);
            
            string cacheKey = $"user:{userId}";
            
            // 1) Redis 캐시 확인
            var json = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(json))
            {
                _logger.LogInformation("[UserStateService] Redis 캐시에서 데이터 로드 - UserId: {UserId}", userId);
                return JsonConvert.DeserializeObject<SaveData>(json);
            }

            _logger.LogInformation("[UserStateService] DB에서 데이터 로드 시도 - UserId: {UserId}", userId);
            
            // 2) DB에서 정규화된 테이블들 조회
            var user = await _dbContext.Users
                .Include(u => u.Items)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var stageRecords = await _dbContext.UserStageRecords
                .Where(r => r.UserId == userId)
                .ToListAsync();



            var progressRecord = await _dbContext.UserAchievementProgress
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (user == null)
            {
                _logger.LogInformation("[UserStateService] 신규 사용자 - 기본값 반환 - UserId: {UserId}", userId);
                return new SaveData();
            }

            // 3) 정규화된 데이터를 SaveData 형태로 매핑
            var dto = new SaveData
            {
                gold = user.Gold,
                gems = user.Gems,
                selectedCharacter = user.SelectedCharacter,
                tutorialDisplayed = user.TutorialDisplayed,
                
                // 아이템 데이터
                items = user.Items.Select(i => new InventoryItemDto
                {
                    itemId = i.ItemId,
                    count = i.Count
                }).ToList(),
                
                // 스테이지 데이터 (클라이언트 캐싱용으로 변환)
                stageClears = BuildStageClears(stageRecords),
                stageFlagPositions = BuildStageFlagPositions(stageRecords),
                bestGemRewards = BuildBestGemRewards(stageRecords),
                bestClearTimes = BuildBestClearTimes(stageRecords),
                bestDeathCounts = BuildBestDeathCounts(stageRecords),
                currentPlayTimes = BuildCurrentPlayTimes(stageRecords),
                currentDeathCounts = BuildCurrentDeathCounts(stageRecords),
                
                // 업적 데이터 (정규화된 구조)
                achievementUnlocked = await _achievementService.GetUserAchievementUnlockedMapAsync(userId),
                achievementRewards = await _achievementService.GetUserAchievementRewardMapAsync(userId),
                achievementProgress = progressRecord != null ? new AchievementProgressDto
                {
                    stagesCompleted = progressRecord.StagesCompleted,
                    perfectClears = progressRecord.PerfectClears,
                    speedClears = progressRecord.SpeedClears,
                    chapter1PerfectStages = progressRecord.Chapter1PerfectStages,
                    itemsPurchased = progressRecord.ItemsPurchased,
                    unlockedCharacters = DeserializeJson<List<string>>(progressRecord.UnlockedCharactersJson) ?? new List<string>(),
                    itemTypesUsed = DeserializeJson<List<string>>(progressRecord.ItemTypesUsedJson) ?? new List<string>(),
                    deathsInCurrentStage = progressRecord.DeathsInCurrentStage,
                    usedItemInCurrentStage = progressRecord.UsedItemInCurrentStage
                } : new AchievementProgressDto(),
                
                version = 5 // 새로운 버전
            };

            // 4) Redis 캐시에 저장
            await _cache.SetStringAsync(
                cacheKey,
                JsonConvert.SerializeObject(dto),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_redisConfig.CacheDurationHours)
                });

            _logger.LogInformation("[UserStateService] LoadStateAsync 완료 - UserId: {UserId}", userId);
            return dto;
        }

        public async Task MergeDeltasAsync(string userId, IEnumerable<DeltaEventDto> deltas)
        {
            _logger.LogInformation("[UserStateService] MergeDeltasAsync 시작 - UserId: {UserId}, 델타 개수: {Count}", 
                userId, deltas.Count());
            
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 기본 사용자 정보 로드/생성
                var user = await _dbContext.Users
                    .Include(u => u.Items)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogInformation("[UserStateService] 신규 사용자 엔티티 생성 - UserId: {UserId}", userId);
                    user = new User { Id = userId, Items = new List<UserItem>() };
                    _dbContext.Users.Add(user);
                }

                // 델타 이벤트 처리 - 새로운 방식으로 각 테이블에 분산 저장
                foreach (var delta in deltas)
                {
                    _logger.LogDebug("[UserStateService] 델타 처리 - Key: {Key}, Value: {Value}", 
                        delta.Key, delta.Value?.Length > 50 ? delta.Value[..50] + "..." : delta.Value);
                    
                    try
                    {
                        await ProcessDeltaAsync(userId, delta);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[UserStateService] 델타 처리 중 오류 - Key: {Key}", delta.Key);
                        throw;
                    }
                }

                // DB 저장
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("[UserStateService] DB 저장 완료 - UserId: {UserId}", userId);

                // Redis 캐시 무효화 (다음 로드 시 최신 데이터로 갱신)
                string cacheKey = $"user:{userId}";
                await _cache.RemoveAsync(cacheKey);
                
                _logger.LogInformation("[UserStateService] MergeDeltasAsync 완료 - UserId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[UserStateService] MergeDeltasAsync 중 오류 발생 - UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 개별 델타를 적절한 테이블에 처리
        /// </summary>
        private async Task ProcessDeltaAsync(string userId, DeltaEventDto delta)
        {
            // 기본 필드들은 기존 ConflictResolver 사용
            if (IsBasicUserField(delta.Key))
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    _conflictResolver.Resolve(user, delta);
                }
                return;
            }

            // 스테이지 관련 델타 처리
            if (delta.Key.StartsWith("currentPlayTimes_") || delta.Key.StartsWith("currentDeathCounts_") ||
                delta.Key.StartsWith("bestClearTimes_") || delta.Key.StartsWith("bestDeathCounts_") ||
                delta.Key.StartsWith("bestGemRewards_") || delta.Key.StartsWith("stageFlagPositions_"))
            {
                await ProcessStageRelatedDelta(userId, delta);
                return;
            }

            // 업적 관련 델타 처리
            if (delta.Key.StartsWith("achievementUnlocked_") || delta.Key == "achievementRewards" ||
                delta.Key == "achievementProgress")
            {
                await ProcessAchievementRelatedDelta(userId, delta);
                return;
            }

            _logger.LogWarning("[UserStateService] 처리되지 않은 델타 키: {Key}", delta.Key);
        }

        private bool IsBasicUserField(string key)
        {
            return key == "gold" || key == "gems" || key == "selectedCharacter" || 
                   key == "tutorialDisplayed" || key.StartsWith("items");
        }

        private async Task ProcessStageRelatedDelta(string userId, DeltaEventDto delta)
        {
            // 스테이지 번호 추출
            var parts = delta.Key.Split('_');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int stageNumber))
            {
                _logger.LogWarning("[UserStateService] 잘못된 스테이지 델타 키: {Key}", delta.Key);
                return;
            }

            // 🔧 유효한 스테이지 번호 범위 검사 (1~10)
            const int MAX_GAME_STAGES = 10;
            if (stageNumber < 1 || stageNumber > MAX_GAME_STAGES)
            {
                _logger.LogWarning("[UserStateService] 유효하지 않은 스테이지 번호: {StageNumber}, 최대: {MaxStages}", 
                    stageNumber, MAX_GAME_STAGES);
                return;
            }

            var record = await _dbContext.UserStageRecords
                .FirstOrDefaultAsync(r => r.UserId == userId && r.StageNumber == stageNumber);

            // 사용자 정보에서 DisplayName 가져오기
            string displayName = "Player"; // 기본값
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null && !string.IsNullOrEmpty(user.SteamDisplayName))
            {
                displayName = user.SteamDisplayName;
            }

            if (record == null)
            {
                record = new UserStageRecord
                {
                    UserId = userId,
                    StageNumber = stageNumber,
                    DisplayName = displayName,
                    IsCleared = false // 기본값으로 미클리어 상태
                };
                _dbContext.UserStageRecords.Add(record);
            }

            // 델타 키별로 적절한 필드 업데이트
            if (delta.Key.StartsWith("currentPlayTimes_"))
            {
                if (float.TryParse(delta.Value, out var playTime))
                {
                    // 🔧 스테이지 클리어 후 초기화 감지 (0으로 초기화)
                    if (playTime == 0f && record.IsCleared)
                    {
                        record.CurrentPlayTime = 0f;
                        _logger.LogInformation("[UserStateService] 스테이지 클리어 후 플레이 시간 초기화 - UserId: {UserId}, Stage: {StageNumber}", 
                            userId, stageNumber);
                    }
                    // 🔧 유효한 플레이 시간만 처리 (5초 이상, MaxValue 미만)
                    else if (playTime >= 5.0f && playTime < float.MaxValue)
                    {
                        record.CurrentPlayTime = playTime;
                        _logger.LogDebug("[UserStateService] 플레이 시간 업데이트 - UserId: {UserId}, Stage: {StageNumber}, Time: {PlayTime}s", 
                            userId, stageNumber, playTime);
                    }
                    else
                    {
                        _logger.LogDebug("[UserStateService] 무효한 플레이 시간으로 업데이트 건너뜀 - UserId: {UserId}, Stage: {StageNumber}, Time: {PlayTime}s", 
                            userId, stageNumber, playTime);
                        return; // 무효한 데이터는 기록하지 않음
                    }
                }
                else
                {
                    _logger.LogWarning("[UserStateService] 플레이 시간 파싱 실패: {Value}", delta.Value);
                    return;
                }
            }
            else if (delta.Key.StartsWith("currentDeathCounts_"))
            {
                if (int.TryParse(delta.Value, out var deathCount))
                {
                    // 🔧 스테이지 클리어 후 초기화 감지 (0으로 초기화)
                    if (deathCount == 0 && record.IsCleared)
                    {
                        record.CurrentDeathCount = 0;
                        _logger.LogInformation("[UserStateService] 스테이지 클리어 후 사망 수 초기화 - UserId: {UserId}, Stage: {StageNumber}", 
                            userId, stageNumber);
                    }
                    // 🔧 유효한 사망 수만 처리 (0 이상, MaxValue 미만)
                    else if (deathCount >= 0 && deathCount < int.MaxValue)
                    {
                        record.CurrentDeathCount = deathCount;
                        _logger.LogDebug("[UserStateService] 사망 수 업데이트 - UserId: {UserId}, Stage: {StageNumber}, Deaths: {DeathCount}", 
                            userId, stageNumber, deathCount);
                    }
                    else
                    {
                        _logger.LogDebug("[UserStateService] 무효한 사망 수로 업데이트 건너뜀 - UserId: {UserId}, Stage: {StageNumber}, Deaths: {DeathCount}", 
                            userId, stageNumber, deathCount);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("[UserStateService] 사망 수 파싱 실패: {Value}", delta.Value);
                    return;
                }
            }
            else if (delta.Key.StartsWith("bestClearTimes_"))
            {
                if (float.TryParse(delta.Value, out var time))
                {
                    // 🔧 유효한 클리어 타임만 처리 (0보다 크고 MaxValue 미만)
                    if (time > 0 && time < float.MaxValue && (time < record.BestClearTime || record.BestClearTime <= 0))
                    {
                        record.BestClearTime = time;
                        record.IsCleared = true;  // 실제 유효한 클리어 타임일 때만 클리어 처리
                        _logger.LogInformation("[UserStateService] 최고 클리어 타임 갱신 - UserId: {UserId}, Stage: {StageNumber}, Time: {Time}s", 
                            userId, stageNumber, time);
                    }
                    else
                    {
                        _logger.LogDebug("[UserStateService] 무효하거나 더 나쁜 클리어 타임으로 업데이트 건너뜀 - UserId: {UserId}, Stage: {StageNumber}, Time: {Time}s", 
                            userId, stageNumber, time);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("[UserStateService] 클리어 타임 파싱 실패: {Value}", delta.Value);
                    return;
                }
            }
            else if (delta.Key.StartsWith("bestDeathCounts_"))
            {
                if (int.TryParse(delta.Value, out var deathCount))
                {
                    // 🔧 유효한 사망 수만 처리 (0 이상, MaxValue 미만)
                    if (deathCount >= 0 && deathCount < int.MaxValue && (record.BestDeathCount < 0 || deathCount < record.BestDeathCount))
                    {
                        record.BestDeathCount = deathCount;
                        _logger.LogInformation("[UserStateService] 최고 사망 수 갱신 - UserId: {UserId}, Stage: {StageNumber}, Deaths: {DeathCount}", 
                            userId, stageNumber, deathCount);
                    }
                    else
                    {
                        _logger.LogDebug("[UserStateService] 무효하거나 더 나쁜 사망 수로 업데이트 건너뜀 - UserId: {UserId}, Stage: {StageNumber}, Deaths: {DeathCount}", 
                            userId, stageNumber, deathCount);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("[UserStateService] 사망 수 파싱 실패: {Value}", delta.Value);
                    return;
                }
            }
            else if (delta.Key.StartsWith("bestGemRewards_"))
            {
                if (int.TryParse(delta.Value, out var gemCount))
                {
                    // 🔧 유효한 보석 수만 처리 (0 이상 3 이하)
                    if (gemCount >= 0 && gemCount <= 3 && gemCount > record.BestGemCount)
                    {
                        record.BestGemCount = gemCount;
                        _logger.LogInformation("[UserStateService] 최고 보석 수 갱신 - UserId: {UserId}, Stage: {StageNumber}, Gems: {GemCount}", 
                            userId, stageNumber, gemCount);
                    }
                    else
                    {
                        _logger.LogDebug("[UserStateService] 무효하거나 더 나쁜 보석 수로 업데이트 건너뜀 - UserId: {UserId}, Stage: {StageNumber}, Gems: {GemCount}", 
                            userId, stageNumber, gemCount);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("[UserStateService] 보석 수 파싱 실패: {Value}", delta.Value);
                    return;
                }
            }
            else if (delta.Key.StartsWith("stageFlagPositions_"))
            {
                // 깃발 위치는 SerializableVector3Dto로 파싱
                try
                {
                    var flagPos = JsonConvert.DeserializeObject<SerializableVector3Dto>(delta.Value);
                    if (flagPos != null)
                    {
                        // 🔧 유효한 좌표만 처리 (NaN이나 무한대 값 제외)
                        if (!float.IsNaN(flagPos.x) && !float.IsNaN(flagPos.y) && !float.IsNaN(flagPos.z) &&
                            !float.IsInfinity(flagPos.x) && !float.IsInfinity(flagPos.y) && !float.IsInfinity(flagPos.z))
                        {
                            // 🔧 nullable float 필드에 할당 (자동으로 float에서 float?로 변환됨)
                            record.FlagX = flagPos.x;
                            record.FlagY = flagPos.y;
                            record.FlagZ = flagPos.z;
                            _logger.LogDebug("[UserStateService] 깃발 위치 업데이트 - UserId: {UserId}, Stage: {StageNumber}, Position: ({X}, {Y}, {Z})", 
                                userId, stageNumber, flagPos.x, flagPos.y, flagPos.z);
                        }
                        else
                        {
                            _logger.LogWarning("[UserStateService] 무효한 깃발 위치로 업데이트 건너뜀 - UserId: {UserId}, Stage: {StageNumber}, Position: ({X}, {Y}, {Z})", 
                                userId, stageNumber, flagPos.x, flagPos.y, flagPos.z);
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[UserStateService] 깃발 위치 데이터가 null입니다 - Value: {Value}", delta.Value);
                        return;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[UserStateService] 깃발 위치 JSON 파싱 실패 - Value: {Value}", delta.Value);
                    return;
                }
            }

            record.UpdatedAt = DateTime.UtcNow;
        }

        private async Task ProcessAchievementRelatedDelta(string userId, DeltaEventDto delta)
        {
            // 새로운 AchievementService를 사용하여 델타 처리
            await _achievementService.ProcessAchievementDeltaAsync(userId, delta.Key, delta.Value);
        }

        #region Helper Methods

        /// <summary>
        /// 스테이지별 클리어 상태를 빌드합니다.
        /// </summary>
        private List<bool> BuildStageClears(List<UserStageRecord> stageRecords)
        {
            var clears = new List<bool>();
            const int MAX_GAME_STAGES = 10; // 실제 게임에 존재하는 스테이지 수
            
            for (int i = 1; i <= MAX_GAME_STAGES; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                clears.Add(record?.IsCleared ?? false);
            }
            
            return clears;
        }

        /// <summary>
        /// 스테이지별 깃발 위치를 빌드합니다.
        /// </summary>
        private List<SerializableVector3Dto> BuildStageFlagPositions(List<UserStageRecord> stageRecords)
        {
            var positions = new List<SerializableVector3Dto>();
            const int MAX_GAME_STAGES = 10;
            
            for (int i = 1; i <= MAX_GAME_STAGES; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                if (record != null)
                {
                    // 🔧 nullable float를 non-nullable로 변환 (null인 경우 0으로 처리)
                    float flagX = record.FlagX ?? 0f;
                    float flagY = record.FlagY ?? 0f;
                    float flagZ = record.FlagZ ?? 0f;
                    
                    // 🔧 유효한 좌표만 전송 (NaN이나 무한대 값 제외)
                    if (!float.IsNaN(flagX) && !float.IsNaN(flagY) && !float.IsNaN(flagZ) &&
                        !float.IsInfinity(flagX) && !float.IsInfinity(flagY) && !float.IsInfinity(flagZ))
                    {
                        positions.Add(new SerializableVector3Dto(flagX, flagY, flagZ));
                    }
                    else
                    {
                        positions.Add(new SerializableVector3Dto(0f, 0f, 0f)); // 기본값
                    }
                }
                else
                {
                    positions.Add(new SerializableVector3Dto(0f, 0f, 0f)); // 기본값
                }
            }
            
            return positions;
        }

        /// <summary>
        /// 스테이지별 최고 보석 개수를 빌드합니다.
        /// </summary>
        private List<int> BuildBestGemRewards(List<UserStageRecord> stageRecords)
        {
            var rewards = new List<int>();
            const int MAX_GAME_STAGES = 10;
            
            for (int i = 1; i <= MAX_GAME_STAGES; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                rewards.Add(record?.BestGemCount ?? 0);
            }
            
            return rewards;
        }

        /// <summary>
        /// 스테이지별 최고 클리어 타임을 빌드합니다.
        /// </summary>
        private List<float> BuildBestClearTimes(List<UserStageRecord> stageRecords)
        {
            var times = new List<float>();
            const int MAX_GAME_STAGES = 10;
            
            for (int i = 1; i <= MAX_GAME_STAGES; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                // 🔧 유효한 클리어 타임만 전송 (MaxValue는 0으로 대체)
                if (record != null && record.BestClearTime > 0 && record.BestClearTime < float.MaxValue)
                {
                    times.Add(record.BestClearTime);
                }
                else
                {
                    times.Add(0f); // 클리어하지 않은 스테이지는 0
                }
            }
            
            return times;
        }

        /// <summary>
        /// 스테이지별 최고 사망 횟수를 빌드합니다.
        /// </summary>
        private List<int> BuildBestDeathCounts(List<UserStageRecord> stageRecords)
        {
            var deaths = new List<int>();
            const int MAX_GAME_STAGES = 10;
            
            for (int i = 1; i <= MAX_GAME_STAGES; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                // 🔧 유효한 사망 횟수만 전송 (MaxValue는 0으로 대체)
                if (record != null && record.BestDeathCount >= 0 && record.BestDeathCount < int.MaxValue)
                {
                    deaths.Add(record.BestDeathCount);
                }
                else
                {
                    deaths.Add(0); // 클리어하지 않은 스테이지는 0
                }
            }
            
            return deaths;
        }

        /// <summary>
        /// 스테이지별 현재 플레이 타임을 빌드합니다.
        /// </summary>
        private List<float> BuildCurrentPlayTimes(List<UserStageRecord> stageRecords)
        {
            var times = new List<float>();
            const int MAX_GAME_STAGES = 10;
            
            for (int i = 1; i <= MAX_GAME_STAGES; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                
                // 🔧 클리어된 스테이지는 current 값을 0으로 반환 (재도전 준비)
                if (record != null && record.IsCleared)
                {
                    times.Add(0f);
                }
                // 🔧 유효한 플레이 타임만 전송 (5초 이상만 실제 플레이로 간주)
                else if (record != null && record.CurrentPlayTime >= 5.0f && record.CurrentPlayTime < float.MaxValue)
                {
                    times.Add(record.CurrentPlayTime);
                }
                else
                {
                    times.Add(0f); // 기본값
                }
            }
            
            return times;
        }

        /// <summary>
        /// 스테이지별 현재 사망 횟수를 빌드합니다.
        /// </summary>
        private List<int> BuildCurrentDeathCounts(List<UserStageRecord> stageRecords)
        {
            var deaths = new List<int>();
            const int MAX_GAME_STAGES = 10;
            
            for (int i = 1; i <= MAX_GAME_STAGES; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                
                // 🔧 클리어된 스테이지는 current 값을 0으로 반환 (재도전 준비)
                if (record != null && record.IsCleared)
                {
                    deaths.Add(0);
                }
                // 🔧 유효한 사망 횟수만 전송
                else if (record != null && record.CurrentDeathCount >= 0 && record.CurrentDeathCount < int.MaxValue)
                {
                    deaths.Add(record.CurrentDeathCount);
                }
                else
                {
                    deaths.Add(0); // 기본값
                }
            }
            
            return deaths;
        }

        private T DeserializeJson<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[UserStateService] JSON 역직렬화 실패 - JSON: {Json}", json?.Length > 100 ? json[..100] + "..." : json);
                return null;
            }
        }



        #endregion
    }
}
