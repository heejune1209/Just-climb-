using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// A component added in root gameobject of game data menu, either a load data menu or save data menu.
    /// </summary>
    public class GameDataMenu : ScrollableListMenu {

        [System.Serializable]
        public class GameDataItem {
            public Sprite image;
            public string saveDataName;
            public string chapterName;
            public string time;
            public string difficulty;
            public string progress;
            public string achievements;

            public string[] texts => new string[] {
                saveDataName, chapterName, time, difficulty, progress, achievements
            };
        }

        [System.Serializable]
        public class GameDataItemEvent : UnityEvent<GameDataItem> { }

        public GameDataItemEvent onSelectGameData;

        public override void SelectMenu() {
            base.SelectMenu();
            if (currentMenuIndex >= 0 && currentMenuIndex < menuItems.Count) {
                GameDataMenuItem gameDataMenuItem = menuItems[currentMenuIndex] as GameDataMenuItem;
                if (gameDataMenuItem != null) {
                    onSelectGameData.Invoke(gameDataMenuItem.gameDataItem);
                }
            }
        }

        /// <summary>
        /// Called in menu item, assigned from inspector.
        /// </summary>
        /// <param name="_dataIndex"></param>
        public void LoadGame(int _dataIndex) {
            Debug.Log("Placeholder: Load game data #" + _dataIndex);
        }

        /// <summary>
        /// Called in create game data menu item, assigned from inspector.
        /// </summary>
        /// <param name="_gameDataMenuItem"></param>
        public void AddGameData(SubMenuItem _gameDataMenuItem) {
            menuItems.Insert(1, _gameDataMenuItem);
            RefreshMenuItemCallbacks();
            HighlightMenu(1);
            menuItems[1].GetComponent<GameDataMenuItem>().EditName();
        }

        public void ShowGameDataItemPreview(GameDataItem _gameDataItem) {
            ItemPreviewer itemPreviewer = GetComponent<ItemPreviewer>();
            if (itemPreviewer != null) {
                itemPreviewer.SetImages(_gameDataItem.image);
                itemPreviewer.SetTexts(_gameDataItem.texts);
            }
        }

    }
}
