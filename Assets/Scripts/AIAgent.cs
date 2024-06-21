using UnityEngine;

public enum Team
{
    Red,
    Blue
}

public class AIAgent : MonoBehaviour
{
    public Team team;
    public bool isControlledByPlayer = false;

    void Update()
    {
        if (!isControlledByPlayer)
        {
            PerformAIActions();
        }
    }

    void PerformAIActions()
    {
        // Implement AI actions here (e.g., navigation, capturing flags)
    }
}