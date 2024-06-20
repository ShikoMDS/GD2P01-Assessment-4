using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public Agent[] aiAgents; // Only the agents controlled by AI
    public float decisionInterval = 2f;
    public float detectionRange = 5f;
    public Vector2 baseMinBounds;
    public Vector2 baseMaxBounds;
    public LayerMask opponentLayer;

    private float decisionTimer;
    private Vector3 targetPosition;
    private List<Agent> detectedEnemies = new List<Agent>();

    private void Start()
    {
        decisionTimer = decisionInterval;
        foreach (var agent in aiAgents)
        {
            agent.isPlayerControlled = false;
            agent.currentState = Agent.AgentState.Patrolling; // Start AI agents in Patrolling state
            Debug.Log($"{agent.name}: Initialized for AI control and set to Patrolling");
        }
        ChooseNewTargetPosition(); // Initial target position
        Debug.Log($"{gameObject.name}: AIController initialized");
    }

    private void Update()
    {
        foreach (var agent in aiAgents)
        {
            if (!agent.isPlayerControlled) // Ensure AI only controls non-player agents
            {
                switch (agent.currentState)
                {
                    case Agent.AgentState.Idle:
                    case Agent.AgentState.Patrolling:
                        Patrol(agent);
                        break;
                    case Agent.AgentState.Chasing:
                        Chase(agent);
                        break;
                    case Agent.AgentState.Avoiding:
                        Avoid(agent);
                        break;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        DetectEnemies();
    }

    private void Patrol(Agent agent)
    {
        Debug.Log($"{agent.name}: Patrolling towards {targetPosition}");
        agent.Move((targetPosition - agent.transform.position).normalized);
        if (Vector3.Distance(agent.transform.position, targetPosition) < 0.1f)
        {
            ChooseNewTargetPosition();
        }
    }

    private void Chase(Agent agent)
    {
        if (detectedEnemies.Count > 0)
        {
            Debug.Log($"{agent.name}: Chasing enemy at {detectedEnemies[0].transform.position}");
            agent.Move((detectedEnemies[0].transform.position - agent.transform.position).normalized);
        }
        else
        {
            agent.currentState = Agent.AgentState.Patrolling;
            ChooseNewTargetPosition();
        }
    }

    private void Avoid(Agent agent)
    {
        Debug.Log($"{agent.name}: Avoid method called");
        // Implement avoidance behavior if needed
    }

    private void ChooseNewTargetPosition()
    {
        targetPosition = new Vector3(
            Random.Range(baseMinBounds.x, baseMaxBounds.x),
            Random.Range(baseMinBounds.y, baseMaxBounds.y),
            transform.position.z
        );
        Debug.Log($"{gameObject.name}: New target position set to {targetPosition}");
    }

    private void DetectEnemies()
    {
        Debug.Log($"{gameObject.name}: DetectEnemies method called");
        detectedEnemies.Clear();
        Collider2D[] opponents = Physics2D.OverlapCircleAll(transform.position, detectionRange, opponentLayer);
        foreach (Collider2D opponent in opponents)
        {
            Agent enemyAgent = opponent.GetComponent<Agent>();
            if (enemyAgent != null && enemyAgent.currentState != Agent.AgentState.InPrison)
            {
                detectedEnemies.Add(enemyAgent);
            }
        }

        if (detectedEnemies.Count > 0)
        {
            foreach (var agent in aiAgents)
            {
                if (!agent.isPlayerControlled && agent.currentState != Agent.AgentState.Chasing)
                {
                    agent.currentState = Agent.AgentState.Chasing;
                    Debug.Log($"{agent.name}: Enemies detected, switching to Chasing state");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
