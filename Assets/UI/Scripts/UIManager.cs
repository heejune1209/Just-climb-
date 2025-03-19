using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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


    void OncloseSettings()     // ESC 버튼 눌렀을때 설정창이 꺼지는 기능 
    {
        Closebtndown = keyboard.escapeKey.wasPressedThisFrame; 
        if (Closebtndown == true && SettingsUI.activeInHierarchy == true)  // esc버튼을 눌렀을때와 이 ui가 활성화 되어있을때만 실행
        {
            SettingsUI settingsUI = FindObjectOfType<SettingsUI>();
            settingsUI.CloseSettingsUI();
        }

    }
}
