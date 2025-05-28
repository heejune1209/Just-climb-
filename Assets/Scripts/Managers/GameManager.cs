using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using DiasGames.Components;
using DiasGames;
using System.Reflection;
using Cinemachine.Examples;
using static UnityEditor.Experimental.GraphView.GraphView;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    // 모든 외부 참조는 Managers.Game 으로 접근.

    // 타이머 일시정지 플래그
    public bool IsTimerPaused { get; set; }

    // 타이머를 계산하고, 플레이어 사망 카운트를 기록만 한다.
    // 경과 시간 업데이트 이벤트 (UI_Stage에서 구독)
    public event Action<TimeSpan> OnTimerUpdated;

    // 사망 횟수 변경 이벤트 (UI_Stage에서 구독)
    public event Action<int> OnDeathCountChanged;

    private int _playerDeathCount;

    public int PlayerDeathCount
    {
        get { return _playerDeathCount; }
        set
        {
            _playerDeathCount = value;
            OnDeathCountChanged?.Invoke(_playerDeathCount);
        }
    }

    // 내부: 누적된 경과 시간(초)
    private double _elapsedSeconds;

    // 마지막으로 깃발을 꽂은 위치
    public Vector3 FlagPosition { get; private set; }
    // 깃발 위치가 저장되어 있는지 여부
    public bool HasFlagPosition { get; private set; }

    // 외부에서 현재 누적된 경과 시간을 가져올 수 있도록 추가
    public TimeSpan ElapsedTime()
    {
        return TimeSpan.FromSeconds(_elapsedSeconds);
    }

    /// <summary>
    /// Managers 컨테이너에서 Awake() 직후 호출됩니다.
    /// </summary>
    public void Init()
    {
        // 1) 씬 전환 콜백 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // 2) 이전에 저장된 깃발 위치 불러오기
        // JSON 로드 후, 현재 스테이지에 저장된 깃발 위치 복원
        if (TryGetStageIndex(out int idx))
        {
            var sd = Managers.Instance.Data.Current;
            if (idx < sd.stageFlagPositions.Length)
            {
                FlagPosition = sd.stageFlagPositions[idx].ToVector3();
                HasFlagPosition = true;
            }
            // ─── 사망 횟수 복원 ───
            if (idx < sd.stageDeathCounts.Length)
            {
                // 프로퍼티로 세팅하면 이벤트도 발행됨
                PlayerDeathCount = sd.stageDeathCounts[idx];
            }
        }
    }

    void Update()
    {
        if (!IsTimerPaused)
        {
            _elapsedSeconds += Time.deltaTime;
            OnTimerUpdated?.Invoke(TimeSpan.FromSeconds(_elapsedSeconds));
        }
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // 플레이어 사망 시 호출.   
    public void OnPlayerDead()
    {
        // (1) 사망 카운트 증가 → 데이터에 반영
        PlayerDeathCount++;

        // (2) JSON에 사망횟수 저장
        if (TryGetStageIndex(out int idx))
        {
            var data = Managers.Instance.Data.Current;
            var deaths = new List<int>(data.stageDeathCounts);
            while (deaths.Count <= idx) deaths.Add(0);
            deaths[idx] = PlayerDeathCount;
            data.stageDeathCounts = deaths.ToArray();
            Managers.Instance.Data.Save();
        }


        // (2) 리스폰
        StartCoroutine(RespawnAfterDelay(3f));
    }

    // 플레이어 사망 후 delay초만큼 연출 대기한 다음
    // 로딩 씬을 거쳐 현재 씬을 완전 리로드.
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 현재 로드된 씬 이름을 가져와서
        string sceneName = SceneManager.GetActiveScene().name;

        // Define.Scene enum으로 변환 시도
        if (Enum.TryParse<Define.Scene>(sceneName, out var sceneType))
        {
            // 3) SceneManagerEx의 LoadScene 호출
            Managers.Instance.Scene.LoadScene(sceneType);
        }
        else
        {
            Debug.LogError($"[GameManager] 씬 이름 '{sceneName}'을 Define.Scene으로 변환할 수 없습니다.");
            // 실패 시엔 기존 방식으로라도 한번 로드
            SceneManager.LoadScene(sceneName);
        }
    }


    /// <summary>
    /// 깃발 사용 시 호출 (FlagUse.Use에서)
    /// JSON의 stageFlagPositions[idx]에 저장
    /// </summary>
    public void SaveFlagPosition(Vector3 pos)
    {
        if (!TryGetStageIndex(out int idx)) return;

        var data = Managers.Instance.Data.Current;

        // 배열 크기 보장
        var flags = new List<SerializableVector3>(data.stageFlagPositions);
        while (flags.Count <= idx) flags.Add(default);
        flags[idx] = new SerializableVector3(pos.x, pos.y, pos.z);

        data.stageFlagPositions = flags.ToArray();
        Managers.Instance.Data.Save();

        FlagPosition = pos;
        HasFlagPosition = true;
    }

    // 씬 로드 시 처리
    // 씬 로드 시마다 실행: 저장된 시간·사망 횟수 복원, 이벤트 구독, 타이머 제어, 깃발 리스폰
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Stage") && TryGetStageIndex(out int idx))
        {
            var sd = Managers.Instance.Data.Current;

            if (sd.stageClears.Length > idx && sd.stageClears[idx])
            {
                // 이미 클리어된 스테이지를 다시 도전 → 0부터 시작
                _elapsedSeconds = 0;
                PlayerDeathCount = 0;
            }
            else
            {
                // 최초 도전 혹은 재진입(일시정지) → 저장된 값 복원
                _elapsedSeconds = idx < sd.stagePlayTimes.Length
                    ? sd.stagePlayTimes[idx]
                    : 0f;
                PlayerDeathCount = idx < sd.stageDeathCounts.Length
                    ? sd.stageDeathCounts[idx]
                    : 0;
            }

            OnTimerUpdated?.Invoke(TimeSpan.FromSeconds(_elapsedSeconds));
            // Health.OnDead 구독
            SubscribePlayerDeath();
            IsTimerPaused = false;

            // 깃발 리스폰
            if (idx < sd.stageFlagPositions.Length)
            {
                var v = sd.stageFlagPositions[idx].ToVector3();
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    player.transform.position = v;
            }
        }
        else
        {
            IsTimerPaused = true;
        }
    }

    // 씬 언로드 시: 스테이지면 현재 값 저장
    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name.StartsWith("Stage") && TryGetStageIndex(out int idx))
        {
            // 플레이 시간 저장
            var data = Managers.Instance.Data.Current;

            // stagePlayTimes 배열의 데이터를 새로 만든 List에 모두 복사
            var plays = new List<float>(data.stagePlayTimes);
            while (plays.Count <= idx) plays.Add(0f);
            plays[idx] = (float)_elapsedSeconds;
            data.stagePlayTimes = plays.ToArray();

            Managers.Instance.Data.Save();
        }
    }

    // 플레이어 Health.OnDead 이벤트에 연결
    private void SubscribePlayerDeath()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var health = player.GetComponent<Health>();
        if (health == null) return;

        health.OnDead -= OnPlayerDead;
        health.OnDead += OnPlayerDead;
    }

    // 헬퍼: 현재 스테이지 번호 반환
    private bool TryGetStageIndex(out int idx)
    {
        string name = SceneManager.GetActiveScene().name;
        if (name.StartsWith("Stage") && int.TryParse(name.Substring(5), out var num))
        {
            idx = num - 1;    
            return true;
        }
            
        idx = -1;
        return false;
    }
}