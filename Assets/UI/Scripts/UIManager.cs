using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/*
public class UIManager : MonoBehaviour
{
    bool Closebtndown;
    public GameObject SettingsUI;
    private Keyboard keyboard;

    void Start()
    {
        keyboard = InputSystem.GetDevice<Keyboard>(); 
    }

    void Update()
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            OncloseSettings();
        }
    }


    void OncloseSettings()     // ESC ��ư �������� ����â�� ������ ��� 
    {
        Closebtndown = keyboard.escapeKey.wasPressedThisFrame; 
        if (Closebtndown == true && SettingsUI.activeInHierarchy == true)  // esc��ư�� ���������� �� ui�� Ȱ��ȭ �Ǿ��������� ����
        {
            SettingsUI settingsUI = FindObjectOfType<SettingsUI>();
            settingsUI.CloseSettingsUI();
        }

    }
}
*/
