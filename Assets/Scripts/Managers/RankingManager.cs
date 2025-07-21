using JustClimb.Data;
using JustClimb.Manager;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;
using Newtonsoft.Json;
using System.Collections;
using System.Text;
using JustClimb.Utils;

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

        // 🔧 중복 요청 방지용 (현재 처리 중인 스테이지들)
        private HashSet<int> _pendingUpdates = new HashSet<int>();

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
                _baseUrl = "https://localhost:5259/api/ranking";
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
            // 중복 방지: BestTimeUpdated만 구독 (하나의 기록 업데이트로 통합)
            _stageManager.OnBestTimeUpdated += OnBestRecordUpdated;

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
            // 최적화: 초기 데이터 로드 시에는 서버 업데이트 건너뛰기
            // (실시간 기록 갱신은 OnBestRecordUpdated로 처리)
            Debug.Log("[RankingManager] 초기 데이터 로드 완료. 실시간 기록 갱신 대기 중...");
            yield break;
            
            /* 기존 코드 주석 처리 - 불필요한 중복 요청 방지
            // Steam 닉네임 가져오기
            string displayName = GetPlayerDisplayName();
            
            // 게임에 실제 존재하는 스테이지 개수로 제한 (10개)
            const int MAX_GAME_STAGES = 10;
            int maxStage = Mathf.Min(Mathf.Max(sd.bestClearTimes.Count, sd.bestDeathCounts.Count), MAX_GAME_STAGES);
            Debug.Log($"[RankingManager] 처리할 스테이지 범위: 1 ~ {maxStage}");
            
            for (int i = 1; i <= maxStage; i++)
            {
                // 유효한 클리어 기록만 서버에 업데이트 (MaxValue 제외)
                if (i <= sd.bestClearTimes.Count && 
                    sd.bestClearTimes[i-1] > 0 && 
                    sd.bestClearTimes[i-1] < float.MaxValue)
                {
                    int deathCount = (i <= sd.bestDeathCounts.Count) ? sd.bestDeathCounts[i-1] : 0;
                    
                    // 사망 횟수도 MaxValue 체크
                    if (deathCount >= int.MaxValue)
                    {
                        deathCount = 0; // 기본값으로 설정
                    }
                    
                    var request = new UpdateRecordRequestDto
                    {
                        StageNumber = i,
                        ClearTime = sd.bestClearTimes[i-1],
                        DeathCount = deathCount,
                        DisplayName = displayName
                    };
                    
                    Debug.Log($"[RankingManager] 유효한 기록 업데이트: Stage {i}, Time={sd.bestClearTimes[i-1]:F2}s, Deaths={deathCount}");
                    yield return UpdateUserRecordCoroutine(request);
                }
                else if (i <= sd.bestClearTimes.Count)
                {
                    Debug.Log($"[RankingManager] 무효한 기록 건너뛰기: Stage {i}, Time={sd.bestClearTimes[i-1]}");
                }
            }
            */
        }

        /// <summary>
        /// 최고 기록이 갱신되었을 때 서버에 업데이트 (통합된 단일 메서드)
        /// </summary>
        private void OnBestRecordUpdated(int stageNum, float clearTime)
        {
            var deathCount = _stageManager.GetBestDeath(stageNum);

            // 🔧 중복 방지: 짧은 시간 내 동일 스테이지 요청 건너뛰기
            if (_pendingUpdates.Contains(stageNum))
            {
                Debug.Log($"[RankingManager] 이미 처리 중인 스테이지 건너뛰기: Stage {stageNum}");
                return;
            }

            // 유효한 기록만 서버에 업데이트 (MaxValue 제외)
            if (clearTime > 0 && clearTime < float.MaxValue)
            {
                // 사망 횟수도 MaxValue 체크
                if (deathCount < 0 || deathCount >= int.MaxValue)
                {
                    deathCount = 0; // 기본값으로 설정
                }
                
                // Steam 닉네임 가져오기
                string displayName = GetPlayerDisplayName();
                
                var request = new UpdateRecordRequestDto
                {
                    StageNumber = stageNum,
                    ClearTime = clearTime,
                    DeathCount = deathCount,
                    DisplayName = displayName
                };

                Debug.Log($"[RankingManager] 기록 업데이트 요청: Stage {stageNum}, Time={clearTime:F2}s, Deaths={deathCount}");

                // 🔧 중복 방지: 처리 중 상태로 마킹
                _pendingUpdates.Add(stageNum);

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
            else
            {
                Debug.Log($"[RankingManager] 무효한 기록으로 업데이트 건너뛰기: Stage {stageNum}, Time={clearTime}");
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
        /// 서버에서 랭킹 데이터 로드 (✅ DataManager 캡슐화 API 사용)
        /// </summary>
        private IEnumerator LoadRankingCoroutine(int stageNum, RankingSortType sortType)
        {
            // enum을 int로 변환해서 전송 (서버는 int 값을 기대함)
            int sortTypeInt = (int)sortType;
            
            Debug.Log($"[RankingManager] 서버 요청 시작 - Stage: {stageNum}, SortType: {sortType}({sortTypeInt})");
            Debug.Log($"[RankingManager] 현재 UserId: {_userId}");
            
            bool requestCompleted = false;
            
            // DataManager의 캡슐화된 랭킹 API 사용
            _dataManager.GetRanking<RankingResponseDto>(
                stageNum, sortTypeInt, 1, 20,
                onSuccess: (response) => {
                    ProcessRankingResponse(response, stageNum, sortType);
                    requestCompleted = true;
                },
                onError: (error) => {
                    Debug.LogError($"[RankingManager] 랭킹 로드 실패: {error}");
                    requestCompleted = true;
                },
                defaultValue: new RankingResponseDto()
            );
            
            // 요청 완료 대기
            yield return new WaitUntil(() => requestCompleted);
        }

        /// <summary>
        /// 랭킹 응답 데이터 처리 헬퍼 메서드
        /// </summary>
        private void ProcessRankingResponse(RankingResponseDto response, int stageNum, RankingSortType sortType)
        {
            if (response == null)
            {
                Debug.LogError("[RankingManager] 응답이 null입니다.");
                return;
            }

            // MaxValue 데이터 필터링 (초기화 값은 제외)
            if (response.TopEntries != null)
            {
                for (int i = response.TopEntries.Count - 1; i >= 0; i--)
                {
                    var entry = response.TopEntries[i];
                    if (entry.ClearTime >= float.MaxValue || entry.DeathCount >= int.MaxValue)
                    {
                        Debug.Log($"[RankingManager] MaxValue 데이터 제거: Rank={entry.Rank}, ClearTime={entry.ClearTime}, DeathCount={entry.DeathCount}");
                        response.TopEntries.RemoveAt(i);
                    }
                }
                
                // 순위 재정렬 (제거 후)
                for (int i = 0; i < response.TopEntries.Count; i++)
                {
                    response.TopEntries[i].Rank = i + 1;
                }
            }
            
            // MyEntry도 MaxValue 체크
            if (response.MyEntry != null && 
                (response.MyEntry.ClearTime >= float.MaxValue || response.MyEntry.DeathCount >= int.MaxValue))
            {
                Debug.Log($"[RankingManager] MyEntry MaxValue 데이터 제거");
                response.MyEntry = null;
            }
            
            Debug.Log($"[RankingManager] 파싱 성공 - TopEntries: {response.TopEntries?.Count ?? 0}, MyEntry: {(response.MyEntry != null ? "있음" : "없음")}");
            
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

            Debug.Log($"[RankingManager] 랭킹 로드 성공: Stage {stageNum}, SortType {sortType}, Count {response.TopEntries?.Count ?? 0}");
        }

        /// <summary>
        /// 서버에 사용자 기록 업데이트 (✅ DataManager 캡슐화 API 사용)
        /// </summary>
        private IEnumerator UpdateUserRecordCoroutine(UpdateRecordRequestDto requestDto)
        {
            // 데이터 유효성 검사
            if (requestDto.ClearTime <= 0 || requestDto.ClearTime >= float.MaxValue)
            {
                Debug.LogError($"[RankingManager] 무효한 클리어 타임으로 업데이트 건너뛰기: {requestDto.ClearTime}");
                _pendingUpdates.Remove(requestDto.StageNumber);
                yield break;
            }

            if (requestDto.DeathCount < 0 || requestDto.DeathCount >= int.MaxValue)
            {
                Debug.LogWarning($"[RankingManager] 무효한 데스카운트를 0으로 수정: {requestDto.DeathCount} -> 0");
                requestDto.DeathCount = 0;
            }

            if (string.IsNullOrEmpty(requestDto.DisplayName))
            {
                requestDto.DisplayName = "Player";
            }

            Debug.Log($"[RankingManager] 기록 업데이트 요청 - Stage: {requestDto.StageNumber}, Time: {requestDto.ClearTime}, Deaths: {requestDto.DeathCount}");
            Debug.Log($"[RankingManager] UserId: {_userId}");
            
            bool requestCompleted = false;
            
            // ✅ DataManager의 캡슐화된 기록 업데이트 API 사용
            _dataManager.UpdateUserRecord<object>(
                requestDto,
                onSuccess: (response) => {
                    Debug.Log($"[RankingManager] 기록 업데이트 성공: Stage {requestDto.StageNumber}");
                    
                    // 해당 스테이지의 캐시 무효화하여 다음 조회 시 최신 데이터 로드
                    if (_cachedRankings.ContainsKey(requestDto.StageNumber))
                    {
                        _cachedRankings[requestDto.StageNumber].Clear();
                    }
                    
                    requestCompleted = true;
                },
                onError: (error) => {
                    Debug.LogError($"[RankingManager] 기록 업데이트 실패: {error}");
                    requestCompleted = true;
                },
                defaultValue: new { }
            );
            
            // 요청 완료 대기
            yield return new WaitUntil(() => requestCompleted);
            
            // 🔧 중복 방지: 처리 완료 후 대기 목록에서 제거
            _pendingUpdates.Remove(requestDto.StageNumber);
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
