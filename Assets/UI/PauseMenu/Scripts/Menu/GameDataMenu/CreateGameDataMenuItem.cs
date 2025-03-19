using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Calcatz.JungleThemeGUI {
    /// <summary>
    /// Sub menu item for Game Data menu to create a new game data. A game data menu should not contain multiple menu item of this type.
    /// </summary>
    public class CreateGameDataMenuItem : SubMenuItem {

        [System.Serializable]
        public class GameDataEvent : UnityEvent<SubMenuItem> { }

        [SerializeField] private GameObject gameDataItemPrototype;

        public GameDataEvent onCreateGameData;

        protected override void OnSelect() {
            base.OnSelect();
            GameObject gameDataGO = Instantiate(gameDataItemPrototype);
            gameDataGO.transform.SetParent(transform.parent, false);
            gameDataGO.transform.SetSiblingIndex(1);
            onCreateGameData.Invoke(gameDataGO.GetComponent<SubMenuItem>());
        }

    }
}
