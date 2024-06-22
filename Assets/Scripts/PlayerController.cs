using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public AIAgent[] BlueTeamAgents;
    public AIAgent ControlledAgent;
    public float MoveSpeed = 5f;
    private Renderer _controlledAgentRenderer;
    private Color _originalColor;
    private Rigidbody2D _controlledAgentRigidbody;

    // Clamping boundaries
    public float MinX = -16f;
    public float MaxX = 16f;
    public float MinY = -9f;
    public float MaxY = 9f;

    private void Update()
    {
        HandleAgentSelection();
    }

    private void FixedUpdate()
    {
        if (ControlledAgent != null && ControlledAgent.IsControlledByPlayer &&
            ControlledAgent.CurrentState != AIAgent.State.Captured &&
            ControlledAgent.CurrentState != AIAgent.State.GoingToPrison) HandlePlayerInput();

        if (ControlledAgent == null || !ControlledAgent.IsControlledByPlayer) return;
        CheckFlagCapture();
        CheckEscort();
    }

    private void HandlePlayerInput()
    {
        var moveHorizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrow keys
        var moveVertical = Input.GetAxis("Vertical"); // W/S or Up/Down arrow keys

        // Combine horizontal and vertical inputs for movement in the X and Y plane
        var movement = new Vector2(moveHorizontal, moveVertical);

        // Calculate the new position
        var newPosition = _controlledAgentRigidbody.position + movement * MoveSpeed * Time.fixedDeltaTime;

        // Clamp the new position
        newPosition.x = Mathf.Clamp(newPosition.x, MinX, MaxX);
        newPosition.y = Mathf.Clamp(newPosition.y, MinY, MaxY);

        // Move the agent using Rigidbody2D
        _controlledAgentRigidbody.MovePosition(newPosition);
    }

    private void HandleAgentSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectAgent(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectAgent(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SelectAgent(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectAgent(3);
    }

    private void SelectAgent(int index)
    {
        if (index < 0 || index >= BlueTeamAgents.Length) return;

        if (ControlledAgent != null)
        {
            ControlledAgent.IsControlledByPlayer = false; // Release previous agent
            _controlledAgentRenderer.material.color = _originalColor; // Reset color
        }

        var selectedAgent = BlueTeamAgents[index];

        // Prevent selection of captured or going to prison agents
        if (selectedAgent.CurrentState is AIAgent.State.Captured or AIAgent.State.GoingToPrison) return;

        ControlledAgent = selectedAgent;
        ControlledAgent.IsControlledByPlayer = true; // Control new agent

        // Highlight the selected agent
        _controlledAgentRenderer = ControlledAgent.GetComponent<Renderer>();
        _originalColor = _controlledAgentRenderer.material.color;
        _controlledAgentRenderer.material.color = Color.yellow; // Highlight color

        // Get the Rigidbody2D component of the controlled agent
        _controlledAgentRigidbody = ControlledAgent.GetComponent<Rigidbody2D>();
    }

    public void ReleaseControl()
    {
        if (ControlledAgent == null) return;
        ControlledAgent.IsControlledByPlayer = false;
        _controlledAgentRenderer.material.color = _originalColor; // Reset color
        ControlledAgent = null;
    }

    private void CheckFlagCapture()
    {
        if (ControlledAgent.CarriedFlag == null || !IsInOwnTerritory()) return;
        ControlledAgent.CaptureFlag();
    }

    private void CheckEscort()
    {
        if (ControlledAgent.CurrentState != AIAgent.State.Escorting || !IsInOwnTerritory()) return;
        ControlledAgent.EscortedAgent.FreeFromPrison();
        ControlledAgent.EscortedAgent = null;
        ControlledAgent.CurrentState = AIAgent.State.Idle;
    }

    private bool IsInOwnTerritory()
    {
        return (ControlledAgent.Team == Team.Red && ControlledAgent.transform.position.x >= 0) ||
               (ControlledAgent.Team == Team.Blue && ControlledAgent.transform.position.x <= 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var flag = collision.GetComponent<Flag>();
        if (flag != null && ControlledAgent != null && ControlledAgent.CurrentState != AIAgent.State.Captured &&
            ControlledAgent.CurrentState != AIAgent.State.GoingToPrison) ControlledAgent.PickUpFlag(flag);

        var allyAgent = collision.GetComponent<AIAgent>();
        if (allyAgent != null && allyAgent.Team == ControlledAgent!.Team &&
            allyAgent.CurrentState == AIAgent.State.Captured) ControlledAgent.StartEscorting(allyAgent);
    }
}