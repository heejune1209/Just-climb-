using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class End : MonoBehaviour
{
    public Slider slider;
    private DateTime startTime;
    private DateTime? pauseTime;
    public GameObject resultPanel;
    public Image[] gems;
    bool isGamePaused = false;
    public TMP_Text timerText;
    public TMP_Text deathCountText;


    void Start()
    {
        startTime = GameManager.Instance.StartTime;
        slider.maxValue = 120;
        slider.value = slider.maxValue;
    }

    void Update()
    {
        if (!isGamePaused)
        {
            TimeSpan duration = DateTime.UtcNow - startTime;
            timerText.text = string.Format("Time: {0:D2}:{1:D2}", duration.Minutes, duration.Seconds);

            if (duration.TotalSeconds > 300)
            {
                slider.value = 100 - ((float)duration.TotalSeconds - 300);

                if (slider.value < 0)
                {
                    slider.value = 0;
                }
            }

            if (duration.TotalSeconds > 300)
            {
                SetAlpha(gems[2].transform, 0f);
            }

            if (duration.TotalSeconds > 600)
            {
                SetAlpha(gems[1].transform, 0f);
            }
        }

        if (GameManager.Instance != null)
        {
            deathCountText.text = $"Deaths : {GameManager.Instance.PlayerDeathCount}";
        }
    }

    public void StageClear()
    {
        TimeSpan duration = DateTime.UtcNow - startTime;

        resultPanel.SetActive(true);
        isGamePaused = true;

        string sceneName = SceneManager.GetActiveScene().name;
        int stageNumber;

        if (!sceneName.Contains("Stage") || !int.TryParse(sceneName.Replace("Stage", ""), out stageNumber))
        {
            return;
        }

        int Gem;
        if (duration.TotalSeconds < 300)
        {
            Gem = 3;
        }
        else if (duration.TotalSeconds < 600)
        {
            Gem = 2;
        }
        else
        {
            Gem = 1;
        }
        bool isStageCleared = false;

        for (int i = 0; i < 3; i++)
        {
            string key = "Gem" + stageNumber + "_" + i;
            float alpha = i < Gem ? 1f : 0f;
            PlayerPrefs.SetFloat(key, alpha);

            if (PlayerPrefs.HasKey(key) && PlayerPrefs.GetFloat(key) == 0f)
            {
                continue;
            }

            if (alpha == 1f)
            {
                StartCoroutine(AnimateGem(gems[i]));
                isStageCleared = true;
            }
        }

        if (isStageCleared)
        {
            int nextStageNumber = stageNumber;

            int oldClearTime = PlayerPrefs.GetInt("ClearTime" + stageNumber, int.MaxValue);
            int newClearTime = (int)duration.TotalSeconds;
            if (newClearTime < oldClearTime)
            {
                PlayerPrefs.SetInt("ClearTime" + stageNumber, newClearTime);
            }

            int totalGems = PlayerPrefs.GetInt("Gem", 0);
            totalGems += Gem;
            PlayerPrefs.SetInt("Gem", totalGems);
            PlayerPrefs.SetInt("Stage" + nextStageNumber, 1);

            PlayerPrefs.Save();
        }
    }

    void SetAlpha(Transform transform, float alpha)
    {
        Image image = transform.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        foreach (Transform child in transform)
        {
            SetAlpha(child, alpha);
        }
    }

    IEnumerator AnimateGem(Image gem)
    {
        float duration = 0.25f;
        float maxSize = 1.5f;
        float minSize = 1f;
        while (true)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float normalizedTime = t / duration;
                float size = Mathf.Lerp(minSize, maxSize, normalizedTime);
                gem.transform.localScale = Vector3.one * size;
                yield return null;
            }

            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float normalizedTime = t / duration;
                float size = Mathf.Lerp(maxSize, minSize, normalizedTime);
                gem.transform.localScale = Vector3.one * size;
                yield return null;
            }
        }
    }
    public void PauseGame()
    {
        pauseTime = DateTime.UtcNow;
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        if (pauseTime.HasValue)
        {
            
            DateTime resumeTime = DateTime.UtcNow;
            TimeSpan pauseDuration = resumeTime - pauseTime.Value;

            startTime = startTime + pauseDuration;

            pauseTime = null;
            
        }
    }

}
