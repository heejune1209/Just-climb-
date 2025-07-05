using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DiasGames.Components;

namespace DiasGames.Controller
{
    public class CSPlayerController : MonoBehaviour
    {
        // Components
        private AbilityScheduler _scheduler = null;
        private Health _health = null;
        private IMover _mover;
        private ICapsule _capsule;

        public float Visibletime = 1f;
        private const float _threshold = 0.01f;
        [SerializeField] private bool hideCursor = true;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;
        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;
        [Tooltip("Speed of camera turn")]
        public Vector2 CameraTurnSpeed = new Vector2(300.0f, 200.0f);
        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // for shooter ui
        public float CurrentRecoil { get; private set; } = 0f;
        private float recoilReturnVel = 0;

        private void Awake()
        {
            _scheduler = GetComponent<AbilityScheduler>();
            _health = GetComponent<Health>();
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();

            if (hideCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }


            // set right angle on start for camera
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.eulerAngles.y;

        }


        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            // subscribe reset action to scheduler to know when to reset actions
            _scheduler.OnUpdatedAbilities += ResetActions;
#endif

            // subscribe for death event
            if (_health != null)
                _health.OnDead += Die;
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            // unsubscribe reset action
            _scheduler.OnUpdatedAbilities -= ResetActions;
#endif
            // unsubscribe for death event
            if (_health != null)
                _health.OnDead -= Die;
        }

        private void Update()
        {
            UpdateCharacterActions();

            if (CurrentRecoil > 0)
                CurrentRecoil = Mathf.SmoothDamp(CurrentRecoil, 0, ref recoilReturnVel, 0.2f);

#if ENABLE_LEGACY_INPUT_MANAGER
            LegacyInput();
#endif
        }


        private void LateUpdate()
        {
            CameraRotation();
        }



        private void Die()
        {
            // 능력 스케줄러·이동 비활성화
            _scheduler.StopScheduler();
            _mover.DisableGravity();
            _mover.StopMovement();
            _capsule.DisableCollision();

        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (Look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                _cinemachineTargetYaw += Look.x * CameraTurnSpeed.x * Time.deltaTime;
                _cinemachineTargetPitch += Look.y * CameraTurnSpeed.y * Time.deltaTime;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void UpdateCharacterActions()
        {
            _scheduler.characterActions.move = Move;
            _scheduler.characterActions.jump = Jump;
            _scheduler.characterActions.walk = Walk;
            _scheduler.characterActions.drop = Drop;
            _scheduler.characterActions.suicide = Suicide;
            // weapon
            _scheduler.characterActions.zoom = Zoom;

            //// ===== 선택 모드 입력 연동 =====
            //_scheduler.characterActions.selectNext = SelectNext;
            //_scheduler.characterActions.selectPrev = SelectPrev;
            //_scheduler.characterActions.confirmSelect = ConfirmSelect;
        }

        #region Input receiver

        [Header("Input")]
        public Vector2 Move = Vector2.zero;
        public Vector2 Look = Vector2.zero;
        public bool Jump = false;
        public bool Walk = false;
        public bool Zoom = false;
        public bool Drop = false;
        public bool Suicide = false;
        public bool Enter = false;

        public bool SelectNext = false;
        public bool SelectPrev = false;
        public bool ConfirmSelect = false;

        public void ResetActions()
        {
            Jump = false;

            Drop = false;
        }

        public void LegacyInput()
        {
            Move.x = Input.GetAxis("Horizontal");
            Move.y = Input.GetAxis("Vertical");

            Look.x = Input.GetAxis("Mouse X");
            Look.y = Input.GetAxis("Mouse Y");

            Walk = Input.GetButton("Walk");
            Jump = Input.GetButtonDown("Jump");
            Zoom = Input.GetButtonDown("Zoom");

            // special actions for climbing
            Drop = Input.GetButtonDown("Drop");

            /*
            // special actions for shooter
            Fire = Input.GetButton("Fire");
            Reload = Input.GetButtonDown("Reload");
            Switch = Input.GetAxisRaw("Switch");
            Toggle = Input.GetButtonDown("Toggle");*/
        }

        public void OnMove(Vector2 value)
        {
            Move = value;
        }
        public void OnLook(Vector2 value)
        {
            Look = value;
        }
        public void OnJump(bool value)
        {
            Jump = value;
        }
        public void OnWalk(bool value)
        {
            Walk = value;
        }

        public void OnZoom(bool value)
        {
            Zoom = value;
        }
        public void OnDrop(bool value)
        {
            Drop = value;
        }
        public void OnSuicide(bool value)
        {
            Suicide = value;
        }
        public void OnEnter(bool value)
        {
            Enter = value;
        }

#if ENABLE_INPUT_SYSTEM
        private void OnMove(InputValue value)
        {
            OnMove(value.Get<Vector2>());
        }


        private void OnLook(InputValue value)
        {
            OnLook(value.Get<Vector2>());
        }

        private void OnJump(InputValue value)
        {
            OnJump(value.isPressed);
        }

        private void OnWalk(InputValue value)
        {
            OnWalk(value.isPressed);
        }

        private void OnZoom(InputValue value)
        {
            OnZoom(value.isPressed);
        }


        private void OnDrop(InputValue value)
        {
            OnDrop(value.isPressed);
        }

        private void OnInformation(InputValue value)
        {


        }
        private void OnSuicide(InputValue value)
        {
            // 기능 추가: 사망한 상태에서는 더 이상 자살 불가
            if (value.isPressed && _health != null && _health.CurrentHP > 0)
            {
                _health.Damage(_health.CurrentHP);
            }
        }
        private void OnEnter(InputValue value)
        {
            //TutorialTrigger tutori = tutorial.GetComponent<TutorialTrigger>();
            //if(tutori != null && tutori.TutorialPanel.activeSelf == true && SceneManager.GetActiveScene().name == "Stage1")
            //{
            //    Time.timeScale = 1f;
            //    tutori.TutorialPanel.SetActive(false);
            //}
        }
        //public void OnSelectNext(InputValue value)
        //{
        //    SelectNext = value.isPressed;
        //}

        //public void OnSelectPrev(InputValue value)
        //{
        //    SelectPrev = value.isPressed;
        //}

        //public void OnConfirmSelect(InputValue value)
        //{
        //    ConfirmSelect = value.isPressed;
        //}


#endif

        #endregion
    }
}