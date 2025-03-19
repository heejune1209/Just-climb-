using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A component used as child menu of a menu.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(EventTrigger))]
    public class SubMenuItem : MonoBehaviour {

        [Tooltip("The image game object used for highlight background.")]
        [SerializeField] protected Image highlightImage;

        [Tooltip("The text game object used as menu title.")]
        [SerializeField] protected TextMeshProUGUI nameText;

        [Tooltip("The text game object used as detailed texts. This is optional.")]
        [SerializeField] protected TextMeshProUGUI[] detailTexts;

        [Tooltip("The detailed panel of this menu item. This is optional.")]
        [SerializeField] protected GameObject panel;

        [HideInInspector] [SerializeField] protected Image eventAreaImage;
        [HideInInspector] [SerializeField] protected EventTrigger eventTrigger;

        [SerializeField] private UnityEvent onSelect;

        protected Color textHighlightedColor;
        protected Color textUnhighlightedColor;

        private bool isOpened;

        protected virtual void Awake() {
            OnValidate();
            if (panel != null) panel.SetActive(false);
        }

        protected virtual void OnValidate() {
            if (eventAreaImage == null) {
                eventAreaImage = GetComponent<Image>();
            }
            if (eventTrigger == null) {
                eventTrigger = GetComponent<EventTrigger>();
            }
        }

        public virtual void Init(Color _textUnhighlightedColor, Color _textHighlightedColor, int _menuIndex, Action<int> _onClick) {
            eventTrigger.triggers.Clear();

            textUnhighlightedColor = _textUnhighlightedColor;
            textHighlightedColor = _textHighlightedColor;

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener(_e => {
                _onClick?.Invoke(_menuIndex);
            });
            eventTrigger.triggers.Add(clickEntry);
        }

        public virtual void Highlight(bool _highlight) {
            if (highlightImage != null) {
                highlightImage.gameObject.SetActive(_highlight);
            }
            if (nameText != null) {
                nameText.color = _highlight ? textHighlightedColor : textUnhighlightedColor;
            }
            foreach (TextMeshProUGUI text in detailTexts) {
                if(text != null)
                text.color = _highlight ? textHighlightedColor : textUnhighlightedColor;
            }
        }

        public bool IsOpened() => isOpened;
        public bool IsOpeningChildPanel() {
            if (panel != null) {
                BaseMenu childMenu = panel.GetComponent<BaseMenu>();
                if (childMenu != null) {
                    return childMenu.IsOpeningChildPanel();
                }
            }
            return false;
        }
        public void OpenPanel() {
            OnSelect();
            if (panel != null) {
                isOpened = true;
                panel.SetActive(true);
                BaseMenu childMenu = panel.GetComponent<BaseMenu>();
                if (childMenu != null) {
                    childMenu.OnOpenedAsChildMenu(this);
                }
            }
        }

        public void ClosePanel() 
        {       // 인스펙터 창에서 할당한 패널이 null이 아닐 경우 함수가 호출되면 패널을 비활성화.
            isOpened = false;
            if (panel != null) 
            {
                panel.SetActive(false);
            }
        }


        protected virtual void OnSelect() {
            if (onSelect != null) {
                onSelect.Invoke();
            }
        }
    }
}