using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainText: MonoBehaviour
{ 

    public TMP_Text textComponent; // 크기를 변경할 텍스트 컴포넌트
    public float minSize = 10f; // 텍스트 크기의 최솟값
    public float maxSize = 20f; // 텍스트 크기의 최댓값
     public float speed = 1f; // 크기 변경 속도

void Start()
    {
    StartCoroutine(ResizeText());

}

IEnumerator ResizeText()
{
    while (true)
    {
        // 텍스트 크기를 점차 바꾸기
        float size = Mathf.PingPong(Time.time * speed, maxSize - minSize) + minSize;
        textComponent.fontSize = size;

        yield return null;
    }
}
}
