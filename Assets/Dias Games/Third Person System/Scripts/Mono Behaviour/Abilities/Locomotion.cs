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
        [SerializeField] private float walkSpeed;
        [SerializeField] private float sprintSpeed;
        [Tooltip("Determine how to use extra key button to handle movement. If shift is hold, tells system if it should walk, run, or do nothing")]
        [SerializeField] private MovementStyle movementByKey = MovementStyle.HoldToWalk;
        [SerializeField] private string groundedAnimBlendState = "Grounded";
        private bool isFeatherActive = false; // 깃털 아이템 활성화 여부
        private float featherDuration = 10f; // 깃털 아이템 지속 시간
        private float featherTimer = 0f; // 깃털 아이템 지속 시간 타이머

        [SerializeField]
        private float Speedchange = 1.5f;

        private IMover _mover = null;
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
            {
                // reset movement parameters
                _animator.SetFloat(_animIDSpeed, 0, 0, Time.deltaTime);
            }
        }

        public override void UpdateAbility()
        {
            float targetSpeed = 0;

            if (isFeatherActive)
            {
                featherTimer += Time.deltaTime;
                if (featherTimer >= featherDuration)
                {
                    walkSpeed /= Speedchange; // walkSpeed 초기값으로 재설정
                    sprintSpeed /= Speedchange; // sprintSpeed 초기값으로 재설정

                    isFeatherActive = false;
                    featherTimer = 0f;
                }
                else
                {
                    targetSpeed = _action.walk ? walkSpeed * Speedchange : sprintSpeed * Speedchange;
                    _mover.Move(_action.move, targetSpeed);
                }
            }
            else
            {
                switch (movementByKey)
                {
                    case MovementStyle.HoldToWalk:
                        targetSpeed = _action.walk ? walkSpeed : sprintSpeed;
                        break;
                    case MovementStyle.HoldToRun:
                        targetSpeed = _action.walk ? sprintSpeed : walkSpeed;
                        break;
                    case MovementStyle.DoNothing:
                        targetSpeed = sprintSpeed;
                        break;
                }

                _mover.Move(_action.move, targetSpeed);
            }
        }

        // 깃털 아이템 활성화 함수
        public void ActivateFeatherItem()
        {
            if (!isFeatherActive)
            {
                isFeatherActive = true;
                walkSpeed *= Speedchange; // walkSpeed 값을 변경
                sprintSpeed *= Speedchange; // sprintSpeed 값을 변경
                featherTimer = 0f;
            }
        }
    }
}