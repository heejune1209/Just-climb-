using JustClimb.Data;
using JustClimb.Manager;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// 스테이지별 랭킹 표시용 팝업 UI
/// 스테이지 버튼 클릭으로 랭킹 패널 열기/닫기
/// 드롭다운으로 정렬 기준 선택, 상위 20명 + 자신의 기록 별도 표시
/// </summary>
public class UI_Ranking : UI_Popup
{
    [Inject] private IRankingManager _rankingManager;

    [Header("UI References")]
    // 스테이지 선택 버튼들
    [SerializeField] private Button[] _stageButtons; // Stage 1~10 버튼 배열
    
    // 랭킹 패널 (토글 가능)
    [SerializeField] private GameObject _rankingPanel; // 전체 랭킹 패널
    [SerializeField] private Button _closeRankingButton; // 랭킹 패널 닫기 버튼
    
    // 랭킹 패널 내부 요소들
    [SerializeField] private TMP_Dropdown _sortDropdown; // 정렬 기준 선택 드롭다운
    [SerializeField] private Button _refreshButton; // 새로고침 버튼
    [SerializeField] private Transform _contentRoot; // 랭킹 항목들이 들어갈 부모 Transform
    [SerializeField] private GameObject _entryPrefab; // 한 줄 랭킹 항목 프리팹
    [SerializeField] private TextMeshProUGUI _noDataText; // 데이터가 없을 때 표시할 텍스트
    
    // 동적 헤더 텍스트들
    [SerializeField] private TextMeshProUGUI _valueHeaderText; // "Time" 또는 "Deaths" 헤더 텍스트

    [Header("Display Settings")]
    [Tooltip("최대 표시할 상위 랭킹 개수")]
    [SerializeField] private int _maxVisibleEntries = 20;
    [Tooltip("기본 항목 배경 색상")]
    [SerializeField] private Color _normalEntryColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [Tooltip("나의 항목 배경 색상 (노란색)")]
    [SerializeField] private Color _highlightEntryColor = new Color(1f, 1f, 0f, 0.9f);

    [Header("Settings")]
    [Tooltip("최대 스테이지 수")]
    [SerializeField] private int _maxStages = 10;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Debug Settings")]
    [Tooltip("테스트용 더미 데이터 사용")]
    [SerializeField] private bool _useTestData = false;
#endif

    // 현재 선택된 값들
    private int _currentStage = 1;
    private RankingSortType _currentSortType = RankingSortType.ClearTime;

    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
        
        // 초기 상태: 랭킹 패널 숨김
        if (_rankingPanel != null)
            _rankingPanel.SetActive(false);
    }

    private void Update()
    {
        // ESC 키 처리
        HandleEscape();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 테스트용 더미 데이터 토글 (T 키)
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            _useTestData = !_useTestData;
            Debug.Log($"[UI_Ranking] 테스트 데이터 모드: {(_useTestData ? "ON" : "OFF")}");
            
            if (_rankingPanel != null && _rankingPanel.activeSelf)
            {
                RefreshRankingDisplay();
            }
        }
