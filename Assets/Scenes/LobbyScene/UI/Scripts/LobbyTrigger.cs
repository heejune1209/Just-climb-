using UnityEngine;
using Zenject;

public class LobbyTrigger : MonoBehaviour
{
    // DI 주입받을 매니저
    [Inject] private IUIManager _uiManager;
    [Inject] private DiContainer _container;

    [Tooltip("Shop, WorldView, SelectStage, Ranking 등 구분할 이름")]
    [SerializeField] private string areaName;

    private UI_Lobby _uiLobby;

    void Start()
    {
        // 1) 이미 씬에 배치된 UI_Lobby 컴포넌트를 우선 찾습니다.
        _uiLobby = FindObjectOfType<UI_Lobby>();

        _container.InjectGameObject(gameObject);
        // 그 뒤에는 _uiManager가 정상 주입됨

        // 2) 없으면 UIManager를 통해 Prefab에서 생성합니다.
        if (_uiLobby == null)
        {
            _uiLobby = _uiManager.ShowSceneUI<UI_Lobby>("UI_Lobby");
        }

        // 3) 그래도 없으면 에러 로그
        if (_uiLobby == null)
        {
            Debug.LogError("[LobbyTrigger] UI_Lobby를 찾거나 생성하지 못했습니다!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _uiLobby.ShowAreaPrompt(areaName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _uiLobby.HideAreaPrompt();
        }
    }
}
