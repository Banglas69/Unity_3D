using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Shooting")]
    public float projectileSpeed = 25f;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            Shoot();
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        SoundManager.Instance?.PlayOneShot2D(SoundId.Shoot);

        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = firePoint.forward * projectileSpeed;
    }
}