#endif
    }

    /// <summary>
    /// UI 요소들을 초기화합니다.
    /// </summary>
    private void InitializeUI()
    {
        InitializeStageButtons();
        InitializeSortDropdown();
        UpdateHeaderText();
    }

    /// <summary>
    /// 이벤트들을 구독합니다.
    /// </summary>
    private void SubscribeToEvents()
    {
        // 버튼 이벤트 바인딩
        if (_refreshButton != null)
            _refreshButton.onClick.AddListener(OnRefreshClicked);
        if (_closeRankingButton != null)
            _closeRankingButton.onClick.AddListener(CloseRankingPanel);
        
        // 랭킹 갱신 이벤트 구독
        if (_rankingManager != null)
            _rankingManager.OnRankingUpdated += OnRankingUpdated;
    }

    /// <summary>
    /// 스테이지 버튼들을 초기화합니다.
    /// </summary>
    private void InitializeStageButtons()
    {
        if (_stageButtons == null) return;
        
        for (int i = 0; i < _stageButtons.Length; i++)
        {
            if (_stageButtons[i] != null)
            {
                int stageNum = i + 1; // 1-based stage number
                _stageButtons[i].onClick.AddListener(() => OpenRankingPanel(stageNum));
            }
        }
    }

    /// <summary>
    /// 정렬 드롭다운을 초기화합니다.
    /// </summary>
    private void InitializeSortDropdown()
    {
        if (_sortDropdown == null) return;

        _sortDropdown.ClearOptions();
        var sortOptions = new List<string>
        {
            "Best Clear Time",  // RankingSortType.ClearTime
            "Least Deaths"      // RankingSortType.DeathCount
        };
        _sortDropdown.AddOptions(sortOptions);
        _sortDropdown.value = 0; // 기본값: ClearTime
        
        // 간단한 이벤트 핸들러 사용
        _sortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);
    }

    /// <summary>
    /// 스테이지 버튼을 클릭했을 때 랭킹 패널을 엽니다.
    /// </summary>
    private void OpenRankingPanel(int stageNum)
    {
        _currentStage = stageNum;
        _currentSortType = RankingSortType.ClearTime; // 기본값으로 리셋
        
        // 드롭다운 값 초기화
        ResetSortDropdown();
        
        // 랭킹 패널 활성화
        if (_rankingPanel != null)
            _rankingPanel.SetActive(true);
        
        // UI 갱신
        RefreshRankingDisplay();
    }

    /// <summary>
    /// 드롭다운을 기본값으로 리셋합니다.
    /// </summary>
    private void ResetSortDropdown()
    {
        if (_sortDropdown != null)
        {
            _sortDropdown.onValueChanged.RemoveListener(OnSortDropdownChanged);
            _sortDropdown.value = 0; // ClearTime
            _sortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);
        }
    }

    /// <summary>
    /// 랭킹 패널을 닫습니다.
    /// </summary>
    private void CloseRankingPanel()
    {
        if (_rankingPanel != null)
            _rankingPanel.SetActive(false);
    }

    /// <summary>
    /// 정렬 드롭다운 값이 변경되었을 때 호출됩니다.
    /// </summary>
    private void OnSortDropdownChanged(int index)
    {
        var newSortType = (RankingSortType)index;
        
        if (newSortType != _currentSortType)
        {
            _currentSortType = newSortType;
            
            // 캐시 무효화 후 새로고침
            _rankingManager.InvalidateCache(_currentStage);
            RefreshRankingDisplay();
        }
    }

    /// <summary>
    /// 랭킹이 갱신되었을 때 호출됩니다.
    /// </summary>
    private void OnRankingUpdated(int stageNum, RankingSortType sortType)
    {
        // 현재 선택된 스테이지와 정렬 기준과 일치하는 경우에만 UI 갱신
        if (stageNum == _currentStage && sortType == _currentSortType)
        {
            RefreshRankingDisplay();
        }
    }

    /// <summary>
    /// 랭킹 표시를 새로고침합니다.
    /// </summary>
    private void RefreshRankingDisplay()
    {
        UpdateHeaderText();
        ClearRankingEntries();
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 테스트 데이터 사용 여부 확인
        if (_useTestData)
        {
            DisplayTestRankingData();
            return;
        }
#endif
        
        // 랭킹 데이터 가져오기
        var (topEntries, myEntry) = _rankingManager.GetRankingWithMyEntry(_currentStage, _currentSortType, _maxVisibleEntries);

        // 데이터가 없는 경우
        if (topEntries.Count == 0 && myEntry == null)
        {
            ShowNoDataMessage();
            return;
        }

        HideNoDataMessage();
        DisplayRankingEntries(topEntries, myEntry);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 테스트용 더미 랭킹 데이터를 표시합니다.
    /// </summary>
    private void DisplayTestRankingData()
    {
        HideNoDataMessage();
        
        // 더미 Top 20 데이터 생성
        var testTopEntries = GenerateTestTopEntries();
        
        // 더미 내 기록 생성 (순위 25등으로 설정)
        var testMyEntry = GenerateTestMyEntry();
        
        DisplayRankingEntries(testTopEntries, testMyEntry);
        
        Debug.Log($"[UI_Ranking] 테스트 데이터 표시 완료 - Top: {testTopEntries.Count}, My: {(testMyEntry != null ? "있음" : "없음")}");
    }

    /// <summary>
    /// 테스트용 상위 20명 데이터를 생성합니다.
    /// </summary>
    private List<RankingEntry> GenerateTestTopEntries()
    {
        var entries = new List<RankingEntry>();
        
        string[] testNames = {
            "ProClimber", "SpeedRunner", "MountainKing", "RockStar", "Climber99",
            "FastFingers", "WallCrawler", "VerticalMaster", "EdgeLord", "GripStrong",
            "ClimbingBeast", "RockHopper", "SummitSeeker", "CragMaster", "BoulderPro",
            "AlpineLegend", "CliffHanger", "StoneSkipper", "PeakChaser", "RopeExpert"
        };
        
        for (int i = 0; i < 20; i++)
        {
            var entry = new RankingEntry
            {
                Rank = i + 1,
                UserId = $"test_user_{i + 1}",
                DisplayName = testNames[i],
                IsMyRecord = false
            };
            
            if (_currentSortType == RankingSortType.ClearTime)
            {
                // 시간 기준: 10초부터 30초까지
                entry.ClearTime = 10f + (i * 1f);
                entry.DeathCount = UnityEngine.Random.Range(0, 5);
            }
            else
            {
                // 사망 기준: 0회부터 19회까지
                entry.DeathCount = i;
                entry.ClearTime = UnityEngine.Random.Range(15f, 45f);
            }
            
            entries.Add(entry);
        }
        
        return entries;
    }

    /// <summary>
    /// 테스트용 내 기록을 생성합니다 (25등으로 설정).
    /// </summary>
    private RankingEntry GenerateTestMyEntry()
    {
        var myEntry = new RankingEntry
        {
            Rank = 25, // Top 20 밖의 순위
            UserId = "my_test_user",
            DisplayName = "You",
            IsMyRecord = true
        };
        
        if (_currentSortType == RankingSortType.ClearTime)
        {
            myEntry.ClearTime = 35f; // 상위 20명보다 느린 시간
            myEntry.DeathCount = 3;
        }
        else
        {
            myEntry.DeathCount = 25; // 상위 20명보다 많은 사망
            myEntry.ClearTime = 28f;
        }
        
        return myEntry;
    }
#endif

    /// <summary>
    /// 정렬 타입에 따라 헤더 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateHeaderText()
    {
        if (_valueHeaderText != null)
        {
            string headerText = _currentSortType == RankingSortType.ClearTime ? "Time" : "Deaths";
            _valueHeaderText.text = headerText;
        }
    }

    /// <summary>
    /// 기존 랭킹 항목들을 모두 제거합니다.
    /// </summary>
    private void ClearRankingEntries()
    {
        foreach (Transform child in _contentRoot)
        {
            if (child != _noDataText?.transform) // NoData 텍스트는 제외
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 데이터가 없을 때 메시지를 표시합니다.
    /// </summary>
    private void ShowNoDataMessage()
    {
        if (_noDataText != null)
        {
            _noDataText.text = $"No ranking data available for Stage {_currentStage}.";
            _noDataText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 데이터 없음 메시지를 숨깁니다.
    /// </summary>
    private void HideNoDataMessage()
    {
        if (_noDataText != null)
            _noDataText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 랭킹 항목들을 표시합니다.
    /// </summary>
    private void DisplayRankingEntries(IReadOnlyList<RankingEntry> topEntries, RankingEntry myEntry)
    {
        // 상위 N개 표시
        foreach (var entry in topEntries)
        {
            bool isMyRecord = entry.IsMyRecord || entry.PlayerName == "You" || entry.DisplayName == "You";
            CreateRankingEntryUI(entry, isMyRecord);
        }

        // 내 기록이 상위에 없으면 별도 표시
        if (myEntry != null && !IsEntryInTopList(myEntry, topEntries))
        {
            AddSeparator();
            CreateRankingEntryUI(myEntry, true);
        }
    }

    /// <summary>
    /// 내 기록이 상위 목록에 포함되어 있는지 확인합니다.
    /// </summary>
    private bool IsEntryInTopList(RankingEntry myEntry, IReadOnlyList<RankingEntry> topEntries)
    {
        foreach (var entry in topEntries)
        {
            if (entry.UserId == myEntry.UserId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 랭킹 항목 UI를 생성합니다.
    /// </summary>
    private void CreateRankingEntryUI(RankingEntry entry, bool isMyRecord)
    {
        if (_entryPrefab == null || _contentRoot == null) return;
        
        var go = Instantiate(_entryPrefab, _contentRoot);
        var ui = go.GetComponent<UI_RankingEntry>();
        
        if (ui != null)
        {
            ui.SetData(entry.Rank, entry.PlayerName, entry.ClearTime, entry.DeathCount, _currentSortType);
            
            Color targetColor = isMyRecord ? _highlightEntryColor : _normalEntryColor;
            ui.SetBackgroundColor(targetColor);
        }
    }

    /// <summary>
    /// Top N과 내 순위 사이에 구분선을 추가합니다.
    /// </summary>
    private void AddSeparator()
    {
        var sepGO = new GameObject("Separator");
        var layout = sepGO.AddComponent<LayoutElement>();
        layout.minHeight = 30;
        layout.preferredHeight = 30;
        
        var line = sepGO.AddComponent<Image>();
        line.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        
        sepGO.transform.SetParent(_contentRoot, false);
    }

    /// <summary>
    /// 새로고침 버튼 클릭 시 호출
    /// </summary>
    private void OnRefreshClicked()
    {
        // 캐시 무효화하여 서버에서 최신 데이터 로드
        _rankingManager.InvalidateCache(_currentStage);
        RefreshRankingDisplay();
    }

    /// <summary>
    /// ESC 키 처리: 랭킹 패널이 열려있으면 패널 닫기, 아니면 팝업 닫기
    /// </summary>
    protected override void HandleEscape()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 1) 랭킹 패널이 열려 있으면 패널 닫기
            if (_rankingPanel != null && _rankingPanel.activeSelf)
            {
                CloseRankingPanel();
            }
            else
            {
                // 2) 아니면 팝업 자체 닫기
                base.HandleEscape();
            }
        }
    }

    // 외부 호출용 메서드들
    
    /// <summary>
    /// 특정 스테이지로 설정하고 랭킹 패널 열기 (외부에서 호출 가능)
    /// </summary>
    public void SetStage(int stageNum)
    {
        OpenRankingPanel(Mathf.Clamp(stageNum, 1, _maxStages));
    }

    /// <summary>
    /// 정렬 기준 설정 (외부에서 호출 가능)
    /// </summary>
    public void SetSortType(RankingSortType sortType)
    {
        _currentSortType = sortType;
        
        // 드롭다운 값 설정
        if (_sortDropdown != null)
        {
            _sortDropdown.onValueChanged.RemoveListener(OnSortDropdownChanged);
            _sortDropdown.value = (int)sortType;
            _sortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);
        }
        
        RefreshRankingDisplay();
    }

    // 메모리 정리
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        // 스테이지 버튼 이벤트 해제
        if (_stageButtons != null)
        {
            for (int i = 0; i < _stageButtons.Length; i++)
            {
                if (_stageButtons[i] != null)
                {
                    int stageNum = i + 1;
                    _stageButtons[i].onClick.RemoveListener(() => OpenRankingPanel(stageNum));
                }
            }
        }
        
        // 다른 버튼 이벤트 해제
        if (_refreshButton != null)
            _refreshButton.onClick.RemoveListener(OnRefreshClicked);
        if (_closeRankingButton != null)
            _closeRankingButton.onClick.RemoveListener(CloseRankingPanel);
        
        // 랭킹 매니저 이벤트 해제
        if (_rankingManager != null)
            _rankingManager.OnRankingUpdated -= OnRankingUpdated;
        
        // 드롭다운 이벤트 해제
        if (_sortDropdown != null)
            _sortDropdown.onValueChanged.RemoveListener(OnSortDropdownChanged);
    }
}