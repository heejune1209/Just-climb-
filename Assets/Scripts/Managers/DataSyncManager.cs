using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using JustClimb.Data;
using Zenject;

namespace JustClimb.Manager
{
    /// <summary>
    /// 델타 이벤트를 큐잉하고, 주기적으로 서버에 POST 전송합니다.
    /// 실패 시 재큐잉, 앱 백그라운드 진입/종료 시 Flush() 호출
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

        // 전송 대기 중인 Δ 큐
        // readonly는 런타임 시 (runtime)에 초기화 가능
        // 선언부에서 생성자에서 둘 중 하나에 할당
        private readonly Queue<DeltaEvent> _queue = new();

        // Zenject 의존성 주입
        [Inject]
        public void Construct([Inject(Id="UserId")] string userId)
        {
            _userId = userId;
            Debug.Log($"[DataSyncManager] UserId 주입: {_userId}");
        }

        /// <summary>
        /// Zenject에서 자동으로 호출됨.
        /// </summary>
        public void Initialize()
        {
            // 서버 설정 로드
            _serverConfig = Resources.Load<ServerConfig>("ServerConfig");
            if (_serverConfig == null)
            {
                Debug.LogError("[DataSyncManager] ServerConfig를 찾을 수 없습니다! Resources/ServerConfig.asset을 생성하세요.");
                _endpointFormat = "https://localhost:7091/api/users/{0}/state/delta";  // 기본값
            }
            else
            {
                _endpointFormat = _serverConfig.GetDeltaApiUrlFormat();
            }

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
            string body = JsonUtility.ToJson(wrapper);

            string url = string.Format(_endpointFormat, _userId);
            Debug.Log($"[DataSyncManager] 델타 전송 시도: {url}");
            Debug.Log($"[DataSyncManager] 전송 데이터: {body}");
            
            using var www = new UnityWebRequest(url, "POST");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
            www.uploadHandler = new UploadHandlerRaw(bytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[DataSyncManager] 델타 전송 성공 ({batch.Count}건)");
            }
            else
            {
                Debug.LogError($"[DataSyncManager] 델타 전송 실패: {www.error}");
                Debug.LogError($"[DataSyncManager] 응답 코드: {www.responseCode}");
                Debug.LogError($"[DataSyncManager] 응답 내용: {www.downloadHandler.text}");
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
        /// coroutine이 아니므로 Application.quitting 안에서도 작동.
        /// </summary>
        public void FlushNow()
        {
            // 1) 큐에서 가져온 델타를 리스트로 복사
            List<DeltaEvent> batch;
            lock (_queue)
            {
                if (_queue.Count == 0) return;
                batch = new List<DeltaEvent>(_queue);
                _queue.Clear();
            }

            // 2) JSON으로 직렬화
            var wrapper = new DeltaWrapper { Deltas = batch };
            string body = JsonUtility.ToJson(wrapper);

            // 3) UnityWebRequest를 동기 모드로 발송
            var url = string.Format(_endpointFormat, _userId);
            Debug.Log($"[DataSyncManager] FlushNow 시도: {url}");
            
            using var req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");

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
        }
    }
}
