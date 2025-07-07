using UnityEngine;
using System.Collections;
using System;
using Zenject;
using Steamworks;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.Networking;
using JustClimb.Data;

namespace JustClimb.Manager
{
    /// <summary>
    /// Steam 인증을 관리하는 매니저
    /// 게임 시작 시 자동으로 Steam에 로그인하고 서버에서 JWT 토큰을 받아옵니다.
    /// </summary>
    public class SteamAuthManager : MonoBehaviour, IInitializable
    {
        [Header("Steam Settings")]
        [SerializeField] private bool _debugMode = true;
        
        [Inject] private ServerConfig _serverConfig;

        // Steam 관련 변수들
        private bool _isSteamInitialized = false;
        private bool _isAuthInProgress = false;
        private bool _isAuthenticated = false;
        private string _jwtToken = "";
        private string _steamId = "";
        private string _steamDisplayName = "";
        private HAuthTicket _authTicket;

        // 콜백들
        private Callback<GetAuthSessionTicketResponse_t> _authTicketCallback;

        // 이벤트들
        public event Action<bool> OnSteamInitialized;
        public event Action<string> OnAuthenticationSuccess;
        public event Action<string> OnAuthenticationFailed;

        // 속성들
        public bool IsAuthenticated => _isAuthenticated;
        public string JwtToken => _jwtToken;
        public string SteamId => _steamId;
        public string SteamDisplayName => _steamDisplayName;
        public bool IsAuthInProgress => _isAuthInProgress;
        public bool IsSteamInitialized => _isSteamInitialized;

        public void Initialize()
        {
            Debug.Log("[SteamAuthManager] Initializing...");
            
            // ServerConfig 체크
            if (_serverConfig == null)
            {
                LogError("ServerConfig is not injected! Make sure ProjectInstaller is properly configured.");
                OnSteamInitialized?.Invoke(false);
                return;
            }
            
            // SteamManager 초기화 대기
            StartCoroutine(WaitForSteamInitialization());
        }

        private IEnumerator WaitForSteamInitialization()
        {
            // SteamManager 초기화 대기 (최대 10초)
            float timeout = 10f;
            while (!SteamManager.Initialized && timeout > 0)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }

            if (!SteamManager.Initialized)
            {
                LogError("Steam is not initialized! Make sure Steam is running and try again.");
                OnSteamInitialized?.Invoke(false);
                yield break;
            }

            _isSteamInitialized = true;
            _steamId = SteamUser.GetSteamID().ToString();
            
            // Steam 닉네임 가져오기
            _steamDisplayName = SteamFriends.GetPersonaName();
            if (string.IsNullOrEmpty(_steamDisplayName))
            {
                _steamDisplayName = "Unknown Player";
            }

            // 콜백 등록
            _authTicketCallback = Callback<GetAuthSessionTicketResponse_t>.Create(OnAuthSessionTicketResponse);

            Debug.Log($"[SteamAuthManager] Steam initialized successfully. SteamID: {_steamId}, DisplayName: {_steamDisplayName}");
            
            // Steam 초기화 성공 이벤트 발생
            OnSteamInitialized?.Invoke(true);

            // 자동으로 인증 시작
            StartCoroutine(AuthenticateWithSteam());
        }

        /// <summary>
        /// Steam 인증 프로세스 시작
        /// </summary>
        public void StartAuthentication()
        {
            if (!_isSteamInitialized)
            {
                LogError("Steam is not initialized");
                return;
            }

            if (_isAuthInProgress)
            {
                LogError("Authentication already in progress");
                return;
            }

            StartCoroutine(AuthenticateWithSteam());
        }

        private IEnumerator AuthenticateWithSteam()
        {
            _isAuthInProgress = true;
            LogInfo("Starting Steam authentication...");

            // Steam 인증 티켓 요청 (SteamNetworkingIdentity 포함)
            byte[] ticketData = new byte[1024];
            uint ticketSize;
            SteamNetworkingIdentity networkingIdentity = new SteamNetworkingIdentity();
            
            try
            {
                _authTicket = SteamUser.GetAuthSessionTicket(ticketData, ticketData.Length, out ticketSize, ref networkingIdentity);
            }
            catch (Exception e)
            {
                LogError($"Steam API exception: {e.Message}");
                _isAuthInProgress = false;
                OnAuthenticationFailed?.Invoke($"Steam API error: {e.Message}");
                yield break;
            }
            
            if (_authTicket == HAuthTicket.Invalid)
            {
                LogError("Failed to get Steam auth ticket");
                _isAuthInProgress = false;
                OnAuthenticationFailed?.Invoke("Failed to get Steam auth ticket");
                yield break;
            }

            LogInfo("Steam auth ticket obtained, waiting for response...");
            
            // 티켓 응답 대기 (최대 10초)
            float timeout = 10f;
            while (timeout > 0 && _isAuthInProgress)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }

