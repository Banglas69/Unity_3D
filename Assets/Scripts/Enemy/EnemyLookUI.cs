using UnityEngine;
using TMPro;

public class EnemyLookUI : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Detection")]
    public float lookDistance = 25f;
    public LayerMask lookMask = ~0;

    [Header("UI")]
    public GameObject enemyInfoPanel;
    public TMP_Text enemyNameText;
    public TMP_Text enemyHealthText;

    private void Update()
    {
        UpdateEnemyInfo();
    }

    private void UpdateEnemyInfo()
    {
        Health targetHealth = GetLookedAtHealth();

        bool show = targetHealth != null && !targetHealth.IsDead;

        if (enemyInfoPanel != null)
            enemyInfoPanel.SetActive(show);

        if (!show)
        {
            if (enemyNameText != null) enemyNameText.text = "";
            if (enemyHealthText != null) enemyHealthText.text = "";
            return;
        }

        if (enemyNameText != null)
            enemyNameText.text = targetHealth.DisplayName;

        if (enemyHealthText != null)
            enemyHealthText.text = $"{Mathf.CeilToInt(targetHealth.CurrentHealth)} / {Mathf.CeilToInt(targetHealth.MaxHealth)}";
    }

    private Health GetLookedAtHealth()
    {
        if (playerCamera == null)
            return null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, lookDistance, lookMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<Health>();
        }

        return null;
    }
}