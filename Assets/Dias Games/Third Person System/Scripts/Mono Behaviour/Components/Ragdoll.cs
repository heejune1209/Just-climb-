using System.Collections.Generic;
using UnityEngine;

namespace DiasGames.Components
{
    public class Ragdoll : MonoBehaviour
    {
        private Health _health;
        private Animator _animator;

        private List<Rigidbody> _ragdollRigidbodies = new List<Rigidbody>();
        private List<Collider> _ragdollColliders = new List<Collider>();

        private void Awake()
        {
            _health = GetComponent<Health>();
            _animator = GetComponent<Animator>();
            GatherRagdollParts();
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.OnDead += ActivateRagdoll;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnDead -= ActivateRagdoll;
        }

        void GatherRagdollParts()
        {
            if (_animator == null) return;

            for (int i = 0; i < 18; i++)
            {
                var bone = _animator.GetBoneTransform((HumanBodyBones)i);
                if (bone == null) continue;

                if (bone.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = true;
                    _ragdollRigidbodies.Add(rb);
                }

                if (bone.TryGetComponent(out Collider col))
                {
                    col.enabled = false;
                    _ragdollColliders.Add(col);
                }
            }
        }

        // 죽었을 때 호출: 애니메이터 끄고 physic 활성화
        void ActivateRagdoll()
        {
            if (_animator != null) _animator.enabled = false;
            _ragdollRigidbodies.ForEach(r =>
            {
                r.isKinematic = false;
                r.useGravity = true;
                //r.velocity = Vector3.zero;
                //r.angularVelocity = Vector3.zero;
            });
            _ragdollColliders.ForEach(c => c.enabled = true);
        }

        // 리스폰 시 원래 상태로 되돌리기
        public void DeactivateRagdoll()
        {
            if (_animator != null) _animator.enabled = true;
            _ragdollRigidbodies.ForEach(r =>
            {
                //r.velocity = Vector3.zero;
                //r.angularVelocity = Vector3.zero;
                r.useGravity = false;
                r.isKinematic = true;
            });
            _ragdollColliders.ForEach(c => c.enabled = false);
        }
    }
}
