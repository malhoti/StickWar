using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEditor.VersionControl;

public class RLAgent : MonoBehaviour
{
    public int agentId;
    public TeamVariables tv;
    public TeamVariables enemytv;
    public GlobalVariables gv;

    public int reward;


    private int _towerHealth;
    private int _enemyTowerHealth;
    private int _unitCount;
    private int _enemyUnitCount;
    // Start is called before the first frame update
    private void Awake()
    {
        
    }
    void Start()
    {
        gv = FindObjectOfType<GlobalVariables>();

        tv = gameObject.GetComponent<TeamVariables>();
        agentId = tv.team;


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
        StartCoroutine(WaitForPythonControllerAndSend());

    }
     

        IEnumerator WaitForPythonControllerAndSend()
    {
        while (PythonController.Instance.stream == null && gv == null)
        {
            Debug.Log("Waiting for PythonController instance to become ready...");
            yield return null; // Wait for the next frame
        }
        SendObservation(); // when we connect we send the observation to python first.

    }
   

    public void PlayAction(JObject message)
    {
        int action = message["action"].Value<int>();
        reward = 0;

        /*
         * 1 : Retreat
         * 2 : Defend
         * 3 : Advance
         * 4 : Spawn Miner
         * 5 : Spawn Swordsman
         * 6 : Spawn Archer
         */

        // These are rewards that are given based off the action
        if (action == 1)
        {
            tv.state = State.Retreat;
            if (tv.units <= 0) // discourage to retreat if there are no units
            {
                reward -= 100;
            }
        }
        else if (action == 2)
        {
            tv.state = State.Defend;
        }
        else if (action == 3)
        {
            tv.state = State.Advance;
            if (tv.frontLineUnits.Count == 0 && tv.rearLineUnits.Count ==0) // discourage to advance if there are no attack units
            {
                reward -= 100;
            }
        }
        else if (action == 4)
        {
            if (tv.spawn.SpawnUnit(gv.miner))
            {
                reward += 10;
            }
            else
            {
                reward -= 10;
            }
        }
        else if (action == 5)
        {
            if (tv.spawn.SpawnUnit(gv.swordsman))
            {
                reward += 10;
            }
            else
            {
                reward -= 10;
            }
        }
        else if (action == 6)
        {
            if (tv.spawn.SpawnUnit(gv.archer))
            {
                reward += 10;
            }
            else
            {
                reward -= 10;
            }
        }
        else
        {
            Debug.LogWarning("Unknown action received: " + action);
            reward -= 5;
        }

        

        // these are rewards that are not action based but rather what is happening in the game such as if we lose units or do damage to tower

        if (tv.gathererUnits.Count > tv.goldList.Count * 2 || tv.gathererUnits.Count < 0) // we do not need more miners then there are gold options its not efficient and we cant have 0 miners as we need miners
        {
            reward -= 100;
        }

        if (tv.gold > 2000) // discourage passive play
        {
            reward -= 100;
        }

        if (tv.gold > 100){
            reward -= 30;
        }


        if (tv.health < _towerHealth) // punish for getting tower hit
        {
            reward -= 10 * (_towerHealth - tv.health);
        }
        if (tv.units < _unitCount) // puinish for losing units
        {
            reward -= 20 * (_unitCount - tv.units);
        }


        if (enemytv.health < _enemyTowerHealth) //reward for attacking tower and killing their units
        {
            reward += 10 * (_enemyTowerHealth - enemytv.health);
        }
        if (enemytv.units < _unitCount)
        {
            reward += 20 * (_enemyUnitCount - enemytv.units);
        }

        if(tv.state == State.Retreat && enemytv.state != State.Advance) // punish for staying retreat if enemy isnt attacking
        {
            reward -= 200;
        }



        _towerHealth = tv.health;
        _unitCount = tv.units;
        _enemyTowerHealth = enemytv.health;      
        _enemyUnitCount = enemytv.units;

    // we will process the message and action to take here


        Debug.Log($"messaged recieved by agent {agentId}, and has been processed");
        SendObservation();
        
    }

    public void SendObservation()
    {
        JObject newState = GetState();
        bool done = tv.isDead;

        // Prepare the message to send back to Python
        JObject response = new JObject
        {
            ["agent_id"] = agentId,
            ["state"] = newState,
            ["reward"] = reward,
            ["done"] = tv.isDead
        };
        PythonController.Instance.SendToPython(response);
    }

    public JObject GetState()
    {
        if (tv == null)
        {
            Debug.LogError($"RLAgent (ID: {agentId}) - 'tv' (TeamVariables) is null.");
            return null; // or handle appropriately
        }
        JObject state = new JObject
        {
            ["gold"] = tv.gold,
            ["health"] = tv.health,
            ["miners"] = tv.gathererUnits.Count,
            ["swordsmen"] = tv.frontLineUnits.Count,
            ["archers"] = tv.rearLineUnits.Count,
            ["stateValue"] = (int)tv.state,
            ["enemies_in_vicinity"] = tv.enemiesInVicinity.Count
        };
        return state;
    }
}
