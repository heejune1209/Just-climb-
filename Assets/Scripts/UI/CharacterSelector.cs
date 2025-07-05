using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;
using Zenject;

public class CharacterSelector : MonoBehaviour
{
    // DI 주입받을 매니저
    [Inject] private ISceneManagerEx _sceneManager;

    public GameObject[] characters; // 개체 배열
    public GameObject[] characterImages;
    public Button[] selectButtons;
    public Light[] characterLights;
    public GameObject cameraParent; // 카메라 부모
    private int selectedIndex = 0; // 선택 인덱스
    public float speed = 1.0f; // 회전 속도
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

        // 초기 버튼 설정
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
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            // Set next scene and load via SceneManager
            PlayerPrefs.SetString("nextScene", "Main");
            _sceneManager.LoadScene(Define.Scene.Loading);
        }

        // 좌우 화살표 키 입력 처리
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            characterLights[selectedIndex].enabled = false;
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = characters.Length - 1;
            t = 0; // t 초기화
        }
        // 좌우 화살표 키 입력 처리
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            characterLights[selectedIndex].enabled = false;
            selectedIndex++;
            if (selectedIndex >= characters.Length) selectedIndex = 0;
            t = 0; // t 초기화
        }
        characterLights[selectedIndex].enabled = true;
        RotateCamera();
        UpdateCharacterImages();
    }

    void RotateCamera()
    {
        // 카메라 회전 로직
        Vector3 direction = characters[selectedIndex].transform.position - cameraParent.transform.position;
        Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
        t += Time.deltaTime * speed; // t 갱신
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
            //개체 활성화/비활성화
            characterImages[i].SetActive(i == selectedIndex);
        }
    }
    /*
    public void OnSelectButtonClick(int index)
    {
        // 버튼 텍스트 초기화
        for (int i = 0; i < selectButtons.Length; i++)
        {
            selectButtons[i].GetComponentInChildren<TMP_Text>().text = "select";
            selectButtons[i].interactable = true;
            selectButtons[i].GetComponent<Image>().color = Color.white; // 버튼 초기화
        }

        //  ư "SELECT" ϰ Ȱȭ
        selectButtons[index].GetComponentInChildren<TMP_Text>().text = "SELECT";
        selectButtons[index].interactable = false;
        selectButtons[index].GetComponent<Image>().color = Color.gray; // ư ȸ 
    }
    */

    // 메모리 누수 방지
    private void OnDestroy()
    {
        // 배열 참조 해제
        characters = null;
        characterImages = null;
        selectButtons = null;
        characterLights = null;
        cameraParent = null;
        selectedButton = null;
        initialButton = null;
        keyboard = null;
        
        // 매니저 참조 해제
        _sceneManager = null;
    }
}