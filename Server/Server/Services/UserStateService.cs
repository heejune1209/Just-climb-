using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Server.Config;            // RedisSyncConfig 참조
using Server.Database;
using Server.Models;
using Server.Utils;            // DeltaEvent, User 엔티티 참조
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 델타 이벤트를 병합하여 사용자 상태를 DB 및 Redis 캐시에 UPSERT 처리하는 서비스 구현체입니다.
    /// SQL Server + Redis 혼합
    /// </summary>
    public class UserStateService : IUserStateService
    {
        private readonly JustClimbDbContext _dbContext;
        private readonly ConflictResolver _conflictResolver;
        // Redis 캐시 관련 코드
        private readonly IDistributedCache _cache;
        private readonly RedisSyncConfig _redisConfig;
        private readonly ILogger<UserStateService> _logger;

        // DI를 통한 DbContext, ConflictResolver, Redis 캐시, 설정 주입
        public UserStateService(
            JustClimbDbContext dbContext,
            ConflictResolver conflictResolver,
            IDistributedCache cache,
            IOptions<RedisSyncConfig> redisOptions,
            ILogger<UserStateService> logger)
        {
            _dbContext = dbContext;
            _conflictResolver = conflictResolver;
            _cache = cache;
            _redisConfig = redisOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// 전체 상태 조회: Redis 캐시 먼저, 없으면 DB에서 로드 후 캐시에 저장
        /// </summary>
        public async Task<SaveData> LoadStateAsync(string userId)
        {
            _logger.LogInformation("[UserStateService] LoadStateAsync 시작 - UserId: {UserId}", userId);
            
            string cacheKey = $"user:{userId}";
            // 1) Redis에서 캐시 조회
            var json = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(json))
            {
                _logger.LogInformation("[UserStateService] Redis 캐시에서 데이터 로드 - UserId: {UserId}", userId);
                return JsonConvert.DeserializeObject<SaveData>(json);
            }

            _logger.LogInformation("[UserStateService] DB에서 데이터 로드 시도 - UserId: {UserId}", userId);
            
            // 2) DB에서 로드 (User 엔티티 → SaveData 매핑)
            var user = await _dbContext.Users
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogInformation("[UserStateService] 신규 사용자 생성 - UserId: {UserId}", userId);
            }
            else
            {
                _logger.LogInformation("[UserStateService] 기존 사용자 로드 - UserId: {UserId}, Gold: {Gold}, Gems: {Gems}, Items: {ItemCount}", 
                    userId, user.Gold, user.Gems, user.Items?.Count ?? 0);
            }

            var dto = user == null
            ? new SaveData()  // 신규 유저 기본값
            : new SaveData
            {
                gold = user.Gold,
                gems = user.Gems,
                selectedCharacter = user.SelectedCharacter,
                tutorialDisplayed = user.TutorialDisplayed,
                stageClears = DeserializeJson<List<bool>>(user.StageClearsJson) ?? new List<bool>(),
                stageFlagPositions = DeserializeJson<List<SerializableVector3Dto>>(user.StageFlagPositionsJson) ?? new List<SerializableVector3Dto>(),
                bestGemRewards = DeserializeJson<List<int>>(user.BestGemRewardsJson) ?? new List<int>(),
                bestClearTimes = DeserializeJson<List<float>>(user.BestClearTimesJson) ?? new List<float>(),
                bestDeathCounts = DeserializeJson<List<int>>(user.BestDeathCountsJson) ?? new List<int>(),
                currentPlayTimes = DeserializeJson<List<float>>(user.CurrentPlayTimesJson) ?? new List<float>(),
                currentDeathCounts = DeserializeJson<List<int>>(user.CurrentDeathCountsJson) ?? new List<int>(),

                items = user.Items.Select(i => new InventoryItemDto
                {
                    itemId = i.ItemId,  // 문자열을 그대로 사용 (ItemType enum 문자열)
                    count = i.Count
                }).ToList(),
                version = user.Version
            };

            // Redis에 캐시에 저장
            await _cache.SetStringAsync(
                cacheKey,  // "user:{userId}" 사용
                JsonConvert.SerializeObject(dto),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(_redisConfig.CacheDurationHours)
                });

            _logger.LogInformation("[UserStateService] LoadStateAsync 완료 - UserId: {UserId}", userId);
            return dto;
        }

        public async Task MergeDeltasAsync(string userId, IEnumerable<DeltaEventDto> deltas)
        {
            _logger.LogInformation("[UserStateService] MergeDeltasAsync 시작 - UserId: {UserId}, 델타 개수: {Count}", 
                userId, deltas.Count());
            
            // DB 트랜잭션 시작
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // SQL Server 관련 코드
                // 사용자 상태 조회 또는 신규 객체 생성
                var userState = await _dbContext.Users
                    .Include(u => u.Items)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (userState == null)
                {
                    _logger.LogInformation("[UserStateService] 신규 사용자 엔티티 생성 - UserId: {UserId}", userId);
                    userState = new User { Id = userId, Items = new List<UserItem>() };
                    _dbContext.Users.Add(userState);
                }
                else
                {
                    _logger.LogInformation("[UserStateService] 기존 사용자 엔티티 로드 - UserId: {UserId}, Gold: {Gold}, Gems: {Gems}", 
                        userId, userState.Gold, userState.Gems);
                }

                // 델타별 충돌 해결 및 상태 병합
                foreach (var delta in deltas)
                {
                    _logger.LogDebug("[UserStateService] 델타 처리 - Key: {Key}, Value: {Value}", 
                        delta.Key, delta.Value?.Length > 50 ? delta.Value[..50] + "..." : delta.Value);
                    
                    try
                    {
                        _conflictResolver.Resolve(userState, delta);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[UserStateService] 델타 처리 중 오류 - Key: {Key}", delta.Key);
                        throw;
                    }
                }

                _logger.LogInformation("[UserStateService] 델타 처리 완료, DB 저장 시도 - UserId: {UserId}", userId);
                
                // DB에 변경사항 저장(UPSERT)
                await _dbContext.SaveChangesAsync();

                // 트랜잭션 커밋
                await transaction.CommitAsync();
                
                _logger.LogInformation("[UserStateService] DB 저장 완료 - UserId: {UserId}, Gold: {Gold}, Gems: {Gems}", 
                    userId, userState.Gold, userState.Gems);

                // Redis 캐시에 최신 상태 직렬화 후 저장
                string cacheKey = $"user:{userId}";
                // UserState 엔티티 → SaveData DTO 매핑
                var dto = new SaveData
                {
                    gold = userState.Gold,
                    gems = userState.Gems,
                    selectedCharacter = userState.SelectedCharacter,
                    tutorialDisplayed = userState.TutorialDisplayed,
                    stageClears = DeserializeJson<List<bool>>(userState.StageClearsJson) ?? new List<bool>(),
                    stageFlagPositions = DeserializeJson<List<SerializableVector3Dto>>(userState.StageFlagPositionsJson) ?? new List<SerializableVector3Dto>(),
                    bestGemRewards = DeserializeJson<List<int>>(userState.BestGemRewardsJson) ?? new List<int>(),
                    bestClearTimes = DeserializeJson<List<float>>(userState.BestClearTimesJson) ?? new List<float>(),
                    bestDeathCounts = DeserializeJson<List<int>>(userState.BestDeathCountsJson) ?? new List<int>(),
                    currentPlayTimes = DeserializeJson<List<float>>(userState.CurrentPlayTimesJson) ?? new List<float>(),
                    currentDeathCounts = DeserializeJson<List<int>>(userState.CurrentDeathCountsJson) ?? new List<int>(),
                    items = userState.Items
                               .Select(i => new InventoryItemDto { itemId = i.ItemId, count = i.Count })
                               .ToList(),
                    version = userState.Version
                    // … 나머지 필드도 DTO에 맞춰 추가 …
                };
                
                string serialized = JsonConvert.SerializeObject(dto);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_redisConfig.CacheDurationHours)
                };
                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions);
                
                _logger.LogInformation("[UserStateService] MergeDeltasAsync 완료 - UserId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                // 트랜잭션 롤백
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[UserStateService] MergeDeltasAsync 중 오류 발생 - UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// JSON 문자열을 안전하게 역직렬화하는 헬퍼 메서드
        /// </summary>
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
    }
}
