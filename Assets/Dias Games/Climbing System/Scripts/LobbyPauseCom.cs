using UnityEngine;
using UnityEngine.InputSystem;
using DiasGames.Controller;
using Calcatz.JungleThemeGUI;
using UnityEngine.SceneManagement;


namespace DiasGames.Components
{
    public class LobbyPauseCom : MonoBehaviour
    {
        public GameObject pauseMenu;
        public End endScript;
        public bool _isPaused;


        CursorLockMode lockMode;
        bool visible;
        LobbyCharater Lc;
        private void Start()
        {
            visible = Cursor.visible;
            lockMode = Cursor.lockState;

            Lc = GetComponent<LobbyCharater>(); // CSPlayerController Ŭ������ ��ü�� ������ �ʵ� ������ ���� ����
        }

        private void OnPause(InputValue value)
        {
            if (value.isPressed)
            {
                OnPause(!_isPaused);
            }


        }

        public void OnPause(bool paused)  // esc������ Ŀ�� ���̰� �ð� ���߰��ϰ� pausemenu settrue ���
        {
            if (pauseMenu && Lc.SelectTrigger.activeSelf == false && Lc.ShopTrigger.activeSelf == false && Lc.WorldViewTrigger.activeSelf == false)
            {
                _isPaused = paused;
                Time.timeScale = _isPaused ? 0f : 1f;
                pauseMenu.SetActive(_isPaused);
                Cursor.visible = paused ? true : visible;
                Cursor.lockState = paused ? CursorLockMode.None : lockMode;
            }
        }
        public void OnInteractGetSelectPanel(bool paused) // selectpanel�� �߸� Ŀ���� ���̰� �ð��� ���߰� �ϴ� ���
        {

            if (Lc.SelectTrigger)
            {
                if (pauseMenu.activeSelf == false && Lc.ShopTrigger.activeSelf == false && Lc.WorldViewTrigger.activeSelf == false)
                {
                    _isPaused = paused;
                    Time.timeScale = _isPaused ? 0f : 1f;
                    Lc.SelectTrigger.SetActive(_isPaused);
                    Cursor.visible = paused ? true : visible;
                    Cursor.lockState = paused ? CursorLockMode.None : lockMode;
                }

            }


        }
        public void OnInteractGetShopPanel(bool paused) // ShopPanel �߸� Ŀ���� ���̰� �ð��� ���߰� �ϴ� ���
        {
            if (Lc.ShopTrigger)
            {
                if (pauseMenu.activeSelf == false && Lc.SelectTrigger.activeSelf == false && Lc.WorldViewTrigger.activeSelf == false)
                {
                    _isPaused = paused;
                    Time.timeScale = _isPaused ? 0f : 1f;
                    Lc.ShopTrigger.SetActive(_isPaused);
                    Cursor.visible = paused ? true : visible;
                    Cursor.lockState = paused ? CursorLockMode.None : lockMode;
                }

            }
        }
        public void OnInteractGetWorldViewPanel(bool paused) // WorldViewPanel �߸� Ŀ���� ���̰� �ð��� ���߰� �ϴ� ���
        {
            if (Lc.WorldViewTrigger)
            {
                if (pauseMenu.activeSelf == false && Lc.ShopTrigger.activeSelf == false && Lc.SelectTrigger.activeSelf == false && pauseMenu.activeSelf == false)
                {
                    _isPaused = paused;
                    Time.timeScale = _isPaused ? 0f : 1f;
                    Lc.WorldViewTrigger.SetActive(_isPaused);
                    Cursor.visible = paused ? true : visible;
                    Cursor.lockState = paused ? CursorLockMode.None : lockMode;
                }

            }
        }
        

    }

}
