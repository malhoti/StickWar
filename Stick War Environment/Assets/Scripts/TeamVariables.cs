using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State
{
    Retreat = 0,
    Defend = 1,
    Advance = 2
}

public class TeamVariables : MonoBehaviour

{
    public GlobalVariables gv;

    [Header("Team Number")]
    public int team;

    [Header("")]
    public Transform retreatPos;
    public Transform unloadPos;
    public Transform defendPos;
    public Transform towerArcherPos;
    public Transform defendMaxPos;
    public ArmySpawn spawn;
    public List<GoldSpawn> goldSpawns;
    public Tower tower;


    [Header("Variables")]
    public int gold;
    public float health;
    public int units;
    public List<Gold> goldList;
    public List<GameObject> gathererUnits;
    public List<GameObject> frontLineUnits;
    public List<GameObject> rearLineUnits;
    public List<GameObject> garrisonLineUnits;
    public List<Unit> enemiesInVicinity;
    public State state;
    public bool isDead;

    private int initialGold;
    public float initialHealth;
    private Vector3 initialPosition;
    private State initialState;

    private Coroutine passiveGoldCoroutine;

    void Start()
    {
        gv = FindObjectOfType<GlobalVariables>();
        team = (gameObject.name == "Team1") ? 1 : 2;

        state = State.Defend;
        isDead = false;
        health = tower.health;

        // Store initial values
        initialGold = gold;             // Adjust based on starting gold
        initialHealth = health;          // Set initial health
        initialPosition = transform.position;
        initialState = State.Defend;
        


        spawn.SpawnUnit(gv.miner);

        for (int i = 0; i < gv.maxUnitsPerColumn; i++)
        {
            
            spawn.SpawnUnit(gv.garrisonArcher);
        }

        foreach (GoldSpawn spawn in goldSpawns)
        {
            spawn.SpawnGoldOres();
        }

        passiveGoldCoroutine =  StartCoroutine(PassiveGoldCoroutine());      
    }



    IEnumerator PassiveGoldCoroutine()
    {
        while (true) // Infinite loop
        {
            yield return new WaitForSeconds(3f);
            gold += gv.passiveGoldRate;
            
        }
    }


    public void ResetEnvironment()
    {

        if (passiveGoldCoroutine != null)
        {
            StopCoroutine(passiveGoldCoroutine);
        }

        // Reset basic variables
        gold = initialGold;
        health = initialHealth;
        units = 0;

        // Destroy all objects in the lists and clear them
        foreach (GameObject gatherer in gathererUnits)
        {
            if(gatherer != null) Destroy(gatherer);
        }
        gathererUnits.Clear();

        foreach (GameObject frontLineUnit in frontLineUnits)
        {
            if(frontLineUnit != null)Destroy(frontLineUnit);
        }
        frontLineUnits.Clear();

        foreach (GameObject rearLineUnit in rearLineUnits)
        {
            if(rearLineUnit!= null)Destroy(rearLineUnit);
        }
        rearLineUnits.Clear();

        foreach (GameObject garrisonLineUnit in garrisonLineUnits)
        {
            if (garrisonLineUnit != null) Destroy(garrisonLineUnit);
        }
        garrisonLineUnits.Clear();

        foreach (Unit enemy in enemiesInVicinity)
        {
            if(enemy != null) Destroy(enemy.gameObject);
        }
        enemiesInVicinity.Clear();

        // Clear gold list if it holds references to GameObjects (assuming these are resources)
        foreach (Gold gold in goldList)
        {
            gold.Destroy();
        }
        goldList.Clear();

        // Reset other states
        state = initialState;
        isDead = false;

        // Reset the gold spawns
        foreach (GoldSpawn spawn in goldSpawns)
        {
            
            spawn.SpawnGoldOres(); // Assuming this resets the gold ores for each spawn point
        }

        // Reset the tower
        tower.health = health;
        tower.alive = true;
        tower.gameObject.SetActive(true);

        
        spawn.SpawnUnit(gv.miner);

        for (int i = 0; i < gv.maxUnitsPerColumn; i++)
        {
            spawn.SpawnUnit(gv.garrisonArcher);
        }
        // Restart any coroutines or timers

        passiveGoldCoroutine = StartCoroutine(PassiveGoldCoroutine());

        //Debug.Log("Environment reset for team: " + team);
    }
}
