using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using UnityEditor.VersionControl;


public class CSVLogger
{
    private StreamWriter writer;

    public CSVLogger(string filename)
    {
        writer = new StreamWriter(filename, false); // overwrite existing file
        writer.WriteLine("Step,Description,RewardChange");
    }

    public void Log(int step, string description, float rewardChange)
    {
        writer.WriteLine($"{step},{description},{rewardChange}");
        writer.Flush();
    }

    public void Close()
    {
        writer.Close();
    }
}

public class RLAgent : MonoBehaviour
{
    
    // Frequency (in seconds) for sending observations.
    

    public int agentId;
    [Tooltip("Keep true to train, false for eval mode")]
    public bool trainingMode;// true for to train and false for eval 
    [Tooltip("Keep true for DQN, false for PPO")]
    public bool modelType; //keep true for dqn , false for ppo

    public TeamVariables tv;
    public TeamVariables enemytv;
    public GlobalVariables gv;

    public float reward;

    private float _towerHealth;
    private float _enemyTowerHealth;
    private int _unitCount;
    private int _enemyUnitCount;
    private bool _towerBonusGiven = false;
    

    private CSVLogger rewardLogger;

   
   
    

    private void Awake()
    {
        tv = GetComponent<TeamVariables>();
        agentId = tv.team;
    }

    private void Start()
    {
        gv = FindObjectOfType<GlobalVariables>();
        

        rewardLogger = new CSVLogger($"RewardLog{agentId}.csv");
        // Initialize tower and unit tracking.
        if (tv.team == 1)
        {
            enemytv = gv.team2;
            _towerHealth = tv.health;
            _unitCount = tv.units;
            _enemyTowerHealth = gv.team2.health;
            _enemyUnitCount = gv.team2.units;
        }
        else if (tv.team == 2)
        {
            enemytv = gv.team1;
            _towerHealth = tv.health;
            _unitCount = tv.units;
            _enemyTowerHealth = gv.team1.health;
            _enemyUnitCount = gv.team1.units;
        }
    }


    // Coroutine to process incoming messages from Python.



    // Process the action message received from Python.
    public void PlayAction(int action, int step, bool maxStepsReached)
    {
        

        // Compute rewards and update game state based on the received action.
        // (This block is your game logic; adjust as needed.)
        reward = 0;
        int attackArmyCount = tv.frontLineUnits.Count + tv.rearLineUnits.Count;
        int enemyAttackArmyCount = enemytv.frontLineUnits.Count + enemytv.rearLineUnits.Count;
        

        if ( maxStepsReached)
        {
            Debug.Log($"Agent {agentId}: Max steps reached. Ending episode.");
            // Set the terminal flag so our protocol sends a terminal observation.
            
            gv.gameOver = true;
        }


        /* 0 : Do Nothing
//         * 1 : Retreat
//         * 2 : Defend
//         * 3 : Advance
//         * 4 : Spawn Miner
//         * 5 : Spawn Swordsman
//         * 6 : Spawn Archer
//         */

        switch (action)
        {
            case 0: // Do Nothing
                    // If not enough gold for spawning or already at max units, a little patience is rewarded.
                if (tv.gold < gv.minerCost || tv.units >= gv.maxUnits)
                    reward += 1;
                else
                    reward -= 1;  // If resources are available and nothing is done, slight penalty.
                                  // If already defending with some units, a slight positive bonus.
                if (tv.state == State.Defend && tv.frontLineUnits.Count > 0)
                    reward += 1;
                if (tv.state == State.Advance && tv.frontLineUnits.Count > 0)
                    reward += 1;
                break;
            case 1: // Retreat
                if (tv.state == State.Retreat)
                    reward -= 5; // Penalize repeating the same retreat command.
                if (tv.units <= 0)
                    reward -= 5; // If no units to retreat, penalize.
                if (enemytv.state != State.Advance)
                    reward -= 5; // Retreat might be less appropriate if the enemy is not advancing.
                if (attackArmyCount < enemyAttackArmyCount)
                    reward += 2; // Reward retreat if you are outnumbered.
                tv.state = State.Retreat;
                break;
            case 2: // Defend
                if (tv.state == State.Defend)
                    reward -= 5; // Avoid spamming the same command.
                tv.state = State.Defend;
                break;
            case 3: // Advance
                if (tv.state == State.Advance)
                    reward -= 5; // Penalize repeated advancing.
                if (tv.frontLineUnits.Count == 0 && tv.rearLineUnits.Count == 0)
                    reward -= 5; // Advancing without units is bad.
                if (enemytv.state == State.Retreat && (attackArmyCount > enemyAttackArmyCount))
                    reward += 10; // Encourage advancing when you have a numerical advantage.
                // every unit after 3 units thats sent to attack gets rewarded more for aggresive play
                if (attackArmyCount > 3)
                {
                    reward += 10 * (attackArmyCount - 3);

                }
                tv.state = State.Advance;
                break;
            case 4: // Spawn Miner
                if (tv.spawn.SpawnUnit(gv.miner))
                    reward += 2; // Moderate positive reward.
                else
                    reward -= 1; // Moderate penalty if failed.
                break;
            case 5: // Spawn Swordsman
                if (tv.spawn.SpawnUnit(gv.swordsman))
                    reward += 4; // Reward successful spawning.
                else
                    reward -= 1;
                break;
            case 6: // Spawn Archer
                if (tv.spawn.SpawnUnit(gv.archer))
                    reward += 4;
                else
                    reward -= 1;
                break;
            default:
                Debug.LogWarning("Unknown action received: " + action);
                reward -= 5;
                break;
        }

        // Additional reward logic.
        ApplyRewards(step);

        //Debug.Log($"Agent {agentId}: {reward} given for step :{step}.");
    }

  

