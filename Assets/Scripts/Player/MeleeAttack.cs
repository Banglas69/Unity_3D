using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;

    [Header("Damage")]
    public float damage = 25f;

    [Header("Range")]
    public float range = 1.5f;
    [Range(1f, 180f)] public float coneAngle = 100f;
    public float overlapRadius = 1.25f;
    public LayerMask hitMask = ~0;

    [Header("Timing")]
    public float attackCooldown = 0.35f;

    [Header("Impact VFX")]
    public GameObject hitEffectPrefab;
    public float hitEffectLifetime = 2f;

    [Header("Debug")]
    public bool drawDebugGizmos = true;
    public bool drawWhenNotSelected = false;

    private float cooldownTimer;

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(1))
            TryMeleeAttack();
    }

    private void TryMeleeAttack()
    {
        if (cooldownTimer > 0f)
            return;

        if (firePoint == null)
            return;

        cooldownTimer = attackCooldown;

        SoundManager.Instance?.PlayOneShot2D(SoundId.Melee);

        Vector3 origin = firePoint.position;
        Vector3 forward = firePoint.forward;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            overlapRadius,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];

            if (col == null)
                continue;

            if (col.transform.root == transform.root)
                continue;

            IDamageable damageable = FindDamageable(col);
            if (damageable == null)
                continue;

            if (damagedTargets.Contains(damageable))
                continue;

            Vector3 closestPoint = GetSafeClosestPoint(col, origin);
            Vector3 toTarget = closestPoint - origin;
            float distance = toTarget.magnitude;

            if (distance > range)
                continue;

            if (distance > 0.001f)
            {
                float angleToTarget = Vector3.Angle(forward, toTarget.normalized);
                if (angleToTarget > coneAngle * 0.5f)
                    continue;
            }

            damagedTargets.Add(damageable);

            Vector3 hitDirection = toTarget.sqrMagnitude > 0.001f
                ? toTarget.normalized
                : forward;

            Vector3 hitNormal = -hitDirection;

            DamageRequest request = new DamageRequest(
                damage,
                gameObject,
                closestPoint,
                hitNormal,
                hitDirection
            );

            damageable.TakeDamage(request);

            if (hitEffectPrefab != null)
            {
                Quaternion rot = Quaternion.LookRotation(hitDirection);
                GameObject vfx = Instantiate(hitEffectPrefab, closestPoint, rot);
                Destroy(vfx, hitEffectLifetime);
            }
        }
    }

    private Vector3 GetSafeClosestPoint(Collider col, Vector3 position)
    {
        if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider)
            return col.ClosestPoint(position);

        MeshCollider mesh = col as MeshCollider;
        if (mesh != null && mesh.convex)
            return col.ClosestPoint(position);

        return col.bounds.ClosestPoint(position);
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

    private void OnDrawGizmos()
    {
        if (drawWhenNotSelected)
            DrawMeleeGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawWhenNotSelected)
            DrawMeleeGizmos();
    }

    private void DrawMeleeGizmos()
    {
        if (!drawDebugGizmos || firePoint == null)
            return;

        Vector3 origin = firePoint.position;
        Vector3 forward = firePoint.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, overlapRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + forward * range);

        Vector3 leftEdge = Quaternion.AngleAxis(-coneAngle * 0.5f, firePoint.up) * forward;
        Vector3 rightEdge = Quaternion.AngleAxis(coneAngle * 0.5f, firePoint.up) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + leftEdge * range);
        Gizmos.DrawLine(origin, origin + rightEdge * range);
    }
}