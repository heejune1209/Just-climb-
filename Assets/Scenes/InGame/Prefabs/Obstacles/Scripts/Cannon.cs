using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    
    public GameObject projectilePrefab; // �߻�ü ������
    public GameObject ExplosionPre;
    public Vector3 fireDirection = Vector3.forward; // �߻� ����
    public float fireRate = 2f; // �߻� �ð� 1�ʿ� fireRate����ŭ �߻�
    public float projectileSpeed = 10f; // �߻� �Ŀ� (�ӵ�)

    private float fireCountdown = 0f;
    private bool isShooting = false;

    

    public void StartShoot()
    {
        isShooting = true;
        if (isShooting)
        {
            if (fireCountdown <= 0f)
            {
                Shoot();
                fireCountdown = 1f / fireRate;
            }

            fireCountdown -= Time.deltaTime;
        }
    }
    public void StopShoot()
    {
        isShooting = false;
        ExplosionPre.SetActive(false);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Vector3 endPoint = gameObject.transform.position + fireDirection * projectileSpeed; // �߻� ����� �߻� �Ŀ��� ���� ���� ���
        Gizmos.DrawLine(gameObject.transform.position, endPoint); // �߻� ����� �߻� �Ŀ��� ��Ÿ���� �� �׸���
    }
    void Shoot()
    {
        GameObject projectile = Instantiate(projectilePrefab, gameObject.transform.position, Quaternion.LookRotation(fireDirection));
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        Managers.Sound.PlaySFX(6);
        // ��ƼŬ ������Ʈ ��������
        ParticleSystem particleSystem = ExplosionPre.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            // ��ƼŬ ����
            ExplosionPre.SetActive(true);
            particleSystem.Play();
        }
        if (rb != null)
        {
            rb.velocity = fireDirection * projectileSpeed; // �߻� �Ŀ��� ����Ͽ� �ӵ� �ο�
        }
    }
}