    public JObject GetState()
    {
        const float maxGold = 1000f;            // maximum gold possible
        float maxHealth = gv.maxHealth;          // maximum health for towers/base
        float maxUnitCount = gv.maxUnits;         // maximum expected count for units (miners, swordsmen, archers)
        float maxResources = gv.maxGoldOres;         // maximum available nearby resources
        const float maxEpisodeTime = 600f;      // maximum episode time (in seconds or your time unit)

        // Create the state JObject with normalized values
        // Normalize by dividing by the maximum value
        JObject state = new JObject
        {
            ["gold"] = tv.gold / maxGold,
            ["health"] = tv.health / maxHealth,
            ["miners"] = tv.gathererUnits.Count / maxUnitCount,
            ["swordsmen"] = tv.frontLineUnits.Count / maxUnitCount,
            ["archers"] = tv.rearLineUnits.Count / maxUnitCount,
            ["stateValue"] = new JArray(OneHotEncode((int)tv.state, 3)),
            ["nearby_resources_available"] = tv.goldList.Count / maxResources,
            ["enemy_health"] = enemytv.health / maxHealth,
            ["enemy_miners"] = enemytv.gathererUnits.Count / maxUnitCount ,
            ["enemy_swordsmen"] = enemytv.frontLineUnits.Count / maxUnitCount,
            ["enemy_archers"] = enemytv.rearLineUnits.Count / maxUnitCount,
            ["enemies_in_vicinity"] = tv.enemiesInVicinity.Count / maxUnitCount,
            ["episode_time"] = gv.time / maxEpisodeTime
        };

        return state;
    }

    public static int[] OneHotEncode(int strategy, int numStrategies = 3)
    {
        // Initialize a vector of zeros
        int[] oneHot = new int[numStrategies];
        if (strategy >= 0 && strategy < numStrategies)
        {
            oneHot[strategy] = 1;
        }
        return oneHot;
    }

    // Class-level or static variables to track last reward steps.
    // Initialise them to a value that ensures the reward is applied at the start (e.g., -interval).
    private int lastGoldRewardStep = -30;
    private int lastArmyFormationStep = -5;
    private int lastGathererPenaltyStep = -20;

