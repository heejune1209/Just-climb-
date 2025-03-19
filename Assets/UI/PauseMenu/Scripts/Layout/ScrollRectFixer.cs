using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {

    /// <summary>
    /// Add this component to a UI element that prevents a scroll rect from being scrolled.
    /// </summary>
    public class ScrollRectFixer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler {

        public ScrollRect MainScroll;

        public void OnBeginDrag(PointerEventData eventData) {
            MainScroll.OnBeginDrag(eventData);
        }


        public void OnDrag(PointerEventData eventData) {
            MainScroll.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData) {
            MainScroll.OnEndDrag(eventData);
        }


        public void OnScroll(PointerEventData data) {
            MainScroll.OnScroll(data);
        }


    }
}
