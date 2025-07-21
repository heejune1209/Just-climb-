using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using JustClimb.Data;
using JustClimb.Utils;
using Zenject;
using System;

namespace JustClimb.Manager
{
    /// <summary>
    /// 통합 네트워크 통신 매니저
    /// 모든 서버 통신을 담당하며 델타 이벤트 큐잉 및 범용 HTTP 통신을 지원합니다.
    /// </summary>
    public class DataSyncManager : MonoBehaviour, IDataSyncManager, IInitializable
    {
        [Header("Sync Settings")]
        [Tooltip("Δ를 서버로 전송할 주기(초)")]
        [SerializeField] private float _syncInterval = 5f;

        // 서버 설정
        private ServerConfig _serverConfig;
        private string _endpointFormat;
        private string _userId;
        private SteamAuthManager _steamAuthManager;  // Steam 인증 매니저

        // 전송 대기 중인 Δ 큐
        private readonly Queue<DeltaEvent> _queue = new();

        // Zenject 의존성 주입
        [Inject]
        public void Construct([Inject(Id="UserId")] string userId, SteamAuthManager steamAuthManager)
        {
            _userId = userId;
            _steamAuthManager = steamAuthManager;
            Debug.Log($"[DataSyncManager] UserId 주입: {_userId}");
        }

        /// <summary>
        /// Zenject에서 자동으로 호출됨.
        /// </summary>
        public void Initialize()
        {
            // ✅ ConfigHelper 사용 (중복 제거)
            _serverConfig = ConfigHelper.GetServerConfig();
            _endpointFormat = ConfigHelper.GetDeltaApiUrlFormat();

            Debug.Log($"[DataSyncManager] 초기화 완료 - 엔드포인트: {_endpointFormat}, UserId: {_userId}");

            // Zenject에서 생성된 GameObject를 루트로 이동
            if (transform.parent != null)
            {
                Debug.Log($"[DataSyncManager] GameObject를 루트로 이동 (기존 부모: {transform.parent.name})");
                transform.SetParent(null);
            }
            
            // DontDestroyOnLoad 적용
            DontDestroyOnLoad(this.gameObject);

            StartCoroutine(SyncLoop());
        }

        #region Delta Queue Management (기존 기능 유지)
        
        /// <summary>
        /// DataManager에서 델타가 생성될 때마다 호출되어,
        /// 내부 큐에 쌓인 델타를 서버로 전송하도록 트리거.
        /// </summary>
        public void EnqueueDelta(DeltaEvent d)
        {
            lock (_queue)
            {
                _queue.Enqueue(d); // 델타 직접 큐잉
            }
            Debug.Log($"[DataSyncManager] Δ 큐잉 → {d}");
        }

