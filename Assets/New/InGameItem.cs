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

public class InGameItem : MonoBehaviour
{
    public TMP_Text itemFeatherCount;
    public TMP_Text itemWingCount;
    public TMP_Text itemLampCount;
    public TMP_Text itemFlagCount;

    public GameObject featherPrefab; // ������ ����
    public GameObject wingPrefab; // �ٸ� ������ ������
    public GameObject lantern;
    public GameObject flag;
    public GameObject ChangeMat;
    private Dictionary<string, float> itemCooldowns = new Dictionary<string, float>();
    public float itemDuration = 10f; // ������ ���ӽð�
    //public CoolTime coolTime;

    public GameObject playerPos;
    [HideInInspector]
    public Vector3 playerRespawnPosition; // ����� ����� ��ġ ����� ����
    private Keyboard keyboard;

    // Start is called before the first frame update
    void Start()
    {
        keyboard = InputSystem.GetDevice<Keyboard>();
        UpdateItemCount();

    }

    // Update is called once per frame
    void Update()
    {
            
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

    void UpdateItemCount()
    {
        itemFeatherCount.text = PlayerPrefs.GetInt("Feather", 0).ToString();
        itemWingCount.text = PlayerPrefs.GetInt("Wing", 0).ToString();
        itemLampCount.text = PlayerPrefs.GetInt("Lamp", 0).ToString();
        itemFlagCount.text = PlayerPrefs.GetInt("Flag", 0).ToString();
    }

    public void UseItem(string itemName)
    {
        if (itemCooldowns.ContainsKey(itemName))
        {
            float cooldownEndTime = itemCooldowns[itemName];
            if (Time.time < cooldownEndTime)
            {
                return;
            }
        }
        int itemCount = PlayerPrefs.GetInt(itemName, 0);

        if (itemCount > 0)
        {
            // ������ ȿ��
            itemCount--;
            PlayerPrefs.SetInt(itemName, itemCount);
            PlayerPrefs.Save();

            UpdateItemCount();

            // ������ ������ ����
            if (itemName == "Feather")
            {
                featherPrefab.SetActive(true);
                playerPos.GetComponent<Locomotion>().ActivateFeatherItem();

                Invoke("Deactivatefeather", itemDuration);
            }
            else if (itemName == "Wing")
            {
                wingPrefab.SetActive(true);
                playerPos.GetComponent<AirControlAbility>().UseJumpBoost(1.5f, itemDuration);

                Invoke("Deactivatewing", itemDuration);
            }
            else if (itemName == "Lamp")
            {
                lantern.SetActive(true);
                ChangeMat _changeMat = ChangeMat.GetComponent<ChangeMat>();
                if (!_changeMat.isCoroutineRunning)
                {
                    _changeMat.Usechange();

                    Invoke("DeactivateLamp", itemDuration);
                }
            }
            else if (itemName == "Flag")
            {
                Instantiate(flag, playerPos.transform.position + new Vector3(1.5f, 3f, 0f), Quaternion.identity);
                PlayerPrefs.SetFloat("PlayerRespawnX", playerPos.transform.position.x);
                PlayerPrefs.SetFloat("PlayerRespawnY", playerPos.transform.position.y);
                PlayerPrefs.SetFloat("PlayerRespawnZ", playerPos.transform.position.z);
                PlayerPrefs.Save();
                Debug.Log("��ǥ ����Ϸ�");
            }
            float cooldownDuration = 10f;
            float cooldownEndTime = Time.time + cooldownDuration;
            itemCooldowns[itemName] = cooldownEndTime;

            StartCoroutine(StartItemCooldown(itemName, cooldownDuration));

        }
    }
    private IEnumerator StartItemCooldown(string itemName, float cooldownDuration)
    {
        yield return new WaitForSeconds(cooldownDuration);

        itemCooldowns.Remove(itemName);
    }


    void Deactivatewing()
    {
        // ���� ������Ʈ�� ��Ȱ��ȭ�մϴ�.
        wingPrefab.SetActive(false);
    }
    void Deactivatefeather()
    {
        // ���� ������Ʈ�� ��Ȱ��ȭ�մϴ�.
        featherPrefab.SetActive(false);
    }
    void DeactivateLamp()
    {
        // ���� ������Ʈ�� ��Ȱ��ȭ�մϴ�.
        lantern.SetActive(false);
        
    }
    

}
