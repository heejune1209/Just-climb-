using UnityEngine;

namespace DiasGames.Abilities
{
    public class CharacterActions
    {
        public Vector2 move = Vector2.zero;

        public bool jump = false;
        public bool walk = false;
        public bool roll = false;
        public bool crouch = false;
        public bool drop = false;
        public bool crawl = false;
        public bool interact = false;
        public bool suicide = false;
        // weapon actions
        public bool zoom = false;
        public bool fire = false;
        public bool reload = false;
        public bool toggle = false;
        public float switchWeapon = 0;

        //// ===== Selection Mode 관련 추가 필드 =====

        //public bool selectMode = false;      // 후보 선택 모드 활성화 여부
        //public KeyCode selectionKey;         // 진입시 사용된 방향키(W/A/S/D) 저장

        //public bool selectNext = false;      // 후보 순환 - 다음(→) 입력
        //public bool selectPrev = false;      // 후보 순환 - 이전(←) 입력

        //public bool confirmSelect = false;   // 선택 확정 (처음 진입한 방향키 재입력)
    }
}