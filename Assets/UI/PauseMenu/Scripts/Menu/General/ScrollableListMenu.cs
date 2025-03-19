using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A menu in which the menu item list is contained inside a scroll view.
    /// </summary>
    public class ScrollableListMenu : BaseMenu {


        [System.Serializable]
        public class ItemEvent : UnityEvent<int> { }

        [Tooltip("Scroll view of the data. This should not be null.")]
        [SerializeField] private ScrollRect scrollView;

        [SerializeField] private ItemEvent onItemSelected;

        protected override void OnInitMenuItem(int _index, SubMenuItem _menuItem) {
            base.OnInitMenuItem(_index, _menuItem);
            ScrollRectFixer[] scrollRectFixers = _menuItem.GetComponentsInChildren<ScrollRectFixer>();
            foreach(ScrollRectFixer s in scrollRectFixers) {
                s.MainScroll = scrollView;
            }
        }

        public override void SelectMenu() {
            base.SelectMenu();
            onItemSelected.Invoke(currentMenuIndex);

        }

        protected override void HighlightPreviousMenu(int _amount = 1) {
            base.HighlightPreviousMenu(_amount);
            //ScrollToElement(menuItems[currentMenuIndex].GetComponent<RectTransform>());
            scrollView.content.localPosition = GetSnapToPositionToBringChildIntoView(menuItems[currentMenuIndex].GetComponent<RectTransform>());
        }

        protected override void HighlightNextMenu(int _amount = 1) {
            base.HighlightNextMenu(_amount);
            scrollView.content.localPosition = GetSnapToPositionToBringChildIntoView(menuItems[currentMenuIndex].GetComponent<RectTransform>());
        }

        private Vector2 GetSnapToPositionToBringChildIntoView(RectTransform child) {
            Vector2 prevPos = scrollView.content.anchoredPosition;

            Canvas.ForceUpdateCanvases();
            Vector2 viewportLocalPosition = scrollView.viewport.localPosition;
            Vector2 childLocalPosition = child.localPosition;
            Vector2 result = new Vector2(
                0 - (viewportLocalPosition.x + childLocalPosition.x),
                0 - (viewportLocalPosition.y + childLocalPosition.y)
            );

            if (!scrollView.horizontal) {
                result.x = prevPos.x;
            }
            if (!scrollView.vertical) {
                result.y = prevPos.y;
            }
            return result;
        }
    }
}
