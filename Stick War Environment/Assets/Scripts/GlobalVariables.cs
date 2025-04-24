using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GlobalVariables : MonoBehaviour
{

    // Start is called before the first frame update
    [Header("Global Settings")]
    public int maxGoldOres;
    public int valuePerGold;
    public int maxUnits;
    public bool gameOver;
    public float maxTimeLimit;
    [Tooltip("Gold recieved every 3 seconds")]
    public int passiveGoldRate;
    public float maxHealth;


    [Header("Army Soldiers")]
    public GameObject miner;
    public GameObject swordsman;
    public GameObject archer;
    public GameObject garrisonArcher;

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


        if (team1.isDead || team2.isDead || time > maxTimeLimit)
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
        
        time = 0;

        

        Debug.Log("Environment reset. All agent flags cleared.");
    }
}
