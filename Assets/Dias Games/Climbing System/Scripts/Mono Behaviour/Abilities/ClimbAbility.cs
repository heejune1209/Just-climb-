using DiasGames.Climbing;
using DiasGames.Components;
using DiasGames.Debugging;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;
using static UnityEditor.PlayerSettings;

namespace DiasGames.Abilities
{

    public class ClimbAbility : AbstractAbility
    {
        // 검사할 레이어 설정
        [Tooltip("클라이밍 가능한 표면(Ledge) 레이어")]
        [SerializeField] private LayerMask climbMask;      // 클라이밍 가능한 표면(Ledge) 레이어
        [Tooltip("점프/드롭 시 충돌을 피해야 할 장애물 레이어")]
        [SerializeField] private LayerMask obstacleMask;   // 점프/드롭 시 충돌을 피해야 할 장애물 레이어
        [Tooltip("검사 중 무시할 태그 리스트 (예: InvisibleObstacle)")]
        [SerializeField] private List<string> ignoreTags = new List<string>();

        [Space]
        [Tooltip("클라이밍 시작 지점(플레이어 핸들 위치) 기준 Transform")]
        [SerializeField] private Transform grabReference;  // 클라이밍 시작 지점(플레이어 핸들 위치) 기준 Transform
        [Tooltip("초기 OverlapSphere 탐색 반경 (디버그 목적)")]
        [SerializeField] private float globalRadiusDetection = 0.5f;
        [Tooltip("클라이밍 후 캐릭터 위치 보정 오프셋 (x: 앞뒤, y: 위)")]
        [SerializeField] private Vector2 offsetOnLedge; 

        [Header("Capsule Cast Parameters")]
        [Tooltip("수평 캡슐 캐스트 길이 (앞뒤 거리)")]
        [SerializeField] private float capsuleCastDistance = 0.75f;
        [Tooltip("수평 캡슐 캐스트 높이 (상하 여유)")]
        [SerializeField] private float capsuleHeight = 1f;
        [Tooltip("수평 캡슐 캐스트 반경 (좌우 넓이)")]
        [SerializeField] private float capsuleRadius = 0.15f;
        [Tooltip("360° 스캔 분할 수 (정밀도)")]
        [SerializeField] private int capsuleCastIterations = 10;

        [Header("Sphere Cast Parameters (For Top Detection)")]
        [Tooltip("수평 히트 지점에서 스피어 캐스트 시작 높이")]
        [SerializeField] private float sphereCastMaxHeight = 1f;
        [Tooltip("스피어 캐스트가 아래로 내리찍는 최대 거리")]
        [SerializeField] private float sphereCastDistance = 2f;
        [Tooltip("스피어 캐스트 반경 (위쪽 면 검사 넓이)")]
        [SerializeField] private float sphereCastRadius = 0.1f;

        [Header("Shimmy Casting")]
        [Tooltip("좌/우 샴미 검사 분할 수")]
        [SerializeField] private int sideCastIterations = 10;
        [Tooltip("좌/우 샴미 검사 거리")]
        [SerializeField] private float sideCastRange = 0.5f;
        [Tooltip("좌/우 샴미 검사 반경")]
        [SerializeField] private float sideCastRadius = 0.1f;
        [Tooltip("좌/우 샴미 검사 높이")]
        [SerializeField] private float sideCastHeight = 0.5f;

        [Header("Foot Casting")]
        [Tooltip("발 위치 검사에 사용할 레이어 마스크")]
        [SerializeField] private LayerMask footMask;
        [Tooltip("발 위치 검사 시 캡슐 반경")]
        [SerializeField] private float footCastRadius = 0.3f;
        [Tooltip("발 위치 검사 캡슐 높이")]
        [SerializeField] private float footCapsuleHeight = 1f;
        [Tooltip("발 위치 검사 거리 (아래로)")]
        [SerializeField] private float footCastDistance = 1f;

        [Header("Matching Position Parameters")]
        [Tooltip("클라이밍 애니메이션 시작 시 MatchTarget 시간")]
        [SerializeField] private float startClimbMatchTime = 0.15f;
        [Tooltip("MatchTarget 진행 커브")]
        [SerializeField] private AnimationCurve defaultMatchingCurve;

        [Space]
        [Tooltip("상태 머신 컨텍스트")]
        [SerializeField] private ClimbStateContext _context;
        // ClimbStateContext: 현재 상태 & 전이 관리

        [Header("Debug")]
        [Tooltip("디버그용 컬러 (Gizmos, 로깅 등)")]
        [SerializeField] private Color debugColor;        // 디버그용 컬러 (Gizmos, 로깅 등)


        // public getters (외부에서 읽기 전용으로 접근할 필드)
        public float CapsuleCastHeight { get { return capsuleHeight; } }
        public float CapsuleCastRadius { get { return capsuleRadius; } }
        public float SphereCastHeight { get { return sphereCastMaxHeight; } }
        public float SphereCastRadius { get { return sphereCastRadius; } }
        public LayerMask ClimbMask { get { return climbMask; } }
        public Collider CurrentCollider { get { return _currentCollider; } }
        public List<string> IgnoreTags { get { return ignoreTags; } }


        // components (런타임에 할당될 컴포넌트 참조)
        private IMover _mover;           // 이동 제어 인터페이스
        private ICapsule _capsule;         // 콜라이더 크기/충돌 인터페이스
        private ClimbIK _climbIK;         // IK 처리 컴포넌트
        private CastDebug _debug;           // 물리 캐스트 시각화 컴포넌트

        // internal climbing controller vars
        private Collider _currentCollider;        // 현재 매달린 ledge 콜라이더
        private RaycastHit _currentHorizontalHit;   // 수평 히트 정보
        private RaycastHit _currentTopHit;          // 상단 히트 정보
        private RaycastHit _wallHit;                // 벽 충돌 히트 정보
        private float _hangWeight = 0;         // 대기 중 매달림 IK 가중치
        private float _hangvel;                // 매달림 IK 스무딩 변수
        private Transform _targetClimbCharPos;     // 목표 캐릭터 위치용 트랜스폼
        private Transform _climbTargetHit;         // 목표 히트 위치용 트랜스폼

