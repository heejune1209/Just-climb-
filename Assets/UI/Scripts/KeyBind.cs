using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KeyBind : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private TextMeshProUGUI buttonLbi1;
    [SerializeField] private TextMeshProUGUI buttonLbi2;
    [SerializeField] private TextMeshProUGUI buttonLbi3;
    [SerializeField] private TextMeshProUGUI buttonLbi4;
    [SerializeField] private TextMeshProUGUI buttonLbi5;
    [SerializeField] private TextMeshProUGUI buttonLbi6;

    private Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>()
    {
        { "CustomKey1", KeyCode.Z },
        { "CustomKey2", KeyCode.X },
        { "CustomKey3", KeyCode.LeftArrow },
        { "CustomKey4", KeyCode.RightArrow },
        { "CustomKey5", KeyCode.UpArrow },
        { "CustomKey6", KeyCode.DownArrow }
    };

    private void Start()
    {
        buttonLbi1.text = GetKeyBindingText("CustomKey1");
        buttonLbi2.text = GetKeyBindingText("CustomKey2");
        buttonLbi3.text = GetKeyBindingText("CustomKey3");
        buttonLbi4.text = GetKeyBindingText("CustomKey4");
        buttonLbi5.text = GetKeyBindingText("CustomKey5");
        buttonLbi6.text = GetKeyBindingText("CustomKey6");
    }

    private void Update()
    {
        foreach (KeyValuePair<string, KeyCode> keyBinding in keyBindings)
        {
            if (IsAwaitingInput(keyBinding.Key))
            {
                foreach (KeyCode keycode in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKey(keycode))
                    {
                        SetKeyBinding(keyBinding.Key, keycode);
                    }
                }
            }
        }
    }

    private string GetKeyCodeText(KeyCode keyCode)
    {
        string buttonTextString = "";
        if (keyCode == KeyCode.UpArrow)
        {
            buttonTextString = "Up";
        }
        else if (keyCode == KeyCode.DownArrow)
        {
            buttonTextString = "Down";
        }
        else if (keyCode == KeyCode.LeftArrow)
        {
            buttonTextString = "Left";
        }
        else if (keyCode == KeyCode.RightArrow)
        {
            buttonTextString = "Right";
        }
        else
        {
            buttonTextString = keyCode.ToString();
        }
        return buttonTextString;
    }

 public void ChangeKey(string keyBindingName)
    {
        if (keyBindings.ContainsKey(keyBindingName))
        {
            SetAwaitingInput(keyBindingName);
        }
        else
        {
            Debug.LogError("Invalid key binding name: " + keyBindingName);
        }
    }
    
    private string GetKeyBindingText(string keyBindingName)
    {
        if (PlayerPrefs.HasKey(keyBindingName))
        {
            KeyCode keyCode = (KeyCode)PlayerPrefs.GetInt(keyBindingName);
            return GetKeyCodeText(keyCode);
        }
        else
        {
            return GetKeyCodeText(keyBindings[keyBindingName]);
        }
    }

    private bool IsAwaitingInput(string keyBindingName)
    {
        return GetKeyBindingText(keyBindingName) == "Awaiting Input";
    }

    private void SetAwaitingInput(string keyBindingName)
    {
        PlayerPrefs.DeleteKey(keyBindingName);
        PlayerPrefs.Save();
        SetKeyBinding(keyBindingName, KeyCode.None);
    }

    private void SetKeyBinding(string keyBindingName, KeyCode keyCode)
{
keyBindings[keyBindingName] = keyCode;
PlayerPrefs.SetInt(keyBindingName, (int)keyCode);
PlayerPrefs.Save();
UpdateButtonText(keyBindingName);
}

private void UpdateButtonText(string keyBindingName)
{
    if (keyBindingName == "CustomKey1")
    {
        buttonLbi1.text = GetKeyBindingText(keyBindingName);
    }
    else if (keyBindingName == "CustomKey2")
    {
        buttonLbi2.text = GetKeyBindingText(keyBindingName);
    }
    else if (keyBindingName == "CustomKey3")
    {
        buttonLbi3.text = GetKeyBindingText(keyBindingName);
    }
    else if (keyBindingName == "CustomKey4")
    {
        buttonLbi4.text = GetKeyBindingText(keyBindingName);
    }
    else if (keyBindingName == "CustomKey5")
    {
        buttonLbi5.text = GetKeyBindingText(keyBindingName);
    }
    else if (keyBindingName == "CustomKey6")
    {
        buttonLbi6.text = GetKeyBindingText(keyBindingName);
    }
}
}