using UnityEngine;
using UnityEngine.Events;
using DiasGames.Components;
using System.Collections;

namespace DiasGames.Abilities
{
    public class AirControlAbility : AbstractAbility
    {
        [Header("Animation State")]
        [SerializeField] private string animJumpState = "Air.Jump";
        [SerializeField] private string animFallState = "Air.Falling";
        [SerializeField] private string animHardLandState = "Air.Hard Land";
        [Header("Jump parameters")]
        // 플레이어가 바닥에서 얼마나 높이 점프할지 결정 (점프 순간 부여되는 초기 속도를 계산할 때 사용)
        [SerializeField] private float jumpHeight = 1.2f;
        // 공중에 떠 있을 때 플레이어가 이동할 수 있는 속도
        [SerializeField] private float speedOnAir = 6f;
        // 공중에서의 입력 반응도 (0~1). 값이 클수록 키 입력에 더 즉각적으로 반응
        [SerializeField] private float airControl = 0.5f;

        [Header("Landing")]
        // 이 높이 이상에서 착지하면 ‘하드 랜딩’ 효과(파티클, 카메라 흔들림 등)를 발생시킬 기준 높이
        [SerializeField] private float heightForHardLand = 3f;
        // 이 높이 이상에서 착지하면 플레이어 즉사 처리할 기준 높이
        [SerializeField] private float heightForKillOnLand = 7f;
        [Header("Sound FX")]
        [SerializeField] private AudioClip jumpEffort;
        [SerializeField] private AudioClip hardLandClip;
        [Header("Event")]
        [SerializeField] private UnityEvent OnLanded = null;

        

        private IMover _mover = null;
        private IDamage _damage;
        private CharacterAudioPlayer _audioPlayer;

        private float _startSpeed;
        private Vector2 _startInput;

        private Vector2 _inputVel;
        private float _angleVel;

        private float _targetRotation;
        private Transform _camera;

        // vars to control landing
        private float _highestPosition = 0;
        private bool _hardLanding = false;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _damage = GetComponent<IDamage>();
            _audioPlayer = GetComponent<CharacterAudioPlayer>();
            _camera = Camera.main.transform;
        }

        public override bool ReadyToRun()
        {
            return !_mover.IsGrounded() || _action.jump;
        }

        public override void OnStartAbility()
        {
            _startInput = _action.move;
            _targetRotation = _camera.eulerAngles.y;

            if (_action.jump && _mover.IsGrounded())
                PerformJump();
            else
            {
                SetAnimationState(animFallState, 0.25f);
                _startSpeed = Vector3.Scale(_mover.GetVelocity(), new Vector3(1, 0, 1)).magnitude;

                _startInput.x = Vector3.Dot(_camera.right, transform.forward);
                _startInput.y = Vector3.Dot(Vector3.Scale(_camera.forward, new Vector3(1, 0, 1)), transform.forward);

                if (_startSpeed > 3.5f)
                    _startSpeed = speedOnAir;
            }

            _highestPosition = transform.position.y;
            _hardLanding = false;
        }

        public override void UpdateAbility()
        {
            if (_hardLanding)
            {
                // apply root motion
                _mover.ApplyRootMotion(Vector3.one, false);

                // wait animation finish
                if (HasFinishedAnimation(animHardLandState))
                    StopAbility();

                return;
            }

            if (_mover.IsGrounded())
            {
                if(_highestPosition - transform.position.y >= heightForHardLand)
                {
                    _hardLanding = true;
                    SetAnimationState(animHardLandState, 0.02f);

                    // call event
                    OnLanded.Invoke();

                    // call damage clip
                    if(_audioPlayer)
                        _audioPlayer.PlayVoice(hardLandClip);

                    // cause damage
                    if(_damage != null)
                    {
                        // calculate damage
                        float currentHeight = _highestPosition - transform.position.y - heightForHardLand;
                        float ratio = currentHeight / (heightForKillOnLand - heightForHardLand);

                        _damage.Damage((int)(200 * ratio));
                    }

                    return;
                }

                StopAbility();
            }

            if (transform.position.y > _highestPosition)
                _highestPosition = transform.position.y;

            _startInput = Vector2.SmoothDamp(_startInput, _action.move, ref _inputVel, airControl);
            _mover.Move(_startInput, _startSpeed, false);

            RotateCharacter();
        }

        private void RotateCharacter()
        {
            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_action.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(_startInput.x, _startInput.y) * Mathf.Rad2Deg + _camera.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _angleVel, airControl);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
        }

        public override void OnStopAbility()
        {
            base.OnStopAbility();

            if (_mover.IsGrounded() && !_hardLanding && _mover.GetVelocity().y < -3f)
                OnLanded.Invoke();

            _hardLanding = false;
            _highestPosition = 0;
            _mover.StopRootMotion();
        }

        /// <summary>
        /// Adds force to rigidbody to allow jump
        /// </summary>
        private void PerformJump()
        {
            Vector3 velocity = _mover.GetVelocity();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * _mover.GetGravity());

            _mover.SetVelocity(velocity);
            _animator.CrossFadeInFixedTime(animJumpState, 0.1f);
            _startSpeed = speedOnAir;

            if (_startInput.magnitude > 0.1f)
                _startInput.Normalize();

            if (_audioPlayer)
                _audioPlayer.PlayVoice(jumpEffort);
        }
        public void UseJumpBoost(float boostMultiplier, float duration)
        {
            if (_mover.IsGrounded())
            {
                
                jumpHeight *= boostMultiplier;

                
                StartCoroutine(ResetJumpHeight(boostMultiplier, duration));
            }
        }

        private IEnumerator ResetJumpHeight(float originalMultiplier, float duration)
        {
            yield return new WaitForSeconds(duration);

            
            jumpHeight /= originalMultiplier;
        }

        private void HardLand()
        {

        }

    }
}