using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainText: MonoBehaviour
{ 

    public TMP_Text textComponent; // ũ�⸦ ������ �ؽ�Ʈ ������Ʈ
    public float minSize = 10f; // �ؽ�Ʈ ũ���� �ּڰ�
    public float maxSize = 20f; // �ؽ�Ʈ ũ���� �ִ�
     public float speed = 1f; // ũ�� ���� �ӵ�

void Start()
    {
    StartCoroutine(ResizeText());

}

IEnumerator ResizeText()
{
    while (true)
    {
        // �ؽ�Ʈ ũ�⸦ ���� �ٲٱ�
        float size = Mathf.PingPong(Time.time * speed, maxSize - minSize) + minSize;
        textComponent.fontSize = size;

        yield return null;
    }
}
}
