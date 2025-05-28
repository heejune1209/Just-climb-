using System;
using System.Collections.Generic;
using JustClimb.Manager;

namespace JustClimb.Manager
{
    /// <summary>
    /// 스테이지별로 Top N 랭킹(클리어 타임, 사망 횟수)을 관리.
    /// 서버가 준비되면 델타 전송 대신 이 매니저를 확장.
    /// </summary>
    public class RankingManager
    {
        /// <summary>
        /// 한 스테이지의 한 명 기록
        /// </summary>
        public class RankingEntry
        {
            public int StageNum { get; set; }
            public string PlayerName { get; set; }
            public float ClearTime { get; set; }
            public int DeathCount { get; set; }
        }

        /// <summary>
        /// 랭킹이 갱신되면 발생 (stageNum)
        /// </summary>
        public event Action<int> OnRankingUpdated;

        // 스테이지 번호 → 그 스테이지의 Top N 기록
        private Dictionary<int, List<RankingEntry>> _stageRankings
            = new Dictionary<int, List<RankingEntry>>();

        // 보여줄 최대 랭킹 개수
        private int _maxEntries = 10;

        /// <summary>
        /// Managers.Awake() 직후 호출.
        /// </summary>
        public void Init()
        {
            var stageMgr = Managers.Instance.Stage;

            // 최단 기록/최저 사망 횟수 갱신 시 랭킹 다시 계산
            stageMgr.OnBestTimeUpdated += (int stage, float time) => UpdateRanking(stage);
            stageMgr.OnBestDeathUpdated += (int stage, int death) => UpdateRanking(stage);

            // 초기 로드된 상태도 한 번 반영
            for (int i = 1; i <= Managers.Instance.Data.Current.stageTimes.Length; i++)
                UpdateRanking(i);
        }

        /// <summary>
        /// 내부: 특정 스테이지의 “You” 기록을 갱신하고 전체 리스트를 다시 정렬합니다.
        /// </summary>
        private void UpdateRanking(int stageNum)
        {
            // 1) 해당 스테이지 리스트 확보
            if (!_stageRankings.TryGetValue(stageNum, out var list))
            {
                list = new List<RankingEntry>();
                _stageRankings[stageNum] = list;
            }

            // 2) JSON 에 저장된 최단 기록·최저 사망 횟수 가져오기
            float bestTime = Managers.Instance.Stage.GetBestTime(stageNum);
            int bestDeath = Managers.Instance.Data.Current.stageDeathCounts.Length >= stageNum
                              ? Managers.Instance.Data.Current.stageDeathCounts[stageNum - 1]
                              : int.MaxValue;

            // 3) “You” 엔트리 찾거나 새로 추가
            var you = list.Find(e => e.PlayerName == "You");
            if (you == null)
            {
                you = new RankingEntry
                {
                    StageNum = stageNum,
                    PlayerName = "You",
                    ClearTime = bestTime,
                    DeathCount = bestDeath
                };
                list.Add(you);
            }
            else
            {
                you.ClearTime = bestTime;
                you.DeathCount = bestDeath;
            }

            // 4) 시간 오름차순 → 사망 수 오름차순으로 정렬
            list.Sort((a, b) =>
            {
                int c = a.ClearTime.CompareTo(b.ClearTime);
                return c != 0 ? c : a.DeathCount.CompareTo(b.DeathCount);
            });

            // 5) Top N 유지
            if (list.Count > _maxEntries)
                list.RemoveRange(_maxEntries, list.Count - _maxEntries);

            // 6) UI 등 구독자에게 알림
            OnRankingUpdated?.Invoke(stageNum);
        }

        /// <summary>
        /// UI 에서 호출: 특정 스테이지의 Top N 리스트를 반환.
        /// </summary>
        public IReadOnlyList<RankingEntry> GetRanking(int stageNum)
        {
            return _stageRankings.TryGetValue(stageNum, out var list)
                ? list
                : new List<RankingEntry>();
        }
    }
}
