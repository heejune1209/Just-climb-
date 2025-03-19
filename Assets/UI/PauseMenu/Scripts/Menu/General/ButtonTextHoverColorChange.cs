using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    public class ButtonTextHoverColorChange : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {

        [Tooltip("Color to set on the menu item text when the menu is highlighted.")]
        [SerializeField] protected Color textHighlightedColor = new Color32(63, 65, 73, 255);

        [Tooltip("Color to set on the menu item text when the menu is unhighlighted.")]
        [SerializeField] protected Color textUnhighlightedColor = new Color32(210, 199, 195, 255);

        [SerializeField] protected Text text;

        public void OnPointerEnter(PointerEventData eventData) {
            text.color = textHighlightedColor;
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (EventSystem.current.currentSelectedGameObject != gameObject) {
                text.color = textUnhighlightedColor;
            }
        }

        public void OnSelect(BaseEventData eventData) {
            text.color = textHighlightedColor;
        }
        public void OnDeselect(BaseEventData eventData) {
            text.color = textUnhighlightedColor;
        }

    }
}