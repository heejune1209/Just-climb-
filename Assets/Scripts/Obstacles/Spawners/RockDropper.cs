using JustClimb.Obstacles.Core;
using JustClimb.Obstacles.Data;
using System.Collections;
using UnityEngine;
using Zenject;

namespace JustClimb.Obstacles.Spawners
{
    [RequireComponent(typeof(ObstacleTrigger))]
    public class RockDropper : ObstacleBase
    {
        [Inject] private IResourceManager _resourceManager;
        [Inject] private ISoundManager _soundmanager;

        [Header("Dropper 설정 데이터")]
        [Tooltip("에디터에서 할당할 DropperData SO")]
        public DropperData data;

        [Header("낙사지점")]
        [Tooltip("바위가 생성되어 떨어질 위치 Transform")]
        public Transform dropPoint;

        // 떨어뜨리기 반복을 제어할 코루틴 핸들
        private Coroutine _dropRoutine;

        // 플레이어가 영역에 들어왔을 때 호출되어 바위 낙하 루틴을 시작.
        public override void Activate()
        {
            if (_dropRoutine == null)
                _dropRoutine = StartCoroutine(DropRoutine());
        }

        // 플레이어가 영역에서 나갔을 때 호출되어 낙하 루틴을 중지.
        public override void Deactivate()
        {
            if (_dropRoutine != null)
            {
                StopCoroutine(_dropRoutine);
                _dropRoutine = null;
            }
        }

        // 경고 시간 경과 후 지정된 간격으로 바위를 스폰하는 코루틴
        private IEnumerator DropRoutine()
        {
            // 1) 경고등 표시 (Light 컴포넌트가 자식에 있을 경우)
            Light warningLight = GetComponentInChildren<Light>();
            if (warningLight != null)
            {
                warningLight.enabled = true;
                yield return new WaitForSeconds(data.warnTime);
                warningLight.enabled = false;
            }
            else
            {
                // Light가 없더라도 경고 시간만큼 대기
                yield return new WaitForSeconds(data.warnTime);
            }

            // 2) 바위 낙하 반복
            WaitForSeconds interval = new WaitForSeconds(data.rate);
            while (true)
            {
                // 풀링+위치·회전 지정 오버로드 사용
                _resourceManager.Instantiate(
                $"Prefabs/Obstacles/{data.rockPrefab.name}",
                dropPoint.position,
                dropPoint.rotation, null, data._initialpoolcount);
                _soundmanager.PlaySFX(5);

                yield return interval;
            }
        }

        // 메모리 누수 방지
        protected override void OnDestroy()
        {
            // 코루틴 정리
            if (_dropRoutine != null)
            {
                StopCoroutine(_dropRoutine);
                _dropRoutine = null;
            }
            
            // 컴포넌트 참조 해제
            data = null;
            dropPoint = null;
            
            // 매니저 참조 해제
            _resourceManager = null;
            _soundmanager = null;
        }
    }
}
