using UnityEngine;

public class SoundEventRelay : MonoBehaviour
{
    public void PlayEnemyDeath()
    {
        SoundManager.Instance?.PlayOneShot3D(SoundId.EnemyDeath, transform.position, transform);
    }

    public void PlayDoorOpen()
    {
        SoundManager.Instance?.PlayOneShot3D(SoundId.DoorOpen, transform.position, transform);
    }

    public void PlayDoorClose()
    {
        SoundManager.Instance?.PlayOneShot3D(SoundId.DoorClose, transform.position, transform);
    }
}