using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JustClimb.Items
{
    /// <summary>
    /// 랜턴 아이템 사용 시 주변 투명 오브젝트를 감지하고 하이라이트 재질을 잠시 적용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "LampUse", menuName = "Game/ItemUse/LampUse", order = 100)]
    public class LampUse : ScriptableObject, IItemUse
    {
        [Header("랜턴 이펙트 Prefab")]
        [Tooltip("플레이어 위치에 소환될 랜턴 프리팹")]
        public GameObject lanternPrefab;

        [Header("감지할 태그들")]
        [Tooltip("하이라이트할 대상 오브젝트들의 태그 목록")]
        public string[] detectTags;

        [Header("하이라이트용 재질들")]
        [Tooltip("detectTags 순서에 대응하는 재질 배열")]
        public Material[] highlightMaterials;

        [Header("감지 지속 시간(초)")]
        [Tooltip("하이라이트 재질을 적용할 시간")]
        public float detectDuration = 10f;

        /// <summary>
        /// IItemUse 인터페이스 구현: 아이템 사용 시 호출됩니다.
        /// </summary>
        /// <param name="user">아이템을 사용하는 GameObject (플레이어)</param>
        public void Use(GameObject user)
        {
            if (user == null)
            {
                Debug.LogWarning("LampUse.Use 호출 시 user가 null 입니다.");
                return;
            }

            // 1) 랜턴 이펙트 소환
            if (lanternPrefab != null)
            {
                var instance = Instantiate(lanternPrefab, user.transform.position, Quaternion.identity);
                instance.transform.SetParent(user.transform);
            }
            else
            {
                Debug.LogWarning("LampUse: lanternPrefab이 설정되지 않았습니다.");
            }

            // 2) 투명 오브젝트 감지 후 하이라이트 재질 적용 및 복원
            var mb = user.GetComponent<MonoBehaviour>();
            if (mb != null)
            {
                mb.StartCoroutine(DetectAndRevert());
            }
            else
            {
                Debug.LogWarning("LampUse: 사용자 GameObject에 MonoBehaviour를 찾을 수 없어 코루틴을 실행할 수 없습니다.");
            }
        }

        private IEnumerator DetectAndRevert()
        {
            // 대상 오브젝트 그룹과 원본 재질 저장 리스트
            var groups = new List<GameObject[]>();
            var originals = new List<Material[]>();

            // 탐지 대상별로 처리
            for (int i = 0; i < detectTags.Length; i++)
            {
                string tag = detectTags[i];
                var objects = GameObject.FindGameObjectsWithTag(tag);
                groups.Add(objects);

                // 원본 재질 저장 및 하이라이트 적용
                var savedMats = new Material[objects.Length];
                for (int j = 0; j < objects.Length; j++)
                {
                    var renderer = objects[j].GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        savedMats[j] = renderer.material;
                        if (i < highlightMaterials.Length && highlightMaterials[i] != null)
                            renderer.material = highlightMaterials[i];
                    }
                }
                originals.Add(savedMats);
            }

            // 지정된 시간 대기
            yield return new WaitForSeconds(detectDuration);

            // 원본 재질로 복원
            for (int i = 0; i < groups.Count; i++)
            {
                var objects = groups[i];
                var savedMats = originals[i];
                for (int j = 0; j < objects.Length; j++)
                {
                    var renderer = objects[j].GetComponent<MeshRenderer>();
                    if (renderer != null && savedMats[j] != null)
                        renderer.material = savedMats[j];
                }
            }
        }
    }
}