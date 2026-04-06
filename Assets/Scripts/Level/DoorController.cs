using UnityEngine;

public class DoorSlideUp : MonoBehaviour
{
    [Header("Door")]
    public float openHeight = 3f;
    public float speed = 3f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;

    private void Start()
    {
        closedPos = transform.localPosition;
        openPos = closedPos + Vector3.up * openHeight;
    }

    private void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
    }

    public void OpenDoor()
    {
        if (isOpen)
            return;

        isOpen = true;
        SoundManager.Instance?.PlayOneShot3D(SoundId.DoorOpen, transform.position, transform);
    }

    public void CloseDoor()
    {
        if (!isOpen)
            return;

        isOpen = false;
        SoundManager.Instance?.PlayOneShot3D(SoundId.DoorClose, transform.position, transform);
    }

    public void ToggleDoor()
    {
        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }
}