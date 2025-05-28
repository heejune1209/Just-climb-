using System;
using System.Collections.Generic;
using JustClimb.Manager;
using UnityEngine;

namespace JustClimb.Manager
{
    // 스테이지의 언락 여부, 보상, 최단 클리어 타임, 최저 사망 횟수 관리를 담당.
    // JSON 로드 직후와 SetCleared 호출 후에 DispatchAll을 통해 초기/갱신된 상태를 이벤트로 발행.
    public class StageManager
    {
        public event Action<int> OnStageUnlocked;       // 스테이지 언락(클리어) 시 발생 (stageNum)
        public event Action<int, int> OnBestRewardUpdated;   // 최고 보상(gem) 갱신 시 발생 (stageNum, bestReward)
        public event Action<int, float> OnBestTimeUpdated;     // 최단 클리어 타임 갱신 시 발생 (stageNum, bestTime)
        public event Action<int, int> OnBestDeathUpdated;  // 스테이지의 최소 사망 횟수(death best) 기록이 갱신될때 호출되는 이벤트 (stageNum, bestDeathCount)

        // Managers.Awake() 직후 호출.
        // OnLoaded 구독 + 즉시 DispatchAll 호출.
        public void Init()
        {
            var dataMgr = Managers.Instance.Data;
            // JSON 파일 로드 완료 시 DispatchAll 실행
            dataMgr.OnLoaded += (SaveData saveData) => DispatchAll();

            // Init 호출 직후에도 한 번 상태를 발행하여 UI를 초기화
            DispatchAll();
        }

        // 현재 메모리에 로드된 SaveData를 반환
        private SaveData Current
        {
            get { return Managers.Instance.Data.Current; }
        }


        // 해당 스테이지가 언락(클리어)되었는지
        public bool IsUnlocked(int stageNum)
        {
            var arr = Current.stageClears;
            return stageNum == 1
                || (stageNum - 1 < arr.Length && arr[stageNum - 1]);
        }
        // 해당 스테이지에서 획득한 최고 보상 개수 (gem)
        public int GetBestReward(int stageNum)
        {
            var arr = Current.stageRewards;
            return (stageNum - 1 < arr.Length)
                ? arr[stageNum - 1]
                : 0;
        }

        // 해당 스테이지의 최단 클리어 타임(초)
        public float GetBestTime(int stageNum)
        {
            var arr = Current.stageTimes;
            return (stageNum - 1 < arr.Length)
                ? arr[stageNum - 1]
                : float.MaxValue;
        }

        // 스테이지 클리어 처리: 플래그, 보상, 기록 업데이트 및 저장
        public void SetCleared(int stageNum, int gemCount, float clearTime, int deathCount)
        {
            // 1) 클리어 플래그
            var clears = new List<bool>(Current.stageClears);
            while (clears.Count < stageNum) clears.Add(false);
            if (!clears[stageNum - 1])
            {
                clears[stageNum - 1] = true;
                Current.stageClears = clears.ToArray();
                OnStageUnlocked?.Invoke(stageNum);
            }

            // 2) 최고 보상
            var rewards = new List<int>(Current.stageRewards);
            while (rewards.Count < stageNum) rewards.Add(0);
            if (gemCount > rewards[stageNum - 1])
            {
                rewards[stageNum - 1] = gemCount;
                Current.stageRewards = rewards.ToArray();
                OnBestRewardUpdated?.Invoke(stageNum, gemCount);
            }

            // 3) 최단 클리어 타임
            var times = new List<float>(Current.stageTimes);
            while (times.Count < stageNum) times.Add(float.MaxValue);
            if (clearTime < times[stageNum - 1])
            {
                times[stageNum - 1] = clearTime;
                Current.stageTimes = times.ToArray();
                OnBestTimeUpdated?.Invoke(stageNum, clearTime);
            }

            // 4) 최저 사망 횟수(death best) 갱신
            var deaths = new List<int>(Current.stageDeathCounts);
            while (deaths.Count < stageNum) 
                deaths.Add(int.MaxValue);
            
            if (deathCount < deaths[stageNum - 1])
            {
                deaths[stageNum - 1] = deathCount;
                Current.stageDeathCounts = deaths.ToArray();
                OnBestDeathUpdated?.Invoke(stageNum, deathCount);
            }

            // JSON 저장
            Managers.Instance.Data.Save();
        }

        // JSON 로드 직후와 Init 직후에 호출되어
        // 저장된 모든 스테이지 데이터를 이벤트로 발행.
        void DispatchAll()
        {
            var sd = Current;

            // 언락
            for (int i = 0; i < sd.stageClears.Length; i++)
                if (sd.stageClears[i])
                    OnStageUnlocked?.Invoke(i + 1);

            // 보상
            for (int i = 0; i < sd.stageRewards.Length; i++)
                if (sd.stageRewards[i] > 0)
                    OnBestRewardUpdated?.Invoke(i + 1, sd.stageRewards[i]);

            // 시간
            for (int i = 0; i < sd.stageTimes.Length; i++)
                if (sd.stageTimes[i] < float.MaxValue)
                    OnBestTimeUpdated?.Invoke(i + 1, sd.stageTimes[i]);

            // 최저 사망 횟수 이벤트
            for (int i = 0; i < sd.stageDeathCounts.Length; i++)
                if (sd.stageDeathCounts[i] < int.MaxValue)
                    OnBestDeathUpdated?.Invoke(i + 1, sd.stageDeathCounts[i]);
        }
    }
}
