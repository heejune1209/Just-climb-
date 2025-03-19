using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoolTime : MonoBehaviour
{
    public Image[] itemImages; // 각 아이템을 나타내는 이미지 UI 배열
    public TMP_Text[] cooldownTexts; // 각 아이템의 쿨타임을 나타내는 텍스트 UI 배열

    public float cooldownTime = 10.0f; // 아이템 쿨타임 (초 단위)
    private float[] nextTimeToUse; // 다음 아이템 사용 가능한 시간 배열

    void Start()
    {
        nextTimeToUse = new float[itemImages.Length]; // 아이템 수에 맞게 배열 초기화
        for (int i = 0; i < nextTimeToUse.Length; i++)
        {
            nextTimeToUse[i] = 0.0f; // 초기값 설정
        }
    }
    

    // UI 업데이트 메서드
    public void UpdateUI()
    {
        for (int i = 0; i < itemImages.Length; i++)
        {
            float remainingTime = Mathf.Max(0, nextTimeToUse[i] - Time.time);
            float fillAmount = remainingTime / cooldownTime;
            cooldownTexts[i].text = remainingTime.ToString("0.0"); // 텍스트 업데이트
            itemImages[i].fillAmount = 1 - fillAmount; // 이미지 UI 업데이트
        }
    }
}