            if (_isAuthInProgress)
            {
                LogError("Steam auth ticket timeout");
                _isAuthInProgress = false;
                OnAuthenticationFailed?.Invoke("Steam auth ticket timeout");
            }
        }

        private void OnAuthSessionTicketResponse(GetAuthSessionTicketResponse_t callback)
        {
            if (callback.m_hAuthTicket == _authTicket)
            {
                if (callback.m_eResult == EResult.k_EResultOK)
                {
                    LogInfo("Steam auth ticket response received successfully");
                    StartCoroutine(SendAuthTicketToServer());
                }
                else
                {
                    LogError($"Steam auth ticket failed: {callback.m_eResult}");
                    _isAuthInProgress = false;
                    OnAuthenticationFailed?.Invoke($"Steam auth ticket failed: {callback.m_eResult}");
                }
            }
        }

        private IEnumerator SendAuthTicketToServer()
        {
            // 티켓 데이터를 HEX 문자열로 변환
            byte[] ticketData = new byte[1024];
            uint ticketSize;
            SteamNetworkingIdentity networkingIdentity = new SteamNetworkingIdentity();
            
            try
            {
                SteamUser.GetAuthSessionTicket(ticketData, ticketData.Length, out ticketSize, ref networkingIdentity);
            }
            catch (Exception e)
            {
                LogError($"Steam API exception during ticket retrieval: {e.Message}");
                _isAuthInProgress = false;
                OnAuthenticationFailed?.Invoke($"Steam API error: {e.Message}");
                yield break;
            }
            
            // 실제 크기만큼 자르기
            byte[] actualTicketData = new byte[ticketSize];
            Array.Copy(ticketData, actualTicketData, ticketSize);
            
            // HEX 문자열로 변환
            string ticketHex = BitConverter.ToString(actualTicketData).Replace("-", "");
            
            // 인증 요청 데이터 생성
            var authRequest = new
            {
                steamId = _steamId,
                authTicket = ticketHex,
                steamDisplayName = _steamDisplayName
            };

            string jsonRequest = JsonConvert.SerializeObject(authRequest);
            
            string serverUrl = _serverConfig.GetBaseUrl();
            LogInfo($"Sending auth request to server: {serverUrl}/api/auth/steam");
            LogInfo($"Request data: SteamID={_steamId}, DisplayName={_steamDisplayName}, TicketLength={ticketHex.Length}");

            // UnityWebRequest로 서버에 전송
            using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/api/auth/steam", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                // HTTPS localhost 개발 환경에서 SSL 검증 우회
                #if UNITY_EDITOR
                request.certificateHandler = new AcceptAllCertificatesHandler();
                #endif

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonConvert.DeserializeObject<AuthResponse>(request.downloadHandler.text);
                        
                        if (response.Success)
                        {
                            _jwtToken = response.Token;
                            _isAuthenticated = true;
                            _isAuthInProgress = false;
                            
                            LogInfo("Steam authentication completed successfully!");
                            OnAuthenticationSuccess?.Invoke(_jwtToken);
                        }
                        else
                        {
                            LogError($"Server authentication failed: {response.Message}");
                            _isAuthInProgress = false;
                            OnAuthenticationFailed?.Invoke(response.Message);
                        }
                    }
                    catch (Exception e)
                    {
                        LogError($"Failed to parse server response: {e.Message}");
                        _isAuthInProgress = false;
                        OnAuthenticationFailed?.Invoke("Failed to parse server response");
                    }
                }
                else
                {
                    LogError($"Server request failed: {request.error}");
                    _isAuthInProgress = false;
                    OnAuthenticationFailed?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// 현재 JWT 토큰이 유효한지 확인
        /// </summary>
        public bool HasValidToken()
        {
            return _isAuthenticated && !string.IsNullOrEmpty(_jwtToken);
        }

        /// <summary>
        /// Steam 닉네임 가져오기
        /// </summary>
        public string GetSteamDisplayName()
        {
            if (_isSteamInitialized && SteamManager.Initialized)
            {
                return SteamFriends.GetPersonaName();
            }
            return _steamDisplayName;
        }

        private void OnDestroy()
        {
            if (_isSteamInitialized && SteamManager.Initialized && _authTicket != HAuthTicket.Invalid)
            {
                try
                {
                    SteamUser.CancelAuthTicket(_authTicket);
                }
                catch (Exception e)
                {
                    LogError($"Error canceling auth ticket: {e.Message}");
                }
            }
        }

        private void LogInfo(string message)
        {
            if (_debugMode)
            {
                Debug.Log($"[SteamAuthManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamAuthManager] {message}");
        }

        [Serializable]
        public class AuthResponse
        {
            public bool Success;
            public string Message;
            public string Token;
        }
    }
    
    /// <summary>
    /// 개발 환경에서 모든 SSL 인증서를 허용하는 핸들러
    /// </summary>
    public class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // 개발 환경에서 모든 인증서 허용
        }
    }
} 