using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIAgent : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chasing,
        ReturningFlag,
        GoingToPrison,
        Captured,
        Escorting,
        BeingEscorted,
        Wandering,
        CapturingFlag,
        Rescuing,
        Evading
    }

    public Team team;
    public bool isControlledByPlayer = false;
    public Transform prison;
    public List<AIAgent> allies = new List<AIAgent>();
    public List<AIAgent> enemies = new List<AIAgent>();
    public List<Flag> enemyFlags = new List<Flag>();
    public float speed = 3f;

    public float wanderMinX;
    public float wanderMaxX;
    public float wanderMinY;
    public float wanderMaxY;

    public State currentState = State.Idle;
    public AIAgent escortedAgent;
    public Flag carriedFlag;
    private Vector3 prisonPosition;
    private AIAgent targetAgent;
    private Vector3 wanderTarget;
    private Flag targetFlag;
    private AIAgent targetAlly;
    private float wanderTimer;
    private const float maxWanderTime = 5f; // Maximum time to wander before re-evaluating decisions
    private const float evadeRange = 5f; // Distance at which the AI should start evading

    void Update()
    {
        if (!isControlledByPlayer)
        {
            UpdateAI();
        }

        if (carriedFlag != null)
        {
            carriedFlag.transform.position = transform.position;
        }

        if (escortedAgent != null)
        {
            escortedAgent.transform.position = transform.position;
        }

        ClampPosition();
    }

    void ClampPosition()
    {
        float clampedX = Mathf.Clamp(transform.position.x, -16f, 16f);
        float clampedY = Mathf.Clamp(transform.position.y, -9f, 9f);
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    void UpdateAI()
    {
        switch (currentState)
        {
            case State.Idle:
                MakeDecision();
                break;
            case State.Chasing:
                ChaseTarget();
                break;
            case State.ReturningFlag:
                ReturnFlag();
                break;
            case State.GoingToPrison:
                GoToPrison();
                break;
            case State.Captured:
                // Do nothing
                break;
            case State.Escorting:
                EscortAlly();
                break;
            case State.BeingEscorted:
                // Do nothing
                break;
            case State.Wandering:
                Wander();
                break;
            case State.CapturingFlag:
                CaptureTargetFlag();
                break;
            case State.Rescuing:
                RescueTargetAlly();
                break;
            case State.Evading:
                Evade();
                break;
        }
    }

    void Evade()
    {
        Vector3 evadeDirection = Vector3.zero;
        float closestDistance = float.MaxValue;

        foreach (var agent in enemies)
        {
            float distance = Vector3.Distance(transform.position, agent.transform.position);
            if (distance < closestDistance && distance < evadeRange)
            {
                closestDistance = distance;
                evadeDirection = transform.position - agent.transform.position;
            }
        }

        evadeDirection.Normalize();
        Vector3 evadeTarget = transform.position + evadeDirection * evadeRange;

        MoveTowards(evadeTarget);

        // Check if the AI has successfully evaded and can continue its previous task
        if (IsInOwnTerritory() || Vector3.Distance(transform.position, evadeTarget) < 0.1f)
        {
            // Determine the previous task and return to it
            if (carriedFlag != null)
            {
                currentState = State.ReturningFlag;
            }
            else if (escortedAgent != null)
            {
                currentState = State.Escorting;
            }
            else
            {
                currentState = State.Idle;
                MakeDecision();
            }
        }
    }

    void MakeDecision()
    {
        // Skip decision making if in critical states
        if (currentState == State.Captured || currentState == State.GoingToPrison || currentState == State.BeingEscorted)
        {
            return;
        }

        if (FindTarget())
        {
            currentState = State.Chasing;
        }
        else if (Random.value < 0.4f && FindFlagToCapture()) // 40% chance to go capture a flag
        {
            currentState = State.CapturingFlag;
        }
        else if (Random.value < 0.3f && FindCapturedAlly()) // 30% chance to rescue an ally
        {
            currentState = State.Rescuing;
        }
        else
        {
            StartWandering();
        }
    }

    bool FindTarget()
    {
        AIAgent closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var agent in enemies)
        {
            if (agent.currentState != State.Captured && agent.currentState != State.GoingToPrison && IsAgentInMyTerritory(agent))
            {
                float distance = Vector3.Distance(transform.position, agent.transform.position);

                if (distance < closestDistance && GetChasersCount(agent) < 2)
                {
                    closestTarget = agent;
                    closestDistance = distance;
                }
            }
        }

        if (closestTarget != null)
        {
            targetAgent = closestTarget;
            Debug.Log($"{gameObject.name} is now chasing {targetAgent.gameObject.name}");
            return true;
        }

        return false;
    }

    void ChaseTarget()
    {
        if (targetAgent == null || targetAgent.currentState == State.Captured || targetAgent.currentState == State.GoingToPrison || !IsAgentInMyTerritory(targetAgent))
        {
            currentState = State.Idle;
            return;
        }

        float currentDistance = Vector3.Distance(transform.position, targetAgent.transform.position);
        foreach (var agent in enemies)
        {
            if (agent != targetAgent && agent.currentState != State.Captured && agent.currentState != State.GoingToPrison && IsAgentInMyTerritory(agent))
            {
                float distance = Vector3.Distance(transform.position, agent.transform.position);
                if (distance < currentDistance && GetChasersCount(agent) < 2)
                {
                    targetAgent = agent;
                    currentDistance = distance;
                    Debug.Log($"{gameObject.name} switched to chasing {targetAgent.gameObject.name}");
                }
            }
        }

        MoveTowards(targetAgent.transform.position);
        Debug.Log($"{gameObject.name} is moving towards {targetAgent.gameObject.name}");
    }

    int GetChasersCount(AIAgent agent)
    {
        return allies.Count(a => a.currentState == State.Chasing && a.targetAgent == agent);
    }

    void ReturnFlag()
    {
        if (IsInOwnTerritory())
        {
            Debug.Log($"{gameObject.name} has entered its own territory with the flag");
            CaptureFlag();
        }
        else
        {
            Vector3 evadeTarget = CalculateEvadeTarget(new Vector3(team == Team.Red ? 10 : -10, transform.position.y, transform.position.z));
            MoveTowards(evadeTarget);
            Debug.Log($"{gameObject.name} is returning the flag to its own territory");
        }
    }

    void GoToPrison()
    {
        if (Vector3.Distance(transform.position, prisonPosition) <= 0.1f)
        {
            currentState = State.Captured;
            transform.position = prisonPosition;
            Debug.Log($"{gameObject.name} reached the prison");
        }
        else
        {
            MoveTowards(prisonPosition);
            Debug.Log($"{gameObject.name} is moving to prison");
        }
    }

    void StartWandering()
    {
        wanderTarget = new Vector3(
            Random.Range(wanderMinX, wanderMaxX),
            Random.Range(wanderMinY, wanderMaxY),
            transform.position.z
        );
        wanderTimer = maxWanderTime;
        currentState = State.Wandering;
        Debug.Log($"{gameObject.name} is wandering to {wanderTarget}");
    }

    void Wander()
    {
        wanderTimer -= Time.deltaTime;

        if (Vector3.Distance(transform.position, wanderTarget) <= 0.1f || wanderTimer <= 0)
        {
            currentState = State.Idle;
            MakeDecision();
        }
        else
        {
            MoveTowards(wanderTarget);
            Debug.Log($"{gameObject.name} is wandering towards {wanderTarget}");
        }
    }

    bool FindFlagToCapture()
    {
        Flag closestFlag = null;
        float closestDistance = float.MaxValue;

        foreach (var flag in enemyFlags)
        {
            if (flag == null)
            {
                Debug.LogWarning($"{gameObject.name} found a null flag reference in enemyFlags list");
                continue;
            }

            if (!flag.isBeingCarried)
            {
                float distance = Vector3.Distance(transform.position, flag.transform.position);
                if (distance < closestDistance)
                {
                    closestFlag = flag;
                    closestDistance = distance;
                }
            }
        }

        if (closestFlag != null)
        {
            targetFlag = closestFlag;
            Debug.Log($"{gameObject.name} is now targeting {targetFlag.gameObject.name} for capture");
            return true;
        }

        return false;
    }

    void CaptureTargetFlag()
    {
        if (targetFlag == null || targetFlag.isBeingCarried)
        {
            currentState = State.Idle;
            MakeDecision();
            return;
        }

        Vector3 evadeTarget = CalculateEvadeTarget(targetFlag.transform.position);
        MoveTowards(evadeTarget);

        if (Vector3.Distance(transform.position, targetFlag.transform.position) <= 0.1f)
        {
            PickUpFlag(targetFlag);
        }
    }

    bool FindCapturedAlly()
    {
        AIAgent closestAlly = null;
        float closestDistance = float.MaxValue;

        foreach (var ally in allies)
        {
            if (ally == null || ally.currentState != State.Captured)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, ally.transform.position);
            if (distance < closestDistance)
            {
                closestAlly = ally;
                closestDistance = distance;
            }
        }

        if (closestAlly != null)
        {
            targetAlly = closestAlly;
            Debug.Log($"{gameObject.name} is now targeting {targetAlly.gameObject.name} for rescue");
            return true;
        }

        return false;
    }

    void RescueTargetAlly()
    {
        if (targetAlly == null || targetAlly.currentState != State.Captured)
        {
            currentState = State.Idle;
            MakeDecision();
            return;
        }

        Vector3 evadeTarget = CalculateEvadeTarget(targetAlly.transform.position);
        MoveTowards(evadeTarget);

        if (Vector3.Distance(transform.position, targetAlly.transform.position) <= 0.1f)
        {
            StartEscorting(targetAlly);
        }
    }

    Vector3 CalculateEvadeTarget(Vector3 target)
    {
        Vector3 evadeDirection = Vector3.zero;
        float closestDistance = float.MaxValue;

        foreach (var agent in enemies)
        {
            float distance = Vector3.Distance(transform.position, agent.transform.position);
            if (distance < closestDistance && distance < evadeRange)
            {
                closestDistance = distance;
                evadeDirection = transform.position - agent.transform.position;
            }
        }

        evadeDirection.Normalize();
        return target + evadeDirection * evadeRange;
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        ClampPosition();
    }

    public void PickUpFlag(Flag flag)
    {
        if (currentState != State.Captured && currentState != State.GoingToPrison && carriedFlag == null && escortedAgent == null && enemyFlags.Contains(flag) && !flag.isBeingCarried)
        {
            carriedFlag = flag;
            flag.isBeingCarried = true;
            flag.transform.SetParent(transform); // Make the flag a child of the agent
            currentState = State.ReturningFlag;
            Debug.Log($"{gameObject.name} picked up a flag");
            // Notify other agents to avoid targeting this flag
            foreach (var agent in allies)
            {
                if (agent != this && agent.currentState == State.CapturingFlag && agent.targetFlag == flag)
                {
                    agent.targetFlag = null;
                    agent.MakeDecision();
                }
            }
        }
    }

    public void CaptureFlag()
    {
        if (carriedFlag != null)
        {
            carriedFlag.isBeingCarried = false;
            carriedFlag.transform.SetParent(null); // Detach the flag from the agent
            GameManager.Instance.CaptureFlag(team);
            Destroy(carriedFlag.gameObject);
            enemyFlags.Remove(carriedFlag);
            carriedFlag = null;
            currentState = State.Idle;
            MakeDecision();
            Debug.Log($"{gameObject.name} captured the flag");
        }
    }

    private bool IsInOwnTerritory()
    {
        return (team == Team.Red && transform.position.x >= 0) || (team == Team.Blue && transform.position.x <= 0);
    }

    private bool IsInEnemyTerritory()
    {
        return (team == Team.Red && transform.position.x < 0) || (team == Team.Blue && transform.position.x > 0);
    }

    private bool IsAgentInMyTerritory(AIAgent agent)
    {
        return (team == Team.Red && agent.transform.position.x > 0) || (team == Team.Blue && agent.transform.position.x < 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        AIAgent enemyAgent = collision.GetComponent<AIAgent>();
        if (enemyAgent != null && enemyAgent.team != team && enemyAgent.currentState != State.Captured && enemyAgent.currentState != State.GoingToPrison)
        {
            if (IsAgentInMyTerritory(enemyAgent))
            {
                enemyAgent.currentState = State.GoingToPrison;
                enemyAgent.prisonPosition = GetRandomPrisonPosition();
                if (enemyAgent.carriedFlag != null)
                {
                    enemyAgent.carriedFlag.ResetPosition();
                    enemyAgent.carriedFlag.isBeingCarried = false;
                    enemyAgent.carriedFlag = null;
                }
                Debug.Log($"{gameObject.name} tagged {enemyAgent.gameObject.name} and is sending them to prison");

                // Release control if the player is controlling the tagged agent
                if (enemyAgent.isControlledByPlayer)
                {
                    enemyAgent.isControlledByPlayer = false;
                    FindObjectOfType<PlayerController>().ReleaseControl();
                }
            }
        }

        Flag flag = collision.GetComponent<Flag>();
        if (flag != null)
        {
            PickUpFlag(flag);
        }

        AIAgent allyAgent = collision.GetComponent<AIAgent>();
        if (allyAgent != null && allyAgent.team == team && allyAgent.currentState == State.Captured)
        {
            StartEscorting(allyAgent);
        }
    }

    Vector3 GetRandomPrisonPosition()
    {
        BoxCollider2D prisonBounds = prison.GetComponent<BoxCollider2D>();
        Vector3 minBounds = prisonBounds.bounds.min;
        Vector3 maxBounds = prisonBounds.bounds.max;

        float randomX = Random.Range(minBounds.x, maxBounds.x);
        float randomY = Random.Range(minBounds.y, maxBounds.y);

        return new Vector3(randomX, randomY, transform.position.z);
    }

    public void FreeFromPrison()
    {
        currentState = State.Idle;
        MakeDecision();
        Debug.Log($"{gameObject.name} has been freed from prison");
    }

    private void OnValidate()
    {
        Debug.Log($"{gameObject.name} state changed to: {currentState}");
    }

    public void StartEscorting(AIAgent allyAgent)
    {
        if (currentState == State.Captured || currentState == State.GoingToPrison || carriedFlag != null || allyAgent == null || allyAgent.team != team || escortedAgent != null)
        {
            return;
        }

        escortedAgent = allyAgent;
        escortedAgent.currentState = State.BeingEscorted;
        currentState = State.Escorting;
        Debug.Log($"{gameObject.name} is escorting {allyAgent.gameObject.name}");
    }

    void EscortAlly()
    {
        if (IsInOwnTerritory())
        {
            escortedAgent.FreeFromPrison();
            escortedAgent = null;
            currentState = State.Idle;
            MakeDecision();
            Debug.Log($"{gameObject.name} has successfully escorted an ally back to their own territory");
        }
        else
        {
            Vector3 evadeTarget = CalculateEvadeTarget(new Vector3(team == Team.Red ? 10 : -10, transform.position.y, transform.position.z));
            MoveTowards(evadeTarget);
            Debug.Log($"{gameObject.name} is escorting an ally to their own territory");
        }
    }
}
