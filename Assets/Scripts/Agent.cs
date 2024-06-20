using System.Xml.Linq;
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
        currentState = newState;
        if (newState == AgentState.InPrison)
        {
            EnterPrison();
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
            }

            if (currentState == AgentState.UnderRescue && IsInOwnTerritory())
            {
                currentState = AgentState.Idle;
                target = null;
                DetachRescuedAgent();
            }

            if (currentState == AgentState.PlayerControlledRescuing && carryingAgent != null && carryingAgent.IsInOwnTerritory())
            {
                currentState = AgentState.PlayerControlled;
                carryingAgent.currentState = AgentState.Idle;
                carryingAgent.target = null;
                carryingAgent = null;
            }
        }

        if (currentState == AgentState.MovingToPrison && Vector2.Distance(transform.position, prisonSpot) < 0.1f)
        {
            EnterPrison();
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
        if (other.CompareTag("Flag") && !hasFlag && carryingAgent == null && currentState != AgentState.UnderRescue)
        {
            Flag flagScript = other.GetComponent<Flag>();
            if (flagScript != null && flagScript.flagTeam != team && flagScript.AttachedToAgent == null)
            {
                hasFlag = true;
                carriedFlag = other.gameObject;
                target = homeBase;
                if (currentState != AgentState.PlayerControlled)
                {
                    currentState = AgentState.MovingToFlag;
                }
                flagScript.AttachToAgent(this);
            }
        }
        else if (other.CompareTag("Agent"))
        {
            Agent otherAgent = other.GetComponent<Agent>();
            if (otherAgent != null)
            {
                if (otherAgent.team == team && otherAgent.currentState == AgentState.InPrison && currentState != AgentState.InPrison)
                {
                    if (!hasFlag && carryingAgent == null && currentState != AgentState.UnderRescue)
                    {
                        FreeFromPrison(otherAgent);
                    }
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
        if (IsInEnemyTerritory() && otherAgent.IsInOwnTerritory() && otherAgent.currentState != Agent.AgentState.InPrison)
        {
            if (currentState == Agent.AgentState.UnderRescue || currentState == Agent.AgentState.PlayerControlledRescuing)
            {
                if (carryingAgent != null)
                {
                    carryingAgent.GoToPrison();
                    carryingAgent = null;
                }
                PlayerController playerController = FindObjectOfType<PlayerController>();
                if (playerController != null && playerController.SelectedAgent == this)
                {
                    playerController.DeselectAgent();
                }
                GoToPrison();
            }
            else if (otherAgent.currentState == Agent.AgentState.UnderRescue || otherAgent.currentState == Agent.AgentState.PlayerControlledRescuing)
            {
                otherAgent.GoToPrison();
                if (otherAgent.carryingAgent != null)
                {
                    otherAgent.carryingAgent.GoToPrison();
                    otherAgent.carryingAgent = null;
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

        currentState = AgentState.MovingToPrison;
        gameObject.layer = LayerMask.NameToLayer(team == Team.Blue ? "BlueTeamToPrison" : "RedTeamToPrison");

        Bounds prisonBounds = prison.GetComponent<Collider2D>().bounds;
        prisonSpot = new Vector3(
            Random.Range(prisonBounds.min.x, prisonBounds.max.x),
            Random.Range(prisonBounds.min.y, prisonBounds.max.y),
            transform.position.z
        );
        target = prison;
        isPlayerControlled = false; // Disable player control
        spriteRenderer.color = Color.gray; // Change color to indicate being captured
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
        if (rescuedAgent.currentState == AgentState.InPrison)
        {
            currentState = AgentState.PlayerControlledRescuing;
            carryingAgent = rescuedAgent;
            rescuedAgent.currentState = AgentState.UnderRescue;
            rescuedAgent.target = this.transform;
            target = homeBase;
        }
    }

    public void DetachRescuedAgent()
    {
        if (carryingAgent != null)
        {
            carryingAgent.target = null;
            carryingAgent.currentState = AgentState.Idle;
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
            currentState = AgentState.Idle;
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
            currentState = AgentState.PlayerControlled;
        }
    }

    public void ResetColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            if (currentState != AgentState.MovingToPrison && currentState != AgentState.InPrison)
            {
                currentState = AgentState.Idle;
                target = null;
            }
        }
    }
}