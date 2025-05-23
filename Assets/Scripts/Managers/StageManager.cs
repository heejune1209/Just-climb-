using System;
using System.Collections.Generic;
using JustClimb.Manager;
using UnityEngine;

namespace JustClimb.Manager
{
    /// <summary>
    /// 스테이지 클리어 플래그, 보상, 최고 기록(클리어 타임) 관리를 전담.
    /// JSON 로드 직후 초기 상태를 발행하고,
    /// SetCleared() 호출 시 즉시 업데이트 후 저장.
    /// </summary>
    public class StageManager
    {
        public event Action<int> OnStageUnlocked;       // 언락 이벤트 (stageNum)
        public event Action<int, int> OnBestRewardUpdated;   // 새로운 최고 보상 이벤트 (stageNum, bestReward)
        public event Action<int, float> OnBestTimeUpdated;     // 새로운 최고 기록 이벤트 (stageNum, bestTime)

        public void Init()
        {
            var dataMgr = Managers.Instance.Data;
            // SaveData 인스턴스를 saveData라는 이름으로 받고, DispatchAll()만 실행
            dataMgr.OnLoaded += (SaveData saveData) => DispatchAll();

            // 시작 직후에도 한 번 실행
            DispatchAll();
        }
        
        // 현재 저장된 데이터를 참조
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
        public void SetCleared(int stageNum, int gemCount, float clearTime)
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

            // 4) JSON 저장
            Managers.Instance.Data.Save();
        }

        // JSON에서 불러온 시점과 저장 이후에, 
        // 현재 SaveData에 담긴 모든 스테이지 정보를 이벤트로 발행.
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
        }
    }
}
