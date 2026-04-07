using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public string playerTag = "Player";
    public Transform eyePoint;
    public Transform attackPoint;

    [Header("Movement")]
    public float idleMoveSpeed = 2f;
    public float chaseMoveSpeed = 3.5f;
    public float turnSpeed = 360f;

    [Header("Gravity")]
    public float gravity = 25f;
    public float groundedStickForce = 2f;

    [Header("Idle Patrol")]
    public float minTurnInterval = 1.5f;
    public float maxTurnInterval = 3.5f;
    public float randomTurnAngleMin = 60f;
    public float randomTurnAngleMax = 140f;

    [Header("Wall Detection")]
    public float wallCheckDistance = 1.0f;
    public float wallCheckHeight = 0.8f;
    public LayerMask wallMask = ~0;

    [Header("Player Detection")]
    public float detectionRange = 10f;
    public LayerMask detectionMask = ~0;

    [Header("Melee Attack")]
    public float meleeDamage = 20f;
    public float meleeRange = 1.6f;
    public float meleeRadius = 0.8f;
    public float attackCooldown = 1.0f;
    public float attackHitDelay = 0.15f;
    public LayerMask attackMask = ~0;

    [Header("Attack VFX")]
    public GameObject attackEffectPrefab;
    public float attackEffectLifetime = 2f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public bool drawWhenNotSelected = false;

    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }

    private EnemyState currentState;
    private CharacterController cc;

    private Vector3 patrolDirection;
    private float patrolTurnTimer;

    private float nextAttackTime;
    private bool isAttacking;
    private float verticalVelocity;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        IdleState = new EnemyIdleState(this);
        ChaseState = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
    }

    private void Start()
    {
        FindPlayerIfMissing();
        ChangeState(IdleState);
    }

    private void Update()
    {
        FindPlayerIfMissing();
        GravityTick();
        currentState?.Tick();
    }

    public void ChangeState(EnemyState newState)
    {
        if (newState == null)
            return;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void GravityTick()
    {
        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -groundedStickForce;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
    }

    public void BeginIdlePatrol()
    {
        patrolDirection = transform.forward;
        ResetPatrolTimer();
    }

    public void UpdateIdlePatrol()
    {
        patrolTurnTimer -= Time.deltaTime;

        if (WallAhead() || patrolTurnTimer <= 0f)
            ChooseNewPatrolDirection();

        MoveInDirection(patrolDirection, idleMoveSpeed);
    }

    private void ChooseNewPatrolDirection()
    {
        float angle = Random.Range(randomTurnAngleMin, randomTurnAngleMax);
        float sign = Random.value < 0.5f ? -1f : 1f;

        patrolDirection = Quaternion.Euler(0f, angle * sign, 0f) * transform.forward;
        patrolDirection.y = 0f;
        patrolDirection.Normalize();

        ResetPatrolTimer();
    }

    private void ResetPatrolTimer()
    {
        patrolTurnTimer = Random.Range(minTurnInterval, maxTurnInterval);
    }

    private bool WallAhead()
    {
        Vector3 origin = transform.position + Vector3.up * wallCheckHeight;
        return Physics.Raycast(origin, transform.forward, wallCheckDistance, wallMask, QueryTriggerInteraction.Ignore);
    }

    public void ChasePlayer()
    {
        Vector3 dir = GetFlatDirectionToPlayer();
        if (dir.sqrMagnitude <= 0.001f)
            return;

        RotateTowards(dir);

        if (WallAhead())
            return;

        MoveInDirection(dir, chaseMoveSpeed);
    }

    public void FacePlayer()
    {
        Vector3 dir = GetFlatDirectionToPlayer();
        if (dir.sqrMagnitude <= 0.001f)
            return;

        RotateTowards(dir);
    }

    public void TryAttack()
    {
        if (isAttacking)
            return;

        if (Time.time < nextAttackTime)
            return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        SpawnAttackEffect();

        yield return new WaitForSeconds(attackHitDelay);

        Vector3 origin = attackPoint != null
            ? attackPoint.position
            : transform.position + Vector3.up * 1.0f + transform.forward * 0.8f;

        Collider[] hits = Physics.OverlapSphere(origin, meleeRadius, attackMask, QueryTriggerInteraction.Ignore);

        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.root == transform.root)
                continue;

            IDamageable damageable = FindDamageable(hits[i]);
            if (damageable == null)
                continue;

            if (hitTargets.Contains(damageable))
                continue;

            hitTargets.Add(damageable);

            Vector3 hitPoint = hits[i].ClosestPoint(origin);
            Vector3 hitDirection = (hitPoint - origin).sqrMagnitude > 0.001f
                ? (hitPoint - origin).normalized
                : transform.forward;

            Vector3 hitNormal = -hitDirection;

            DamageRequest request = new DamageRequest(
                meleeDamage,
                gameObject,
                hitPoint,
                hitNormal,
                hitDirection
            );

            damageable.TakeDamage(request);
        }

        isAttacking = false;
    }

    private void SpawnAttackEffect()
    {
        if (attackEffectPrefab == null)
            return;

        Vector3 pos = attackPoint != null
            ? attackPoint.position
            : transform.position + Vector3.up * 1.0f + transform.forward * 0.8f;

        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);
        GameObject vfx = Instantiate(attackEffectPrefab, pos, rot);
        Destroy(vfx, attackEffectLifetime);
    }

    public bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = eyePoint != null
            ? eyePoint.position
            : transform.position + Vector3.up * 1.0f;

        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 toPlayer = target - origin;
        float distance = toPlayer.magnitude;

        if (distance > detectionRange)
            return false;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, distance, detectionMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root == player.root;
        }

        return false;
    }

    public bool IsPlayerInAttackRange()
    {
        return IsPlayerInAttackRange(meleeRange);
    }

    public bool IsPlayerInAttackRange(float range)
    {
        if (player == null)
            return false;

        float dist = Vector3.Distance(GetFlatPosition(transform.position), GetFlatPosition(player.position));
        return dist <= range;
    }

    private void MoveInDirection(Vector3 dir, float speed)
    {
        if (dir.sqrMagnitude <= 0.001f)
        {
            ApplyVerticalMove();
            return;
        }

        dir.y = 0f;
        dir.Normalize();

        Vector3 move = dir * speed;
        move.y = verticalVelocity;

        cc.Move(move * Time.deltaTime);
    }

    private void ApplyVerticalMove()
    {
        Vector3 move = new Vector3(0f, verticalVelocity, 0f);
        cc.Move(move * Time.deltaTime);
    }

    private void RotateTowards(Vector3 dir)
    {
        if (dir.sqrMagnitude <= 0.001f)
            return;

        dir.y = 0f;
        dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    private Vector3 GetFlatDirectionToPlayer()
    {
        if (player == null)
            return Vector3.zero;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        return dir.normalized;
    }

    private Vector3 GetFlatPosition(Vector3 pos)
    {
        pos.y = 0f;
        return pos;
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

    private void FindPlayerIfMissing()
    {
        if (player != null)
            return;

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        if (found != null)
            player = found.transform;
    }

    private void OnDrawGizmos()
    {
        if (drawWhenNotSelected)
            DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawWhenNotSelected)
            DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        if (!drawGizmos)
            return;

        Vector3 wallOrigin = transform.position + Vector3.up * wallCheckHeight;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(wallOrigin, wallOrigin + transform.forward * wallCheckDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 attackOrigin = attackPoint != null
            ? attackPoint.position
            : transform.position + Vector3.up * 1.0f + transform.forward * 0.8f;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(attackOrigin, meleeRadius);

        if (player != null)
        {
            Vector3 origin = eyePoint != null
                ? eyePoint.position
                : transform.position + Vector3.up * 1.0f;

            Vector3 target = player.position + Vector3.up * 1.0f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, target);
        }
    }
}