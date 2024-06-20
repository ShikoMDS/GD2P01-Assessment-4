using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int flagsToWin = 4;

    private int blueTeamScore = 0;
    private int redTeamScore = 0;

    private void Awake()
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
    }

    private void DeclareWinner(Team winningTeam)
    {
        Debug.Log($"{winningTeam} Team wins!");
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
}