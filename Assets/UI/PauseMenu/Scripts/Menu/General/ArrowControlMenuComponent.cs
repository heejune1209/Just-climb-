using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Calcatz.JungleThemeGUI {
    [RequireComponent(typeof(BaseMenu))]
    public class ArrowControlMenuComponent : MonoBehaviour {

        [HideInInspector] [SerializeField] private BaseMenu baseMenu;

        [Tooltip("A key used to decrease the value of child arrow selection menu item.")]
        [SerializeField] protected InputAction decreaseValueButton;

        [Tooltip("A key used to increase the value of child arrow selection menu item.")]
        [SerializeField] protected InputAction increaseValueButton;

        private void Awake() {
            OnValidate();
        }

        private void OnValidate() {
            if (baseMenu == null) baseMenu = GetComponent<BaseMenu>();
        }

        private void Update() {
            SubMenuItem subMenuItem = baseMenu.GetCurrentSubMenuItem();
            if (subMenuItem == null) return;
            if (subMenuItem is ArrowSelectionSubMenuItem arrowMenuItem) {

                if (!arrowMenuItem.IsOpened() && !arrowMenuItem.IsOpeningChildPanel()) {
                    if (decreaseValueButton.triggered) {
                        arrowMenuItem.DecreaseValue();
                    }
                    if (increaseValueButton.triggered) {
                        arrowMenuItem.IncreaseValue();
                    }
                }

            }
        }

    }
}
