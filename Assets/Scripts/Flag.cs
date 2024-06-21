using UnityEngine;

public class Flag : MonoBehaviour
{
    private Vector3 originalPosition;
    public bool isBeingCarried = false;

    void Start()
    {
        originalPosition = transform.position;
    }

    public void ResetPosition()
    {
        transform.SetParent(null); // Detach the flag from the agent
        transform.position = originalPosition;
        isBeingCarried = false;
        gameObject.SetActive(true);
    }
}