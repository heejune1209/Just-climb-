using JustClimb.Data;
using JustClimb.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace JustClimb.Manager
{
    /// <summary>
    /// 네트워크 상태를 감지하여 DataSyncManager의 동기화 기능을 제어하고,
    /// UI_SyncStatus에 현재 싱크 상태를 전달하는 매니저.
    /// </summary>
    public class OfflineCacheManager : MonoBehaviour, IInitializable
    {
        [Inject] private IDataSyncManager _dataSyncManager;  // DataSyncManager 제어용 DI 주입
        [Inject] private IDataManager _dataManager;

        private UI_SyncStatus _uiSyncStatus;  // UI 컴포넌트 레퍼런스

        private bool _isOnline;
        private readonly List<DeltaEvent> _offlineQueue = new();


        [Header("Offline Cache Settings")]
        [Tooltip("네트워크 체크 주기 (초)")]
        [SerializeField] private float _checkInterval = 2f;  // 기능 추가: 네트워크 상태 폴링 주기

        /// <summary>
        /// Zenject IInitializable 구현 — 매니저 초기화 시 호출됩니다.
        /// </summary>
        public void Initialize()
        {
            // 기능 추가: UI_SyncStatus 컴포넌트 찾기
            _uiSyncStatus = FindObjectOfType<UI_SyncStatus>();
            if (_uiSyncStatus == null)
                Debug.LogWarning("[OfflineCacheManager] UI_SyncStatus 컴포넌트를 찾을 수 없습니다.");

            // 2) DataManager 델타 생성 구독
            _dataManager.OnDeltaGenerated += OnDeltaGenerated;

            // 기능 추가: 씬 전환 시에도 유지
            //DontDestroyOnLoad(this.gameObject);

            // 기능 추가: 초기 네트워크 상태에 따른 동기화 제어
            _isOnline = Application.internetReachability != NetworkReachability.NotReachable;
            SetSyncState(_isOnline);

            // 기능 추가: 주기적 네트워크 상태 확인 시작
            StartCoroutine(NetworkCheckLoop());
        }

        /// <summary>
        /// 기능 추가: 일정 간격으로 네트워크 상태를 확인하고 변동 시 Sync 상태를 토글합니다.
        /// </summary>
        private IEnumerator NetworkCheckLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_checkInterval);

                bool current = Application.internetReachability != NetworkReachability.NotReachable;
                if (current != _isOnline)
                {
                    _isOnline = current;
                    SetSyncState(_isOnline);
                }
            }
        }
        /// <summary>
        /// DataManager에서 델타가 생성될 때마다 호출됩니다.
        /// 오프라인 상태면 로컬 큐에 보관만 하고, 온라인 상태면 DataSyncManager가 이미 처리하므로 무시.
        /// </summary>
        private void OnDeltaGenerated(DeltaEvent delta)
        {
            if (!_isOnline)
            {
                _offlineQueue.Add(delta);
                Debug.Log($"[OfflineCacheManager] Δ 캐싱됨: {delta.Key}");
            }
        }

        /// <summary>
        /// 네트워크 상태에 따라 동기화 활성화/비활성화,
        /// 온라인 복귀 시 캐시된 델타 일괄 전송,
        /// UI 업데이트를 수행합니다.
        /// </summary>
        private void SetSyncState(bool online)
        {
            // DataSyncManager On/Off
            // DataSyncManager 일시중지/재개
            if (online) _dataSyncManager.ResumeSync();
            else _dataSyncManager.PauseSync();

            // UI 표시
            _uiSyncStatus?.SetSyncStatus(online);

            if (online && _offlineQueue.Count > 0)
            {
                // 오프라인 중에 쌓인 델타 전송
                Debug.Log($"[OfflineCacheManager] 온라인 복귀! {_offlineQueue.Count}건 전송 시작");
                foreach (var d in _offlineQueue)
                {
                    _dataSyncManager.EnqueueDelta(d);
                }
                _offlineQueue.Clear();
            }

            Debug.Log($"[OfflineCacheManager] 네트워크 상태: {(online ? "Online" : "Offline")}");
        }

        // 메모리 누수 방지
        private void OnDestroy()
        {
            // 코루틴 정리
            StopAllCoroutines();
            
            // 이벤트 해제
            if (_dataManager != null)
                _dataManager.OnDeltaGenerated -= OnDeltaGenerated;
            
            // 큐 정리
            _offlineQueue?.Clear();
            
            // 컴포넌트 참조 해제
            _uiSyncStatus = null;
            
            // 매니저 참조 해제
            _dataSyncManager = null;
            _dataManager = null;
        }
    }
}
