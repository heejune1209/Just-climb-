using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using JustClimb.Data;

namespace JustClimb.Manager
{
    /// <summary>
    /// DataManager에서 발생한 델타 이벤트를 모아서
    /// 주기적으로 /api/users/{uid}/state/delta 로 전송.
    /// 실패 시 재큐잉, 백그라운드 진입/앱 종료 시 즉시 Flush().
    /// </summary>
    public class DataSyncManager : MonoBehaviour
    {
        [Header("Sync Settings")]
        [Tooltip("Δ를 서버로 전송할 주기(초)")]
        [SerializeField] private float _syncInterval = 5f;

        [Tooltip("API 엔드포인트 형식. {0}에 userId가 들어갑니다.")]
        [SerializeField]
        private string _endpointFormat = "https://your.server.com/api/users/{0}/state/delta";

        [Header("User Settings")]
        [Tooltip("동기화 대상 유저 ID")]
        [SerializeField] private string _userId;

        // 전송 대기 중인 Δ 큐
        // readonly는 런타임 시 (runtime)에 초기화 가능
        // 선언부에서 생성자에서 둘 중 하나에 할당
        private readonly List<DeltaEvent> _deltaQueue = new();

        void Awake()
        {
            // 파괴 방지
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            // 델타 발생 시 콜백 등록
            Managers.Instance.Data.OnDeltaGenerated += EnqueueDelta;

            // 주기적 Sync 시작
            StartCoroutine(SyncLoop());
        }

        void OnDisable()
        {
            Managers.Instance.Data.OnDeltaGenerated -= EnqueueDelta;
            StopAllCoroutines();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                StartCoroutine(Flush());
        }

        void OnApplicationQuit()
        {
            // 앱 종료 직전 마지막 Flush
            StartCoroutine(Flush());
        }

        private void EnqueueDelta(DeltaEvent d)
        {
            lock (_deltaQueue)
            {
                _deltaQueue.Add(d);
            }
            Debug.Log($"[DataSyncManager] Δ 큐잉 → {d}");
        }

        private IEnumerator SyncLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_syncInterval);
                yield return Flush();
            }
        }

        /// <summary>
        /// 큐에 모인 델타를 모두 서버에 전송.
        /// 실패 시 다시 앞쪽에 재큐잉.
        /// </summary>
        private IEnumerator Flush()
        {
            List<DeltaEvent> batch;
            lock (_deltaQueue)
            {
                if (_deltaQueue.Count == 0)
                    yield break;
                batch = new List<DeltaEvent>(_deltaQueue);
                _deltaQueue.Clear();
            }

            // JSON 직렬화용 래퍼
            var wrapper = new DeltaWrapper { deltas = batch };
            string body = JsonUtility.ToJson(wrapper);

            string url = string.Format(_endpointFormat, _userId);
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
                // 실패한 델타 다시 재큐잉
                lock (_deltaQueue)
                {
                    _deltaQueue.InsertRange(0, batch);
                }
            }
        }

        // 델타 배열을 JsonUtility로 직렬화하기 위한 래퍼 클래스
        [System.Serializable]
        private class DeltaWrapper
        {
            public List<DeltaEvent> deltas;
        }
    }
}
