using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 20f;

    [Header("Lifetime")]
    public float lifetime = 5f;

    [Header("Impact VFX")]
    public GameObject impactEffectPrefab;
    public float impactEffectLifetime = 2f;

    private bool hasHit;
    private GameObject source;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Initialize(GameObject sourceObject)
    {
        source = sourceObject;
        IgnoreSourceCollisions();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
            return;

        if (source != null && collision.transform.root == source.transform.root)
            return;

        hasHit = true;

        ContactPoint contact = collision.contacts[0];
        Vector3 hitPoint = contact.point;
        Vector3 hitNormal = contact.normal;

        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 hitDirection = (rb != null && rb.linearVelocity.sqrMagnitude > 0.001f)
            ? rb.linearVelocity.normalized
            : transform.forward;

        IDamageable damageable = FindDamageable(collision.collider);
        if (damageable != null)
        {
            DamageRequest request = new DamageRequest(
                damage,
                source != null ? source : gameObject,
                hitPoint,
                hitNormal,
                hitDirection
            );

            damageable.TakeDamage(request);
        }

        if (impactEffectPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(contact.normal);
            GameObject vfx = Instantiate(impactEffectPrefab, hitPoint, rot);
            Destroy(vfx, impactEffectLifetime);
        }

        Destroy(gameObject);
    }

    private void IgnoreSourceCollisions()
    {
        if (source == null)
            return;

        Collider[] projectileColliders = GetComponentsInChildren<Collider>(true);
        Collider[] sourceColliders = source.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            if (projectileColliders[i] == null)
                continue;

            for (int j = 0; j < sourceColliders.Length; j++)
            {
                if (sourceColliders[j] == null)
                    continue;

                Physics.IgnoreCollision(projectileColliders[i], sourceColliders[j], true);
            }
        }
    }

    private IDamageable FindDamageable(Collider col)
    {
        MonoBehaviour[] behaviours = col.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDamageable damageable)
                return damageable;
        }

        return null;
    }
}