using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Global Settings")]
    public int valuePerGold;
    public int maxUnits;
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
    
    
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
