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
        //public float rockDropInterval = 2f; // �� ���� ����(�±װ� ��������ϰ��)
    }

    public List<DropAreaInfo> dropAreaTimes = new List<DropAreaInfo>();
    //private bool isPlayerInside = false; // �÷��̾ ���� �ȿ� �ִ��� ���θ� �����մϴ�.


    //private Coroutine rockDropCoroutine; // �� ���� �ڷ�ƾ�� ������ ����


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
            StartDroppingRocks(); // �÷��̾ ������ ������ �� ���� ����
            rockDropCoroutine = StartCoroutine(DropRocksWithInterval()); // �� ���� �ڷ�ƾ ����
        }
    }
    
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || gameObject.CompareTag("Stone Shower"))
        {
            isPlayerInside = false;
            if (rockDropCoroutine != null && (isPlayerInside = false))
            {
                StopCoroutine(rockDropCoroutine); // �÷��̾ ������ ���������� �ڷ�ƾ�� ����ϴ�.
                rockDropCoroutine = null;
            }
        }
    }
    
    
    private IEnumerator DropRocksWithInterval()
    {
        while (isPlayerInside) // �÷��̾ ���� �ȿ� ���� ��
        {
            foreach (DropAreaInfo dropAreaInfo in dropAreaTimes)
            {
                GameObject dropAreaCenter = dropAreaInfo.dropArea;
                SpawnRocks(dropAreaCenter); // �ش� dropArea�� ���� ��ġ�� �� ����
                yield return new WaitForSeconds(dropAreaInfo.rockDropInterval); // �� DropAreaInfo�� ������ dropTime��ŭ ���
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
    // �� ��ũ��Ʈ���� ���� ����� ��ó���� ���������� ��. �� �ε����� �������(ù��°�� ������ �ι�°���� ����)������ �ƴ϶� ���� ���������� ����.
    async Task DropRockWithDelay(GameObject dropArea, float dropTime, float turnonLight)
    {
        DisplayLight(dropArea);
        await Task.Delay((int)(turnonLight * 1000)); // Milliseconds, 1�ʴ� 1000�и����Դϴ�. dropTime�� 1.5�ʶ�� (int)(dropTime * 1000)�� 1500��
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
        Quaternion rotation = Quaternion.Euler(30f, 45f, 60f); // ȸ���� ����
        Instantiate(rocks, dropAreaCenter.transform.position, rotation);
    }
}