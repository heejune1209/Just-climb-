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
        // ��ư�� Text ������Ʈ ã��
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        // ���� ���� ����
        originalColor = buttonText.color;

        
    }

    public void OnClickChangeColor()
    {
        // Ŭ�� �� ���� ����
        buttonText.color = changeColor;

        // ���� �ð� �Ŀ� ���� �������� ������
        Invoke("RestoreOriginalColor", colorChangeDuration);

    }
    

    void RestoreOriginalColor()
    {
        // ���� �������� ������
        buttonText.color = originalColor;        
    }
}



