using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextColorChange : MonoBehaviour
{
    public TextMeshProUGUI buttonText;
    public Color originalColor;
    public Color changeColor;
    private float colorChangeDuration = 0.1f;

    

    void Start()
    {
        // 버튼의 Text 컴포넌트 찾기
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        // 현재 색상 저장
        originalColor = buttonText.color;

        
    }

    public void OnClickChangeColor()
    {
        // 클릭 시 색상 변경
        buttonText.color = changeColor;

        // 일정 시간 후에 원래 색상으로 돌리기
        Invoke("RestoreOriginalColor", colorChangeDuration);

    }
    

    void RestoreOriginalColor()
    {
        // 원래 색상으로 돌리기
        buttonText.color = originalColor;        
    }
}



