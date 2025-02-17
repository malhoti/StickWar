using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables : MonoBehaviour
{

    // Start is called before the first frame update
    [Header("Global Settings")]
    public int valuePerGold;
    public int maxUnits;
    public bool gameOver;
    public float spawnCooldown;
    [Tooltip("Gold recieved every 3 seconds")]
    public int passiveGoldRate;


    [Header("Army Soldiers")]
    public GameObject miner;
    public GameObject swordsman;
    public GameObject archer;

    [Header("Army Soldier Cost")]
    public int minerCost;
    public int swordsmanCost;
    public int archerCost;

    [Header("Combat Settings")]
    public int maxUnitsPerColumn;
    public float horizontalSpacing; // the spacing between each unit in that row, so that means the spacing between the units vertically
    public float verticalSpacing; // the gap between each column or the space between each line horizontally

    public TeamVariables team1;
    public TeamVariables team2;

    public float timescale;

    public float time;

    public int endOfEpisodeCount = 0;
    public bool environmentResetInitiated = false;
    public bool newEpisodeReady = false;
    public int totalAgent = 0;
    public int terminalAgentCount= 0;


    void Awake()
    {
        team1 = GameObject.Find("Team1").GetComponent<TeamVariables>();
        team2 = GameObject.Find("Team2").GetComponent<TeamVariables>();
        gameOver = false;

    }



    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        if (team1.isDead || team2.isDead)
        {
            gameOver = true;
            if (gameOver)
            {


                time = 0;
            }
            
        }
        Time.timeScale = timescale;
    }

    public void ResetEnvironment()
    {
        team1.ResetEnvironment();
        team2.ResetEnvironment();   
        gameOver = false;
        endOfEpisodeCount = 0;
        time = 0;

        RLAgent[] agents = FindObjectsOfType<RLAgent>();
        foreach (var agent in agents)
        {
            agent.hasReported = false;
        }

        Debug.Log("Environment reset. All agent flags cleared.");
    }
}
