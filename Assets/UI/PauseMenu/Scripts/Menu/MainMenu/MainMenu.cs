using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A component added in root gameobject of main menu.
    /// </summary>
    public class MainMenu : BaseMenu {

        [SerializeField] private GameObject quitWarningPanel;
        [SerializeField] private Button proceedQuitButton;
        [SerializeField] private Button cancelQuitButton;

        [SerializeField] private GameObject[] panelBackgrounds;

        protected override void OnEnable() {
            base.OnEnable();
            if (proceedQuitButton != null) {
                proceedQuitButton.onClick.AddListener(delegate {
                    Debug.Log("Quit");
                    Application.Quit();
                });
            }

            if (cancelQuitButton != null) {
                cancelQuitButton.onClick.AddListener(delegate {
                    this.enabled = true;
                    if (quitWarningPanel != null) {
                        quitWarningPanel.SetActive(false);
                    }
                });
            }

            ShowPanelBackground(false);
        }

        private void ShowPanelBackground(bool _show) {
            foreach (GameObject bg in panelBackgrounds) {
                bg.SetActive(_show);
            }
        }

        protected override void Update() {
            base.Update();

            if (closeMenuKey.triggered) {
                ShowPanelBackground(false);
            }
        }

        /// <summary>
        /// Called in menu item, assigned from inspector.
        /// </summary>
        public void QuitGame() {
            this.enabled = false;
            if (quitWarningPanel != null) {
                quitWarningPanel.SetActive(true);
                EventSystem.current.SetSelectedGameObject(null);
                cancelQuitButton.Select();
            }
        }

        /// <summary>
        /// Called in menu item, assigned from inspector.
        /// </summary>
        public void LoadScene(string _sceneName) {
            SceneManager.LoadScene(_sceneName);
        }

        public override void SelectMenu() {
            base.SelectMenu();
            ShowPanelBackground(IsOpeningChildPanel());
        }
    }
}
