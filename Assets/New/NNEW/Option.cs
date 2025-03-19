using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class Option : MonoBehaviour
{
    public GameObject keyTab; 
    public GameObject itemTab; 
    public Button keyButton;
    public Button itemButton; 
    public Sprite keyButtonSelectedImage;
    public Sprite itemButtonSelectedImage;
    public Sprite defaultButtonImage;
    public GameObject SpeedWG;
    public Color selectedTextColor; 
    public Color defaultTextColor;

    private Keyboard keyboard;

    private void Start()
    {
        keyboard = InputSystem.GetDevice<Keyboard>();
        ShowKeyTab();
    }

    private void Update()
    {
        

        if (SpeedWG.activeSelf)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                ShowKeyTab();
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                ShowItemTab();
            }
        }
    }

    public void ShowKeyTab()
    {
        keyTab.SetActive(true);
        itemTab.SetActive(false);

        keyButton.GetComponent<Image>().sprite = keyButtonSelectedImage;
        keyButton.GetComponentInChildren<Text>().color = selectedTextColor; 
        itemButton.GetComponent<Image>().sprite = defaultButtonImage;
        itemButton.GetComponentInChildren<Text>().color = defaultTextColor; 
    }

    public void ShowItemTab()
    {
        keyTab.SetActive(false);
        itemTab.SetActive(true);

        keyButton.GetComponent<Image>().sprite = defaultButtonImage;
        keyButton.GetComponentInChildren<Text>().color = defaultTextColor;
        itemButton.GetComponent<Image>().sprite = itemButtonSelectedImage;
        itemButton.GetComponentInChildren<Text>().color = selectedTextColor; // 선택된 버튼의 글씨 색 변경
    }
}
