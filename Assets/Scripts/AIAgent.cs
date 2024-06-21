using System.Collections.Generic;
using UnityEngine;

public class AIAgent : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chasing,
        ReturningFlag,
        GoingToPrison,
        Captured
    }

    public Team team;
    public bool isControlledByPlayer = false;
    public Transform prison;
    public List<AIAgent> allies = new List<AIAgent>();
    public List<AIAgent> enemies = new List<AIAgent>();
    public List<Flag> enemyFlags = new List<Flag>();
    public float speed = 3f;

    public State currentState = State.Idle;

    private AIAgent targetAgent;
    public Flag carriedFlag;
    private Vector3 prisonPosition;

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
    }

    void UpdateAI()
    {
        switch (currentState)
        {
            case State.Idle:
                FindTarget();
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
        }
    }

    void FindTarget()
    {
        foreach (var agent in enemies)
        {
            if (agent.currentState != State.Captured && agent.currentState != State.GoingToPrison && IsEnemyAgentInTerritory(agent))
            {
                targetAgent = agent;
                currentState = State.Chasing;
                Debug.Log($"{gameObject.name} is now chasing {targetAgent.gameObject.name}");
                break;
            }
        }
    }

    void ChaseTarget()
    {
        if (targetAgent == null || targetAgent.currentState == State.Captured || targetAgent.currentState == State.GoingToPrison || !IsEnemyAgentInTerritory(targetAgent))
        {
            currentState = State.Idle;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetAgent.transform.position, speed * Time.deltaTime);
        //Debug.Log($"{gameObject.name} is moving towards {targetAgent.gameObject.name}");
    }

    void ReturnFlag()
    {
        if (!IsInOwnTerritory())
        {
            Debug.Log($"{gameObject.name} has entered its own territory with the flag");
            CaptureFlag();
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(team == Team.Red ? 10 : -10, transform.position.y, transform.position.z), speed * Time.deltaTime);
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
            transform.position = Vector3.MoveTowards(transform.position, prisonPosition, speed * Time.deltaTime);
            Debug.Log($"{gameObject.name} is moving to prison");
        }
    }

    public void PickUpFlag(Flag flag)
    {
        if (currentState != State.Captured && currentState != State.GoingToPrison && carriedFlag == null && enemyFlags.Contains(flag) && !flag.isBeingCarried)
        {
            carriedFlag = flag;
            carriedFlag.isBeingCarried = true;
            carriedFlag.transform.SetParent(transform); // Make the flag a child of the agent
            currentState = State.ReturningFlag;
            Debug.Log($"{gameObject.name} picked up a flag");
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
            carriedFlag = null;
            currentState = State.Idle;
            Debug.Log($"{gameObject.name} captured the flag");
        }
    }

    private bool IsInOwnTerritory()
    {
        return (team == Team.Red && transform.position.x <= 0) || (team == Team.Blue && transform.position.x >= 0);
    }

    private bool IsInEnemyTerritory()
    {
        return (team == Team.Red && transform.position.x > 0) || (team == Team.Blue && transform.position.x < 0);
    }

    private bool IsEnemyAgentInTerritory(AIAgent agent)
    {
        return (team == Team.Red && agent.transform.position.x > 0) || (team == Team.Blue && agent.transform.position.x < 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        AIAgent enemyAgent = collision.GetComponent<AIAgent>();
        if (enemyAgent != null && enemyAgent.team != team && enemyAgent.currentState != State.Captured && enemyAgent.currentState != State.GoingToPrison)
        {
            if (IsEnemyAgentInTerritory(enemyAgent))
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
        Debug.Log($"{gameObject.name} has been freed from prison");
    }

    private void OnValidate()
    {
        Debug.Log($"{gameObject.name} state changed to: {currentState}");
    }
}