        private Vector3 _lastHitPoint;              // 이전 히트 지점 (Tween 보간용)
        private float _timeWithoutLedge = 0;      // ledge를 못 찾은 누적 시간

        // internal state vars
        private Camera _mainCamera;                // 메인 카메라 참조
        private Vector2 _localCoordMove;            // 입력 벡터를 로컬 좌표계로 프로젝션한 값
        private bool _updateTransform = true;    // Transform 직접 업데이트 허용 플래그

        // 애니메이션 매칭 대기
        private bool _waitingAnimation;         // 애니메이션 종료 대기 중
        private string _animationStateToWait;     // 대기 대상 애니메이션 이름
        private bool _matchTarget;              // MatchTarget 동작 플래그
        private Vector3 _matchTargetPosition;      // MatchTarget 위치 목표
        private Quaternion _matchTargetRotation;    // MatchTarget 회전 목표
        private float _targetNormalizedTime;     // 애니메이션 정규화된 목표 시간

        // tween parameters (부드러운 이동용)
        private bool _isDoingTween = false;    // Tween 동작 중 여부
        private float _currentTweenWeight;      // Tween 보간 가중치
        private float _tweenDuration;           // Tween 총 지속 시간
        private float _tweenStartTime;          // Tween 시작 시간
        private float _tweenStep;               // Tween 진행 단계
        private Vector3 _tweenStartPosition;      // Tween 시작 위치
        private Quaternion _tweenStartRotation;     // Tween 시작 회전
        private Vector3 _tweenBezierPoint;        // Bezier 곡선 중간 제어점
        private Transform _tweenTarget;             // Tween 대상 Transform (ledges)
        private AnimationCurve _targetCurve;        // Tween 적용할 커브

        // shimmy control (벽 가장자리 이동)
        private float _leftDistanceToShimmy;        // 왼쪽 샴미 가능한 최대 거리
        private float _rightDistanceToShimmy;       // 오른쪽 샴미 가능한 최대 거리
        private float _shimmyMinRatio = 0.5f;       // 최소 샴미 허용 비율
        private bool _stopRightShimmy;             // 오른쪽 샴미 중지 플래그
        private bool _stopLeftShimmy;              // 왼쪽 샴미 중지 플래그

        // ledge 중복 탐색 방지
        private Collider _ledgeBlocked;             // 잠깐 블록된 ledge 콜라이더
        private float _timeBlockStarted;         // 블록 시작 시간

        //[Header("Selection Settings")]
        //[SerializeField, Range(0.5f, 10f)]
        //private float searchRadius = 3f;            // 기능 추가: 후보 탐색 반경
        //[SerializeField, Range(0.05f, 1f)]
        //private float searchInterval = 0.2f;        // 기능 추가: 탐색 주기 (초)

        //// 기능 추가: NonAlloc OverlapSphere 버퍼 및 타이머
        //private Collider[] _overlapBuffer = new Collider[32];

        //// 기능 추가: Selection Mode 상태 머신
        //private enum SelectionState { None, Highlight, Navigating }
        //private SelectionState _selState = SelectionState.None;
        //private float _highlightStartTime;                         // 기능 추가: 하이라이트 시작 시간
        //private int _currentCandidateIdx = 0;                      // 기능 추가: 후보 인덱스

        //private List<CandidateData> _candidates = new List<CandidateData>();

        //// 기능 추가: 후보 순환 (엣지 감지)
        //private bool _prevSelectNext = false;
        //private bool _prevSelectPrev = false;

        //// 클래스 최상단에 추가
        //private struct CandidateData
        //{
        //    public Collider collider;
        //    public Vector3 point;
        //    public Vector3 normal;
        //}

        #region State Machine Methods

        public void Idle() => _context.CurrentClimbState.Idle(_context);
        public void ClimbUp() => _context.CurrentClimbState.ClimbUp(_context);
        public void Jump() => _context.CurrentClimbState.Jump(_context);
        public void Drop() => _context.CurrentClimbState.Drop(_context);
        public void CornerOut(CornerSide side)
        {
            _context.CornerOut.cornerSide = side;
            _context.CurrentClimbState.CornerOut(_context);
        }

        #endregion

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();
            _climbIK = GetComponent<ClimbIK>();
            _debug = GetComponent<CastDebug>();

            _mainCamera = Camera.main;

            CreateTransforms();
        }

        private void CreateTransforms()
        {
            if (_targetClimbCharPos == null)
                _targetClimbCharPos = new GameObject("Climb Char Target").transform;

            if (_tweenTarget == null)
                _tweenTarget = new GameObject("Climb Tween Target").transform;

            if (_climbTargetHit == null)
                _climbTargetHit = new GameObject("Climb Hit").transform;
        }

        public override bool ReadyToRun()
        {
            if (_mover.IsGrounded()) return false;

            return HasLedge();
        }
        public override void OnStartAbility()
        {
            UpdateContextVars();

            _mover.StopMovement();
            _mover.DisableGravity();

            _climbIK.RunIK();
            _climbIK.UpdateIKReferences(climbMask, footMask, _currentHorizontalHit);

            _hangWeight = HasWall() ? 0 : 1;
            _animator.SetFloat("HangWeight", _hangWeight);

            _waitingAnimation = false;
            _matchTarget = false;
            _updateTransform = true;

            _context.SetState(_context.Idle);

            DoTween(GetCharacterPositionOnLedge(), GetCharacterRotationOnLedge(), startClimbMatchTime, _currentCollider);

            SetAnimationState("Climb.Start Climb");

            _timeWithoutLedge = 0;
        }

