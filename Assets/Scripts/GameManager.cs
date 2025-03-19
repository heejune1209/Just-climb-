using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using DiasGames.Controller;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    //public GameObject player;




    public int PlayerDeathCount { get; set; }
    public Vector3 FlagPosition { get; set; }

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();

                if (_instance == null)
                {
                    GameObject gameObject = new GameObject("GameManager");
                    _instance = gameObject.AddComponent<GameManager>();
                }
            }

            return _instance;
        }
    }

    public DateTime StartTime { get; set; }

    void Awake()
    {

        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            StartTime = DateTime.UtcNow;
        }
        else
        {
            Destroy(gameObject);
            
        }
    }


    
    public void GoToLobby()
    {
        GameManager.Instance.StartTime = DateTime.UtcNow;
        GameManager.Instance.PlayerDeathCount = 0;
    }

    // 초기화
    public void GoToMain()
    {
        GameManager.Instance.StartTime = DateTime.UtcNow;
    }


    public void PlayerDie() // 죽었을떄
    {
        GameManager.Instance.PlayerDeathCount++;
        string sceneName = SceneManager.GetActiveScene().name;
        int stageNumber;
        if (sceneName.Contains("Stage") && int.TryParse(sceneName.Replace("Stage", ""), out stageNumber))
        {
            PlayerPrefs.SetInt("Death" + stageNumber, GameManager.Instance.PlayerDeathCount);
        }
        
    }
}
