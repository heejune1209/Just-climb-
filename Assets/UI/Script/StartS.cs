using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StartS : MonoBehaviour
{
    void Start()
    {
        DateTime startTime = DateTime.UtcNow;

        PlayerPrefs.SetString("StartTime", startTime.ToString("o")); 
        PlayerPrefs.Save();
    }

    void Update()
    {
        
    }
}
