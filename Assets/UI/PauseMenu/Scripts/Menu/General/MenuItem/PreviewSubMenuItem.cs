using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// Base class for menu item that contains preview image and detailed texts.
    /// </summary>
    [RequireComponent(typeof(ScrollRectFixer))]
    public class PreviewSubMenuItem : SubMenuItem {

        [Tooltip("The image game object used for preview image. This should not be null.")]
        [SerializeField] private Image image;

        [Tooltip("Locked initial status. Image and texts will not be shown if locked.")]
        [SerializeField] private bool m_locked;

        [Tooltip("An additional object used to show locked status. This is optional.")]
        [SerializeField] private GameObject lockedStatusElement;

        [HideInInspector] [SerializeField] private ScrollRectFixer m_scrollRectFixer;
        public ScrollRectFixer scrollRectFixer => m_scrollRectFixer;

        public override void Init(Color _textUnhighlightedColor, Color _textHighlightedColor, int _menuIndex, Action<int> _onClick) {
            base.Init(_textUnhighlightedColor, _textHighlightedColor, _menuIndex, _onClick);
            locked = m_locked;
        }

        protected override void OnValidate() {
            base.OnValidate();
            if (m_scrollRectFixer == null) m_scrollRectFixer = GetComponent<ScrollRectFixer>();
        }

        public bool locked {
            get => m_locked;
            set {
                m_locked = value;
                Image.gameObject.SetActive(!m_locked);
                //nameText.gameObject.SetActive(!m_locked);
                foreach(TextMeshProUGUI text in detailTexts) {
                    text.gameObject.SetActive(!m_locked);
                }
                if (lockedStatusElement != null) {
                    lockedStatusElement.SetActive(m_locked);
                }
            }
        }

        public Image Image { get => image; set => image = value; }
    }
}
