using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public AIAgent[] redTeamAgents;
    public AIAgent[] blueTeamAgents;
    public Transform redTeamPrison;
    public Transform blueTeamPrison;
    public Transform redTeamBase;
    public Transform blueTeamBase;
    public int flagsToCaptureToWin = 4;
    public GameObject gameOverUI;
    public Text gameOverText;

    private int redTeamCapturedFlags = 0;
    private int blueTeamCapturedFlags = 0;

    public PlayerController playerController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        gameOverUI.SetActive(false);
    }

    void Update()
    {
        CheckForWin();
    }

    public void CaptureFlag(Team team)
    {
        if (team == Team.Red)
        {
            redTeamCapturedFlags++;
        }
        else if (team == Team.Blue)
        {
            blueTeamCapturedFlags++;
        }
    }

    void CheckForWin()
    {
        if (redTeamCapturedFlags >= flagsToCaptureToWin)
        {
            EndGame(Team.Red);
        }
        else if (blueTeamCapturedFlags >= flagsToCaptureToWin)
        {
            EndGame(Team.Blue);
        }
    }

    void EndGame(Team winningTeam)
    {
        gameOverUI.SetActive(true);
        if (winningTeam == Team.Red)
        {
            gameOverText.text = "Red Team Wins!";
        }
        else if (winningTeam == Team.Blue)
        {
            gameOverText.text = "Blue Team Wins!";
        }

        // Disable further gameplay
        foreach (var agent in redTeamAgents)
        {
            agent.enabled = false;
        }

        foreach (var agent in blueTeamAgents)
        {
            agent.enabled = false;
        }

        // If using the player controller to control agents, disable player control
        if (playerController != null && playerController.controlledAgent != null)
        {
            playerController.controlledAgent.isControlledByPlayer = false;
            playerController.controlledAgent = null;
        }
    }
}