        public override void OnStopAbility()
        {
            _climbIK.StopIK();
            _mover.StopRootMotion();
            _mover.EnableGravity();

            if (!string.IsNullOrEmpty(_animationStateToWait))
            {
                if (_animationStateToWait.Contains("Drop"))
                {
                    _mover.SetVelocity(Vector3.down * 3f);
                }

                if (_animationStateToWait.Contains("Jump"))
                {
                }
            }

            _capsule.EnableCollision();
        }

        public override void UpdateAbility()
        {
            _climbIK.UpdateIKReferences(climbMask, footMask, _currentHorizontalHit);

            //// ===== 기능 추가: 선택 모드 처리 =====
            //HandleSelectionMode();
            //if (_selState != SelectionState.None)
            //    return; // 선택 모드 중에는 기본 클라이밍 로직 스킵
            //            // ====================================

            //_climbIK.UpdateIKReferences(climbMask, footMask, _currentHorizontalHit);

            UpdateFootWall();
            UpdateTween();

            if (_waitingAnimation)
            {
                if (_animator.IsInTransition(0)) return;

                var state = _animator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = Mathf.Repeat(state.normalizedTime, 1);
                if (state.IsName(_animationStateToWait))
                {
                    if (_matchTarget && !_animator.isMatchingTarget)
                    {
                        _capsule.DisableCollision();
                        _animator.MatchTarget(_matchTargetPosition, _matchTargetRotation, AvatarTarget.RightFoot,
                            new MatchTargetWeightMask(Vector3.one, 0f), 0.4f, 0.9f);

                        _matchTarget = false;
                    }

                    if (normalizedTime >= _targetNormalizedTime)
                    {
                        StopAbility();
                        return;
                    }
                }

                return;
            }

            if (_isDoingTween)
                return;

            if (Vector3.Distance(_lastHitPoint, _targetClimbCharPos.position) < 0.25f && _updateTransform)
            {
                _mover.SetPosition(transform.position + (_targetClimbCharPos.position - _lastHitPoint));
            }

            ProccessInput();

            if (HasCurrentLedge())
            {
                SetCharacterPosition();
                CheckLedgeSide();

                ProccesMovement();

                _mover.ApplyRootMotion(Vector3.one);

                _context.CurrentClimbState.Idle(_context);

                if (_context.CurrentClimbState == _context.Idle)
                {
                    if (_rightDistanceToShimmy < sideCastRange * _shimmyMinRatio)
                        _stopRightShimmy = true;
                    if (_rightDistanceToShimmy >= sideCastRange * 0.95f)
                        _stopRightShimmy = false;

                    if (_leftDistanceToShimmy < sideCastRange * _shimmyMinRatio)
                        _stopLeftShimmy = true;
                    if (_leftDistanceToShimmy >= sideCastRange * 0.95f)
                        _stopLeftShimmy = false;

                    if ((_localCoordMove.x > 0 && _stopRightShimmy) ||
                    (_localCoordMove.x < 0 && _stopLeftShimmy))
                        _animator.SetFloat("Horizontal", 0);
                    else
                        _animator.SetFloat("Horizontal", _localCoordMove.x, 0.1f, Time.deltaTime);
                }
                else
                    _animator.SetFloat("Horizontal", 0);

                _animator.SetFloat("Vertical", _localCoordMove.y, 0.1f, Time.deltaTime);

                _context.CurrentClimbState.CornerIn(_context);
                _lastHitPoint = _targetClimbCharPos.position;
                _timeWithoutLedge = 0;
            }
            else
            {
                if (_updateTransform)
                    _timeWithoutLedge += Time.deltaTime;

                _climbTargetHit.parent = null;

                if (_timeWithoutLedge > 0.1f)
                {
                    BlockCurrentLedge();
                    StopAbility();
                }
            }

            UpdateContextVars();
        }

        public void SetVelocity(Vector3 velocity, bool gravity = false)
        {
            _capsule.EnableCollision();
            _mover.StopRootMotion();
            _mover.SetVelocity(velocity);
            if (gravity) _mover.EnableGravity();
        }

        private void UpdateFootWall()
        {
            float targetWeight = HasWall() ? 0 : 1;
            _hangWeight = Mathf.SmoothDamp(_hangWeight, targetWeight, ref _hangvel, 0.12f);
            _animator.SetFloat("HangWeight", _hangWeight);
        }

