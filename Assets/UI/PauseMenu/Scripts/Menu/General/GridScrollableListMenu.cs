using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A menu in which the menu item list is shown within a grid layout and contained inside a scroll view.
    /// </summary>
    public class GridScrollableListMenu : ScrollableListMenu {

        [Tooltip("A key used to select the previous row item.")]
        [SerializeField] protected KeyCode previousRowKey = KeyCode.UpArrow;

        [Tooltip("A key used to select the next row item.")]
        [SerializeField] protected KeyCode nextRowKey = KeyCode.DownArrow;

        [SerializeField] private int gridColumnCount = 2;

        protected override void Update() {
            base.Update();

            if (!menuItems[currentMenuIndex].IsOpened() && !menuItems[currentMenuIndex].IsOpeningChildPanel()) {

                if (Input.GetKeyDown(previousRowKey)) {
                    HighlightPreviousMenu(gridColumnCount);
                }
                if (Input.GetKeyDown(nextRowKey)) {
                    HighlightNextMenu(gridColumnCount);
                }

            }
        }
    }
}
