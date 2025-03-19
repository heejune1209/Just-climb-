using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ItemManager : MonoBehaviour
{
    public int gems;
    public int itemPrice;
    public int gold;
    public int gemToGoldRatio = 400;

    public TMP_Text gemText;
    public TMP_Text goldText;
    public TMP_Text[] itemCountTexts;

    public Image featherDescriptionImage;
    public Image flagDescriptionImage;
    public Image lampDescriptionImage;
    public Image wingDescriptionImage;

    public Button gemToGoldButton1;
    public Button gemToGoldButton10;

    public GameObject EmptyGoldPanel; // rh ¾øÀ»¶§ ÆÐ³Î
    public GameObject EmptyGemsPanel;

    private Keyboard keyboard;

    public class Item
    {
        public string name; 
        public int price;

        public Item(string name, int price)
        {
            this.name = name;
            this.price = price;
        }
    }

    public Item[] itemArray; 

    private Dictionary<string, Item> items; 
    private Dictionary<string, int> itemCounts;

    void Start()
    {
        PlayerPrefs.SetInt("Gem", 1000);
        keyboard = InputSystem.GetDevice<Keyboard>();
        gems = PlayerPrefs.GetInt("Gem", 0);
        gold = PlayerPrefs.GetInt("Gold", 0);
        items = new Dictionary<string, Item>(); 
        itemCounts = new Dictionary<string, int>();

        itemArray = new Item[]
   {
        new Item("Feather", 100),
        new Item("Wing", 100),
        new Item("Lamp", 200),
        new Item("Flag", 300),
   };
        foreach (Item item in itemArray)
        {
            items.Add(item.name, item);
            itemCounts[item.name] = PlayerPrefs.GetInt(item.name, 0); 
        }

        gemToGoldButton1.onClick.AddListener(() =>
        {
            if (gems >= 1)
            {
                ExchangeGemsToGold(1); // 1Áª -> 100°ñµå
                AudioManager.instance.PlaySFX(0);
            }
            else
            {
                EmptyGemsPanel.SetActive(true);
            }
        });

        gemToGoldButton10.onClick.AddListener(() =>
        {
            if (gems >= 10)
            {
                ExchangeGemsToGold(10); // 10Áª -> 1000°ñµå
                AudioManager.instance.PlaySFX(0);
            }
            else
            {
                EmptyGemsPanel.SetActive(true); 
            }
        });

        UpdateUI();
    }

    void Update()
    {
        gems = PlayerPrefs.GetInt("Gem", 0);
        UpdateUI();

        
    }

    public void BuyItem(string itemName)
    {
        Item item = items[itemName];

        if (gold >= item.price)
        {
            gold -= item.price;
            PlayerPrefs.SetInt("Gold", gold);
            PlayerPrefs.Save();

            if (itemCounts.ContainsKey(itemName))
            {
                itemCounts[itemName]++;
            }
            else
            {
                itemCounts.Add(itemName, 1);
            }

            PlayerPrefs.SetInt(itemName, itemCounts[itemName]);
            PlayerPrefs.Save();

            UpdateUI();
        }
        else
        {
            EmptyGoldPanel.SetActive(true); // °ñµå°¡ ºÎÁ·ÇÏ¸é ÆÐ³ÎÀ» È°¼ºÈ­
        }
    }


    void UpdateUI()
    {
        gemText.text = ": " + gems;
        goldText.text = ": " + gold; 

        foreach (var item in items)
        {
            for (int i = 0; i < itemCountTexts.Length; i++)
            {
                //  °¹¼ö Ç¥½Ã
                if (itemCountTexts[i].name == item.Key)
                {
                    itemCountTexts[i].text = (itemCounts.ContainsKey(item.Key) ? itemCounts[item.Key].ToString() : "0");
                    break;
                }
            }
        }
    }

    public void ExchangeGemsToGold(int gemAmount)
    {
        // Áª °³¼ö°¡ ÃæºÐÇÑÁö È®ÀÎ
        if (gems >= gemAmount)
        {
            gems -= gemAmount;
            gold += gemAmount * gemToGoldRatio; // ÁªÀ» °ñµå·Î È¯Àü

            PlayerPrefs.SetInt("Gem", gems);
            PlayerPrefs.SetInt("Gold", gold); // °ñµå °ª ÀúÀå
            PlayerPrefs.Save();

            UpdateUI();
        }
    }

    public void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }

    public void OFFGemsPanel()
    {
        EmptyGemsPanel.SetActive(false);
    }

    public void OFFGoldPanel()
    {
        EmptyGoldPanel.SetActive(false);
    }

    public void OnBuyButtonClick(string itemName)
    {
        BuyItem(itemName);
    }

    public void ShowFeatherDescription()
    {
        featherDescriptionImage.gameObject.SetActive(true);
    }

    public void ShowFlagDescription()
    {
        flagDescriptionImage.gameObject.SetActive(true);
    }

    public void ShowLampDescription()
    {
        lampDescriptionImage.gameObject.SetActive(true);
    }
    public void ShowWingDescription()
    {
        wingDescriptionImage.gameObject.SetActive(true);
    }

    public void HideFeatherDescription()
    {
        featherDescriptionImage.gameObject.SetActive(false);
    }

    public void HideFlagDescription()
    {
        flagDescriptionImage.gameObject.SetActive(false);
    }

    public void HideLampDescription()
    {
        lampDescriptionImage.gameObject.SetActive(false);
    }

    public void HideWingDescription()
    {
        wingDescriptionImage.gameObject.SetActive(false);
    }
}