using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public Button newGameButton;
    public Button selectChapterButton;
    public Button settingsButton;
    public Button quitButton;
    public GameObject settingsPanel;
    public Button closeSettingsButton;

    void Start()
    {
        newGameButton.onClick.AddListener(StartNewGame);
        selectChapterButton.onClick.AddListener(SelectChapter);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(QuitGame);
        closeSettingsButton.onClick.AddListener(CloseSettings);
    }

    void StartNewGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    void SelectChapter()
    {
        SceneManager.LoadScene("ChapterSelectionScene");
    }

    void OpenSettings()
    {
        settingsPanel.SetActive(true);  
    }

    void CloseSettings()
    {
        settingsPanel.SetActive(false);  
    }

    void QuitGame()
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false; 
#else
        Application.Quit();  
#endif
    }
}