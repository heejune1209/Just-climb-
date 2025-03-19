using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// Sub menu item for Glossary menu.
    /// </summary>
    public class GlossarySubMenuItem : PreviewSubMenuItem {

        [Header("Glossary Item Data")]
        [SerializeField] private string m_itemName;
        [SerializeField] private Sprite m_itemSprite;
        [TextArea(3, 10)]
        [SerializeField] private string m_itemDescription;

        public string itemName { get => m_itemName; set => m_itemName = value; }
        public string itemDescription { get => m_itemDescription; set => m_itemDescription = value; }
        public Sprite itemSprite { get => m_itemSprite; set => m_itemSprite = value; }
    }
}
