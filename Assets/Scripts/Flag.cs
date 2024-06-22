using UnityEngine;

public class Flag : MonoBehaviour
{
    private Vector3 _originalPosition;
    public bool IsBeingCarried;

    private void Start()
    {
        _originalPosition = transform.position;
    }

    public void ResetPosition()
    {
        transform.SetParent(null); // Detach the flag from the agent
        transform.position = _originalPosition;
        IsBeingCarried = false;
        gameObject.SetActive(true);
    }
}