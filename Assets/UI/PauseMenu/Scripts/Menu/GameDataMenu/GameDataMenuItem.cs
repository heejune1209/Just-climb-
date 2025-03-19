using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A component used as child menu of a game data menu for either save data or load data.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(EventTrigger))]
    public class GameDataMenuItem : PreviewSubMenuItem {

        [Tooltip("The Event Trigger object used for triggering edit save data name. This should not be null.")]
        [SerializeField] private EventTrigger saveNameEventTrigger;

        [Tooltip("The Input Field object used for editing save data name. This should not be null.")]
        [SerializeField] private InputField saveNameInputField;

        public GameDataMenu.GameDataItem gameDataItem;

        protected override void Awake() {
            base.Awake();
            saveNameInputField.text = gameDataItem.saveDataName;
            saveNameInputField.gameObject.SetActive(false);

            Image.sprite = gameDataItem.image;
            nameText.text = gameDataItem.saveDataName;
            detailTexts[0].text = gameDataItem.chapterName;
            detailTexts[1].text = gameDataItem.time;
        }

        public override void Init(Color _textUnhighlightedColor, Color _textHighlightedColor, int _menuIndex, Action<int> _onClick) {
            base.Init(_textUnhighlightedColor, _textHighlightedColor, _menuIndex, _onClick);

            EventTrigger.Entry saveNameClick = new EventTrigger.Entry();
            saveNameClick.eventID = EventTriggerType.PointerClick;
            saveNameClick.callback.AddListener(_e => {
                EditName();
            });
            saveNameEventTrigger.triggers.Add(saveNameClick);

            saveNameInputField.onEndEdit.AddListener(_value => {
                EndEditName();
            });
        }

        public void EditName() {
            nameText.gameObject.SetActive(false);
            saveNameInputField.gameObject.SetActive(true);
            saveNameInputField.Select();
        }

        private void EndEditName() {
            nameText.text = saveNameInputField.text;
            nameText.gameObject.SetActive(true);
            saveNameInputField.gameObject.SetActive(false);
            gameDataItem.saveDataName = nameText.text;
        }

        public override void Highlight(bool _highlight) {
            base.Highlight(_highlight);
            if (saveNameInputField.gameObject.activeInHierarchy) {
                EndEditName();
            }
        }

        protected override void OnSelect() {
            if (saveNameInputField.gameObject.activeInHierarchy) return;
            base.OnSelect();
        }

    }
}
