using UnityEngine;

public class Button_EnemySpawn : MonoBehaviour, IInteractable
{
    [Header("Button")]
    public string interactionText = "Press Button";
    public bool onlyOnce = true;

    [Header("Spawner")]
    public EnemySpawnerArea targetSpawner;

    private bool hasPressed;

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (targetSpawner == null)
            return false;

        if (onlyOnce && hasPressed)
            return false;

        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
            return;

        hasPressed = true;
        targetSpawner.SpawnEnemies();
    }

    public string GetInteractionText()
    {
        return interactionText;
    }
}