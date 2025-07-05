using System.Collections;
using UnityEngine;
using Zenject;

// 발사체에만 붙이기
[RequireComponent(typeof(Poolable))]
public class PooledProjectile : MonoBehaviour
{
    [Tooltip("발사 후 자동 반환까지의 시간(초)")]
    public float lifeTime = 3f;

    [Inject] private IPoolManager _poolManager;
    private Coroutine _returnRoutine;

    void OnEnable()
    {
        // Pop 될 때마다 실행
        if (lifeTime > 0f)
            _returnRoutine = StartCoroutine(AutoReturn());
    }

    void OnDisable()
    {
        // Push 될 때 중단
        if (_returnRoutine != null)
            StopCoroutine(_returnRoutine);
    }

    private IEnumerator AutoReturn()
    {
        yield return new WaitForSeconds(lifeTime);
        _poolManager.Push(GetComponent<Poolable>());
    }
}
