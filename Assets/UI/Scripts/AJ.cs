using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class AJ : MonoBehaviour
{
    public TMP_Text targetText;
    public TMP_Text targetText2; 
    public TMP_Text targetText3;
    public TMP_Text targetText4;
    public TMP_Text targetText5;
    public TMP_Text targetText6;
    public TMP_Text targetText7;
    public TMP_Text targetText8;
    public TMP_Text targetText9;
    public GameObject[] objects;
    private Keyboard keyboard;

    void Start()
    {
        keyboard = InputSystem.GetDevice<Keyboard>();
    }

    void Update()
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            PlayerPrefs.SetString("nextScene", "MainMenu");
            SceneManager.LoadScene("LoadingScene");
        }
    }

    public void ChangeText1(string newText)
    {
        targetText.text = newText;
    }

    public void ChangeText2(string newText)
    {
        targetText2.text = newText;
    }

    public void ChangeText3(string newText)
    {
        targetText3.text = newText;
    }

    public void ChangeText4(string newText)
    {
        targetText4.text = newText;
    }

    public void ChangeText5(string newText)
    {
        targetText5.text = newText;
    }

    public void ChangeText6(string newText)
    {
        targetText6.text = newText;
    }

    public void ChangeText7(string newText)
    {
        targetText7.text = newText;
    }

    public void ChangeText8(string newText)
    {
        targetText8.text = newText;
    }

    public void ChangeText9(string newText)
    {
        targetText9.text = newText;
    }

    public void ActivateObject(int index)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(i == index);
        }
    }
}
