using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// Base class a menu that contains a previewer that previews current selected item.
    /// </summary>
    public class ItemPreviewer : MonoBehaviour {

        [SerializeField] private Image[] previewImages;
        [SerializeField] private Text[] previewTexts;

        /// <summary>
        /// Set the sprites of the preview image game objects.
        /// </summary>
        /// <param name="_images"></param>
        public void SetImages(params Sprite[] _images) {
            for (int i = 0; i<_images.Length; i++) {
                if (i<previewImages.Length) {
                    previewImages[i].sprite = _images[i];
                }
            }
        }

        /// <summary>
        /// Set the texts of the preview text game objects.
        /// </summary>
        /// <param name="_texts"></param>
        public void SetTexts(params string[] _texts) {
            for (int i = 0; i < _texts.Length; i++) {
                if (i < previewTexts.Length) {
                    previewTexts[i].text = _texts[i];
                }
            }
        }

    }
}
