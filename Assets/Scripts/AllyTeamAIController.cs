using UnityEngine;

public class AllyTeamAIController : MonoBehaviour
{
    public Agent[] allyAgents; // Only the agents controlled by AI
    public float decisionInterval = 2f;
    public Vector2 baseMinBounds;
    public Vector2 baseMaxBounds;

    private float decisionTimer;

    private void Start()
    {
        decisionTimer = decisionInterval;
        foreach (var agent in allyAgents)
        {
            agent.isPlayerControlled = false;
            agent.currentState = Agent.AgentState.Patrolling; // Start AI agents in Patrolling state
            ChooseNewTargetPosition(agent); // Initial target position
        }
    }

    private void Update()
    {
        foreach (var agent in allyAgents)
        {
            if (!agent.isPlayerControlled) // Ensure AI only controls non-player agents
            {
                Patrol(agent);
            }
        }

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            foreach (var agent in allyAgents)
            {
                if (!agent.isPlayerControlled) // Ensure AI only controls non-player agents
                {
                    MakeDecision(agent);
                }
            }
        }
    }

    private void MakeDecision(Agent agent)
    {
        if (agent.currentState == Agent.AgentState.Idle || agent.currentState == Agent.AgentState.Patrolling)
        {
            agent.currentState = Agent.AgentState.Patrolling;
            ChooseNewTargetPosition(agent);
        }
    }

    private void Patrol(Agent agent)
    {
        if (agent.target == null)
        {
            ChooseNewTargetPosition(agent);
        }
        agent.Move((agent.target.position - agent.transform.position).normalized);
        if (Vector3.Distance(agent.transform.position, agent.target.position) < 0.1f)
        {
            ChooseNewTargetPosition(agent);
        }
    }

    private void ChooseNewTargetPosition(Agent agent)
    {
        Vector3 newTargetPosition = new Vector3(
            Random.Range(baseMinBounds.x, baseMaxBounds.x),
            Random.Range(baseMinBounds.y, baseMaxBounds.y),
            agent.transform.position.z
        );

        if (agent.target == null)
        {
            GameObject targetObject = new GameObject("Target");
            targetObject.transform.position = newTargetPosition;
            agent.target = targetObject.transform;
        }
        else
        {
            agent.target.position = newTargetPosition;
        }
    }
}
