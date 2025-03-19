using UnityEngine;
using UnityEngine.InputSystem;
using DiasGames.Controller;
using Calcatz.JungleThemeGUI;
using UnityEngine.SceneManagement;

namespace DiasGames.Components
{
    public class PauseComponent : MonoBehaviour
    {
        public GameObject pauseMenu;
        public End endScript;
        public bool _isPaused;
        public GameObject ResultPanel;


        CursorLockMode lockMode;
        bool visible;
        CSPlayerController cS;
        private void Start()
        {
            visible = Cursor.visible;
            lockMode = Cursor.lockState;

            cS = GetComponent<CSPlayerController>(); // CSPlayerController 클래스의 객체를 생성후 필드 참조를 위해 선언
        }

        private void OnPause(InputValue value)
        {
            if(value.isPressed)
            {
                OnPause(!_isPaused);
            }
            
            
        }

        public void OnPause(bool paused)  // esc누르면 커서 보이고 시간 멈추게하고 pausemenu settrue 기능
        {           
            SelectPanelTrigger selectPanel = ResultPanel.GetComponent<SelectPanelTrigger>(); 
            if (pauseMenu && cS.HowtoPlayTrigger.activeSelf == false && selectPanel.targetTrigger.activeSelf == false)
            {
                _isPaused = paused;
                Time.timeScale = _isPaused ? 0f : 1f;
                pauseMenu.SetActive(_isPaused);
                if (_isPaused)
                {
                    endScript.PauseGame();
                }
                else
                {
                    endScript.ResumeGame();
                }
                Cursor.visible = paused ? true : visible;
                Cursor.lockState = paused ? CursorLockMode.None : lockMode;
            }
        }
        
        
        
        public void OnTapGetHTPPanel(bool paused) // HTPPanel 뜨면 커서도 보이고 시간이 멈추게 하는 기능
        {
            SelectPanelTrigger selectPanel = ResultPanel.GetComponent<SelectPanelTrigger>();
            if (cS.HowtoPlayTrigger && pauseMenu.activeSelf == false && selectPanel.targetTrigger.activeSelf == false)
            {
                _isPaused = paused;
                Time.timeScale = _isPaused ? 0f : 1f;
                cS.HowtoPlayTrigger.SetActive(_isPaused);
                if (_isPaused)
                {
                    endScript.PauseGame();
                }
                else
                {
                    endScript.ResumeGame();
                }

                Cursor.visible = paused ? true : visible;
                Cursor.lockState = paused ? CursorLockMode.None : lockMode;
            }
            
        }

    }
}