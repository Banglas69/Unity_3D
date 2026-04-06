using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    [Header("Scene")]
    public string sceneToLoad;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (string.IsNullOrEmpty(sceneToLoad))
            return;

        hasTriggered = true;
        SceneManager.LoadScene(sceneToLoad);
    }
}