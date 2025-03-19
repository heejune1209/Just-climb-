using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Calcatz.JungleThemeGUI {
    public class GameplayMenuDemo : MonoBehaviour {

        // [SerializeField] private KeyCode pauseMenuKey = KeyCode.Escape;
        // [SerializeField] private KeyCode glossaryKey = KeyCode.I;


        //[SerializeField] private GameObject pauseMenuRoot;
        //[SerializeField] private BaseMenu pauseMenu;

        //[SerializeField] private GameObject glossaryRoot;

        void Start() {

        }
        /*
        private void OnGUI() {
            if (!pauseMenuRoot.activeInHierarchy && !glossaryRoot.activeInHierarchy) {
                if (GUILayout.Button("Show Pause Menu or Press Esc")) {
                    pauseMenuRoot.SetActive(true);
                }
                if (GUILayout.Button("Show Glossary or Press I")) {
                    glossaryRoot.SetActive(true);
                }
            }
        }
        */
        /*
        void Update() {

            
                else if (!glossaryRoot.activeInHierarchy) {
                    pauseMenuRoot.SetActive(true);
                }
            }

            if (Input.GetKeyDown(glossaryKey)) {
                if (glossaryRoot.activeInHierarchy) {
                    glossaryRoot.SetActive(false);
                }
                else if (!pauseMenuRoot.activeInHierarchy) {
                    glossaryRoot.SetActive(true);
                }
            }

            if (glossaryRoot.activeInHierarchy) {
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    glossaryRoot.SetActive(false);
                }
            }
        }
        */
        /// <summary>
        /// Called in menu item, assigned from inspector.
        /// </summary>
        

        /// <summary>
        /// Called in menu item, assigned from inspector.
        /// </summary>
        public void BackToMainMenu() {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