    private void ApplyRewards(int step)
    {
        // Reward/Penalty applied every update.
        // Penalize losing tower health (scaled: 1 reward per 10 health lost).
        if (tv.health < _towerHealth)
        {
            float change = (_towerHealth - tv.health) / 5f;
            reward -= change;
            rewardLogger.Log(step, "Lost tower health", -change);
        }
        // Penalize losing units moderately.
        if (tv.units < _unitCount)
        {
            float change = 2 * (_unitCount - tv.units);
            reward -= change;
            rewardLogger.Log(step, "Lost units", -change);
        }
        // Reward damaging the enemy tower (scaled).
        if (enemytv.health < _enemyTowerHealth)
        {
            float change = (_enemyTowerHealth - enemytv.health) / 3f;
            reward += change;
        }
        if (enemytv.health < 0.75f * tv.initialHealth && !_towerBonusGiven)
        {
            float bonus = 50; // adjust as necessary
            reward += bonus;
            rewardLogger.Log(step, "Enemy Tower Damaged Bonus", bonus);
            _towerBonusGiven = true;  // Ensure you only give this bonus once per episode
        }

        // Reward enemy unit losses moderately.
        if (enemytv.units < _enemyUnitCount)
        {
            float change = 2 * (_enemyUnitCount - enemytv.units);
            reward += change;
            rewardLogger.Log(step, "Enemy unit loss", change);
        }
        // Death penalties and rewards.
        if (tv.isDead)
        {
            float change = 200;
            reward -= change;
            rewardLogger.Log(step, "Agent died", -change);
        }
        if (enemytv.isDead)
        {
            float change = 200;
            reward += change;
            rewardLogger.Log(step, "Enemy died", change);
        }
        // Encourage balanced force composition.
        if (step - lastArmyFormationStep >= 5)
        {
            if (tv.frontLineUnits.Count + tv.rearLineUnits.Count > 0)
            {
                float change = 2;
                reward += change;
                rewardLogger.Log(step, "Have an Army", change);
            }
            else
            {
                float change = 2;
                reward -= change;
                rewardLogger.Log(step, "Dont have an Army", change);
            }
            float desiredRatio = 0.7f;
            float currentRatio = (float)tv.frontLineUnits.Count / (tv.frontLineUnits.Count + tv.rearLineUnits.Count + 1);
            if (Mathf.Abs(currentRatio - desiredRatio) < 0.1f)
            {
                float change = 5;
                reward += change;
                rewardLogger.Log(step, "Balanced forces bonus", change);
            }
            else
            {
                float change = 5;
                reward -= change;
                rewardLogger.Log(step, "Imbalanced forces penalty", -change);
            }
        }
        

        // Apply rewards/penalties at specific step intervals.

        // Every 20 steps: Apply punishment for too many gatherers relative to gold sources.
        if (step - lastGathererPenaltyStep >= 20)
        {
            if (tv.gathererUnits.Count > tv.goldList.Count * 2)
            {
                float change = -50;
                reward += change;
                rewardLogger.Log(step, "Too many gatherers relative to gold sources", change);
            }
            else if (tv.gathererUnits.Count > tv.goldList.Count)
            {
                float change = -20;
                reward += change;
                rewardLogger.Log(step, "No need to have this many gatherers", change);
            }
            else if (tv.gathererUnits.Count == 0)
            {
                float change = -50;
                reward += change;
                rewardLogger.Log(step, "No Gatherers is bad", change);
            }
            else
            {
                if (tv.gathererUnits.Count > 0 || tv.gathererUnits.Count < tv.goldList.Count)
                {
                    float change = 10;
                    reward += change;
                    rewardLogger.Log(step, "Perfect amount of miners", change);
                }
            }
            lastGathererPenaltyStep = step;
        }

        // Every 30 steps: Apply gold-related rewards/penalties.
        if (step - lastGoldRewardStep >= 30)
        {
            if (tv.gold > 2000)
            {
                float change = -50;
                reward += change;
                rewardLogger.Log(step, "Gold > 2000 (hoarding penalty)", change);
            }
            else if (tv.gold > 1000)
            {
                float change = -20;
                reward += change;
                rewardLogger.Log(step, "Gold > 1000", change);
            }
            else if (tv.gold < 1000 && tv.gathererUnits.Count > 0)
            {
                float change = 2;
                reward += change;
                rewardLogger.Log(step, "Gold < 1000 and active gatherers", change);
            }
            lastGoldRewardStep = step;
        }

        // Update baseline values for next step.
        _towerHealth = tv.health;
        _unitCount = tv.units;
        _enemyTowerHealth = enemytv.health;
        _enemyUnitCount = enemytv.units;
    }




    public void ResetAgentEnv()
    {
        if (tv.team == 1)
        {
            enemytv = gv.team2;
            _towerHealth = tv.health;
            _unitCount = tv.units;
            _enemyTowerHealth = gv.team2.health;
            _enemyUnitCount = gv.team2.units;
        }
        else if (tv.team == 2)
        {
            enemytv = gv.team1;
            _towerHealth = tv.health;
            _unitCount = tv.units;
            _enemyTowerHealth = gv.team1.health;
            _enemyUnitCount = gv.team1.units;
        }
        _towerBonusGiven = false;
    }

    
}
