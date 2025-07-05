using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageScene : BaseScene
{
    [Header("Inspector용: 이 씬에 대응하는 Define.Scene 이름 (예: Stage1, Stage2, …)")]
    [SerializeField] private string sceneString;

    [Header("플레이어 기본 시작 지점")]
    [Tooltip("깃발 위치가 무효할 때 여기로 리스폰")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Header("깃발 유효 범위 콜라이더")]
    [Tooltip("깃발 위치가 이 범위 안에 있어야만 유효")]
    [SerializeField] private Collider validAreaCollider;

    protected override void Init()
    {
        base.Init();
        // 문자열을 Define.Scene enum으로 변환
        if (Enum.TryParse<Define.Scene>(sceneString, out var parsed))
        {
            SceneType = parsed;
        }
        else
        {
            Debug.LogError($"[StageScene] 잘못된 sceneString: '{sceneString}'. Define.Scene에 해당 이름이 없습니다.");
            SceneType = Define.Scene.Unknown;
        }
        _uiManager.ShowSceneUI<UI_Stage>("UI_Stage");
    }

    /// <summary>
    /// 저장된 깃발 위치(savedPos)가 유효 범위(validAreaCollider)에 있으면 true, 아니면 false
    /// </summary>
    public bool IsValidFlagPos(Vector3 savedPos)
    {
        if (validAreaCollider == null)
            return false;
        return validAreaCollider.bounds.Contains(savedPos);
    }

    /// <summary>
    /// 기본 시작 위치 반환 (Inspector에서 지정)
    /// </summary>
    public Vector3 GetDefaultSpawnPos()
    {
        return defaultSpawnPoint != null
            ? defaultSpawnPoint.position
            : Vector3.zero;
    }

    public override void Clear()
    {
        // Scene 전환 직전 기존 UI 정리
        // Managers.Clear()는 더 이상 사용하지 않음 (DI 패턴에서는 불필요)
        base.Clear();
    }

    // 메모리 누수 방지
    protected override void OnDestroy()
    {
        // Transform 참조 해제
        defaultSpawnPoint = null;
        validAreaCollider = null;
        
        base.OnDestroy();
    }
}
