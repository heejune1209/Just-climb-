using UnityEngine;

public class LobbyTrigger : MonoBehaviour
{
    [Tooltip("Shop, WorldView, SelectStage, Ranking 등 구분할 이름")]
    [SerializeField] private string areaName;

    private UI_Lobby _uiLobby;

    private void Start()
    {
        // Managers.UI.Root 아래에 붙어 있는 UI_Lobby 컴포넌트 찾기
        _uiLobby = Managers.UI.Root.GetComponentInChildren<UI_Lobby>();
        if (_uiLobby == null)
            Debug.LogError("UI_Lobby를 찾을 수 없습니다!");
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
