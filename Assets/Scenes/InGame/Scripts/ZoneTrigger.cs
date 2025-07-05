using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public enum TriggerMode
{
    Fireworks,  // 폭죽 터트리기
    Goal        // 결과창 띄우기
}

[RequireComponent(typeof(Collider))]
public class ZoneTrigger : MonoBehaviour
{
    // DI 주입받을 매니저들
    [Inject] private IUIManager _uiManager;
    [Inject] private IGameManager _gameManager;

    [Header("이 트리거의 역할")]
    public TriggerMode mode;

    [Header("폭죽 모드용: 터뜨릴 파티클들")]
    public GameObject[] fireworks;

    [Tooltip("결과창 띄우기 전 딜레이(초)")]
    public float delayBeforeResult = 1f;

    bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        switch (mode)
        {
            case TriggerMode.Fireworks:
                //Managers.Instance.Sound.PlaySFX(9);
                //Managers.Instance.Sound.PlaySFX(10);
                PlayFireworks();
                break;

            case TriggerMode.Goal:
                ShowResult();
                break;
        }
    }

    void PlayFireworks()
    {
        foreach (var fx in fireworks)
            if (fx != null)
                fx.SetActive(true);
    }

    void ShowResult()
    {
        var popup = _uiManager.ShowPopupUI<UI_Result>("UI_Result");
        popup.ShowResult(_gameManager.ElapsedTime());
    }
}
