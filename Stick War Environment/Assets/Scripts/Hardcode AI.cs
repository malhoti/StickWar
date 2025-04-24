using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HardcodeAI : MonoBehaviour
{

    public TeamVariables tv;
    public GlobalVariables gv;
    public bool easyMode;
    public int easyModeSpawnCounter;
    // Start is called before the first frame update
    void Start()
    {
        tv = GetComponent<TeamVariables>();
        gv = FindObjectOfType<GlobalVariables>();
        easyModeSpawnCounter = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (gv.gameOver) easyModeSpawnCounter = 0; // resets the counter per episode
        if (easyMode && easyModeSpawnCounter <=3)
        {
            // --- Step 1: Default to Defend ---
            tv.state = State.Defend;

            // --- Step 2: Basic Miner Logic ---
            // If it has less than 2 miners and has gold, try to spawn one
            if (tv.gathererUnits.Count < 2 && tv.gold >= 200)
            {
                easyModeSpawnCounter++;
                tv.spawn.SpawnUnit(gv.miner);
                return;
            }

            // --- Step 3: Random Unit Spamming ---
            // If enough gold, spam units without any real strategy.
            if (tv.frontLineUnits.Count + tv.rearLineUnits.Count < 2)
            {
                if (tv.gold >= 150)
                {
                    easyModeSpawnCounter++;
                    tv.spawn.SpawnUnit(gv.archer); // more expensive, so usually less spammed
                    return;
                }

                if (tv.gold >= 100)
                {
                    easyModeSpawnCounter++;
                    tv.spawn.SpawnUnit(gv.swordsman);
                    return;
                }
                
            }
            if (tv.frontLineUnits.Count + tv.rearLineUnits.Count >= 2)
            {
                tv.state = State.Advance;
            }
        }
        else if (!easyMode)
        {
            // --- Step 1: Miner Recovery ---
            // If gather units (miners) are lost, prioritize rebuilding them.
            if (tv.gathererUnits.Count <= 1)
            {
                tv.state = State.Defend;
                // Try to spawn miners until you have 2, if gold permits.
                while (tv.gathererUnits.Count < 2 && tv.gold >= 200)
                {
                    tv.spawn.SpawnUnit(gv.miner);
                }
                return; // Wait for next cycle.
            }

            // --- Step 2: Extra Miner at 200 Gold ---
            // Early in the game, once you have 200 gold, spawn an extra miner if needed.
            if (tv.gold >= 200 && tv.gathererUnits.Count < 2)
            {
                tv.spawn.SpawnUnit(gv.miner);
                return;
            }

            // --- Step 3: Combat Unit Recovery ---
            // Check if combat units (swordsmen + archers) are wiped out.
            int combatUnits = tv.frontLineUnits.Count + tv.rearLineUnits.Count;
            if (combatUnits == 0)
            {
                tv.state = State.Defend;
                // Spawn one swordsman (cost: 100) if possible.
                if (tv.gold >= 100)
                {
                    tv.spawn.SpawnUnit(gv.swordsman);
                }
                // Spawn one archer (cost: 150) if possible.
                if (tv.gold >= 150)
                {
                    tv.spawn.SpawnUnit(gv.archer);
                }
                return;
            }

            // --- Step 4: Army Building ---
            // Aim to have 3 swordsmen.
            if (tv.frontLineUnits.Count < 3)
            {
                if (tv.gold >= 100)
                {
                    tv.spawn.SpawnUnit(gv.swordsman);
                }
                return;
            }

            // And aim to have 2 archers.
            if (tv.rearLineUnits.Count < 2)
            {
                if (tv.gold >= 150)
                {
                    tv.spawn.SpawnUnit(gv.archer);
                }
                return;
            }

            // --- Step 5: Attack ---
            // With the required army built, change the state to attack.
            tv.state = State.Advance;
        }
        
    }
}

