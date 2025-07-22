using System;
using System.Collections.Generic;
using JustClimb.Data;
using UnityEngine;
using Zenject;

namespace JustClimb.Manager
{
    // 스테이지의 언락 여부, 보상, 최단 클리어 타임, 최저 사망 횟수 관리를 담당.
    // JSON 로드 직후와 SetCleared 호출 후에 DispatchAll을 통해 초기/갱신된 상태를 이벤트로 발행.
    public class StageManager : IStageManager, IInitializable
    {
        // ✅ 유지되는 이벤트들 (best 기록만)
        public event Action<int, int> OnBestRewardUpdated;
        public event Action<int, float> OnBestTimeUpdated;
        public event Action<int, int> OnBestDeathUpdated;

        public event Action<int> OnStageUnlocked;

        readonly IDataManager _dataManager;
        readonly ICurrencyManager _currencyManager;

        // Zenject 생성자 주입
        [Inject]
        public StageManager(IDataManager dataManager, ICurrencyManager currencyManager)
        {
            _dataManager = dataManager;
            _currencyManager = currencyManager;
        }

        // Zenject IInitializable
        public void Initialize()
        {
            Init();
        }

        // 실제 초기화 로직: 로드 직후/Init 직후에 상태 발행
        public void Init()
        {
            _dataManager.OnLoaded += data => DispatchAll();

            // 이미 Current가 설정된 경우(동기 로드 시나리오)만 즉시 호출
            if (_dataManager.Current != null)
            {
                DispatchAll();
            }
        }

        // 편의 프로퍼티: 현재 메모리에 로드된 SaveData
        SaveData Current => _dataManager.Current;

        // 조회 API

        // 해당 스테이지에서 획득한 최고 보상 개수 (gem)
        public int GetBestReward(int stageNum)
        {
            var list = Current?.bestGemRewards;
            if (list == null || list.Count < stageNum)
                return 0;
            return list[stageNum - 1];
        }
        // 해당 스테이지의 최단 클리어 타임(초)
        public float GetBestTime(int stageNum)
        {
            var list = Current?.bestClearTimes;
            if (list == null || list.Count < stageNum)
                return 0f; // MaxValue 대신 0 반환
            
            var time = list[stageNum - 1];
            return time >= float.MaxValue ? 0f : time; // MaxValue는 0으로 변환
        }
        // 해당 스테이지의 최소 사망 횟수
        public int GetBestDeath(int stageNum)
        {
            // Current가 null이거나 리스트가 null/충분치 않으면 기본값 반환
            var list = Current?.bestDeathCounts;
            if (list == null || list.Count < stageNum)
                return 0; // MaxValue 대신 -1 반환
            
            var deaths = list[stageNum - 1];
            return deaths >= int.MaxValue ? 0 : deaths; // MaxValue는 -1로 변환
        }

        // 해당 스테이지가 언락(클리어)되었는지
        public bool IsUnlocked(int stageNum)
        {
            if (stageNum == 1) return true;
            var clears = Current.stageClears;
            int prev = stageNum - 2;
            return prev >= 0 && prev < clears.Count && clears[prev];
        }

