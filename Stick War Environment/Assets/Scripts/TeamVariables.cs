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
    public int health;
    public int units;
    public List<Gold> goldList;
    public List<GameObject> gathererUnits;
    public List<GameObject> frontLineUnits;
    public List<GameObject> rearLineUnits;
    public List<Unit> enemiesInVicinity;
    public State state;
    public bool isDead;

    private int initialGold;
    private int initialHealth;
    private Vector3 initialPosition;
    private State initialState;

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

        foreach (GoldSpawn spawn in goldSpawns)
        {
            spawn.SpawnGoldOres();
        }
        
        StartCoroutine(PassiveGoldCoroutine());      
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
        // Reset basic variables
        gold = initialGold;
        health = initialHealth;
        units = 0;

        // Destroy all objects in the lists and clear them
        foreach (GameObject gatherer in gathererUnits)
        {
            Destroy(gatherer);
        }
        gathererUnits.Clear();

        foreach (GameObject frontLineUnit in frontLineUnits)
        {
            Destroy(frontLineUnit);
        }
        frontLineUnits.Clear();

        foreach (GameObject rearLineUnit in rearLineUnits)
        {
            Destroy(rearLineUnit);
        }
        rearLineUnits.Clear();

        foreach (Unit enemy in enemiesInVicinity)
        {
            Destroy(enemy.gameObject);
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

        print(initialGold);
        spawn.SpawnUnit(gv.miner);


        // Restart any coroutines or timers
        StopCoroutine(PassiveGoldCoroutine());
        StartCoroutine(PassiveGoldCoroutine());

        //Debug.Log("Environment reset for team: " + team);
    }
}
