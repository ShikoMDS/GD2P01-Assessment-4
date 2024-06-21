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
        if (controlledAgent != null && controlledAgent.isControlledByPlayer)
        {
            HandlePlayerInput();
        }
    }

    void HandlePlayerInput()
    {
        float moveHorizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrow keys
        float moveVertical = Input.GetAxis("Vertical"); // W/S or Up/Down arrow keys

        // Debug log to check the input values
        Debug.Log($"Horizontal Input: {moveHorizontal}, Vertical Input: {moveVertical}");

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

        controlledAgent = blueTeamAgents[index];
        controlledAgent.isControlledByPlayer = true; // Control new agent

        // Highlight the selected agent
        controlledAgentRenderer = controlledAgent.GetComponent<Renderer>();
        originalColor = controlledAgentRenderer.material.color;
        controlledAgentRenderer.material.color = Color.yellow; // Highlight color

        // Get the Rigidbody2D component of the controlled agent
        controlledAgentRigidbody = controlledAgent.GetComponent<Rigidbody2D>();
    }
}
