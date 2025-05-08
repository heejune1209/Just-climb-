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

    // 체크포인트 저장 키 템플릿
    private const string KEY_FLAG_X = "Stage{0}_FlagX";
    private const string KEY_FLAG_Y = "Stage{0}_FlagY";
    private const string KEY_FLAG_Z = "Stage{0}_FlagZ";

    // 마지막으로 깃발을 꽂은 위치
    public Vector3 FlagPosition { get; private set; }
    // 깃발 위치가 저장되어 있는지 여부
    public bool HasFlagPosition { get; private set; }

    // 외부에서 현재 누적된 경과 시간을 가져올 수 있도록 추가
    public TimeSpan ElapsedTime()
    {
        return TimeSpan.FromSeconds(_elapsedSeconds);
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 씬 전환 콜백
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // PlayerPrefs에서 깃발 위치 불러오기
        if (TryGetStageIndex(out int idx))
        {
            string kx = string.Format(KEY_FLAG_X, idx);
            string ky = string.Format(KEY_FLAG_Y, idx);
            string kz = string.Format(KEY_FLAG_Z, idx);

            if (PlayerPrefs.HasKey(kx) &&
                PlayerPrefs.HasKey(ky) &&
                PlayerPrefs.HasKey(kz))
            {
                FlagPosition = new Vector3(
                    PlayerPrefs.GetFloat(kx),
                    PlayerPrefs.GetFloat(ky),
                    PlayerPrefs.GetFloat(kz)
                );
                HasFlagPosition = true;
            }
        }
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
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // 플레이어 사망 시 호출.
    // 사망 카운트 증가·저장하고, 깃발 위치로 리스폰.
    // out 매개변수는 넘겨줄 때 초기화가 필요 없고, 메서드 내부에서 idx = … 처리를 반드시 해 주기 때문에,
    // 호출부로 돌아오면 idx에 그 값이 담겨 있다. 그리고 Call by Reference 방식이다.
    public void OnPlayerDead()
    {
        // 1) 사망 카운트 증가·저장
        PlayerDeathCount++;
        if (TryGetStageIndex(out int idx))
        {
            PlayerPrefs.SetInt($"Death{idx}", PlayerDeathCount);
            PlayerPrefs.Save();
        }

        // 2) 3초 뒤에 실제 리스폰 처리 실행
        StartCoroutine(RespawnAfterDelay(3f));
    }

    // 플레이어 사망 후 delay초만큼 연출 대기한 다음
    // 로딩 씬을 거쳐 현재 씬을 완전 리로드.
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 현재 씬 다시 로드
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);       
    }


    // 체크포인트 저장 메서드
    public void SaveFlagPosition(Vector3 pos)
    {
        if (!TryGetStageIndex(out int idx)) return;
        // Stage1_FlagX, Stage2_FlagX 처럼 키를 만듭니다.
        PlayerPrefs.SetFloat(string.Format(KEY_FLAG_X, idx), pos.x);
        PlayerPrefs.SetFloat(string.Format(KEY_FLAG_Y, idx), pos.y);
        PlayerPrefs.SetFloat(string.Format(KEY_FLAG_Z, idx), pos.z);
        PlayerPrefs.Save();

        FlagPosition = pos;
        HasFlagPosition = true;
    }

    // 씬 로드 시 처리
    // 씬 로드 시마다 실행: 저장된 시간·사망 횟수 복원, 이벤트 구독, 타이머 제어, 깃발 리스폰
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Stage") && TryGetStageIndex(out int idx))
        {
            // 사망 횟수 복원
            int savedDeaths = PlayerPrefs.GetInt($"Death{idx}", 0);
            PlayerDeathCount = savedDeaths;

            // 시간 복원
            float savedTime = PlayerPrefs.GetFloat($"Time{idx}", 0f);
            _elapsedSeconds = savedTime;
            OnTimerUpdated?.Invoke(TimeSpan.FromSeconds(_elapsedSeconds));

            // Health.OnDead 이벤트 구독
            SubscribePlayerDeath();

            // 타이머 재개
            IsTimerPaused = false;

            // 깃발 리스폰
            if (HasFlagPosition)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    player.transform.position = FlagPosition;
            }
        }
        else
        {
            // 스테이지가 아니면 타이머 정지
            IsTimerPaused = true;
        }
    }

    // 씬 언로드 시: 스테이지면 현재 값 저장
    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name.StartsWith("Stage") && TryGetStageIndex(out int idx))
        {
            PlayerPrefs.SetFloat($"Time{idx}", (float)_elapsedSeconds);
            PlayerPrefs.Save();
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
        if (name.StartsWith("Stage") && int.TryParse(name.Substring(5), out idx))
            return true;
        idx = -1;
        return false;
    }
}