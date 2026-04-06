using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3f;
    public float interactRadius = 0.2f;
    public LayerMask interactMask = ~0;

    [Header("Debug")]
    public bool drawRayGizmo = true;
    public bool drawWhenNotSelected = false;
    public float gizmoOriginSize = 0.04f;
    public float gizmoHitSize = 0.08f;

    public IInteractable CurrentInteractable { get; private set; }
    public RaycastHit CurrentHit { get; private set; }

    private Highlight currentHighlight;

    private void Update()
    {
        UpdateCurrentInteractable();

        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    private void UpdateCurrentInteractable()
    {
        IInteractable newInteractable = null;
        Highlight newHighlight = null;

        if (TryGetInteractHit(out RaycastHit hit))
        {
            CurrentHit = hit;
            newInteractable = FindInteractable(hit.collider);

            if (newInteractable != null && newInteractable.CanInteract(this))
                newHighlight = hit.collider.GetComponentInParent<Highlight>();
        }

        if (currentHighlight != newHighlight)
        {
            if (currentHighlight != null)
                currentHighlight.SetHighlighted(false);

            currentHighlight = newHighlight;

            if (currentHighlight != null)
                currentHighlight.SetHighlighted(true);
        }

        CurrentInteractable = newInteractable;
    }

    public bool TryInteract()
    {
        if (CurrentInteractable == null)
            return false;

        if (!CurrentInteractable.CanInteract(this))
            return false;

        CurrentInteractable.Interact(this);
        return true;
    }

    public string GetCurrentInteractionText()
    {
        if (CurrentInteractable == null)
            return string.Empty;

        if (!CurrentInteractable.CanInteract(this))
            return string.Empty;

        return CurrentInteractable.GetInteractionText();
    }

    private bool TryGetInteractHit(out RaycastHit hit)
    {
        Vector3 origin = GetRayOrigin();
        Vector3 direction = GetRayDirection();

        return Physics.SphereCast(
            origin,
            interactRadius,
            direction,
            out hit,
            interactDistance,
            interactMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private IInteractable FindInteractable(Collider col)
    {
        MonoBehaviour[] behaviours = col.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInteractable interactable)
                return interactable;
        }

        return null;
    }

    private Vector3 GetRayOrigin()
    {
        if (playerCamera != null)
            return playerCamera.transform.position;

        return transform.position + Vector3.up * 1.6f;
    }

    private Vector3 GetRayDirection()
    {
        if (playerCamera != null)
            return playerCamera.transform.forward.normalized;

        return transform.forward.normalized;
    }

    private void OnDrawGizmos()
    {
        if (drawWhenNotSelected)
            DrawGizmosNow();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawWhenNotSelected)
            DrawGizmosNow();
    }

    private void DrawGizmosNow()
    {
        if (!drawRayGizmo)
            return;

        Vector3 origin = GetRayOrigin();
        Vector3 direction = GetRayDirection();

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();

        Vector3 end = origin + direction * interactDistance;

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(origin, gizmoOriginSize);

        // Show interaction volume
        Gizmos.DrawWireSphere(origin, interactRadius);
        Gizmos.DrawWireSphere(end, interactRadius);

        if (TryGetInteractHit(out RaycastHit hit))
        {
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawSphere(hit.point, gizmoHitSize);
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.25f);
        }
        else
        {
            Gizmos.DrawLine(origin, end);
        }
    }
}