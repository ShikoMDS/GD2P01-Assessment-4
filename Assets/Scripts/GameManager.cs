using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public AIAgent[] RedTeamAgents;
    public AIAgent[] BlueTeamAgents;
    public Transform RedTeamPrison;
    public Transform BlueTeamPrison;
    public int FlagsToCaptureToWin = 4;

    public GameObject GameOverUi;
    public TextMeshProUGUI RedTeamWinText;
    public TextMeshProUGUI BlueTeamWinText;
    public Button ReplayButton;
    public Button MenuButton;

    public GameObject PauseMenuUi;

    private int _redTeamCapturedFlags;
    private int _blueTeamCapturedFlags;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Time.timeScale = 1;
    }

    private void Start()
    {
        GameOverUi.SetActive(false);
        RedTeamWinText.gameObject.SetActive(false);
        BlueTeamWinText.gameObject.SetActive(false);
        ReplayButton.gameObject.SetActive(false);
        MenuButton.gameObject.SetActive(false);

        ReplayButton.onClick.AddListener(Retry);
        MenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void Update()
    {
        CheckForWin();

        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (Time.timeScale == 0)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        PauseMenuUi.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        PauseMenuUi.SetActive(false);
        Time.timeScale = 1f;
    }

    public void CaptureFlag(Team team)
    {
        switch (team)
        {
            case Team.Red:
                _redTeamCapturedFlags++;
                break;
            case Team.Blue:
                _blueTeamCapturedFlags++;
                break;
        }

        CheckForWin();
    }

    private void CheckForWin()
    {
        if (_redTeamCapturedFlags >= FlagsToCaptureToWin)
        {
            RedTeamWinText.gameObject.SetActive(true);
            EndGame();
        }
        else if (_blueTeamCapturedFlags >= FlagsToCaptureToWin)
        {
            BlueTeamWinText.gameObject.SetActive(true);
            EndGame();
        }

        var blueTeamInPrison = BlueTeamAgents.Count(agent => agent.CurrentState == AIAgent.State.Captured);

        var redTeamInPrison = RedTeamAgents.Count(agent => agent.CurrentState == AIAgent.State.Captured);

        if (blueTeamInPrison >= BlueTeamAgents.Length)
        {
            RedTeamWinText.gameObject.SetActive(true);
            EndGame();
        }

        if (redTeamInPrison < RedTeamAgents.Length) return;
        BlueTeamWinText.gameObject.SetActive(true);
        EndGame();
    }

    private void EndGame()
    {
        GameOverUi.SetActive(true);
        ReplayButton.gameObject.SetActive(true);
        MenuButton.gameObject.SetActive(true);

        foreach (var agent in RedTeamAgents) agent.enabled = false;

        foreach (var agent in BlueTeamAgents) agent.enabled = false;

        var playerController = FindObjectOfType<PlayerController>();
        if (playerController.ControlledAgent != null)
        {
            playerController.ControlledAgent.IsControlledByPlayer = false;
            playerController.ControlledAgent = null;
        }

        Time.timeScale = 0;
    }

    public void Retry()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Main Menu");
    }
}