        // 주기적으로 Flush() 호출
        private IEnumerator SyncLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_syncInterval);
                yield return Flush();
            }
        }

        // IDataSyncManager.PauseSync 구현
        public void PauseSync()
        {
            this.enabled = false;
            Debug.Log("[DataSyncManager] Sync paused.");
        }

        // IDataSyncManager.ResumeSync 구현
        public void ResumeSync()
        {
            this.enabled = true;
            Debug.Log("[DataSyncManager] Sync resumed.");
        }

        /// <summary>
        /// 큐에 모인 델타를 모두 서버에 전송.
        /// 실패 시 다시 앞쪽에 재큐잉.
        /// </summary>
        private IEnumerator Flush()
        {
            List<DeltaEvent> batch;
            lock (_queue)
            {
                if (_queue.Count == 0)
                    yield break;
                batch = new List<DeltaEvent>(_queue);
                _queue.Clear();
            }

            // JSON 직렬화용 래퍼
            var wrapper = new DeltaWrapper { Deltas = batch };
            string body = JsonHelper.SerializeObject(wrapper);

            string url = string.Format(_endpointFormat, _userId);
            Debug.Log($"[DataSyncManager] 델타 전송 시도: {url}");
            
            // ✅ NetworkHelper 사용 (통합된 네트워크 처리)
            using var request = NetworkHelper.CreatePostRequest(url, wrapper, _steamAuthManager);
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[DataSyncManager] 델타 전송 성공 ({batch.Count}건)");
            }
            else
            {
                Debug.LogError($"[DataSyncManager] 델타 전송 실패: {request.error}");
                Debug.LogError($"[DataSyncManager] 응답 코드: {request.responseCode}");
                Debug.LogError($"[DataSyncManager] 응답 내용: {request.downloadHandler.text}");
                // 실패한 델타 다시 재큐잉
                lock (_queue)
                {
                    for (int i = batch.Count - 1; i >= 0; i--)
                        _queue.Enqueue(batch[i]);
                }
            }
        }

        /// <summary>
        /// 앱 종료 직전에 동기식으로 델타를 전부 서버에 전송.
        /// </summary>
        public void FlushNow()
        {
            List<DeltaEvent> batch;
            lock (_queue)
            {
                if (_queue.Count == 0) return;
                batch = new List<DeltaEvent>(_queue);
                _queue.Clear();
            }

            var wrapper = new DeltaWrapper { Deltas = batch };
            var url = string.Format(_endpointFormat, _userId);
            Debug.Log($"[DataSyncManager] FlushNow 시도: {url}");
            
            // ✅ NetworkHelper 사용
            using var req = NetworkHelper.CreatePostRequest(url, wrapper, _steamAuthManager);

            // SendWebRequest를 블록킹 호출
            var op = req.SendWebRequest();
            while (!op.isDone) { /* busy-wait */ }

            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log("[DataSyncManager] FlushNow 성공");
            else
            {
                Debug.LogError($"[DataSyncManager] FlushNow 실패: {req.error}");
                Debug.LogError($"[DataSyncManager] FlushNow 응답 코드: {req.responseCode}");
                Debug.LogError($"[DataSyncManager] FlushNow 응답 내용: {req.downloadHandler.text}");
            }
        }

        #endregion

        #region 범용 HTTP 통신 API (새로 추가)

        /// <summary>
        /// 범용 GET 요청 (코루틴)
        /// </summary>
        public IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError, T defaultValue = default)
        {
            Debug.Log($"[DataSyncManager] GET 요청: {url}");
            
            using var request = NetworkHelper.CreateGetRequest(url, _steamAuthManager);
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var result = NetworkHelper.ParseResponse<T>(request, defaultValue);
                onSuccess?.Invoke(result);
                Debug.Log($"[DataSyncManager] GET 성공: {url}");
            }
            else
            {
                string error = $"GET 요청 실패: {request.error} (Code: {request.responseCode})";
                Debug.LogError($"[DataSyncManager] {error}");
                onError?.Invoke(error);
            }
        }

        /// <summary>
        /// 범용 POST 요청 (코루틴)
        /// </summary>
        public IEnumerator PostRequest<T>(string url, object data, Action<T> onSuccess, Action<string> onError, T defaultValue = default)
        {
            Debug.Log($"[DataSyncManager] POST 요청: {url}");
            
            using var request = NetworkHelper.CreatePostRequest(url, data, _steamAuthManager);
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var result = NetworkHelper.ParseResponse<T>(request, defaultValue);
                onSuccess?.Invoke(result);
                Debug.Log($"[DataSyncManager] POST 성공: {url}");
            }
            else
            {
                string error = $"POST 요청 실패: {request.error} (Code: {request.responseCode})";
                Debug.LogError($"[DataSyncManager] {error}");
                onError?.Invoke(error);
            }
        }

        /// <summary>
        /// 범용 PUT 요청 (코루틴)
        /// </summary>
        public IEnumerator PutRequest<T>(string url, object data, Action<T> onSuccess, Action<string> onError, T defaultValue = default)
        {
            Debug.Log($"[DataSyncManager] PUT 요청: {url}");
            
            string json = JsonHelper.SerializeObject(data);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "PUT")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };
            
            request.SetRequestHeader("Content-Type", "application/json");
            NetworkHelper.AddAuthorizationHeader(request, _steamAuthManager);
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var result = NetworkHelper.ParseResponse<T>(request, defaultValue);
                onSuccess?.Invoke(result);
                Debug.Log($"[DataSyncManager] PUT 성공: {url}");
            }
            else
            {
                string error = $"PUT 요청 실패: {request.error} (Code: {request.responseCode})";
                Debug.LogError($"[DataSyncManager] {error}");
                onError?.Invoke(error);
            }
        }

        /// <summary>
        /// 즉시 실행 가능한 코루틴 시작 헬퍼
        /// </summary>
        public Coroutine StartNetworkCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }

        #endregion

        // 델타 배열을 JsonUtility로 직렬화하기 위한 래퍼 클래스
        [System.Serializable]
        private class DeltaWrapper
        {
            public List<DeltaEvent> Deltas;  // 서버의 SaveRequest.Deltas와 일치하도록 대문자로 변경
        }

        // 메모리 누수 방지
        private void OnDestroy()
        {
            // 코루틴 정리
            StopAllCoroutines();
            
            // 마지막 플러시 시도
            try
            {
                FlushNow();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DataSyncManager] OnDestroy FlushNow 실패: {ex.Message}");
            }
            
            // 큐 정리
            lock (_queue)
            {
                _queue.Clear();
            }
            
            // 참조 해제
            _serverConfig = null;
            _endpointFormat = null;
            _userId = null;
            _steamAuthManager = null;
        }
    }
}