        private bool HasLedge()
        {
            // first step: overlap a sphere around climbing grab position. If find some ledge, keep logic
            Collider[] colls = Physics.OverlapSphere(grabReference.position, globalRadiusDetection, climbMask, QueryTriggerInteraction.Collide);

            if (colls.Length == 0) return false;

            // set capsule points to cast
            Vector3 capsuleBotPoint = grabReference.position + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius);
            Vector3 capsuleTopPoint = grabReference.position + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);
            float angleStep = 360.0f / capsuleCastIterations;

            // create two lists: one for horizontal hits and other for top hits
            // they must have the same index, to match final result
            List<RaycastHit> horizontalHits = new List<RaycastHit>();
            List<RaycastHit> topHits = new List<RaycastHit>();

            // cast a capsule around all directions
            // it will cast a capsule in all directions to allow choose the ledge
            // that has the best direction to match current character direction
            for (int i = 0; i < capsuleCastIterations; i++)
            {
                // get current angle direction in radians
                float currentAngleRad = i * angleStep * Mathf.Deg2Rad;

                // calculate direction to cast
                Vector3 direction = new Vector3(Mathf.Cos(currentAngleRad), 0, Mathf.Sin(currentAngleRad)).normalized;

                // perform capsule cast all. It will allow to check all available ledges
                // also set start point a little back to allow more flexible ledge climbing
                RaycastHit[] hitsArray = Physics.CapsuleCastAll(capsuleBotPoint - direction * capsuleCastDistance, capsuleTopPoint - direction * capsuleCastDistance,
                    capsuleRadius, direction, capsuleCastDistance * 2, climbMask, QueryTriggerInteraction.Collide);

                // loop through all ledges found
                foreach (RaycastHit horHit in hitsArray)
                {
                    // check if this ledge is blocked
                    if (_ledgeBlocked != null && _ledgeBlocked == horHit.collider && Time.time - _timeBlockStarted < 1f)
                        continue;

                    // check if this ledge is to be ignored
                    if (ignoreTags.Contains(horHit.collider.tag)) continue;

                    // is it a valid hit?
                    if (horHit.distance != 0)
                    {  // now, perform a top cast, to check if it's a valid ledge

                        // check angle
                        if (Vector3.Dot(transform.forward, -horHit.normal) < -0.1f) continue;

                        // set start sphere cast
                        Vector3 startSphere = horHit.point;
                        startSphere.y = grabReference.position.y + sphereCastMaxHeight;

                        // perform sphere cast all
                        var topHitsArray = Physics.SphereCastAll(startSphere, sphereCastRadius, Vector3.down,
                            sphereCastDistance, climbMask, QueryTriggerInteraction.Collide);

                        // create a temporary list to choose the best hit after cast
                        List<RaycastHit> possibleTopHits = new List<RaycastHit>();
                        foreach (var topHit in topHitsArray)
                        {
                            // is it a valid hit?
                            if (topHit.distance == 0) continue;

                            // is it the same collider?
                            if (topHit.collider != horHit.collider) continue;

                            // has possible normal?
                            if (Vector3.Dot(Vector3.up, topHit.normal) < 0.5f) continue;

                            // add this hit in possible hits
                            possibleTopHits.Add(topHit);
                        }

                        // found any possible hit?
                        if (possibleTopHits.Count == 0) continue;

                        // now select the closest hit 
                        RaycastHit closestHit = possibleTopHits[0];
                        float currentDistance = Mathf.Abs(closestHit.point.y - grabReference.position.y);
                        foreach (var closestCandidate in possibleTopHits)
                        {
                            if (Mathf.Abs(closestHit.point.y - grabReference.position.y) < currentDistance)
                                closestHit = closestCandidate;
                        }

                        RaycastHit hor = horHit;
                        RaycastHit top = closestHit;

                        if (top.collider.TryGetComponent(out Ledge ledge))
                        {
                            Transform closest = ledge.GetClosestPoint(top.point);
                            if (closest != null)
                            {
                                if (Vector3.Dot(closest.forward, transform.forward) < 0.2f)
                                {
                                    hor.normal = closest.forward;
                                    top.point = closest.position;
                                }
                            }
                        }

                        // check if point is free to climb
                        if (!PositionFreeToClimb(hor, top)) continue;

                        // finally add both hits to possible selection
                        horizontalHits.Add(hor);
                        topHits.Add(top);
                    }
                }
            }

            // found any valid climbing?
            if (horizontalHits.Count == 0) return false;

            int index = 0;
            float bestDot = -1;
            for (int i = 0; i < horizontalHits.Count && i < topHits.Count; i++)
            {
                // caluclate dot to check wich ledge has the best match
                float dot = Vector3.Dot(transform.forward, -horizontalHits[i].normal);

                // if dot is greater than currento best dot, update best dot
                if (dot > bestDot)
                {
                    bestDot = dot;
                    index = i;
                }
            }

            // set controller vars

            // set current collider in use
            _currentCollider = topHits[index].collider;

            // set current raycast hits to access for positioning methods
            _currentHorizontalHit = horizontalHits[index];
            _currentTopHit = topHits[index];

            UpdateClimbHit();
            _lastHitPoint = _targetClimbCharPos.position;

            return true;
        }

        private void CheckLedgeSide()
        {
            // cast left
            CastShimmy(ref _leftDistanceToShimmy, -1);

            // cast right
            CastShimmy(ref _rightDistanceToShimmy, 1);

        }

        /// <summary>
        /// This function cast multiples spheres in side direction.
        /// It sets how many meters left to shimmy.
        /// </summary>
        /// <param name="shimmyDistance"></param>
        /// <param name="direction"></param>
        private void CastShimmy(ref float shimmyDistance, int direction)
        {
            // calculate steps to cast spheres
            float step = sideCastRange / sideCastIterations;

            // set current max distance to the maximum
            shimmyDistance = sideCastRange;

            // do iterations
            for (int i = 0; i < sideCastIterations; i++)
            {
                // set start position to cast
                Vector3 center = grabReference.position + transform.right * direction * (sideCastRange - step * i);
                Vector3 capsuleTop = center + Vector3.up * (sideCastHeight / 2f - sideCastRadius);
                Vector3 capsuleBot = center + Vector3.down * (sideCastHeight / 2f - sideCastRadius);

                // debug start sphere position
                DrawCapsule(capsuleBot, capsuleTop, sideCastRadius, debugColor);

                // create a list of hits that is available to shimmy
                List<RaycastHit> hits = new List<RaycastHit>();

                // do sphere cast and loop through all
                foreach (var hit in Physics.CapsuleCastAll(capsuleTop, capsuleBot, sideCastRadius, transform.forward,
                    capsuleCastDistance, climbMask, QueryTriggerInteraction.Collide))
                {
                    // is a valid hit?
                    if (hit.distance == 0) continue;

                    // check angle
                    if (Vector3.Dot(_currentHorizontalHit.normal, hit.normal) < 0.7f) continue;

                    // if hit is the same of current collider
                    // TODO: allow climb different collider
                    if (hit.collider == _currentCollider)
                    {
                        // add this hit to the list
                        hits.Add(hit);

                        // debug final hit pos
                        DrawSphere(hit.point, sideCastRadius, debugColor);
                        Debug.DrawLine(center, hit.point, debugColor);
                    }
                }

                // if nothing was found, update max distance available
                if (hits.Count == 0)
                    shimmyDistance = sideCastRange - step * i;
            }

            if (shimmyDistance > sideCastRange * _shimmyMinRatio) return;

            if (Mathf.Abs(_localCoordMove.x) < 0.2f) return;

            if (_localCoordMove.x < 0 && direction == 1) return;
            if (_localCoordMove.x > 0 && direction == -1) return;

            CornerOut(direction == 1 ? CornerSide.Right : CornerSide.Left);
        }

        /// <summary>
        /// this function is called inside update ability. 
        /// It assumes character has already found a ledge
        /// </summary>
        /// <returns></returns>
        private bool HasCurrentLedge()
        {
            // start sphere position for horizontal cast
            Vector3 capsuleBot = grabReference.position + Vector3.down * (sideCastHeight / 2f - sideCastRadius);
            Vector3 capsuleTop = grabReference.position + Vector3.up * (sideCastHeight / 2f - sideCastRadius);

            // debug initial sphere cast
            DrawCapsule(capsuleBot, capsuleTop, capsuleRadius, Color.red);

            // list of climbable points
            List<ClimbablePoint> climbables = new List<ClimbablePoint>();

            // do sphere cast on forward direction and loop through all hits
            foreach (var hit in Physics.CapsuleCastAll(capsuleTop, capsuleBot, capsuleRadius, transform.forward,
                capsuleCastDistance, climbMask, QueryTriggerInteraction.Collide))
            {
                // is it a valid hit?
                if (hit.distance == 0) continue;

                // only keep checking if this hit is the same of current collider or
                // if current collider is null
                // TODO: improve to allow climb other colliders
                if (hit.collider == _currentCollider)
                {
                    // debug horizontal cast found
                    DrawSphere(hit.point, capsuleRadius, Color.red);

                    // set top start cast position
                    Vector3 initial = grabReference.position + Vector3.up;
                    int lineIterations = 20;
                    // loop raycast for top
                    for (int i = 0; i < lineIterations; i++)
                    {
                        Vector3 topStart = initial + transform.forward * i * (1f / lineIterations);

                        foreach (var top in Physics.RaycastAll(topStart, Vector3.down, 3f, climbMask, QueryTriggerInteraction.Collide))
                        {
                            // is top hit valid?
                            if (top.distance == 0) continue;

                            // check if top hit is the same as horizontal hit
                            if (top.collider == hit.collider)
                            {
                                // check if point is free to climb
                                if (!PositionFreeToClimb(hit, top))
                                    continue;

                                if (Vector3.Dot(top.normal, Vector3.up) < 0.5f)
                                    continue;

                                // update current climb parameters
                                _currentCollider = top.collider;
                                _currentTopHit = top;
                                _currentHorizontalHit = hit;

                                if (ignoreTags.Contains(_currentCollider.tag))
                                    return false;


                                // correct top point
                                Vector3 point = _currentHorizontalHit.point;
                                point.y = top.point.y;
                                _currentTopHit.point = point;

                                UpdateClimbHit();

                                // debug final hit found
                                DrawSphere(top.point, sphereCastRadius, Color.red);
                                Debug.DrawLine(topStart, top.point, Color.red);

                                // debug ray
                                Debug.DrawLine(topStart, top.point, Color.red);

                                return true;
                            }
                        }

                        // debug ray
                        Debug.DrawLine(topStart, topStart + Vector3.down * sphereCastMaxHeight, Color.red);
                    }
                }


                // closest precision cast if loose current collider
                if (_currentCollider == null)
                {
                    // set top start cast position
                    Vector3 initial = hit.point;
                    initial.y = grabReference.position.y + SphereCastHeight;

                    // loop through all hits
                    foreach (var top in Physics.SphereCastAll(initial, sphereCastRadius,
                        Vector3.down, 3f, climbMask, QueryTriggerInteraction.Collide))
                    {
                        // is top hit valid?
                        if (top.distance == 0) continue;

                        // check if top hit is the same as horizontal hit
                        if (top.collider == hit.collider)
                        {
                            // check if point is free to climb
                            if (!PositionFreeToClimb(hit, top))
                                continue;

                            if (Vector3.Dot(top.normal, Vector3.up) < 0.5f)
                                continue;

                            // create climbable point
                            ClimbablePoint climbable = new ClimbablePoint();
                            climbable.horizontalHit = hit;
                            climbable.verticalHit = top;

                            // correct top point
                            Vector3 point = hit.point;
                            point.y = top.point.y;
                            climbable.verticalHit.point = point;

                            // try get ledge component
                            if (top.collider.TryGetComponent(out Ledge ledge))
                            {
                                var closest = ledge.GetClosestPoint(climbable.verticalHit.point);
                                if (closest)
                                {
                                    climbable.verticalHit.point = closest.position;
                                    climbable.horizontalHit.normal = closest.forward;
                                }
                            }

                            // calculate factor to get closest point
                            climbable.factor = Mathf.Abs(_currentTopHit.point.y - grabReference.position.y);

                            // add to list
                            climbables.Add(climbable);
                        }
                    }


                    if (climbables.Count > 0)
                    {
                        // sort by closest distance
                        climbables.Sort((x, y) => y.factor.CompareTo(x.factor));
                        var climb = climbables[0];

                        // update current climb parameters
                        _currentCollider = climb.verticalHit.collider;
                        _currentTopHit = climb.verticalHit;
                        _currentHorizontalHit = climb.horizontalHit;

                        UpdateClimbHit();

                        return true;
                    }
                }
            }

            ResetClimbVars();
            return false;
        }

        private void UpdateClimbHit()
        {
            if (Mathf.Abs(_localCoordMove.x) > 0.4f || _climbTargetHit.parent != _currentCollider.transform
                || !IsAbilityRunning || Time.time - StartTime < 0.1f)
            {
                _climbTargetHit.parent = _currentCollider.transform;
                _climbTargetHit.position = _currentTopHit.point;
                _climbTargetHit.forward = _currentHorizontalHit.normal;
            }

            _targetClimbCharPos.parent = _currentCollider.transform;
            _targetClimbCharPos.position = GetCharacterPositionOnLedge();
        }

        private bool HasWall()
        {
            Vector3 targetPos = _currentHorizontalHit.collider != null && !_isDoingTween ? GetCharacterPositionOnLedge() : transform.position;
            Vector3 direction = _currentHorizontalHit.collider != null && !_isDoingTween ? -_currentHorizontalHit.normal : transform.forward;

            Vector3 capsuleBot = targetPos + Vector3.up * footCastRadius;
            Vector3 capsuleTop = targetPos + Vector3.up * (footCapsuleHeight - footCastRadius);

            DrawCapsule(capsuleTop, capsuleBot, footCastRadius, Color.cyan);

            if (Physics.CapsuleCast(capsuleBot, capsuleTop, footCastRadius, direction, out _wallHit, footCastDistance,
                footMask, QueryTriggerInteraction.Collide))
            {
                DrawCapsule(capsuleTop + direction * _wallHit.distance, capsuleBot + direction * _wallHit.distance, footCastRadius, Color.blue);
                return true;
            }

            return false;
        }

        private void UpdateContextVars()
        {
            _context.climb = this;
            _context.ik = _climbIK;
            _context.animator = _animator;
            _context.grabReference = grabReference;
            _context.transform = transform;
            _context.currentCollider = _currentCollider;
            _context.horizontalHit = _currentHorizontalHit;
            _context.topHit = _currentTopHit;
            _context.input = _localCoordMove;
        }


        public void FinishAfterAnimation(string animationState, Vector3 targetMatchPosition, Quaternion targetMatchRotation, float targetNormalizedTime = 0.9f)
        {
            _animationStateToWait = animationState;
            _waitingAnimation = true;

            _capsule.DisableCollision();

            _matchTarget = true;
            _matchTargetPosition = targetMatchPosition;
            _matchTargetRotation = targetMatchRotation;
            _targetNormalizedTime = targetNormalizedTime;
        }
        public void FinishAfterAnimation(string animationState, float targetNormalizedTime = 0.9f)
        {
            FinishAfterAnimation(animationState, Vector3.zero, Quaternion.identity, targetNormalizedTime);
            _matchTarget = false;
        }

        private void ProccesMovement()
        {
            Vector3 CamForward = Vector3.Scale(_mainCamera.transform.forward, new Vector3(1, 0, 1));
            Vector3 cameraRelativeMove = _action.move.x * _mainCamera.transform.right + _action.move.y * CamForward;
            cameraRelativeMove.Normalize();

            _localCoordMove.x = Vector3.Dot(cameraRelativeMove, transform.right);
            _localCoordMove.y = Vector3.Dot(cameraRelativeMove, transform.forward);
        }

        private void ProccessInput()
        {
            if (_action.jump)
            {
                if (Mathf.Approximately(_localCoordMove.x, 0) || _localCoordMove.y > 0.5f)
                    ClimbUp();

                if (_localCoordMove != Vector2.zero)
                    Jump();
            }

            if (_action.drop)
                Drop();
        }

        /// <summary>
        /// This function disable logic that set character position on ledge
        /// </summary>
        public void DisableTransformUpdate()
        {
            _updateTransform = false;
        }

        /// <summary>
        /// Allow logic to set character position on ledge
        /// </summary>
        public void EnableTransformUpdate()
        {
            _updateTransform = true;

            _hangWeight = 0;
            _animator.SetFloat("HangWeight", _hangWeight);
        }

        public void DoTween(Vector3 targetPosition, Quaternion targetRotation, float duration, Collider targetLedge)
        {
            DoTween(targetPosition, targetRotation, duration, defaultMatchingCurve, targetLedge);
        }

        public void DoTween(Vector3 targetPosition, Quaternion targetRotation, float duration, AnimationCurve curve, Collider targetLedge)
        {
            // set target
            _tweenTarget.parent = targetLedge != null ? targetLedge.transform : null;

            // set base parameters for tween
            _isDoingTween = true;
            _currentTweenWeight = 0;
            _tweenDuration = duration;

            // set position paramters
            _tweenStartPosition = transform.position;
            _tweenTarget.position = targetPosition;

            // set rotation parameters
            _tweenStartRotation = transform.rotation;
            _tweenTarget.rotation = targetRotation;

            // set time control parameters
            _tweenStartTime = Time.time;
            _tweenStep = 1 / duration;

            // set curve
            _targetCurve = curve;

            // calculate bezier point
            Quaternion midRot = Quaternion.Lerp(_tweenStartRotation, _tweenTarget.rotation, 0.5f);
            Vector3 forward = midRot * Vector3.forward;
            _tweenBezierPoint = Vector3.Lerp(_tweenStartPosition, _tweenTarget.position, 0.5f) - forward;

            // stops root motion
            _mover.StopRootMotion();
        }

        private void UpdateTween()
        {
            if (!_isDoingTween) return;

            if (Time.time - _tweenStartTime > _tweenDuration + 0.1f || Mathf.Approximately(_currentTweenWeight, 1f))
            {
                if (_tweenTarget.position != Vector3.zero)
                    _mover.SetPosition(_tweenTarget.position);

                transform.rotation = _tweenTarget.rotation;

                _targetClimbCharPos.parent = _tweenTarget.parent;
                _targetClimbCharPos.position = _tweenTarget.position;
                _targetClimbCharPos.rotation = _tweenTarget.rotation;
                _lastHitPoint = _tweenTarget.position;

                _isDoingTween = false;
                return;
            }

            _currentTweenWeight = Mathf.MoveTowards(_currentTweenWeight, 1, _tweenStep * Time.deltaTime);

            float weight = _targetCurve.Evaluate(_currentTweenWeight);

            if (_tweenTarget.position != Vector3.zero)
            {
                if (Quaternion.Dot(_tweenStartRotation, _tweenTarget.rotation) > 0.85f)
                    _mover.SetPosition(Vector3.Lerp(_tweenStartPosition, _tweenTarget.position, weight));
                else
                    _mover.SetPosition(BezierLerp(_tweenStartPosition, _tweenTarget.position, _tweenBezierPoint, weight));
            }

            transform.rotation = Quaternion.Lerp(_tweenStartRotation, _tweenTarget.rotation, weight);
        }

        public Vector3 BezierLerp(Vector3 start, Vector3 end, Vector3 bezier, float t)
        {
            Vector3 point = Mathf.Pow(1 - t, 2) * start;
            point += 2 * (1 - t) * t * bezier;
            point += t * t * end;

            return point;
        }

        // blocks current time to be climbed during 1 second
        public void BlockCurrentLedge()
        {
            _ledgeBlocked = _currentCollider;
            _timeBlockStarted = Time.time;
        }

        public bool PositionFreeToClimb(RaycastHit horHit, RaycastHit topHit)
        {
            Vector3 targetCharacterPosition = GetCharacterPositionOnLedge(horHit, topHit);

            Vector3 bot = targetCharacterPosition + Vector3.up * _capsule.GetCapsuleRadius();
            Vector3 top = targetCharacterPosition + Vector3.up * (_capsule.GetCapsuleHeight() - _capsule.GetCapsuleRadius());

            if (Physics.OverlapCapsule(bot, top, _capsule.GetCapsuleRadius(), obstacleMask, QueryTriggerInteraction.Ignore).Length > 0)
                return false;

            return true;
        }

        private void SetCharacterPosition()
        {
            if (_isDoingTween || !_updateTransform) return;

            _mover.SetPosition(GetCharacterPositionOnLedge());
            transform.rotation = GetCharacterRotationOnLedge();
        }

        private Vector3 GetCharacterPositionOnLedge()
        {
            Vector3 normal = _climbTargetHit.forward;
            normal.y = 0;
            normal.Normalize();

            return _climbTargetHit.position + Vector3.up * offsetOnLedge.y + normal * offsetOnLedge.x;
        }

        public Vector3 GetCharacterPositionOnLedge(RaycastHit horHit, RaycastHit topHit)
        {
            Vector3 normal = horHit.normal;
            normal.y = 0;
            normal.Normalize();

            return topHit.point + Vector3.up * offsetOnLedge.y + normal * offsetOnLedge.x;
        }

        private Quaternion GetCharacterRotationOnLedge()
        {
            Vector3 normal = _climbTargetHit.forward;
            normal.y = 0;
            normal.Normalize();

            return Quaternion.LookRotation(-normal);
        }

        public Quaternion GetCharacterRotationOnLedge(RaycastHit horHit)
        {
            Vector3 normal = horHit.normal;
            normal.y = 0;
            normal.Normalize();

            return Quaternion.LookRotation(-normal);
        }

        public void ResetClimbVars()
        {
            _currentHorizontalHit = new RaycastHit();
            _currentTopHit = new RaycastHit();
            _currentCollider = null;
        }

        private void OnDrawGizmos()
        {
            // 기존 Gizmo
            if (grabReference != null && !IsAbilityRunning)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(grabReference.position, globalRadiusDetection);
            }
            //// 선택 모드 탐색 반경 (노란색)
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawWireSphere(transform.position, searchRadius);

            if (grabReference == null) return;

            // 1) 수평 캡슐 캐스트 범위 (capsuleCastDistance)
            Gizmos.color = Color.red;
            // grabReference 위치에서 반경만큼 원형으로 표시
            Gizmos.DrawWireSphere(grabReference.position, capsuleCastDistance);
            Handles.color = Color.red;
            Handles.DrawWireDisc(grabReference.position, Vector3.up, capsuleCastDistance);

            // 2) 상단 스피어 캐스트 시작점 & 반경 (sphereCastMaxHeight, sphereCastRadius)
            Gizmos.color = Color.yellow;
            Vector3 sphereCenter = grabReference.position
                                   + Vector3.up * sphereCastMaxHeight;
            Gizmos.DrawWireSphere(sphereCenter, sphereCastRadius);
            // 아래로 내리찍는 영역 표시
            Gizmos.DrawLine(
                sphereCenter,
                sphereCenter + Vector3.down * sphereCastDistance
            );

            // 3) (선택) globalRadiusDetection 시각화 유지
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(
                grabReference.position,
                globalRadiusDetection
            );
        }
        

        public void DrawSphere(Vector3 center, float radius, Color color, float duration = 0)
        {
            if (_debug)
                _debug.DrawSphere(center, radius, color, duration);
        }

        public void DrawCapsule(Vector3 p1, Vector3 p2, float radius, Color color, float duration = 0)
        {
            if (_debug)
                _debug.DrawCapsule(p1, p2, radius, color, duration);
        }

        public void DrawLabel(string text, Vector3 position, Color color, float duration = 0)
        {
            if (_debug)
                _debug.DrawLabel(text, position, color, duration);
        }

        //// 기능 추가: 입력 벡터로 방향키 W/A/S/D를 반환
        //private KeyCode DetectDirectionKey(Vector2 move)
        //{
        //    if (move == Vector2.zero)
        //        return KeyCode.None;
        //    float absX = Mathf.Abs(move.x);
        //    float absY = Mathf.Abs(move.y);
        //    if (absY >= absX)
        //        return move.y > 0 ? KeyCode.W : KeyCode.S;
        //    else
        //        return move.x > 0 ? KeyCode.D : KeyCode.A;
        //}

        //// 기능 추가: 선택 모드 입력 및 시각 처리
        //private void HandleSelectionMode()
        //{
        //    // 1) 진입: W/A/S/D 방향키 입력으로 선택 모드 시작
        //    if (_selState == SelectionState.None && _action.move != Vector2.zero)
        //    {
        //        KeyCode key = DetectDirectionKey(_action.move);
        //        if (key != KeyCode.None)
        //            EnterHighlightState(key);
        //    }
        //    // 2) 하이라이트 또는 네비게이션 중
        //    else if (_selState == SelectionState.Highlight || _selState == SelectionState.Navigating)
        //    {
        //        // ← 또는 → 를 누르면 후보 순환 (엣지 감지 포함)
        //        if (_action.selectNext || _action.selectPrev)
        //            CheckNavigate();

        //        // 선택 확정
        //        else if (_action.confirmSelect)
        //            ConfirmSelection();

        //        // 타임아웃 시 자동 취소
        //        else if (Time.time > _highlightStartTime + 3f)
        //            CancelSelectionMode();
        //    }
        //}

        //// 1) 선택 진입 & 하이라이트
        //// 1) 진입 & 하이라이트
        //private void EnterHighlightState(KeyCode key)
        //{
        //    Debug.Log($"[ClimbAbility] EnterHighlightState: {key}");

        //    _action.selectionKey = key;               // 진입 키 저장
        //    _selState = SelectionState.Highlight;
        //    _highlightStartTime = Time.time;
        //    _candidates.Clear();

        //    // OverlapSphereNonAlloc로 주변 콜라이더 수집
        //    int count = Physics.OverlapSphereNonAlloc(
        //        transform.position, searchRadius,
        //        _overlapBuffer, climbMask);

        //    for (int i = 0; i < count; i++)
        //    {
        //        Collider col = _overlapBuffer[i];
        //        Transform t = col.transform;
        //        Vector3 localPos = transform
        //            .InverseTransformPoint(t.position)
        //            .normalized;
        //        if (!IsInDirection(localPos, key))
        //            continue;

        //        // CandidateData로 저장 (collider, point, normal)
        //        _candidates.Add(new CandidateData
        //        {
        //            collider = col,
        //            point = t.position,
        //            normal = t.forward
        //        });
        //    }

        //    // EnterHighlightState 내부, 후보 수집 후
        //    Debug.Log($"[ClimbAbility] candidates count = {_candidates.Count}");

        //    // 후보 없으면 취소
        //    if (_candidates.Count == 0)
        //    {
        //        CancelSelectionMode();
        //        return;
        //    }

        //    // UI 하이라이트: 전체 노랑 → 첫 번째 빨강
        //    foreach (var c in _candidates)
        //        SetMaterialColor(c.collider.transform, Color.yellow);

        //    _currentCandidateIdx = 0;
        //    SetMaterialColor(
        //        _candidates[0].collider.transform,
        //        Color.red
        //    );
        //}

        //// 기능 추가: 후보 순환
        //// 엣지 감지와 함께 호출하는 헬퍼
        //private void CheckNavigate()
        //{
        //    if (_action.selectNext && !_prevSelectNext)
        //        DoNavigate(+1);
        //    if (_action.selectPrev && !_prevSelectPrev)
        //        DoNavigate(-1);

        //    _prevSelectNext = _action.selectNext;
        //    _prevSelectPrev = _action.selectPrev;
        //}

        //// 실제 순환 처리
        //private void DoNavigate(int dir)
        //{
        //    Debug.Log($"[ClimbAbility] CheckNavigate selectNext={_action.selectNext} selectPrev={_action.selectPrev}");


        //    if (_candidates.Count == 0) return;

        //    // 이전 빨강 → 노랑
        //    var prevT = _candidates[_currentCandidateIdx].collider.transform;
        //    SetMaterialColor(prevT, Color.yellow);

        //    // 인덱스 갱신
        //    _currentCandidateIdx =
        //        (_currentCandidateIdx + dir + _candidates.Count)
        //        % _candidates.Count;

        //    // 새 노랑 → 빨강
        //    var currT = _candidates[_currentCandidateIdx].collider.transform;
        //    SetMaterialColor(currT, Color.red);

        //    _selState = SelectionState.Navigating;
        //}

        //// 기능 추가: 선택 확정 및 이동
        //// 3) 스페이스바 확정 → 기존 부드러운 클라이밍 로직 재활용
        //// 3) 스페이스바 확정 → 기존 부드러운 이동 로직 재활용
        //private void ConfirmSelection()
        //{
        //    Debug.Log($"[ClimbAbility] ConfirmSelection idx={_currentCandidateIdx} target={_candidates[_currentCandidateIdx].collider.name}");

        //    // 3-1) 선택된 후보 데이터 꺼내오기
        //    var cd = _candidates[_currentCandidateIdx];
        //    _currentCollider = cd.collider;
        //    _currentHorizontalHit.point = cd.point;
        //    _currentHorizontalHit.normal = cd.normal;
        //    _currentTopHit.point = cd.point;
        //    // 필요시 _currentTopHit.normal = cd.normal;

        //    // 3-2) 부드러운 매칭/이동 호출
        //    UpdateClimbHit();  // 내부: _climbTargetHit·_targetClimbCharPos 세팅

        //    Debug.Log("[ClimbAbility] Calling DoTween()");
        //    DoTween(
        //        GetCharacterPositionOnLedge(),
        //        GetCharacterRotationOnLedge(),
        //        startClimbMatchTime,
        //        defaultMatchingCurve,
        //        _currentCollider
        //    );

        //    // 3-3) 시각 효과 초기화
        //    foreach (var c in _candidates)
        //        SetMaterialColor(c.collider.transform, Color.white);

        //    _candidates.Clear();
        //    _selState = SelectionState.None;
        //}


        //// 4) 취소
        //private void CancelSelectionMode()
        //{
        //    foreach (var c in _candidates)
        //        SetMaterialColor(c.collider.transform, Color.white);

        //    _candidates.Clear();
        //    _selState = SelectionState.None;
        //}

        //// 기능 추가: 특정 방향키 영역 판정
        //private bool IsInDirection(Vector3 localPos, KeyCode key)
        //{
        //    switch (key)
        //    {
        //        case KeyCode.W:
        //            return localPos.z > Mathf.Abs(localPos.x);
        //        case KeyCode.S:
        //            return -localPos.z > Mathf.Abs(localPos.x);
        //        case KeyCode.A:
        //            return localPos.x < -Mathf.Abs(localPos.z);
        //        case KeyCode.D:
        //            return localPos.x > Mathf.Abs(localPos.z);
        //    }
        //    return false;
        //}

        //// 기능 추가: 머티리얼 색상 변경 유틸
        //private void SetMaterialColor(Transform t, Color c)
        //{
        //    var rend = t.GetComponent<Renderer>();
        //    if (rend) rend.material.color = c;
        //}

    }

}