        /// <summary>
        /// 스테이지 클리어 시 호출 (개선된 버전)
        /// 1) current 값을 사용하여 best 기록과 비교
        /// 2) 더 좋은 기록일 때만 best 갱신
        /// 3) current 값은 초기화
        /// 4) 보상 차액 지급
        /// 5) 업적 시스템에 클리어 이벤트 전달
        /// </summary>
        public void SetCleared(int stageNum, int gemCount, float clearTime, int deathCount)
        {
            int idx = stageNum - 1;
            var sd = _dataManager.Current;

            // 보상 차액만 지급
            // 이전까지 지급된 최고 보상
            while (sd.bestGemRewards.Count <= idx) sd.bestGemRewards.Add(0);
            int prevBest = sd.bestGemRewards[idx];
            int delta = Math.Max(0, gemCount - prevBest);
            if (delta > 0)
            {
                _currencyManager.AddGems(delta);
            }

            // 언락 처리
            while (sd.stageClears.Count <= idx) sd.stageClears.Add(false);
            if (!sd.stageClears[idx])
            {
                sd.stageClears[idx] = true;
                OnStageUnlocked?.Invoke(stageNum);
            }

            // best 기록 조건부 갱신 (이벤트는 마지막에 한 번만 발생)
            bool recordUpdated = false;
            
            // 보석 개수 (더 많이 획득했을 때)
            if (gemCount > prevBest)
            {
                sd.bestGemRewards[idx] = gemCount;
                OnBestRewardUpdated?.Invoke(stageNum, gemCount);
                recordUpdated = true;
            }

            // 클리어 타임 (더 빨리 클리어했을 때)
            while (sd.bestClearTimes.Count <= idx) sd.bestClearTimes.Add(float.MaxValue);
            if (clearTime < sd.bestClearTimes[idx])
            {
                sd.bestClearTimes[idx] = clearTime;
                recordUpdated = true;
            }

            // 사망 횟수 (더 적게 죽었을 때)
            while (sd.bestDeathCounts.Count <= idx) sd.bestDeathCounts.Add(int.MaxValue);
            if (deathCount < sd.bestDeathCounts[idx])
            {
                sd.bestDeathCounts[idx] = deathCount;
                recordUpdated = true;
            }

            // 기록이 갱신된 경우에만 한 번의 이벤트 발생 (중복 방지)
            if (recordUpdated)
            {
                OnBestTimeUpdated?.Invoke(stageNum, sd.bestClearTimes[idx]);
                Debug.Log($"[StageManager] 기록 갱신 이벤트 발생: Stage {stageNum}");
            }

            // 업적 시스템에 스테이지 클리어 알림 (한 번만 호출)
            try
            {
                const int totalGems = 3; // 게임의 최대 젬 개수
                AchievementIntegration.OnStageCleared(stageNum, clearTime, deathCount, gemCount, totalGems);
                Debug.Log($"[StageManager] 업적 시스템 알림 완료: Stage {stageNum}, Gems: {gemCount}/{totalGems}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[StageManager] 업적 시스템 알림 실패: {e.Message}");
            }

            // 스테이지 클리어 시 current 값들 초기화 (재도전 준비)
            while (sd.currentPlayTimes.Count <= idx) sd.currentPlayTimes.Add(0f);
            sd.currentPlayTimes[idx] = 0f;
            
            while (sd.currentDeathCounts.Count <= idx) sd.currentDeathCounts.Add(0);
            sd.currentDeathCounts[idx] = 0;

            // 깃발 초기화 (null 대신 Vector3.zero 사용)
            while (sd.stageFlagPositions.Count <= idx) sd.stageFlagPositions.Add(new SerializableVector3Dto());
            sd.stageFlagPositions[idx] = new SerializableVector3Dto(0f, 0f, 0f);

            // 저장 
            _dataManager.SaveLocal();

            // 기능 추가: 스테이지 관련 델타 생성 (개선된 버전)
            // 전체 stageClears 리스트 델타
            _dataManager.GenerateDelta("stageClears", sd.stageClears);
            // 이번 스테이지 best 기록 델타 (실제 갱신된 경우에만, MaxValue는 제외)
            _dataManager.GenerateDelta($"bestGemRewards_{stageNum}", sd.bestGemRewards[idx]);
            
            // 유효한 클리어 타임일 때만 델타 전송 (float.MaxValue 제외)
            if (sd.bestClearTimes[idx] < float.MaxValue)
            {
                _dataManager.GenerateDelta($"bestClearTimes_{stageNum}", sd.bestClearTimes[idx]);
            }
            
            // 유효한 사망 횟수일 때만 델타 전송 (int.MaxValue 제외)
            if (sd.bestDeathCounts[idx] < int.MaxValue)
            {
                _dataManager.GenerateDelta($"bestDeathCounts_{stageNum}", sd.bestDeathCounts[idx]);
            }
            
            // 깃발 위치 초기화 델타
            _dataManager.GenerateDelta($"stageFlagPositions_{stageNum}", sd.stageFlagPositions[idx]);
            
            // current 값들 초기화 델타 전송 (서버 동기화)
            _dataManager.GenerateDelta($"currentPlayTimes_{stageNum}", sd.currentPlayTimes[idx]);
            _dataManager.GenerateDelta($"currentDeathCounts_{stageNum}", sd.currentDeathCounts[idx]);
            
            Debug.Log($"[StageManager] 스테이지 {stageNum} 클리어 완료 - current 값들 초기화됨 (PlayTime: {sd.currentPlayTimes[idx]}, DeathCount: {sd.currentDeathCounts[idx]})");
        }

        // Load 직후 & Initialize 직후 저장된 모든 스테이지 상태를 이벤트로 발행
        void DispatchAll()
        {
            var sd = Current;
            const int MAX_GAME_STAGES = 10; // 실제 게임에 존재하는 스테이지 수
            
            // 실제 게임 스테이지 수로 제한
            int maxStageToProcess = Math.Min(sd.stageClears.Count, MAX_GAME_STAGES);
            
            for (int i = 0; i < maxStageToProcess; i++)
            {
                int stageNum = i + 1;
                
                if (sd.stageClears[i]) OnStageUnlocked?.Invoke(stageNum);
                
                // 유효한 값만 이벤트 발행 (MaxValue는 초기값이므로 제외)
                if (i < sd.bestGemRewards.Count) 
                    OnBestRewardUpdated?.Invoke(stageNum, sd.bestGemRewards[i]);
                
                // 유효한 클리어 타임만 이벤트 발행 (MaxValue 제외)
                if (i < sd.bestClearTimes.Count && sd.bestClearTimes[i] > 0 && sd.bestClearTimes[i] < float.MaxValue)
                    OnBestTimeUpdated?.Invoke(stageNum, sd.bestClearTimes[i]);
                
                // 유효한 사망 횟수만 이벤트 발행 (MaxValue 제외)
                if (i < sd.bestDeathCounts.Count && sd.bestDeathCounts[i] >= 0 && sd.bestDeathCounts[i] < int.MaxValue)
                    OnBestDeathUpdated?.Invoke(stageNum, sd.bestDeathCounts[i]);
            }
            
            Debug.Log($"[StageManager] DispatchAll 완료 - 처리된 스테이지: {maxStageToProcess}/{sd.stageClears.Count}");
        }

        // 메모리 누수 방지 (Zenject 싱글톤용 IDisposable 구현)
        public void Dispose()
        {
            // DataManager 이벤트 해제
            if (_dataManager != null)
                _dataManager.OnLoaded -= data => DispatchAll();
            
            // 외부 구독자들이 남아있을 수 있으니 이벤트 초기화
            OnBestRewardUpdated = null;
            OnBestTimeUpdated = null;
            OnBestDeathUpdated = null;
            OnStageUnlocked = null;
        }
    }
}
