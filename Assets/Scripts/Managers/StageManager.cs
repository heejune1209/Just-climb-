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
                return float.MaxValue;
            return list[stageNum - 1];
        }
        // 해당 스테이지의 최소 사망 횟수
        public int GetBestDeath(int stageNum)
        {
            // Current가 null이거나 리스트가 null/충분치 않으면 기본값 반환
            var list = Current?.bestDeathCounts;
            if (list == null || list.Count < stageNum)
                return int.MaxValue;
            return list[stageNum - 1];
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

            // ✅ best 기록 조건부 갱신 (개선된 로직)
            // 보석 개수 (더 많이 획득했을 때)
            if (gemCount > prevBest)
            {
                sd.bestGemRewards[idx] = gemCount;
                OnBestRewardUpdated?.Invoke(stageNum, gemCount);
            }

            // 클리어 타임 (더 빨리 클리어했을 때)
            while (sd.bestClearTimes.Count <= idx) sd.bestClearTimes.Add(float.MaxValue);
            if (clearTime < sd.bestClearTimes[idx])
            {
                sd.bestClearTimes[idx] = clearTime;
                OnBestTimeUpdated?.Invoke(stageNum, clearTime);
            }

            // 사망 횟수 (더 적게 죽었을 때)
            while (sd.bestDeathCounts.Count <= idx) sd.bestDeathCounts.Add(int.MaxValue);
            if (deathCount < sd.bestDeathCounts[idx])
            {
                sd.bestDeathCounts[idx] = deathCount;
                OnBestDeathUpdated?.Invoke(stageNum, deathCount);
            }

            // 깃발 초기화
            while (sd.stageFlagPositions.Count <= idx) sd.stageFlagPositions.Add(default);
            sd.stageFlagPositions[idx] = default(SerializableVector3);

            // 저장 
            _dataManager.SaveLocal();

            // 기능 추가: 스테이지 관련 델타 생성 (개선된 버전)
            // 전체 stageClears 리스트 델타
            _dataManager.GenerateDelta("stageClears", sd.stageClears);
            // 이번 스테이지 best 기록 델타 (갱신된 경우에만)
            _dataManager.GenerateDelta($"bestGemRewards_{stageNum}", sd.bestGemRewards[idx]);
            _dataManager.GenerateDelta($"bestClearTimes_{stageNum}", sd.bestClearTimes[idx]);
            _dataManager.GenerateDelta($"bestDeathCounts_{stageNum}", sd.bestDeathCounts[idx]);
            
            // 깃발 위치 초기화 델타
            _dataManager.GenerateDelta($"stageFlagPositions_{stageNum}", sd.stageFlagPositions[idx]);
            
            Debug.Log($"[StageManager] 스테이지 {stageNum} 클리어 완료 - 깃발 위치 초기화됨");
        }

        // Load 직후 & Initialize 직후 저장된 모든 스테이지 상태를 이벤트로 발행
        void DispatchAll()
        {
            var sd = Current;
            for (int i = 0; i < sd.stageClears.Count; i++)
            {
                if (sd.stageClears[i]) OnStageUnlocked?.Invoke(i + 1);
                
                // best 이벤트만 발행
                if (i < sd.bestGemRewards.Count) OnBestRewardUpdated?.Invoke(i + 1, sd.bestGemRewards[i]);
                if (i < sd.bestClearTimes.Count) OnBestTimeUpdated?.Invoke(i + 1, sd.bestClearTimes[i]);
                if (i < sd.bestDeathCounts.Count) OnBestDeathUpdated?.Invoke(i + 1, sd.bestDeathCounts[i]);
            }
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
