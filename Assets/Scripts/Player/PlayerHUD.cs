using UnityEngine;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    public Health playerHealth;
    public PlayerInteractor playerInteractor;

    [Header("Player UI")]
    public TMP_Text playerHealthText;

    [Header("Interaction UI")]
    public GameObject interactionPanel;
    public TMP_Text interactionText;

    private void Update()
    {
        UpdatePlayerStats();
        UpdateInteractionPrompt();
    }

    private void UpdatePlayerStats()
    {
        if (playerHealth != null && playerHealthText != null)
        {
            playerHealthText.text = $"HP: {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}";
        }
    }

    private void UpdateInteractionPrompt()
    {
        if (playerInteractor == null || interactionText == null)
            return;

        string prompt = playerInteractor.GetCurrentInteractionText();

        bool show = !string.IsNullOrWhiteSpace(prompt);

        if (interactionPanel != null)
            interactionPanel.SetActive(show);

        interactionText.text = show ? $"[E] {prompt}" : "";
    }
}