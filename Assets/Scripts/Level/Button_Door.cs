using UnityEngine;

public class ButtonOpenDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Button")]
    public DoorSlideUp targetDoor;
    public string interactionText = "Press Button";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return targetDoor != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (targetDoor == null)
            return;

        targetDoor.ToggleDoor();
    }

    public string GetInteractionText()
    {
        return interactionText;
    }
}