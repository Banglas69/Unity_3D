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

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
            return;

        hasHit = true;

        ContactPoint contact = collision.contacts[0];
        Vector3 hitPoint = contact.point;
        Vector3 hitDirection = GetComponent<Rigidbody>() != null && GetComponent<Rigidbody>().linearVelocity.sqrMagnitude > 0.001f
            ? GetComponent<Rigidbody>().linearVelocity.normalized
            : transform.forward;

        IDamageable damageable = FindDamageable(collision.collider);
        if (damageable != null)
        {
            damageable.TakeDamage(damage, hitPoint, hitDirection);
        }

        if (impactEffectPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(contact.normal);
            GameObject vfx = Instantiate(impactEffectPrefab, hitPoint, rot);
            Destroy(vfx, impactEffectLifetime);
        }

        Destroy(gameObject);
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