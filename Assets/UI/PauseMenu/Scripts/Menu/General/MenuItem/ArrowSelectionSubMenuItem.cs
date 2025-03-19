using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A menu item that contains arrow selection to change certain value.
    /// </summary>
    public class ArrowSelectionSubMenuItem : SubMenuItem {

        [System.Serializable]
        public class ArrowSelectionEvent : UnityEvent<int> { }

        [Tooltip("The text game object used to show the value. This should not be null.")]
        [SerializeField] private TextMeshProUGUI valueText;

        [SerializeField] private Image leftArrow;
        [SerializeField] private EventTrigger leftArrowTrigger;
        [SerializeField] private Image rightArrow;
        [SerializeField] private EventTrigger rightArrowTrigger;
        [SerializeField] private bool disableRotarySelection = false;

        [SerializeField] private List<string> options = new List<string>();

        public ArrowSelectionEvent onValueChanged;

        private int currentValueIndex = 0;

        protected override void Awake() {
            base.Awake();
        }

        public override void Init(Color _textUnhighlightedColor, Color _textHighlightedColor, int _menuIndex, Action<int> _onClick) {
            base.Init(_textUnhighlightedColor, _textHighlightedColor, _menuIndex, _onClick);

            EventTrigger.Entry leftArrowClick = new EventTrigger.Entry();
            leftArrowClick.eventID = EventTriggerType.PointerClick;
            leftArrowClick.callback.AddListener(_e => {
                DecreaseValue();
            });
            leftArrowTrigger.triggers.Add(leftArrowClick);

            EventTrigger.Entry rightArrowClick = new EventTrigger.Entry();
            rightArrowClick.eventID = EventTriggerType.PointerClick;
            rightArrowClick.callback.AddListener(_e => {
                IncreaseValue();
            });
            rightArrowTrigger.triggers.Add(rightArrowClick);

        }

        public int value {
            get => currentValueIndex;
            set {
                currentValueIndex = value;
                valueText.text = options[currentValueIndex];
                onValueChanged.Invoke(currentValueIndex);
            }
        }

        public override void Highlight(bool _highlight) {
            base.Highlight(_highlight);
            leftArrow.gameObject.SetActive(_highlight);
            rightArrow.gameObject.SetActive(_highlight);
        }

        public void DecreaseValue() {
            int newIndex = value - 1;
            if (newIndex < 0) newIndex = disableRotarySelection? 0 : options.Count - 1;
            value = newIndex;
        }

        public void IncreaseValue() {
            int newIndex = value + 1;
            if (newIndex >= options.Count) newIndex = disableRotarySelection? options.Count-1 : 0;
            value = newIndex;
        }
    }
}
