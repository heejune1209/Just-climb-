using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// Base class for menu that contains selectable menu item list with navigation behaviour.
    /// </summary>
    public class BaseMenu : MonoBehaviour {

        #region PROPERTIES
        [Tooltip("List of available menu items for the menu.")]
        [SerializeField] protected List<SubMenuItem> menuItems = new List<SubMenuItem>();

        [Tooltip("Color to set on the menu item text when the menu is highlighted.")]
        [SerializeField] protected Color textHighlightedColor = new Color32(63, 65, 73, 255);

        [Tooltip("Color to set on the menu item text when the menu is unhighlighted.")]
        [SerializeField] protected Color textUnhighlightedColor = new Color32(210, 199, 195, 255);

        [Tooltip("A key used to select the previous menu item.")]
        [SerializeField] protected InputAction previousMenuKey;

        [Tooltip("A key used to select the next menu item.")]
        [SerializeField] protected InputAction nextMenuKey;

        [Tooltip("A key used to open the menu item.")]
        [SerializeField] protected InputAction openMenuKey;

        [Tooltip("A key used to close the menu item.")]
        [SerializeField] protected InputAction closeMenuKey;

        [Tooltip("List of game object that are hidden when a sub menu is selected. This is optional.")]
        [SerializeField] private GameObject[] hiddenMenuWhenOpeningChild;

        protected int currentMenuIndex;

        #endregion PROPERTIES

        private bool initialized;
        protected virtual void OnEnable() {
            if (initialized) return;
            initialized = true;
            RefreshMenuItemCallbacks();
            if (menuItems.Count > 0) {
                HighlightMenu(0);
            }
        }

        protected void RefreshMenuItemCallbacks() {
            for (int i = 0; i < menuItems.Count; i++) {
                menuItems[i].Init(textUnhighlightedColor, textHighlightedColor, i, _index => {
                    HighlightMenu(_index);
                    SelectMenu();
                });
                menuItems[i].Highlight(false);
                OnInitMenuItem(i, menuItems[i]);
            }
        }

        protected virtual void OnInitMenuItem(int _index, SubMenuItem _menuItem) {

        }

        /// <summary>
        /// A callback called when opened by the parent menu as a child meu.
        /// </summary>
        public virtual void OnOpenedAsChildMenu(SubMenuItem _parentMenuItem) {

        }

        public SubMenuItem GetCurrentSubMenuItem() {
            if (currentMenuIndex >= 0 && currentMenuIndex < menuItems.Count) {
                return menuItems[currentMenuIndex];
            }
            return null;
        }

        /// <summary>
        /// Highlight the selected menu and unhighlight current menu if there is any.
        /// </summary>
        /// <param name="_index"></param>
        public virtual void HighlightMenu(int _index) {
            SubMenuItem currentMenuItem = menuItems[currentMenuIndex];
            currentMenuItem.ClosePanel();
            if (currentMenuItem != null) {
                currentMenuItem.Highlight(false);
            }

            currentMenuIndex = _index;

            if (_index < menuItems.Count) {
                currentMenuItem = menuItems[_index];
                currentMenuItem.Highlight(true);
            }
        }

        /// <summary>
        /// Called when a menu is selected, either by clicking or pressing Open Menu Key.
        /// </summary>
        public virtual void SelectMenu() {
            menuItems[currentMenuIndex].OpenPanel();
            foreach(GameObject go in hiddenMenuWhenOpeningChild) {
                go.SetActive(false);
            }
        }

        protected virtual void Update() {
            if (!menuItems[currentMenuIndex].IsOpened() && !menuItems[currentMenuIndex].IsOpeningChildPanel()) {

                foreach (GameObject go in hiddenMenuWhenOpeningChild) {
                    if (!go.activeInHierarchy) {
                        go.SetActive(true);
                    }
                }
                
                if (previousMenuKey.triggered) {
                    HighlightPreviousMenu();
                }
                if ((nextMenuKey.triggered)) {
                    HighlightNextMenu();
                }
                if ((openMenuKey.triggered)) {
                    SelectMenu();
                }
            }
            else if (!menuItems[currentMenuIndex].IsOpeningChildPanel()) {
                if ((closeMenuKey.triggered)) {
                    CloseChildPanel();
                }
                
            }
        }

        
        protected virtual void CloseChildPanel() {
            menuItems[currentMenuIndex].ClosePanel();
        }

        protected virtual void HighlightPreviousMenu(int _amount = 1) {
            int newIndex = currentMenuIndex - _amount;
            if (newIndex < 0) newIndex = menuItems.Count - 1;
            HighlightMenu(newIndex);
        }

        protected virtual void HighlightNextMenu(int _amount = 1) {
            int newIndex = currentMenuIndex + _amount;
            if (newIndex >= menuItems.Count) newIndex = 0;
            HighlightMenu(newIndex);
        }

        /// <summary>
        /// Check whether or not this sub menu item is opening a child panel. If panel is not set, then the return value will always be false.
        /// </summary>
        /// <returns></returns>
        public bool IsOpeningChildPanel() {
            return menuItems[currentMenuIndex].IsOpened();
        }
    }
}