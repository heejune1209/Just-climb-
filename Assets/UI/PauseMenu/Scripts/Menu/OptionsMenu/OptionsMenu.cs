using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A component added in root gameobject of options menu.
    /// </summary>
    public class OptionsMenu : BaseMenu {

        [SerializeField] private InputAction changeTabShortcut;
        [SerializeField] private GameObject pausemenu;
        private SubMenuItem parentMenuItem;

        public override void OnOpenedAsChildMenu(SubMenuItem _parentMenuItem) {
            base.OnOpenedAsChildMenu(_parentMenuItem);
            parentMenuItem = _parentMenuItem;
            menuItems[currentMenuIndex].OpenPanel();
        }

        protected override void Update() {
            base.Update();

            if (changeTabShortcut.triggered) {
                HighlightNextMenu();
                SelectMenu();
            }

            if (parentMenuItem != null && closeMenuKey.triggered) {
                menuItems[currentMenuIndex].ClosePanel();
                parentMenuItem.ClosePanel();
                /*
                if(gameObject.activeSelf == true && pausemenu.activeSelf == false)
                {
                    gameObject.SetActive(false);
                    pausemenu.SetActive(true);
                }
                */

            }

        }

    }
}
