using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using JustClimb.Manager;

/// <summary>
/// Steam 인증 상태를 표시하는 UI 컴포넌트 (우측 상단에 작게 표시)
/// </summary>
public class UI_SteamStatus : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private Image statusIcon;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button retryButton;

    [Header("Status Colors")]
    [SerializeField] private Color connectingColor = Color.yellow;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;

    [Inject] private SteamAuthManager _steamAuthManager;

    private void Start()
    {
        // Steam 매니저 이벤트 구독
        if (_steamAuthManager != null)
        {
            _steamAuthManager.OnSteamInitialized += OnSteamInitialized;
            _steamAuthManager.OnAuthenticationSuccess += OnAuthenticationSuccess;
            _steamAuthManager.OnAuthenticationFailed += OnAuthenticationFailed;
        }

        // 재시도 버튼 이벤트
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        // 초기 상태 설정
        UpdateStatusUI(SteamStatus.Connecting, "Steam connecting...");
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_steamAuthManager != null)
        {
            _steamAuthManager.OnSteamInitialized -= OnSteamInitialized;
            _steamAuthManager.OnAuthenticationSuccess -= OnAuthenticationSuccess;
            _steamAuthManager.OnAuthenticationFailed -= OnAuthenticationFailed;
        }
    }

    private void OnSteamInitialized(bool success)
    {
        if (success)
        {
            UpdateStatusUI(SteamStatus.Connecting, "Steam Authenticating...");
        }
        else
        {
            UpdateStatusUI(SteamStatus.Error, "Steam Connection failed");
        }
    }

    private void OnAuthenticationSuccess(string jwtToken)
    {
        UpdateStatusUI(SteamStatus.Connected, "Steam Connected");
        
        // 3초 후 상태창 숨기기
        Invoke(nameof(HideStatusPanel), 3f);
    }

    private void OnAuthenticationFailed(string errorMessage)
    {
        UpdateStatusUI(SteamStatus.Error, $"Steam Auth Failed");
        Debug.LogError($"Steam Auth Failed : {errorMessage}");
    }

    private void OnRetryButtonClicked()
    {
        if (_steamAuthManager != null)
        {
            UpdateStatusUI(SteamStatus.Connecting, "Reconnecting...");
            _steamAuthManager.StartAuthentication();
        }
    }

    private void UpdateStatusUI(SteamStatus status, string message)
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text = message;
        }

        if (statusIcon != null)
        {
            switch (status)
            {
                case SteamStatus.Connecting:
                    statusIcon.color = connectingColor;
                    break;
                case SteamStatus.Connected:
                    statusIcon.color = connectedColor;
                    break;
                case SteamStatus.Error:
                    statusIcon.color = errorColor;
                    break;
            }
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(status == SteamStatus.Error);
        }
    }

    private void HideStatusPanel()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 개발자 모드에서 상태창을 강제로 표시
    /// </summary>
    [ContextMenu("Show Status Panel")]
    public void ShowStatusPanel()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(true);
        }
    }

    private enum SteamStatus
    {
        Connecting,
        Connected,
        Error
    }
} 