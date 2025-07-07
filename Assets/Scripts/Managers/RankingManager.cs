using JustClimb.Data;
using JustClimb.Manager;
using JustClimb.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;
using Newtonsoft.Json;
using System.Collections;
using System.Text;

namespace JustClimb.Manager
{
    /// <summary>
    /// 서버 기반 랭킹 매니저
    /// 정렬은 서버에서 처리하고 클라이언트는 결과만 받아서 표시
    /// </summary>
    public class RankingManager : IRankingManager, IInitializable
    {

        /// <summary>
        /// 랭킹이 갱신되면 발생 (stageNum, sortType)
        /// </summary>
        public event Action<int, RankingSortType> OnRankingUpdated;

        private readonly IDataManager _dataManager;
        private readonly IStageManager _stageManager;
        private readonly string _userId;
        private readonly SteamAuthManager _steamAuthManager;  // Steam 인증 매니저

        // 캐시된 랭킹 데이터 (스테이지 → 정렬타입 → 응답)
        private Dictionary<int, Dictionary<RankingSortType, RankingResponseDto>> _cachedRankings
            = new Dictionary<int, Dictionary<RankingSortType, RankingResponseDto>>();

        // 서버 설정
        private ServerConfig _serverConfig;
        private string _baseUrl;

        // 생성자 주입
        public RankingManager(IDataManager dataManager, IStageManager stageManager, [Inject(Id="UserId")] string userId, SteamAuthManager steamAuthManager)
        {
            _dataManager = dataManager;
            _stageManager = stageManager;
            _userId = userId;
            _steamAuthManager = steamAuthManager;

            // 서버 설정 로드
            _serverConfig = Resources.Load<ServerConfig>("ServerConfig");
            if (_serverConfig == null)
            {
                Debug.LogError("[RankingManager] ServerConfig를 찾을 수 없습니다!");
                _baseUrl = "https://localhost:7091/api/ranking";
            }
            else
            {
                _baseUrl = $"{_serverConfig.GetBaseUrl()}/api/ranking";
            }
        }

        /// <summary>
        /// UnityWebRequest에 JWT 토큰 헤더 추가
        /// </summary>
        private void AddAuthorizationHeader(UnityWebRequest request)
        {
            if (_steamAuthManager != null && _steamAuthManager.HasValidToken())
            {
                request.SetRequestHeader("Authorization", $"Bearer {_steamAuthManager.JwtToken}");
            }
        }

        /// <summary>
        /// Zenject에서 자동으로 호출됨.
        /// </summary>
        public void Initialize()
        {
            Init();
        }

        /// <summary>
        /// 실제 초기화 로직.
        /// </summary>
        public void Init()
        {
            // 최단 기록/최저 사망 횟수 갱신 시 서버에 기록 업데이트
            _stageManager.OnBestTimeUpdated += OnBestRecordUpdated;
            _stageManager.OnBestDeathUpdated += OnBestRecordUpdated;

            // 데이터가 로드된 이후에만 처리
            _dataManager.OnLoaded += HandleDataLoaded;

            // 이미 동기 로드로 Current가 세팅된 상태라면 즉시 호출
            if (_dataManager.Current != null)
                HandleDataLoaded(_dataManager.Current);
        }

