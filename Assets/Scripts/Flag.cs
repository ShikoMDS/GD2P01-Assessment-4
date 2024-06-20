using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour
{
    public Team flagTeam; // The team the flag belongs to
    private Agent attachedToAgent; // The agent currently carrying the flag
    private Vector3 originalPosition; // The original position of the flag

    private void Start()
    {
        originalPosition = transform.position;
    }

    public void AttachToAgent(Agent agent)
    {
        attachedToAgent = agent;
        transform.SetParent(agent.transform);
        transform.localPosition = Vector3.zero;
    }

    public void DetachFromAgent()
    {
        if (attachedToAgent != null)
        {
            attachedToAgent = null;
            transform.SetParent(null);
        }
    }

    public void ResetPosition()
    {
        transform.position = originalPosition;
    }

    public Agent AttachedToAgent
    {
        get { return attachedToAgent; }
    }
}