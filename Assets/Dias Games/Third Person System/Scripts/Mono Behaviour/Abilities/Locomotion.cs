using UnityEngine;
using DiasGames.Components;

namespace DiasGames.Abilities
{
    public enum MovementStyle
    {
        HoldToWalk, HoldToRun, DoNothing
    }

    public class Locomotion : AbstractAbility
    {
        [SerializeField]
        private float _walkSpeed;         // 인스펙터에 노출되는 백킹 필드
        [SerializeField]
        private float _sprintSpeed;       // 인스펙터에 노출되는 백킹 필드

        /// <summary>걷기 속도 (프로퍼티)</summary>
        public float WalkSpeed
        {
            get { return _walkSpeed; }
            set { _walkSpeed = value; }
        }

        /// <summary>전력 질주 속도 (프로퍼티)</summary>
        public float SprintSpeed
        {
            get { return _sprintSpeed; }
            set { _sprintSpeed = value; }
        }

        [Tooltip("Determine how to use extra key button to handle movement. If shift is hold, tells system if it should walk, run, or do nothing")]
        [SerializeField]
        private MovementStyle movementByKey = MovementStyle.HoldToWalk;

        [SerializeField]
        private string groundedAnimBlendState = "Grounded";

        private bool isFeatherActive = false;
        private float featherDuration = 10f;
        private float featherTimer = 0f;

        [SerializeField]
        private float speedChangeMultiplier = 1.5f;

        private IMover _mover;
        private int _animIDSpeed;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _animIDSpeed = Animator.StringToHash("Speed");
        }

        public override bool ReadyToRun()
        {
            return _mover.IsGrounded();
        }

        public override void OnStartAbility()
        {
            SetAnimationState(groundedAnimBlendState, 0.25f);

            if (_action.move.magnitude < 0.1f)
                _animator.SetFloat(_animIDSpeed, 0, 0, Time.deltaTime);
        }

        public override void UpdateAbility()
        {
            float targetSpeed = 0f;

            if (isFeatherActive)
            {
                featherTimer += Time.deltaTime;
                if (featherTimer >= featherDuration)
                {
                    // 지속시간 끝나면 원래 속도로 복원
                    WalkSpeed /= speedChangeMultiplier;
                    SprintSpeed /= speedChangeMultiplier;
                    isFeatherActive = false;
                    featherTimer = 0f;
                }
                else
                {
                    // 깃털 활성화 중에는 속도 증가 적용
                    targetSpeed = _action.walk
                        ? WalkSpeed * speedChangeMultiplier
                        : SprintSpeed * speedChangeMultiplier;
                    _mover.Move(_action.move, targetSpeed);
                    return;
                }
            }

            // 깃털 비활성화된 일반 이동 로직
            switch (movementByKey)
            {
                case MovementStyle.HoldToWalk:
                    targetSpeed = _action.walk ? WalkSpeed : SprintSpeed;
                    break;
                case MovementStyle.HoldToRun:
                    targetSpeed = _action.walk ? SprintSpeed : WalkSpeed;
                    break;
                case MovementStyle.DoNothing:
                    targetSpeed = SprintSpeed;
                    break;
            }
            _mover.Move(_action.move, targetSpeed);
        }

        /// <summary>
        /// 깃털 아이템 사용 시 호출: 속도 증가 시작
        /// </summary>
        public void ActivateFeatherItem()
        {
            if (!isFeatherActive)
            {
                isFeatherActive = true;
                WalkSpeed *= speedChangeMultiplier;
                SprintSpeed *= speedChangeMultiplier;
                featherTimer = 0f;
            }
        }
    }
}