        // DataManager.OnLoaded에 바인딩할 콜백
        private void HandleDataLoaded(SaveData sd)
        {
            // 저장된 기록들을 서버에 업데이트 (코루틴으로 실행)
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var dispatcher = UnityMainThreadDispatcher.Instance();
                if (dispatcher != null)
                {
                    dispatcher.StartCoroutineOnMainThread(HandleDataLoadedCoroutine(sd));
                }
            });
        }

        private IEnumerator HandleDataLoadedCoroutine(SaveData sd)
        {
            // Steam 닉네임 가져오기
            string displayName = GetPlayerDisplayName();
            
            int maxStage = Mathf.Max(sd.bestClearTimes.Count, sd.bestDeathCounts.Count);
            for (int i = 1; i <= maxStage; i++)
            {
                if (i <= sd.bestClearTimes.Count && sd.bestClearTimes[i-1] > 0)
                {
                    var request = new UpdateRecordRequestDto
                    {
                        StageNumber = i,
                        ClearTime = sd.bestClearTimes[i-1],
                        DeathCount = i <= sd.bestDeathCounts.Count ? sd.bestDeathCounts[i-1] : 0,
                        DisplayName = displayName
                    };
                    yield return UpdateUserRecordCoroutine(request);
                }
            }
        }

        /// <summary>
        /// 최고 기록이 갱신되었을 때 서버에 업데이트
        /// </summary>
        private void OnBestRecordUpdated(int stageNum, float value)
        {
            OnBestRecordUpdated(stageNum, (int)value);
        }

        private void OnBestRecordUpdated(int stageNum, int value)
        {
            var clearTime = _stageManager.GetBestTime(stageNum);
            var deathCount = _stageManager.GetBestDeath(stageNum);

            if (clearTime > 0)
            {
                // Steam 닉네임 가져오기
                string displayName = GetPlayerDisplayName();
                
                var request = new UpdateRecordRequestDto
                {
                    StageNumber = stageNum,
                    ClearTime = clearTime,
                    DeathCount = deathCount,
                    DisplayName = displayName
                };

                // 메인 스레드에서 코루틴으로 업데이트
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    var dispatcher = UnityMainThreadDispatcher.Instance();
                    if (dispatcher != null)
                    {
                        dispatcher.StartCoroutineOnMainThread(UpdateUserRecordCoroutine(request));
                    }
                });
            }
        }

        /// <summary>
        /// 플레이어 표시 이름 가져오기 (Steam 닉네임 우선)
        /// </summary>
        private string GetPlayerDisplayName()
        {
            // Steam 인증 매니저에서 닉네임 가져오기
            if (_steamAuthManager != null && _steamAuthManager.IsSteamInitialized)
            {
                string steamDisplayName = _steamAuthManager.GetSteamDisplayName();
                if (!string.IsNullOrEmpty(steamDisplayName) && steamDisplayName != "Unknown Player")
                {
                    return steamDisplayName;
                }
            }
            
            // PlayerPrefs에서 저장된 닉네임 확인
            string savedDisplayName = PlayerPrefs.GetString("SteamDisplayName", "");
            if (!string.IsNullOrEmpty(savedDisplayName))
            {
                return savedDisplayName;
            }
            
            // 기본값
            return "Player";
        }

        /// <summary>
        /// UI에서 호출: 특정 스테이지의 특정 정렬 기준 Top N 리스트를 반환.
        /// </summary>
        public IReadOnlyList<RankingEntry> GetRanking(int stageNum, RankingSortType sortType)
        {
            Debug.Log($"[RankingManager] GetRanking 호출 - Stage: {stageNum}, SortType: {sortType}");
            
            if (_cachedRankings.TryGetValue(stageNum, out var stageDictionary) &&
                stageDictionary.TryGetValue(sortType, out var response))
            {
                Debug.Log($"[RankingManager] 캐시에서 데이터 반환 - {response.TopEntries.Count}개 항목");
                return response.TopEntries;
            }

            Debug.Log("[RankingManager] 캐시에 없어서 서버에서 로드 시작");
            
            // 캐시에 없으면 서버에서 로드 (코루틴으로 실행)
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var dispatcher = UnityMainThreadDispatcher.Instance();
                if (dispatcher != null)
                {
                    dispatcher.StartCoroutineOnMainThread(LoadRankingCoroutine(stageNum, sortType));
                }
            });

            return new List<RankingEntry>();
        }

        /// <summary>
        /// Top N에 표시할 랭킹과 내 랭킹을 분리해서 반환
        /// </summary>
        public (IReadOnlyList<RankingEntry> topEntries, RankingEntry myEntry) GetRankingWithMyEntry(int stageNum, RankingSortType sortType, int maxTopEntries = 20)
        {
            Debug.Log($"[RankingManager] GetRankingWithMyEntry 호출 - Stage: {stageNum}, SortType: {sortType}");
            
            if (_cachedRankings.TryGetValue(stageNum, out var stageDictionary) &&
                stageDictionary.TryGetValue(sortType, out var response))
            {
                Debug.Log($"[RankingManager] 캐시에서 데이터 반환 - TopEntries: {response.TopEntries.Count}, MyEntry: {(response.MyEntry != null ? "있음" : "없음")}");
                return (response.TopEntries, response.MyEntry);
            }

            Debug.Log("[RankingManager] 캐시에 없어서 서버에서 로드 시작");
            
            // 캐시에 없으면 서버에서 로드 (코루틴으로 실행)
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var dispatcher = UnityMainThreadDispatcher.Instance();
                if (dispatcher != null)
                {
                    dispatcher.StartCoroutineOnMainThread(LoadRankingCoroutine(stageNum, sortType));
                }
            });

            return (new List<RankingEntry>(), null);
        }

        /// <summary>
        /// 서버에서 랭킹 데이터 로드 (코루틴 버전)
        /// </summary>
        private System.Collections.IEnumerator LoadRankingCoroutine(int stageNum, RankingSortType sortType)
        {
            var url = $"{_baseUrl}?stageNumber={stageNum}&sortType={sortType}&page=1&pageSize=20&userId={_userId}";
            Debug.Log($"[RankingManager] 서버 요청 시작 - URL: {url}");
            Debug.Log($"[RankingManager] 현재 UserId: {_userId}");
            
            using var request = UnityWebRequest.Get(url);
            
            // JWT 토큰 헤더 추가
            AddAuthorizationHeader(request);
            
            yield return request.SendWebRequest();

            Debug.Log($"[RankingManager] 서버 응답 - Result: {request.result}, ResponseCode: {request.responseCode}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var json = request.downloadHandler.text;
                    Debug.Log($"[RankingManager] 서버 응답 JSON: {json}");
                    
                    var response = JsonConvert.DeserializeObject<RankingResponseDto>(json);

                    if (response != null)
                    {
                        Debug.Log($"[RankingManager] 파싱 성공 - TopEntries: {response.TopEntries.Count}, MyEntry: {(response.MyEntry != null ? "있음" : "없음")}");
                        
                        // TopEntries의 각 항목 상세 정보 로그
                        for (int i = 0; i < response.TopEntries.Count; i++)
                        {
                            var entry = response.TopEntries[i];
                            Debug.Log($"[RankingManager] TopEntry[{i}]: Rank={entry.Rank}, UserId={entry.UserId}, DisplayName={entry.DisplayName}, IsMyRecord={entry.IsMyRecord}");
                        }
                        
                        // MyEntry 정보 로그
                        if (response.MyEntry != null)
                        {
                            Debug.Log($"[RankingManager] MyEntry: Rank={response.MyEntry.Rank}, UserId={response.MyEntry.UserId}, DisplayName={response.MyEntry.DisplayName}, IsMyRecord={response.MyEntry.IsMyRecord}");
                        }
                        
                        // 캐시에 저장
                        if (!_cachedRankings.TryGetValue(stageNum, out var stageDictionary))
                        {
                            stageDictionary = new Dictionary<RankingSortType, RankingResponseDto>();
                            _cachedRankings[stageNum] = stageDictionary;
                        }

                        stageDictionary[sortType] = response;

                        // UI 구독자에게 알림
                        Debug.Log($"[RankingManager] OnRankingUpdated 이벤트 발생 - Stage: {stageNum}, SortType: {sortType}");
                        OnRankingUpdated?.Invoke(stageNum, sortType);

                        Debug.Log($"[RankingManager] 랭킹 로드 성공: Stage {stageNum}, SortType {sortType}, Count {response.TopEntries.Count}");
                    }
                    else
                    {
                        Debug.LogError("[RankingManager] 파싱된 응답이 null입니다.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RankingManager] 랭킹 데이터 파싱 실패: {ex.Message}");
                    Debug.LogError($"[RankingManager] 원본 JSON: {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"[RankingManager] 랭킹 로드 실패: {request.error}");
                Debug.LogError($"[RankingManager] 응답 내용: {request.downloadHandler.text}");
            }
        }

        /// <summary>
        /// 서버에 사용자 기록 업데이트 (코루틴 버전)
        /// </summary>
        private IEnumerator UpdateUserRecordCoroutine(UpdateRecordRequestDto requestDto)
        {
            var url = $"{_baseUrl}/{_userId}/record";
            var json = JsonConvert.SerializeObject(requestDto);
            
            using var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // JWT 토큰 헤더 추가
            AddAuthorizationHeader(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[RankingManager] 기록 업데이트 성공: Stage {requestDto.StageNumber}");
                
                // 해당 스테이지의 캐시 무효화하여 다음 조회 시 최신 데이터 로드
                if (_cachedRankings.ContainsKey(requestDto.StageNumber))
                {
                    _cachedRankings[requestDto.StageNumber].Clear();
                }
            }
            else
            {
                Debug.LogError($"[RankingManager] 기록 업데이트 실패: {request.error}");
            }
        }

        /// <summary>
        /// 특정 스테이지의 캐시 무효화 (UI에서 새로고침할 때 사용)
        /// </summary>
        public void InvalidateCache(int stageNum)
        {
            if (_cachedRankings.ContainsKey(stageNum))
            {
                _cachedRankings[stageNum].Clear();
            }
        }

        /// <summary>
        /// 모든 캐시 무효화
        /// </summary>
        public void InvalidateAllCache()
        {
            _cachedRankings.Clear();
        }

        // 메모리 누수 방지 (Zenject 싱글톤용 IDisposable 구현)
        public void Dispose()
        {
            // StageManager 이벤트 해제
            if (_stageManager != null)
            {
                _stageManager.OnBestTimeUpdated -= OnBestRecordUpdated;
                _stageManager.OnBestDeathUpdated -= OnBestRecordUpdated;
            }
            
            // DataManager 이벤트 해제
            if (_dataManager != null)
                _dataManager.OnLoaded -= HandleDataLoaded;
            
            // Dictionary 정리
            if (_cachedRankings != null)
            {
                _cachedRankings.Clear();
                _cachedRankings = null;
            }
            
            // 외부 구독자들이 남아있을 수 있으니 이벤트 초기화
            OnRankingUpdated = null;
        }
    }
}
