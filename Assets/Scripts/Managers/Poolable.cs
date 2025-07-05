using UnityEngine;

public class Poolable : MonoBehaviour
{
    // 이 스크립트가 붙어 있으면 ResourceManager.Instantiate() 시
    // 자동으로 PoolManager.Pop/Push 흐름이 작동.
    public bool IsUsing;
}
