using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropRocks : MonoBehaviour
{
    [SerializeField]
    private float deleteTime = 2.0f;
    public float rotationSpeed = 50f; // ���� ȸ���� ���ӵ�
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, deleteTime);
    }
    private void Update()
    {
        if(gameObject.CompareTag("Stone"))
        {
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        }
    }


}
