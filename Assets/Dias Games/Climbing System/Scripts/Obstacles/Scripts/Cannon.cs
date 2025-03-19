using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    
    public GameObject projectilePrefab; // 발사체 프리팹
    public GameObject ExplosionPre;
    public Vector3 fireDirection = Vector3.forward; // 발사 방향
    public float fireRate = 2f; // 발사 시간 1초에 fireRate값만큼 발사
    public float projectileSpeed = 10f; // 발사 파워 (속도)

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
        Vector3 endPoint = gameObject.transform.position + fireDirection * projectileSpeed; // 발사 방향과 발사 파워를 곱한 지점 계산
        Gizmos.DrawLine(gameObject.transform.position, endPoint); // 발사 방향과 발사 파워를 나타내는 선 그리기
    }
    void Shoot()
    {
        GameObject projectile = Instantiate(projectilePrefab, gameObject.transform.position, Quaternion.LookRotation(fireDirection));
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        AudioManager.instance.PlaySFX(6);
        // 파티클 컴포넌트 가져오기
        ParticleSystem particleSystem = ExplosionPre.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            // 파티클 시작
            ExplosionPre.SetActive(true);
            particleSystem.Play();
        }
        if (rb != null)
        {
            rb.velocity = fireDirection * projectileSpeed; // 발사 파워를 사용하여 속도 부여
        }
    }
}
