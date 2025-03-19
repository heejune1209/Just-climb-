using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A component added in root gameobject of glossary menu.
    /// </summary>
    public class Glossary : ItemPreviewer {

        [SerializeField] private ScrollRect scrollView;

        private bool initialized = false;

        private List<GlossarySubMenuItem> menuItems;
        private GameObject lastSelectedGameObject;

        private void OnEnable() {
            StartCoroutine(SkipFrame(delegate {
                Button firstItemButton = GetComponentsInChildren<Button>()[0];
                EventSystem.current.SetSelectedGameObject(null);
                firstItemButton.Select();
                lastSelectedGameObject = firstItemButton.gameObject;
            }));

            if (!initialized) {
                initialized = true;
                menuItems = GetComponentsInChildren<GlossarySubMenuItem>().ToList();
                for(int i=0; i<menuItems.Count; i++) {
                    GlossarySubMenuItem menuItem = menuItems[i];

                    menuItem.locked = menuItem.locked;

                    ScrollRectFixer[] scrollRectFixers = menuItem.GetComponentsInChildren<ScrollRectFixer>();
                    foreach(ScrollRectFixer fixer in scrollRectFixers) {
                        fixer.MainScroll = scrollView;
                    }

                    Button btn = menuItem.GetComponentInChildren<Button>();
                    if (btn != null) {
                        btn.onClick.AddListener(delegate {
                            if (!menuItem.locked) {
                                SetImages(menuItem.itemSprite);
                                SetTexts(menuItem.itemName, menuItem.itemDescription);
                            }
                        });
                    }
                }
            }

        }

        private IEnumerator SkipFrame(System.Action _action) {
            yield return null;
            yield return null;
            _action.Invoke();
        }

        private void Update() {

            if (Input.GetButtonDown("Horizontal")) {
                MaintainFocus();
            }
            if (Input.GetButtonDown("Vertical")) {
                MaintainFocus();
            }
        }

        private void MaintainFocus() {
            if (EventSystem.current.currentSelectedGameObject == null) {
                EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
            }
            else {
                GlossarySubMenuItem menuItem = EventSystem.current.currentSelectedGameObject.GetComponent<GlossarySubMenuItem>();
                Button button = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
                if (menuItem != null && menuItem.locked) {
                    EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                }
                else if (button == null) {
                    EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                }
                else {
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                }
            }
            lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
        }
    }
}