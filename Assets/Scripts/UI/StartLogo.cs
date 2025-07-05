using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class StartLogo : UI_Popup
{
    public Image panel; // г
    public Image img; // �̹���
    private InputAction skipAction;
    private bool skipLogo = false;
    private static bool gameStarted = false;

    public override void Init()
    {
        base.Init();
        // Start logo animation as popup
        panel.gameObject.SetActive(true);
        StartCoroutine(StartGame());
    }

    private void Awake()
    {
        skipAction = new InputAction(type: InputActionType.PassThrough, binding: "<Keyboard>/anyKey");
        skipAction.performed += _ => SkipLogo();
        skipAction.Enable();
    }

    protected override void OnDestroy()
    {
        // InputAction 정리
        if (skipAction != null)
    {
        skipAction.performed -= _ => SkipLogo();
        skipAction.Disable();
            skipAction.Dispose();
            skipAction = null;
        }
        
        // 컴포넌트 참조 해제
        panel = null;
        img = null;
        
        base.OnDestroy();
    }

    private void SkipLogo()
    {
        skipLogo = true;
    }

    IEnumerator StartGame()
    {
        if (!gameStarted)
        {
            // panel 활성화는 Init에서 처리
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