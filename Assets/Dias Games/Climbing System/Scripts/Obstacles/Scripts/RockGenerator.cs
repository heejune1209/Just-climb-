using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class RockGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject rocks;

    [System.Serializable]
    public class DropAreaInfo
    {
        public GameObject dropArea;
        public float dropTime;
        public float turnonLight;
        //public float rockDropInterval = 2f; // 돌 생성 간격(태그가 스톤샤워일경우)
    }

    public List<DropAreaInfo> dropAreaTimes = new List<DropAreaInfo>();
    //private bool isPlayerInside = false; // 플레이어가 영역 안에 있는지 여부를 추적합니다.


    //private Coroutine rockDropCoroutine; // 돌 생성 코루틴을 저장할 변수


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartDroppingRocks();
        }
    }
    /*
    private void OnTriggerStay(Collider other)
    {
        isPlayerInside = true;
        if (other.CompareTag("Player") && isPlayerInside && gameObject.CompareTag("Stone Shower"))
        {
            StartDroppingRocks(); // 플레이어가 영역에 있으면 돌 생성 시작
            rockDropCoroutine = StartCoroutine(DropRocksWithInterval()); // 돌 생성 코루틴 시작
        }
    }
    
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || gameObject.CompareTag("Stone Shower"))
        {
            isPlayerInside = false;
            if (rockDropCoroutine != null && (isPlayerInside = false))
            {
                StopCoroutine(rockDropCoroutine); // 플레이어가 영역을 빠져나가면 코루틴을 멈춥니다.
                rockDropCoroutine = null;
            }
        }
    }
    
    
    private IEnumerator DropRocksWithInterval()
    {
        while (isPlayerInside) // 플레이어가 영역 안에 있을 때
        {
            foreach (DropAreaInfo dropAreaInfo in dropAreaTimes)
            {
                GameObject dropAreaCenter = dropAreaInfo.dropArea;
                SpawnRocks(dropAreaCenter); // 해당 dropArea에 따른 위치에 돌 생성
                yield return new WaitForSeconds(dropAreaInfo.rockDropInterval); // 각 DropAreaInfo에 설정된 dropTime만큼 대기
            }
        }
    }
    */
    async void StartDroppingRocks()
    {
        List<Task> tasks = new List<Task>();

        foreach (DropAreaInfo dropAreaTime in dropAreaTimes)
        {
            AudioManager.instance.PlaySFX(5);
            GameObject dropArea = dropAreaTime.dropArea;
            float dropTime = dropAreaTime.dropTime;
            float turnonLight = dropAreaTime.turnonLight;

            tasks.Add(DropRockWithDelay(dropArea, dropTime, turnonLight));
        }

        await Task.WhenAll(tasks);
    }
    // 이 스크립트에서 쓰는 방법은 일처리를 병렬적으로 함. 즉 인덱스가 순서대로(첫번째꺼 끝나고 두번째것이 진행)진행이 아니라 각각 개별적으로 가능.
    async Task DropRockWithDelay(GameObject dropArea, float dropTime, float turnonLight)
    {
        DisplayLight(dropArea);
        await Task.Delay((int)(turnonLight * 1000)); // Milliseconds, 1초는 1000밀리초입니다. dropTime이 1.5초라면 (int)(dropTime * 1000)은 1500임
        OffLight(dropArea);
        await Task.Delay((int)(dropTime * 1000)); // Milliseconds
        SpawnRocks(dropArea);
        await Task.Delay((int)(dropTime * 1000)); // Milliseconds
    }
    

    private void DisplayLight(GameObject dropAreaCenter)
    {
        Light lightComponent = dropAreaCenter.GetComponent<Light>();
        lightComponent.enabled = true;
    }

    private void OffLight(GameObject dropAreaCenter)
    {
        Light lightComponent = dropAreaCenter.GetComponent<Light>();
        lightComponent.enabled = false;
    }

    void SpawnRocks(GameObject dropAreaCenter)
    {
        Quaternion rotation = Quaternion.Euler(30f, 45f, 60f); // 회전값 설정
        Instantiate(rocks, dropAreaCenter.transform.position, rotation);
    }
}