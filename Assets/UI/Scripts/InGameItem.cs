using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DiasGames.Abilities;
using DiasGames.Climbing;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class InGameItem : UI_Scene
{
    // 아이템 개수를 표시할 UI 텍스트 요소들
    public TMP_Text itemFeatherCount;
    public TMP_Text itemWingCount;
    public TMP_Text itemLampCount;
    public TMP_Text itemFlagCount;

    // 아이템 효과 프리팹 및 게임 오브젝트들
    public GameObject featherPrefab;  // 깃털 효과 프리팹
    public GameObject wingPrefab;     // 날개 효과 프리팹
    public GameObject lantern;        // 랜턴(램프) 오브젝트
    public GameObject flag;           // 깃발 오브젝트
    public GameObject ChangeMat;      // 재질 변경 또는 시각 효과를 담당하는 오브젝트

    // 아이템별 쿨다운 시간을 관리하는 딕셔너리
    private Dictionary<string, float> itemCooldowns = new Dictionary<string, float>();

    // 아이템 효과 지속 시간 (초 단위)
    public float itemDuration = 10f;

    // 플레이어의 위치를 참조하는 게임 오브젝트
    public GameObject playerPos;

    // 플레이어의 리스폰 위치 저장 (인스펙터에 표시되지 않음)
    [HideInInspector]
    public Vector3 playerRespawnPosition;

    // 키보드 입력 장치 참조
    private Keyboard keyboard;

    // Start 메서드: 스크립트가 시작될 때 한 번 호출됨
    void Start()
    {
        // InputSystem을 사용하여 키보드 장치를 가져옴
        keyboard = InputSystem.GetDevice<Keyboard>();
        // PlayerPrefs에 저장된 아이템 개수를 UI에 업데이트
        UpdateItemCount();
    }

    // 매 프레임 호출되는 Update 메서드
    void Update()
    {
        // 1~4 키 입력을 확인하고, 해당하는 아이템 사용 메서드 호출
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            UseItem("Feather");
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            UseItem("Wing");
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            UseItem("Lamp");
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            UseItem("Flag");
        }
    }

    // PlayerPrefs에 저장된 아이템 개수를 읽어와 UI 텍스트를 업데이트하는 메서드
    void UpdateItemCount()
    {
        itemFeatherCount.text = PlayerPrefs.GetInt("Feather", 0).ToString();
        itemWingCount.text = PlayerPrefs.GetInt("Wing", 0).ToString();
        itemLampCount.text = PlayerPrefs.GetInt("Lamp", 0).ToString();
        itemFlagCount.text = PlayerPrefs.GetInt("Flag", 0).ToString();
    }

    // 아이템 이름을 받아 사용 처리를 하는 메서드
    public void UseItem(string itemName)
    {
        // 아이템이 쿨다운 중이면 사용하지 않음
        if (itemCooldowns.ContainsKey(itemName))
        {
            float cooldownEndTime = itemCooldowns[itemName];
            if (Time.time < cooldownEndTime)
            {
                return; // 아직 쿨다운 중
            }
        }

        // PlayerPrefs에서 해당 아이템의 개수를 가져옴
        int itemCount = PlayerPrefs.GetInt(itemName, 0);

        // 아이템 개수가 0보다 클 때만 사용 가능
        if (itemCount > 0)
        {
            // 아이템 개수를 1 감소시키고 PlayerPrefs에 저장
            itemCount--;
            PlayerPrefs.SetInt(itemName, itemCount);
            PlayerPrefs.Save();

            // UI에 아이템 개수를 업데이트
            UpdateItemCount();

            // 아이템 종류에 따라 효과를 활성화
            if (itemName == "Feather")
            {
                // 깃털 효과 활성화 및 플레이어의 깃털 능력 활성화
                featherPrefab.SetActive(true);
                playerPos.GetComponent<Locomotion>().ActivateFeatherItem();
                // 지정된 지속 시간 후 깃털 효과 비활성화
                Invoke("Deactivatefeather", itemDuration);
            }
            else if (itemName == "Wing")
            {
                // 날개 효과 활성화 및 점프 부스트 효과 적용
                wingPrefab.SetActive(true);
                playerPos.GetComponent<AirControlAbility>().UseJumpBoost(1.5f, itemDuration);
                // 지정된 지속 시간 후 날개 효과 비활성화
                Invoke("Deactivatewing", itemDuration);
            }
            else if (itemName == "Lamp")
            {
                // 랜턴 활성화
                lantern.SetActive(true);
                // ChangeMat 스크립트를 통해 재질 변경 효과 적용 (이미 실행 중이 아니면)
                ChangeMat changeMatScript = ChangeMat.GetComponent<ChangeMat>();
                if (!changeMatScript.isCoroutineRunning)
                {
                    changeMatScript.Usechange();
                    // 지정된 지속 시간 후 랜턴 효과 비활성화
                    Invoke("DeactivateLamp", itemDuration);
                }
            }
            else if (itemName == "Flag")
            {
                // 플레이어 위치 근처에 깃발 인스턴스 생성
                Instantiate(flag, playerPos.transform.position + new Vector3(1.5f, 3f, 0f), Quaternion.identity);
                // 플레이어의 현재 위치를 리스폰 위치로 저장
                PlayerPrefs.SetFloat("PlayerRespawnX", playerPos.transform.position.x);
                PlayerPrefs.SetFloat("PlayerRespawnY", playerPos.transform.position.y);
                PlayerPrefs.SetFloat("PlayerRespawnZ", playerPos.transform.position.z);
                PlayerPrefs.Save();
                Debug.Log("리스폰 위치 설정 완료");
            }

            // 모든 아이템은 사용 후 10초 동안 쿨다운을 적용
            float cooldownDuration = 10f;
            float cooldownEndTime = Time.time + cooldownDuration;
            itemCooldowns[itemName] = cooldownEndTime;

            // 쿨다운 기간이 끝나면 딕셔너리에서 해당 아이템을 제거하는 코루틴 시작
            StartCoroutine(StartItemCooldown(itemName, cooldownDuration));
        }
    }

    // 아이템의 쿨다운 기간을 관리하는 코루틴
    private IEnumerator StartItemCooldown(string itemName, float cooldownDuration)
    {
        yield return new WaitForSeconds(cooldownDuration);
        itemCooldowns.Remove(itemName);
    }

    // 날개 효과를 비활성화하는 메서드
    void Deactivatewing()
    {
        wingPrefab.SetActive(false);
    }

    // 깃털 효과를 비활성화하는 메서드
    void Deactivatefeather()
    {
        featherPrefab.SetActive(false);
    }

    // 랜턴(램프) 효과를 비활성화하는 메서드
    void DeactivateLamp()
    {
        lantern.SetActive(false);
    }
}