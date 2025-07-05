using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    private UI_Stage _uiStage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // PlayerPrefs는 UI_Stage.ShowTutorial 에서 관리하므로
            // 여기서는 단순히 호출만
            if (_uiStage == null)
                _uiStage = FindObjectOfType<UI_Stage>();
            _uiStage?.ShowTutorial();
        }
    }

    // 메모리 누수 방지
    private void OnDestroy()
    {
        // 컴포넌트 참조 해제
        _uiStage = null;
    }
}
