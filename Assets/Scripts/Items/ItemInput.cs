using JustClimb.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemInput : MonoBehaviour
{
    [Tooltip("아이템 사용 시 대상(대개 플레이어) 게임오브젝트")]
    public GameObject player;

    void Update()
    {
        // 1번 키 → Feather
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ItemManager.Instance.UseItem("Feather", player);
        }
        // 2번 키 → Wing
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ItemManager.Instance.UseItem("Wing", player);
        }
        // 3번 키 → Lamp
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ItemManager.Instance.UseItem("Lamp", player);
        }
        // 4번 키 → Flag
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            ItemManager.Instance.UseItem("Flag", player);
        }
    }
}
