using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int flagsToWin = 4;

    private int blueTeamScore = 0;
    private int redTeamScore = 0;

    public GameObject winTextPrefab; // Reference to Win text prefab
    public GameObject loseTextPrefab; // Reference to Lose text prefab
    public Transform uiCanvas; // Reference to UI Canvas

    private Agent[] blueTeamAgents; // Array to hold all Blue team agents
    private Agent[] redTeamAgents; // Array to hold all Red team agents

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to sceneLoaded event
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        AssignReferences();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignReferences(); // Reassign references after the scene is loaded
    }

    private void AssignReferences()
    {
        // Only assign references and log debug information in the "Game" scene
        if (SceneManager.GetActiveScene().name == "Game")
        {
            blueTeamAgents = FindObjectsOfType<Agent>().Where(agent => agent.gameObject.layer == LayerMask.NameToLayer("BlueTeam")).ToArray();
            redTeamAgents = FindObjectsOfType<Agent>().Where(agent => agent.gameObject.layer == LayerMask.NameToLayer("RedTeam")).ToArray();

            Debug.Log($"Blue Team Agents: {blueTeamAgents.Length}");
            Debug.Log($"Red Team Agents: {redTeamAgents.Length}");

            if (blueTeamAgents.Length == 0 || redTeamAgents.Length == 0)
            {
                Debug.LogError("Team agents not initialized properly. Make sure agents are assigned to the correct layers.");
            }

            if (winTextPrefab == null || loseTextPrefab == null || uiCanvas == null)
            {
                Debug.LogError("UI elements are not assigned in the Inspector.");
            }
        }
    }

    public void ScorePoint(Team team)
    {
        if (team == Team.Blue)
        {
            blueTeamScore++;
            Debug.Log($"Blue Team scored! Current score: {blueTeamScore}");
            if (blueTeamScore >= flagsToWin)
            {
                DeclareWinner(Team.Blue);
            }
        }
        else if (team == Team.Red)
        {
            redTeamScore++;
            Debug.Log($"Red Team scored! Current score: {redTeamScore}");
            if (redTeamScore >= flagsToWin)
            {
                DeclareWinner(Team.Red);
            }
        }

        CheckGameEnd(); // Check if game should end after scoring
    }

    private void DeclareWinner(Team winningTeam)
    {
        Debug.Log($"{winningTeam} Team wins!");
        DisplayEndText(winningTeam == Team.Blue); // Display win/lose text
    }

    public void ReturnFlagToBase(GameObject flag)
    {
        Flag flagScript = flag.GetComponent<Flag>();
        if (flagScript != null)
        {
            flagScript.ResetPosition();
        }
    }

    public void ResetScores()
    {
        blueTeamScore = 0;
        redTeamScore = 0;
        Debug.Log("Scores reset");
    }

    private void CheckGameEnd() // Method to check game end conditions
    {
        if (blueTeamAgents == null || redTeamAgents == null)
        {
            Debug.LogError("Team agents not initialized properly.");
            return;
        }

        bool blueTeamWins = blueTeamScore >= flagsToWin || redTeamAgents.All(agent => agent.currentState == Agent.AgentState.InPrison);
        bool redTeamWins = redTeamScore >= flagsToWin || blueTeamAgents.All(agent => agent.currentState == Agent.AgentState.InPrison);

        if (blueTeamWins)
        {
            DisplayEndText(true);
        }
        else if (redTeamWins)
        {
            DisplayEndText(false);
        }
    }

    private void DisplayEndText(bool blueWins) // Method to display win/lose text
    {
        if (winTextPrefab == null || loseTextPrefab == null || uiCanvas == null)
        {
            Debug.LogError("UI elements are not assigned in the Inspector.");
            return;
        }

        GameObject endText = Instantiate(blueWins ? winTextPrefab : loseTextPrefab, uiCanvas);
        UnityEngine.UI.Text textComponent = endText.GetComponent<UnityEngine.UI.Text>();
        if (textComponent != null)
        {
            textComponent.text = blueWins ? "Win" : "Lose";
        }
    }
}
