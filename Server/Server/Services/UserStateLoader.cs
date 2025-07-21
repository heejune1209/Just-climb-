using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Server.Database;
using Server.Models;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 사용자 상태 로딩 전담 서비스
    /// Redis 캐시와 데이터베이스에서 사용자 상태를 로드합니다.
    /// </summary>
    public class UserStateLoader
    {
        private readonly JustClimbDbContext _dbContext;
        private readonly UserStateCacheManager _cacheManager;
        private readonly UserStateMapper _mapper;
        private readonly IAchievementService _achievementService;
        private readonly ILogger<UserStateLoader> _logger;

        public UserStateLoader(
            JustClimbDbContext dbContext,
            UserStateCacheManager cacheManager,
            UserStateMapper mapper,
            IAchievementService achievementService,
            ILogger<UserStateLoader> logger)
        {
            _dbContext = dbContext;
            _cacheManager = cacheManager;
            _mapper = mapper;
            _achievementService = achievementService;
            _logger = logger;
        }

        /// <summary>
        /// 사용자 상태를 로드합니다 (캐시 우선, DB 보조)
        /// </summary>
        public async Task<SaveData> LoadUserStateAsync(string userId)
        {
            _logger.LogInformation("[UserStateLoader] 사용자 상태 로드 시작 - UserId: {UserId}", userId);

            // 1. Redis 캐시에서 시도
            var cachedData = await _cacheManager.GetCachedStateAsync(userId);
            if (cachedData != null)
            {
                _logger.LogInformation("[UserStateLoader] Redis 캐시에서 데이터 로드 완료 - UserId: {UserId}", userId);
                return cachedData;
            }

            // 2. 데이터베이스에서 로드
            _logger.LogInformation("[UserStateLoader] DB에서 데이터 로드 시도 - UserId: {UserId}", userId);

            var dbData = await LoadFromDatabaseAsync(userId);
            
            // 3. 캐시에 저장
            await _cacheManager.CacheStateAsync(userId, dbData);

            _logger.LogInformation("[UserStateLoader] 사용자 상태 로드 완료 - UserId: {UserId}", userId);
            return dbData;
        }

        /// <summary>
        /// 데이터베이스에서 정규화된 데이터를 조회하고 SaveData로 변환
        /// </summary>
        private async Task<SaveData> LoadFromDatabaseAsync(string userId)
        {
            // 병렬로 모든 데이터 조회
            var userTask = _dbContext.Users
                .Include(u => u.Items)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var stageRecordsTask = _dbContext.UserStageRecords
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var progressRecordTask = _dbContext.UserAchievementProgress
                .FirstOrDefaultAsync(p => p.UserId == userId);

            var achievementUnlockedTask = _achievementService.GetUserAchievementUnlockedMapAsync(userId);
            var achievementRewardsTask = _achievementService.GetUserAchievementRewardMapAsync(userId);

            // 모든 비동기 작업 완료 대기
            await Task.WhenAll(userTask, stageRecordsTask, progressRecordTask, achievementUnlockedTask, achievementRewardsTask);

            var user = await userTask;
            var stageRecords = await stageRecordsTask;
            var progressRecord = await progressRecordTask;
            var achievementUnlocked = await achievementUnlockedTask;
            var achievementRewards = await achievementRewardsTask;

            // 신규 사용자인 경우 기본값 반환
            if (user == null)
            {
                _logger.LogInformation("[UserStateLoader] 신규 사용자 - 기본값 반환 - UserId: {UserId}", userId);
                return new SaveData();
            }

            // 정규화된 데이터를 SaveData로 매핑
            var saveData = _mapper.MapToSaveData(
                user, 
                stageRecords, 
                progressRecord, 
                achievementUnlocked, 
                achievementRewards);

            return saveData;
        }

        /// <summary>
        /// 특정 사용자의 기본 정보만 로드 (경량)
        /// </summary>
        public async Task<User> LoadUserBasicInfoAsync(string userId)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <summary>
        /// 특정 사용자의 스테이지 기록만 로드
        /// </summary>
        public async Task<List<UserStageRecord>> LoadUserStageRecordsAsync(string userId)
        {
            return await _dbContext.UserStageRecords
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// 특정 사용자의 업적 진행률만 로드
        /// </summary>
        public async Task<UserAchievementProgress> LoadUserAchievementProgressAsync(string userId)
        {
            return await _dbContext.UserAchievementProgress
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
    }
} 