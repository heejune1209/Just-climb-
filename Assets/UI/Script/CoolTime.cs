using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoolTime : MonoBehaviour
{
    public Image[] itemImages; // �� �������� ��Ÿ���� �̹��� UI �迭
    public TMP_Text[] cooldownTexts; // �� �������� ��Ÿ���� ��Ÿ���� �ؽ�Ʈ UI �迭

    public float cooldownTime = 10.0f; // ������ ��Ÿ�� (�� ����)
    private float[] nextTimeToUse; // ���� ������ ��� ������ �ð� �迭

    void Start()
    {
        nextTimeToUse = new float[itemImages.Length]; // ������ ���� �°� �迭 �ʱ�ȭ
        for (int i = 0; i < nextTimeToUse.Length; i++)
        {
            nextTimeToUse[i] = 0.0f; // �ʱⰪ ����
        }
    }
    

    // UI ������Ʈ �޼���
    public void UpdateUI()
    {
        for (int i = 0; i < itemImages.Length; i++)
        {
            float remainingTime = Mathf.Max(0, nextTimeToUse[i] - Time.time);
            float fillAmount = remainingTime / cooldownTime;
            cooldownTexts[i].text = remainingTime.ToString("0.0"); // �ؽ�Ʈ ������Ʈ
            itemImages[i].fillAmount = 1 - fillAmount; // �̹��� UI ������Ʈ
        }
    }
}