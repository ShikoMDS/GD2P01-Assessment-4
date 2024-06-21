using UnityEngine;

public class Flag : MonoBehaviour
{
    public Team team;

    void OnTriggerEnter(Collider other)
    {
        AIAgent agent = other.GetComponent<AIAgent>();
        if (agent != null && agent.team != team)
        {
            GameManager.Instance.CaptureFlag(agent.team);
            // Additional logic for flag capture (e.g., respawn flag, notify agents)
        }
    }
}