using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class StartLogo : MonoBehaviour
{
    public Image panel; // 검은 패널
    public Image img; // 이미지
    private InputAction skipAction;
    private bool skipLogo = false;
    private static bool gameStarted = false;

    private void Start()
    {
        StartCoroutine(StartGame());
    }

    private void Awake()
    {
        skipAction = new InputAction(type: InputActionType.PassThrough, binding: "<Keyboard>/anyKey");
        skipAction.performed += _ => SkipLogo();
        skipAction.Enable();
    }

    private void OnDestroy()
    {
        skipAction.performed -= _ => SkipLogo();
        skipAction.Disable();
    }

    private void SkipLogo()
    {
        skipLogo = true;
    }

    IEnumerator StartGame()
    {
        if (!gameStarted)
        {
            panel.gameObject.SetActive(true);

            gameStarted = true;

            try
            {
                while (img.color.a < 1f)
                {
                    if (skipLogo)
                        break;

                    Color newColor = img.color;
                    newColor.a += Time.deltaTime * 0.5f;
                    img.color = newColor;
                    yield return null;
                }

                while (img.color.a > 0f)
                {
                    if (skipLogo)
                        break;

                    Color newColor = img.color;
                    newColor.a -= Time.deltaTime * 0.5f;
                    img.color = newColor;
                    yield return null;
                }
            }
            finally
            {
                panel.gameObject.SetActive(false);
            }
        }
    }
}