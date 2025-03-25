using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class Loading : MonoBehaviour
{
    public TMP_Text loadingText;

    void Start()
    {
        string nextScene = PlayerPrefs.GetString("nextScene");
        StartCoroutine(LoadSceneAsync(nextScene));

        /*PlayerPrefs.SetString("nextScene", "�̵��� �� �̸�");
        SceneManager.LoadScene("LoadingScene");*/
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        StartCoroutine(LoadingAnimation());

        while (!operation.isDone)
        {
            // �ε��� ���� �Ϸ�� ���
            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(3f);  // 5�� ���� ��ٸ�

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    IEnumerator LoadingAnimation()
    {
        while (true)
        {
            loadingText.text = "Loading...";
            yield return new WaitForSeconds(0.3f);
            loadingText.text = "Loading....";
            yield return new WaitForSeconds(0.3f);
            loadingText.text = "Loading.....";
            yield return new WaitForSeconds(0.3f);
            loadingText.text = "Loading....";
            yield return new WaitForSeconds(0.3f);
        }
    }
}