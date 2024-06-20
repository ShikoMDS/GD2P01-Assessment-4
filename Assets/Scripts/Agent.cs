using UnityEngine;

public class Agent : MonoBehaviour
{
    public enum AgentState
    {
        Idle,
        PlayerControlled,
        MovingToFlag,
        MovingToPrison,
        InPrison,
        UnderRescue,
        PlayerControlledRescuing,
        Patrolling,
        Chasing,
        Avoiding
    }

    public AgentState currentState;
    public Team team;
    public Transform target;
    public float speed = 5f;
    private Rigidbody2D rb;
    public Transform homeBase;
    public Transform prison;
    private SpriteRenderer spriteRenderer;
    public Color originalColor;

    private float fieldCenterX = 0f;
    private Vector3 prisonSpot;
    private Agent carryingAgent;
    private GameObject carriedFlag;
    private bool hasFlag = false;

    public bool isPlayerControlled = false;

    public Vector2 patrolMinBounds;
    public Vector2 patrolMaxBounds;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.drag = 5f;
        SetTeamColor();

        if (prison == null)
        {
            Debug.LogError($"{name}: Prison reference is not set!");
        }

        if (homeBase == null)
        {
            Debug.LogError($"{name}: Home base reference is not set!");
        }
    }

    public void Move(Vector2 direction)
    {
        rb.velocity = direction * speed;
    }

    public void Stop()
    {
        rb.velocity = Vector2.zero;
    }

    public void ChangeState(AgentState newState)
    {
        Debug.Log($"{name}: Changing state from {currentState} to {newState}");
        currentState = newState;
        if (newState == AgentState.InPrison)
        {
            EnterPrison();
        }

        // Check if the state changes from UnderRescue to MovingToPrison
        if (currentState == AgentState.UnderRescue && newState == AgentState.MovingToPrison && carryingAgent != null)
        {
            carryingAgent.ChangeState(AgentState.MovingToPrison);
            carryingAgent.DetachRescuedAgent();
        }

        // Ensure target is maintained or reassigned during state changes
        if (newState == AgentState.Patrolling && target == null)
        {
            ChooseNewTargetPosition();
        }
    }

    private void EnterPrison()
    {
        currentState = AgentState.InPrison;
        gameObject.layer = LayerMask.NameToLayer(team == Team.Blue ? "BlueTeam" : "RedTeam");
        target = null;
        SetTeamColor();
        transform.position = prisonSpot;
    }

    private void Update()
    {
        if (hasFlag && IsInOwnTerritory())
        {
            CaptureFlag();
        }

        if (currentState == AgentState.MovingToPrison)
        {
            MoveToTarget(prisonSpot);
        }
        else if (isPlayerControlled)
        {
            HandlePlayerControlledMovement();
        }
        else
        {
            switch (currentState)
            {
                case AgentState.PlayerControlled:
                case AgentState.PlayerControlledRescuing:
                    HandlePlayerControlledMovement();
                    break;
                case AgentState.UnderRescue:
                    if (target != null) MoveToTarget(target.position);
                    break;
                case AgentState.Idle:
                case AgentState.InPrison:
                    rb.velocity = Vector2.zero;
                    break;
                case AgentState.Patrolling:
                    MoveToTarget(target.position);
                    break;
            }

            // Handle state change for rescued agents when they cross the center line
            if (currentState == AgentState.UnderRescue && carryingAgent != null && carryingAgent.IsInOwnTerritory())
            {
                Debug.Log($"{name}: Rescued agent is back in own territory. Changing state from UnderRescue to Idle.");
                ChangeState(AgentState.Idle);
                target = null;
                carryingAgent.ChangeState(AgentState.PlayerControlled);
                carryingAgent = null;
            }
        }

        if (currentState == AgentState.MovingToPrison && Vector2.Distance(transform.position, prisonSpot) < 0.1f)
        {
            EnterPrison();
        }

        // Check if the carrying agent should be set to null
        if (carryingAgent != null && carryingAgent.currentState == AgentState.Idle)
        {
            Debug.Log($"{name}: Detaching rescued agent.");
            carryingAgent = null;
        }

        // Manually set the state for the agent being carried
        if (currentState == AgentState.PlayerControlledRescuing && carryingAgent != null &&
            carryingAgent.IsInOwnTerritory())
        {
            Debug.Log($"{name}: Manually changing state from PlayerControlledRescuing to PlayerControlled.");
            currentState = AgentState.PlayerControlled;
            carryingAgent.ChangeState(AgentState.Idle);
            carryingAgent.target = null;
            carryingAgent = null;
        }
    }

    private void HandlePlayerControlledMovement()
    {
        if (currentState == AgentState.InPrison || currentState == AgentState.MovingToPrison)
        {
            return;
        }

        Vector2 moveDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        rb.velocity = moveDirection * speed;

        if (currentState == AgentState.InPrison)
        {
            Bounds prisonBounds = prison.GetComponent<Collider2D>().bounds;
            Vector3 clampedPosition = new Vector3(
                Mathf.Clamp(transform.position.x, prisonBounds.min.x, prisonBounds.max.x),
                Mathf.Clamp(transform.position.y, prisonBounds.min.y, prisonBounds.max.y),
                transform.position.z
            );
            transform.position = clampedPosition;
        }
    }

    private void MoveToTarget(Vector3 targetPosition)
    {
        Vector2 direction = (targetPosition - transform.position).normalized;
        rb.velocity = direction * speed;

        if (currentState == AgentState.MovingToPrison && Vector2.Distance(transform.position, prisonSpot) < 0.1f)
        {
            EnterPrison();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Flag") && !hasFlag && carryingAgent == null && currentState != AgentState.UnderRescue &&
            currentState != AgentState.PlayerControlledRescuing)
        {
            Flag flagScript = other.GetComponent<Flag>();
            if (flagScript != null && flagScript.flagTeam != team && flagScript.AttachedToAgent == null)
            {
                hasFlag = true;
                carriedFlag = other.gameObject;
                target = homeBase;
                if (currentState != AgentState.PlayerControlled)
                {
                    ChangeState(AgentState.MovingToFlag);
                }

                flagScript.AttachToAgent(this);
            }
        }
        else if (other.CompareTag("Agent"))
        {
            Agent otherAgent = other.GetComponent<Agent>();
            if (otherAgent != null)
            {
                if (otherAgent.team == team && otherAgent.currentState == AgentState.InPrison &&
                    currentState != AgentState.InPrison)
                {
                    FreeFromPrison(otherAgent);
                }
                else if (otherAgent.team != team)
                {
                    HandleEnemyTag(otherAgent);
                }
            }
        }
    }

    private void HandleEnemyTag(Agent otherAgent)
    {
        if (IsInEnemyTerritory() && otherAgent.IsInOwnTerritory() && otherAgent.currentState != AgentState.InPrison)
        {
            if (currentState == AgentState.UnderRescue || currentState == AgentState.PlayerControlledRescuing)
            {
                if (carryingAgent != null)
                {
                    carryingAgent.GoToPrison();
                }

                GoToPrison();
            }
            else if (otherAgent.currentState == AgentState.UnderRescue ||
                     otherAgent.currentState == AgentState.PlayerControlledRescuing)
            {
                otherAgent.GoToPrison();
                if (otherAgent.carryingAgent != null)
                {
                    otherAgent.carryingAgent.GoToPrison();
                }
            }
            else
            {
                GoToPrison();
            }
        }
    }

    public void GoToPrison()
    {
        if (hasFlag)
        {
            DropFlag();
        }

        if (carryingAgent != null)
        {
            carryingAgent.GoToPrison();
            carryingAgent = null;
        }

        ChangeState(AgentState.MovingToPrison);
        gameObject.layer = LayerMask.NameToLayer(team == Team.Blue ? "BlueTeamToPrison" : "RedTeamToPrison");

        Bounds prisonBounds = prison.GetComponent<Collider2D>().bounds;
        prisonSpot = new Vector3(
            Random.Range(prisonBounds.min.x, prisonBounds.max.x),
            Random.Range(prisonBounds.min.y, prisonBounds.max.y),
            transform.position.z
        );
        target = prison;
        isPlayerControlled = false; // Disable player control
        Debug.Log($"{gameObject.name}: Going to prison at position {prisonSpot}");

        // Notify PlayerController to deselect this agent if it is selected
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null && playerController.SelectedAgent == this)
        {
            playerController.DeselectAgent();
        }
    }

    public void FreeFromPrison(Agent rescuedAgent)
    {
        if (carryingAgent == null && rescuedAgent.currentState == AgentState.InPrison)
        {
            ChangeState(AgentState.PlayerControlledRescuing);
            carryingAgent = rescuedAgent;
            rescuedAgent.ChangeState(AgentState.UnderRescue);
            rescuedAgent.target = this.transform;
            target = homeBase;
        }
    }

    public void DetachRescuedAgent()
    {
        if (carryingAgent != null)
        {
            carryingAgent.target = null;
            carryingAgent.ChangeState(AgentState.Idle);
            carryingAgent = null;
        }
    }

    private bool IsInEnemyTerritory()
    {
        return (team == Team.Blue && transform.position.x > fieldCenterX) ||
               (team == Team.Red && transform.position.x < fieldCenterX);
    }

    private bool IsInOwnTerritory()
    {
        return (team == Team.Blue && transform.position.x < fieldCenterX) ||
               (team == Team.Red && transform.position.x > fieldCenterX);
    }

    public void CaptureFlag()
    {
        hasFlag = false;
        GameManager.Instance.ScorePoint(team);
        if (currentState != AgentState.PlayerControlled && currentState != AgentState.PlayerControlledRescuing)
        {
            ChangeState(AgentState.Idle);
        }

        if (carriedFlag != null)
        {
            Destroy(carriedFlag);
            carriedFlag = null;
        }
    }

    private void DropFlag()
    {
        if (hasFlag && carriedFlag != null)
        {
            Flag flagScript = carriedFlag.GetComponent<Flag>();
            if (flagScript != null)
            {
                flagScript.DetachFromAgent();
                flagScript.ResetPosition();
            }

            carriedFlag = null;
            hasFlag = false;
        }
    }

    private void SetTeamColor()
    {
        if (spriteRenderer != null)
        {
            originalColor = team == Team.Blue ? Color.blue : Color.red;
            spriteRenderer.color = originalColor;
        }
    }

    public void ControlledColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.yellow;
            ChangeState(AgentState.PlayerControlled);
        }
    }

    public void ResetColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            if (currentState != AgentState.MovingToPrison && currentState != AgentState.InPrison)
            {
                ChangeState(AgentState.Idle);
                target = null;
            }
        }
    }

    private void ChooseNewTargetPosition()
    {
        // Ensure target is maintained or reassigned during state changes
        Vector3 newTargetPosition = new Vector3(
            Random.Range(patrolMinBounds.x, patrolMaxBounds.x),
            Random.Range(patrolMinBounds.y, patrolMaxBounds.y),
            transform.position.z
        );

        if (target == null)
        {
            GameObject targetObject = new GameObject("Target");
            targetObject.transform.position = newTargetPosition;
            target = targetObject.transform;
        }
        else
        {
            target.position = newTargetPosition;
        }
    }
}
