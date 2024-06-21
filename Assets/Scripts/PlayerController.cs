using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public AIAgent[] blueTeamAgents; // Assign this in the Inspector with your blue team agents
    public AIAgent controlledAgent;
    public float moveSpeed = 5f;
    private Renderer controlledAgentRenderer;
    private Color originalColor;
    private Rigidbody2D controlledAgentRigidbody;

    void Update()
    {
        HandleAgentSelection();
    }

    void FixedUpdate()
    {
        if (controlledAgent != null && controlledAgent.isControlledByPlayer && controlledAgent.currentState != AIAgent.State.Captured && controlledAgent.currentState != AIAgent.State.GoingToPrison)
        {
            HandlePlayerInput();
        }

        if (controlledAgent != null && controlledAgent.isControlledByPlayer)
        {
            CheckFlagCapture();
            CheckEscort();
        }
    }

    void HandlePlayerInput()
    {
        float moveHorizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrow keys
        float moveVertical = Input.GetAxis("Vertical"); // W/S or Up/Down arrow keys

        // Combine horizontal and vertical inputs for movement in the X and Y plane
        Vector2 movement = new Vector2(moveHorizontal, moveVertical);

        // Move the agent using Rigidbody2D
        controlledAgentRigidbody.MovePosition(controlledAgentRigidbody.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    void HandleAgentSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectAgent(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectAgent(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectAgent(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectAgent(3);
        }
    }

    void SelectAgent(int index)
    {
        if (index < 0 || index >= blueTeamAgents.Length)
        {
            return;
        }

        if (controlledAgent != null)
        {
            controlledAgent.isControlledByPlayer = false; // Release previous agent
            controlledAgentRenderer.material.color = originalColor; // Reset color
        }

        AIAgent selectedAgent = blueTeamAgents[index];

        // Prevent selection of captured or going to prison agents
        if (selectedAgent.currentState == AIAgent.State.Captured || selectedAgent.currentState == AIAgent.State.GoingToPrison)
        {
            return;
        }

        controlledAgent = selectedAgent;
        controlledAgent.isControlledByPlayer = true; // Control new agent

        // Highlight the selected agent
        controlledAgentRenderer = controlledAgent.GetComponent<Renderer>();
        originalColor = controlledAgentRenderer.material.color;
        controlledAgentRenderer.material.color = Color.yellow; // Highlight color

        // Get the Rigidbody2D component of the controlled agent
        controlledAgentRigidbody = controlledAgent.GetComponent<Rigidbody2D>();
    }

    public void ReleaseControl()
    {
        if (controlledAgent != null)
        {
            controlledAgent.isControlledByPlayer = false;
            controlledAgentRenderer.material.color = originalColor; // Reset color
            controlledAgent = null;
        }
    }

    void CheckFlagCapture()
    {
        if (controlledAgent.carriedFlag != null && IsInOwnTerritory())
        {
            Debug.Log($"{controlledAgent.gameObject.name} has entered its own territory with the flag");
            controlledAgent.CaptureFlag();
        }
    }

    void CheckEscort()
    {
        if (controlledAgent.currentState == AIAgent.State.Escorting && IsInOwnTerritory())
        {
            controlledAgent.escortedAgent.FreeFromPrison();
            controlledAgent.escortedAgent = null;
            controlledAgent.currentState = AIAgent.State.Idle;
            Debug.Log($"{controlledAgent.gameObject.name} has successfully escorted an ally back to their own territory");
        }
    }

    private bool IsInOwnTerritory()
    {
        return (controlledAgent.team == Team.Red && controlledAgent.transform.position.x >= 0) ||
               (controlledAgent.team == Team.Blue && controlledAgent.transform.position.x <= 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Flag flag = collision.GetComponent<Flag>();
        if (flag != null && controlledAgent != null && controlledAgent.currentState != AIAgent.State.Captured && controlledAgent.currentState != AIAgent.State.GoingToPrison)
        {
            controlledAgent.PickUpFlag(flag);
        }

        AIAgent allyAgent = collision.GetComponent<AIAgent>();
        if (allyAgent != null && allyAgent.team == controlledAgent.team && allyAgent.currentState == AIAgent.State.Captured)
        {
            controlledAgent.StartEscorting(allyAgent);
        }
    }
}
