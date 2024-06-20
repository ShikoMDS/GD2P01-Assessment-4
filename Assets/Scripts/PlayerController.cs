using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Agent[] playerAgents; // Only the agents that the player can control
    public Agent SelectedAgent { get; private set; }

    private void Start()
    {
        if (playerAgents.Length > 0)
        {
            SelectAgent(0); // Start by selecting the first agent
        }
    }

    private void Update()
    {
        for (int i = 0; i < playerAgents.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectAgent(i);
            }
        }

        if (SelectedAgent != null)
        {
            if (SelectedAgent.currentState == Agent.AgentState.PlayerControlled)
            {
                Vector2 moveDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                SelectedAgent.Move(moveDirection);
            }
            else if (SelectedAgent.currentState == Agent.AgentState.MovingToPrison || SelectedAgent.currentState == Agent.AgentState.InPrison)
            {
                DeselectAgent();
            }
        }
    }

    private void SelectAgent(int index)
    {
        if (index >= 0 && index < playerAgents.Length)
        {
            if (SelectedAgent != null)
            {
                DeselectAgent();
            }

            Agent newSelectedAgent = playerAgents[index];
            if (newSelectedAgent.currentState != Agent.AgentState.InPrison &&
                newSelectedAgent.currentState != Agent.AgentState.MovingToPrison &&
                newSelectedAgent.currentState != Agent.AgentState.UnderRescue)
            {
                SelectedAgent = newSelectedAgent;
                SelectedAgent.isPlayerControlled = true;
                SelectedAgent.ControlledColor();
                HighlightSelectedAgent();
            }
            else
            {
                SelectedAgent = null;
            }
        }
    }

    private void HighlightSelectedAgent()
    {
        foreach (Agent agent in playerAgents)
        {
            if (agent == SelectedAgent)
            {
                agent.GetComponent<SpriteRenderer>().color = Color.yellow;
            }
            else
            {
                agent.GetComponent<SpriteRenderer>().color = agent.originalColor;
            }
        }
    }

    public void DeselectAgent()
    {
        if (SelectedAgent != null)
        {
            SelectedAgent.isPlayerControlled = false;
            SelectedAgent.ResetColor();
            if (SelectedAgent.currentState != Agent.AgentState.MovingToPrison && SelectedAgent.currentState != Agent.AgentState.InPrison)
            {
                SelectedAgent.ChangeState(Agent.AgentState.Idle);
            }
            SelectedAgent = null;
        }
    }
}
