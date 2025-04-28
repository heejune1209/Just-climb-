using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using DiasGames.Controller;

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

    private DateTime _startTime;
    private int _playerDeathCount;

    public int PlayerDeathCount
    {
        get => _playerDeathCount;
        set
        {
            _playerDeathCount = value;
            OnDeathCountChanged?.Invoke(_playerDeathCount);
        }
    }
    public Vector3 FlagPosition { get; private set; }

    // 내부: 누적된 경과 시간(초)
    private double _elapsedSeconds;

    void Awake()
    {
        // Managers.Game 으로 싱글톤 보장
        if (Managers.Game != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // 초기화
        _startTime = DateTime.UtcNow;
        PlayerDeathCount = 0;
        IsTimerPaused = false;

        _elapsedSeconds = 0.0;
    }

    void Update()
    {
        // 타이머 계산
        if (!IsTimerPaused)
        {
            _elapsedSeconds += Time.deltaTime;
            OnTimerUpdated?.Invoke(TimeSpan.FromSeconds(_elapsedSeconds));
        }
    }


    public void GoToLobby()
    {
        ResetTimer();
        PlayerDeathCount = 0;
        OnDeathCountChanged?.Invoke(0);
    }

    public void GoToMain()
    {
        ResetTimer();
    }

    public void ResetTimer()
    {
        _elapsedSeconds = 0.0;
        OnTimerUpdated?.Invoke(TimeSpan.Zero);
    }

    public void ResetDeathCount() => PlayerDeathCount = 0;

    public void PlayerDie()
    {
        PlayerDeathCount++;
        OnDeathCountChanged?.Invoke(PlayerDeathCount);

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Stage")
            && int.TryParse(sceneName.Substring(5), out int stageNumber))
        {
            PlayerPrefs.SetInt($"Death{stageNumber}", PlayerDeathCount);
            PlayerPrefs.Save();
        }
    }
}