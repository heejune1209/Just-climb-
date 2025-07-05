using JustClimb.Data;
using System;
using UnityEngine;
using Zenject;

namespace JustClimb.Manager
{
    /// <summary>
    /// 애플리케이션 종료(또는 백그라운드 진입) 시에
    /// 로컬 저장 + 델타(full dump) 발생 + 델타 전송을 강제 수행.
    /// </summary>
    public class SaveManager : MonoBehaviour, IInitializable, IDisposable
    {
        [Inject] private IDataManager _dataManager;
        [Inject] private IDataSyncManager _syncManager;

        public void Initialize()
        {
            //DontDestroyOnLoad(this.gameObject);
            Application.quitting += OnQuit;
            Application.focusChanged += OnFocusChanged;
        }

        /// <summary>
        /// 에디터 Play 중단 시나 실제 빌드 종료 시 호출.
        /// </summary>
        private void OnQuit()
        {
            QuickSaveAndSync();
        }

        /// <summary>
        /// 모바일 등에서 백그라운드 진입할 때도 저장하고 싶으면 이 콜백을 사용.
        /// </summary>
        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                QuickSaveAndSync();
        }

        private void QuickSaveAndSync()
        {
            // 풀 덤프 + 로컬 저장
            _dataManager.Save();

            // 데이터 전송 일시 중지 해제(온라인이면 바로 풀 플러시)
            _syncManager.ResumeSync();

            // 즉시 플러시. 인터페이스에 FlushImmediate() 가 없다면
            // DataSyncManager 내부에 public Flush() 메서드를 추가.
            if (_syncManager is DataSyncManager dsm)
                dsm.FlushNow();  
        }

        public void Dispose()
        {
            Application.quitting -= OnQuit;
            Application.focusChanged -= OnFocusChanged;
        }

        // 메모리 누수 방지
        private void OnDestroy()
        {
            // Dispose 호출
            Dispose();
            
            // 매니저 참조 해제
            _dataManager = null;
            _syncManager = null;
        }
    }
}
