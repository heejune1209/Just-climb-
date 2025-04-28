using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JustClimb.Obstacles.Core;

namespace JustClimb.Obstacles.Effects
{
    // 플레이어가 이 오브젝트(홀드)에 닿으면
    // 일정 시간 동안 지정 머티리얼로 바꿨다가 원래대로 복원.
    [RequireComponent(typeof(Collider))]
    public class MaterialChanger : ObstacleBase
    {
        [Header("변경 후 사용할 머티리얼")]
        [Tooltip("플레이어가 닿았을 때 적용할 머티리얼")]
        public Material highlightMaterial;

        [Header("효과 지속 시간")]
        [Tooltip("초 단위")]
        public float duration = 2f;

        // 원래 갖고 있던 머티리얼(하위 Renderer 모두)
        private Dictionary<Renderer, Material[]> _originals;

        private void Awake()
        {
            // Trigger 모드 자동 설정
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            // 하위 모든 MeshRenderer/SkinnedMeshRenderer의 원본 머티리얼 보관
            _originals = new Dictionary<Renderer, Material[]>();
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                _originals[rend] = rend.sharedMaterials;
            }
        }

        public override void Activate()
        {
            // 닿았을 때 한 번만 실행되도록
            StopAllCoroutines();
            // 모든 렌더러를 highlightMaterial로 교체
            foreach (var kv in _originals)
            {
                var rend = kv.Key;
                var mats = new Material[kv.Value.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = highlightMaterial;
                rend.materials = mats;
            }
            // duration 후에 원복
            StartCoroutine(RevertAfter());
        }
       
        private IEnumerator RevertAfter()
        {
            yield return new WaitForSeconds(duration);
            // 원본 머티리얼 복원
            foreach (var kv in _originals)
            {
                kv.Key.materials = kv.Value;
            }
        }
    }
}
