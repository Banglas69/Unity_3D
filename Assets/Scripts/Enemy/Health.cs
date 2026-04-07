using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Identity")]
    public string entityName = "";

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool startAtMaxHealth = true;

    [Header("Damage")]
    public bool invulnerable = false;
    public float damageMultiplier = 1f;

    [Header("Debug")]
    public bool logDamageToConsole = true;

    [Header("Death")]
    public bool destroyOnDeath = false;
    public float destroyDelay = 0f;
    public bool disableObjectOnDeath = false;

    [Header("Respawn")]
    public bool respawnOnDeath = false;
    public float respawnDelay = 3f;
    public bool respawnAtStartPosition = true;

    [Header("Death VFX")]
    public GameObject deathEffectPrefab;
    public float deathEffectLifetime = 3f;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;
    public UnityEvent onRespawn;

    public bool IsDead { get; private set; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public string DisplayName => string.IsNullOrWhiteSpace(entityName) ? gameObject.name : entityName;
    public DamageRequest LastDamageRequest { get; private set; }

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Collider[] cachedColliders;
    private Renderer[] cachedRenderers;
    private MonoBehaviour[] cachedBehaviours;
    private Rigidbody cachedRigidbody;

    private void Awake()
    {
        if (startAtMaxHealth)
            currentHealth = maxHealth;

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        startPosition = transform.position;
        startRotation = transform.rotation;

        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
        cachedRigidbody = GetComponent<Rigidbody>();

        if (currentHealth <= 0f)
            IsDead = true;
    }

    public void TakeDamage(DamageRequest request)
    {
        if (IsDead)
            return;

        if (invulnerable)
            return;

        LastDamageRequest = request;

        float finalDamage = Mathf.Max(0f, request.amount * damageMultiplier);
        if (finalDamage <= 0f)
            return;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (logDamageToConsole)
        {
            string sourceName = request.source != null ? request.source.name : "Unknown";
            Debug.Log(
                $"{DisplayName} took {finalDamage} damage from {sourceName}. Health left: {currentHealth}/{maxHealth}",
                this
            );
        }

        onDamaged?.Invoke();

        if (currentHealth <= 0f)
        {
            Vector3 deathPoint = request.hitPoint.sqrMagnitude > 0.0001f
                ? request.hitPoint
                : transform.position;

            Die(deathPoint);
        }
    }

    
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        Vector3 dir = hitDirection.sqrMagnitude > 0.001f
            ? hitDirection.normalized
            : transform.forward;

        DamageRequest request = new DamageRequest(
            damage,
            null,
            hitPoint,
            -dir,
            dir
        );

        TakeDamage(request);
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        if (amount <= 0f)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);

        if (!IsDead && currentHealth <= 0f)
            Die(transform.position);
    }

    public void RestoreToFull()
    {
        if (IsDead)
            return;

        currentHealth = maxHealth;
    }

    private void Die(Vector3 deathPoint)
    {
        if (IsDead)
            return;

        IsDead = true;
        currentHealth = 0f;

        SpawnDeathEffect(deathPoint);

        if (logDamageToConsole)
            Debug.Log($"{DisplayName} died.", this);

        onDeath?.Invoke();

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
            return;
        }

        if (disableObjectOnDeath)
            gameObject.SetActive(false);

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }

    private IEnumerator RespawnRoutine()
    {
        SetActiveStateForRespawn(false);

        yield return new WaitForSeconds(respawnDelay);

        if (respawnAtStartPosition)
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        currentHealth = maxHealth;
        IsDead = false;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        SetActiveStateForRespawn(true);

        if (logDamageToConsole)
            Debug.Log($"{DisplayName} respawned with {currentHealth}/{maxHealth} health.", this);

        onRespawn?.Invoke();
    }

    private void SetActiveStateForRespawn(bool enabledState)
    {
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
                cachedRenderers[i].enabled = enabledState;
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
                cachedColliders[i].enabled = enabledState;
        }

        for (int i = 0; i < cachedBehaviours.Length; i++)
        {
            if (cachedBehaviours[i] != null && cachedBehaviours[i] != this)
                cachedBehaviours[i].enabled = enabledState;
        }

        if (cachedRigidbody != null)
            cachedRigidbody.isKinematic = !enabledState;
    }

    private void SpawnDeathEffect(Vector3 position)
    {
        if (deathEffectPrefab == null)
            return;

        GameObject vfx = Instantiate(deathEffectPrefab, position, Quaternion.identity);
        Destroy(vfx, deathEffectLifetime);
    }
}