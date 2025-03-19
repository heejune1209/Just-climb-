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
    public GameObject[] characters; // 캐릭터 배열
    public GameObject[] characterImages;
    public Button[] selectButtons;
    public Light[] characterLights;
    public GameObject cameraParent; // 카메라의 부모 오브젝트
    private int selectedIndex = 0; // 현재 선택된 캐릭터의 인덱스
    public float speed = 1.0f; // 카메라 회전 속도
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

        // 초기 버튼을 'SELECT'로 설정하고 비활성화
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

        //왼쪽 화살표 키가 눌리면 선택된 캐릭터를 왼쪽으로 이동
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            characterLights[selectedIndex].enabled = false;
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = characters.Length - 1;
            t = 0; // t 값을 초기화
        }
        //오른쪽 화살표 키가 눌리면 선택된 캐릭터를 오른쪽으로 이동
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            characterLights[selectedIndex].enabled = false;
            selectedIndex++;
            if (selectedIndex >= characters.Length) selectedIndex = 0;
            t = 0; // t 값을 초기화
        }
        characterLights[selectedIndex].enabled = true;
        RotateCamera();
        UpdateCharacterImages();
    }

    void RotateCamera()
    {
        // 카메라를 선택된 캐릭터를 향하도록 회전
        Vector3 direction = characters[selectedIndex].transform.position - cameraParent.transform.position;
        Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
        t += Time.deltaTime * speed; // t 값을 누적
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
            //선택된 캐릭터의 이미지만 활성화하고, 나머지는 비활성화
            characterImages[i].SetActive(i == selectedIndex);
        }
    }
    /*
    public void OnSelectButtonClick(int index)
    {
        //모든 버튼을 "select"로 변경하고 활성화
        for (int i = 0; i < selectButtons.Length; i++)
        {
            selectButtons[i].GetComponentInChildren<TMP_Text>().text = "select";
            selectButtons[i].interactable = true;
            selectButtons[i].GetComponent<Image>().color = Color.white; // 버튼을 흰색으로 변경
        }

        // 누른 버튼을 "SELECT"로 변경하고 비활성화
        selectButtons[index].GetComponentInChildren<TMP_Text>().text = "SELECT";
        selectButtons[index].interactable = false;
        selectButtons[index].GetComponent<Image>().color = Color.gray; // 버튼을 회색으로 변경
    }
    */
}