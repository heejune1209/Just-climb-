using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JustClimb.UI
{
    /// <summary>
    /// 싱크 상태를 화면에 표시하는 컴포넌트입니다.
    /// OfflineCacheManager에서 네트워크 상태 변경 시 SetSyncStatus를 호출하여 UI를 갱신합니다.
    /// </summary>
    // 네트워크·동기화가 실제로 일어나는 화면(또는 패널)에 붙여 두는 게 자연스럽습니다.
    // 예를 들어 랭킹 화면을 띄우는 RankingUI 패널 안쪽에 UI_SyncStatus 컴포넌트를 넣고,
    // 패널이 활성화될 때 OfflineCacheManager 가 이미 초기화된 상태라면 바로 현재 온라인/오프라인 상태를 보여 주고,
    // 사용자가 랭킹을 보는 동안 주기적으로 갱신된 상태를 아이콘·텍스트로 확인할 수 있게 됩니다.
    public class UI_SyncStatus : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private Image _statusIcon;           // 기능 추가: 동기화 상태 아이콘

        [SerializeField]
        private TextMeshProUGUI _statusText; // 기능 추가: 동기화 상태 텍스트

        [Header("Colors")]
        [SerializeField]
        private Color _onlineColor = Color.green;  // 기능 추가: 온라인 상태 시 아이콘 색상

        [SerializeField]
        private Color _offlineColor = Color.red;   // 기능 추가: 오프라인 상태 시 아이콘 색상

        /// <summary>
        /// 네트워크 온라인/오프라인 상태에 따라 UI를 갱신.
        /// </summary>
        /// <param name="online">true면 온라인, false면 오프라인</param>
        public void SetSyncStatus(bool online)
        {
            // 기능 추가: 텍스트 설정
            if (_statusText != null)
                _statusText.text = online ? "Sync: Online" : "Sync: Offline";

            // 기능 추가: 아이콘 색상 변경
            if (_statusIcon != null)
                _statusIcon.color = online ? _onlineColor : _offlineColor;
        }

        // 메모리 누수 방지
        private void OnDestroy()
        {
            // 컴포넌트 참조 해제
            _statusIcon = null;
            _statusText = null;
        }
    }
}
