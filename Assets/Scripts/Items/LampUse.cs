using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace JustClimb.Items
{
    /// <summary>
    /// 랜턴 아이템 사용 시 주변 투명 오브젝트를 감지하고 하이라이트 재질을 잠시 적용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "LampUse", menuName = "Game/ItemUse/LampUse", order = 100)]
    public class LampUse : ScriptableObject, IItemUse
    {
        [Inject] private IResourceManager _rm;
        [Inject] private ISoundManager _sm;

        [Header("랜턴 이펙트 Prefab")]
        [Tooltip("플레이어 위치에 소환될 랜턴 프리팹")]
        public GameObject lanternPrefab;

        [Header("감지할 태그들")]
        [Tooltip("하이라이트할 대상 오브젝트들의 태그 목록")]
        public string[] detectTags;

        [Header("하이라이트용 재질들")]
        [Tooltip("detectTags 순서에 대응하는 재질 배열")]
        public Material[] highlightMaterials;

        [Header("Item Data (buffDuration 포함)")]
        [Tooltip("지속시간 등 메타데이터를 가진 SO를 할당")]
        public ItemData data;

        // IItemUse 인터페이스 구현: 아이템 사용 시 호출됩니다.
        // 아이템을 사용하는 GameObject (플레이어)
        public void Use(GameObject user)
        {
            if (user == null)
            {
                Debug.LogWarning("LampUse: user가 null 입니다.");
                return;
            }

            // SO에서 설정한 감지 지속시간
            float duration = data.buffDuration;
            if (duration <= 0f)
            {
                Debug.LogWarning($"LampUse: data.buffDuration이 0 이하입니다. ({duration})");
                return;
            }

            // 1) 랜턴 이펙트 인스턴스화
            GameObject lanternInstance = null;
            if (lanternPrefab != null)
            {
                lanternInstance = _rm.Instantiate($"Prefabs/Items/{lanternPrefab.name}",
                    user.transform.position, Quaternion.identity, null, data._initialpoolcount);
                lanternInstance.transform.SetParent(user.transform);

                _sm.PlaySFX(3);
            }
            else
            {
                Debug.LogWarning("LampUse: lanternPrefab이 설정되지 않았습니다.");
            }

            // 2) 감지 + 복원 + 이펙트 제거
            var mb = user.GetComponent<MonoBehaviour>();
            if (mb != null)
            {
                // 여기서 prefab이 아니라 Instantiate 결과물을 넘겨야 한다
                mb.StartCoroutine(DetectAndRevert(duration, lanternInstance));
            }
            else
            {
                Debug.LogWarning("LampUse: Coroutine 실행을 위한 MonoBehaviour를 찾을 수 없습니다.");
            }
        }

        private IEnumerator DetectAndRevert(float duration, GameObject lanternInstance)
{
    // (1) 렌더러별 원본(Material, enabled) 상태 저장 리스트
    var originalStates = new List<(MeshRenderer rend, Material mat, bool wasEnabled)>();

    // (2) 태그별로 처리
    for (int i = 0; i < detectTags.Length; i++)
    {
        string tag = detectTags[i];
        Material highlightMat = (i < highlightMaterials.Length) 
            ? highlightMaterials[i] 
            : null;

        // 해당 태그를 가진 루트 오브젝트들
        var roots = GameObject.FindGameObjectsWithTag(tag);
        foreach (var root in roots)
        {
            // 부모+자식 모두, 비활성화된 렌더러까지 포함
            var rends = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var rend in rends)
            {
                // 원본 상태 저장
                originalStates.Add((rend, rend.material, rend.enabled));

                // (A) 하이라이트 재질 적용 (있을 때만)
                if (highlightMat != null)
                    rend.material = highlightMat;

                // (B) InvisibleObstacle 태그인 경우
                if (tag == "InvisibleObstacle" && !rend.enabled)
                    rend.enabled = true;
            }
        }
    }

    // (3) 지정 시간 대기
    yield return new WaitForSeconds(duration);

    // (4) 저장된 상태로 복원
    foreach (var (rend, mat, wasEnabled) in originalStates)
    {
        if (rend == null) continue;
        rend.material = mat;
        rend.enabled  = wasEnabled;
    }

    // (5) 랜턴 이펙트 제거
    if (lanternInstance != null)
        Destroy(lanternInstance);
}
    }
}