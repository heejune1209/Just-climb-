using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JustClimb.Items
{
    public interface IItemUse
    {
        // 아이템을 사용했을 때 실행될 로직
        /// <param name="user">아이템을 사용하는 주체(게임 오브젝트)</param>
        void Use(GameObject user);
    }
}

