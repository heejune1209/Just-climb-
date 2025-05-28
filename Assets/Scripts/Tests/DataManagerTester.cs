using System;
using System.Linq;
using JustClimb.Manager;
using UnityEngine;

public class DataManagerTester : MonoBehaviour
{
    void Start()
    {
        //// 1) 초기화 (Init() 내부에서 Load()까지 실행됨)
        //Managers.Instance.Data.Init();

        //// 2) 강제 Load 호출 (Init 이후 재확인용)
        //Managers.Instance.Data.Load();

        //// 3) 마이그레이션 수행
        //MigrateZeroBasedIndexes(Managers.Instance.Data.Current);

        //// 4) 마이그레이션 후 바로 저장
        //Managers.Instance.Data.Save();

        //// 5) 결과 로그
        //Debug.Log("[Test] stagePlayTimes after migration: " +
        //          string.Join(", ", Managers.Instance.Data.Current.stagePlayTimes));
        //Debug.Log("[Test] stageDeathCounts after migration: " +
        //          string.Join(", ", Managers.Instance.Data.Current.stageDeathCounts));
        //for (int i = 0; i < Managers.Instance.Data.Current.stageFlagPositions.Length; i++)
        //{
        //    var p = Managers.Instance.Data.Current.stageFlagPositions[i];
        //    Debug.Log($"[Test] stageFlagPositions[{i}] after migration: {p.x}, {p.y}, {p.z}");
        //}
    }

    /// <summary>
    /// 1-based 인덱스였던 Stage 데이터를 0-based로 당겨 줍니다.
    /// (예: 기존 slot[1] → slot[0], slot[2] → slot[1], …)
    /// </summary>
    void MigrateZeroBasedIndexes(SaveData data)
    {
        // 플레이 타임
        if (data.stagePlayTimes.Length > 1
         && Math.Abs(data.stagePlayTimes[0]) < 1e-6f
         && data.stagePlayTimes[1] > 0f)
        {
            data.stagePlayTimes = data.stagePlayTimes.Skip(1).ToArray();
        }

        // 사망 카운트
        if (data.stageDeathCounts.Length > 1
         && data.stageDeathCounts[0] == 0
         && data.stageDeathCounts[1] > 0)
        {
            data.stageDeathCounts = data.stageDeathCounts.Skip(1).ToArray();
        }

        // 깃발 위치
        if (data.stageFlagPositions.Length > 1
         && data.stageFlagPositions[0].x == 0f
         && (data.stageFlagPositions[1].x != 0f
          || data.stageFlagPositions[1].y != 0f
          || data.stageFlagPositions[1].z != 0f))
        {
            data.stageFlagPositions = data.stageFlagPositions.Skip(1).ToArray();
        }
    }
}
