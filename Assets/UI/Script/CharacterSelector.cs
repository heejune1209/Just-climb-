using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    public GameObject[] characters; // ĳ���� �迭
    public GameObject[] characterImages;
    public Button[] selectButtons;
    public Light[] characterLights;
    public GameObject cameraParent; // ī�޶��� �θ� ������Ʈ
    private int selectedIndex = 0; // ���� ���õ� ĳ������ �ε���
    public float speed = 1.0f; // ī�޶� ȸ�� �ӵ�
    private float t = 0.0f;
    public Button selectedButton;
    public Button initialButton;

    private Keyboard keyboard;

    void Start()
    {
        RotateCamera();
        keyboard = InputSystem.GetDevice<Keyboard>();

         foreach (Button button in selectButtons)
        {
            button.GetComponentInChildren<TMP_Text>().text = "select";
            button.interactable = true;
            button.GetComponent<Image>().color = Color.white;
        }

        // �ʱ� ��ư�� 'SELECT'�� �����ϰ� ��Ȱ��ȭ
        if (initialButton != null)
        {
            initialButton.GetComponentInChildren<TMP_Text>().text = "SELECT";
            initialButton.interactable = false;
            initialButton.GetComponent<Image>().color = Color.gray;
        }

        foreach (Light light in characterLights)
        {
            light.enabled = false;
        }

        characterLights[selectedIndex].enabled = true;
    }

    void Update()
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            PlayerPrefs.SetString("nextScene", "MainMenu");
            SceneManager.LoadScene("LoadingScene");
        }

        //���� ȭ��ǥ Ű�� ������ ���õ� ĳ���͸� �������� �̵�
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            characterLights[selectedIndex].enabled = false;
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = characters.Length - 1;
            t = 0; // t ���� �ʱ�ȭ
        }
        //������ ȭ��ǥ Ű�� ������ ���õ� ĳ���͸� ���������� �̵�
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            characterLights[selectedIndex].enabled = false;
            selectedIndex++;
            if (selectedIndex >= characters.Length) selectedIndex = 0;
            t = 0; // t ���� �ʱ�ȭ
        }
        characterLights[selectedIndex].enabled = true;
        RotateCamera();
        UpdateCharacterImages();
    }

    void RotateCamera()
    {
        // ī�޶� ���õ� ĳ���͸� ���ϵ��� ȸ��
        Vector3 direction = characters[selectedIndex].transform.position - cameraParent.transform.position;
        Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
        t += Time.deltaTime * speed; // t ���� ����
        cameraParent.transform.rotation = Quaternion.Slerp(cameraParent.transform.rotation, toRotation, t);
    }

    public void OnLeftButtonClick()
    {
        selectedIndex--;
        if (selectedIndex < 0) selectedIndex = characters.Length - 1;
        t = 0; 
        RotateCamera();
        UpdateCharacterImages();
    }

    public void OnRightButtonClick()
    {
        selectedIndex++;
        if (selectedIndex >= characters.Length) selectedIndex = 0;
        t = 0; 
        RotateCamera();
        UpdateCharacterImages();
    }

    void UpdateCharacterImages()
    {
        for (int i = 0; i < characterImages.Length; i++)
        {
            //���õ� ĳ������ �̹����� Ȱ��ȭ�ϰ�, �������� ��Ȱ��ȭ
            characterImages[i].SetActive(i == selectedIndex);
        }
    }
    /*
    public void OnSelectButtonClick(int index)
    {
        //��� ��ư�� "select"�� �����ϰ� Ȱ��ȭ
        for (int i = 0; i < selectButtons.Length; i++)
        {
            selectButtons[i].GetComponentInChildren<TMP_Text>().text = "select";
            selectButtons[i].interactable = true;
            selectButtons[i].GetComponent<Image>().color = Color.white; // ��ư�� ������� ����
        }

        // ���� ��ư�� "SELECT"�� �����ϰ� ��Ȱ��ȭ
        selectButtons[index].GetComponentInChildren<TMP_Text>().text = "SELECT";
        selectButtons[index].interactable = false;
        selectButtons[index].GetComponent<Image>().color = Color.gray; // ��ư�� ȸ������ ����
    }
    */
}