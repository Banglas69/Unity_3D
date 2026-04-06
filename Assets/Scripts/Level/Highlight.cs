using UnityEngine;

public class Highlight : MonoBehaviour
{
    [Header("Highlight")]
    public GameObject highlightVisual;

    private void Awake()
    {
        SetHighlighted(false);
    }

    public void SetHighlighted(bool value)
    {
        if (highlightVisual != null)
            highlightVisual.SetActive(value);
    }
}