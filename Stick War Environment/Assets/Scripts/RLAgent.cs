using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEditor.VersionControl;
using System.IO;
using System.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Text;
using Unity.VisualScripting;

public class RLAgent : MonoBehaviour
{
    private TcpClient client;
    public NetworkStream stream;
    private ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();
    private bool isConnected = false;
    private float sendInterval = 0.5f;


    public int agentId;
    public TeamVariables tv;
    public TeamVariables enemytv;
    public GlobalVariables gv;

    public int reward;


    private int _towerHealth;
    private int _enemyTowerHealth;
    private int _unitCount;
    private int _enemyUnitCount;

    private bool renderEpisodeStarted;
    // Start is called before the first frame update
    private void Awake()
    {
        
    }
    void Start()
    {
        gv = FindObjectOfType<GlobalVariables>();

        tv = gameObject.GetComponent<TeamVariables>();
        agentId = tv.team;
        renderEpisodeStarted = true;

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
        StartCoroutine(ConnectAndCommunicateWithServer());

    }


    IEnumerator ConnectAndCommunicateWithServer()
    {
        // Attempt connection
        try
        {
            client = new TcpClient("192.168.1.206", 5000);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log($"Agent {agentId} connected to Python server.");
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
            yield break;
        }

        // Start sending initial observation
        SendObservation();

        // Begin asynchronous reception
        while (isConnected)
        {
            yield return new WaitForSeconds(sendInterval);
            ReceiveFromPython();
        }
    }

    async void ReceiveFromPython()
    {
        if (stream == null || !stream.CanRead) return;

        try
        {
            byte[] data = new byte[256];
            int bytes = await stream.ReadAsync(data, 0, data.Length);
            string response = Encoding.ASCII.GetString(data, 0, bytes);
            JObject message = JObject.Parse(response);

            PlayAction(message);
        }
        catch (Exception e)
        {
            Debug.LogError("Receive error: " + e.Message);
            isConnected = false;
        }
    }

    private void UpdateRenderSettings(bool renderEpisode)
    {
        if (renderEpisode)
        {
            Time.timeScale = 1;
            Camera.main.enabled = true;

        }
        else if (!renderEpisode)
        {
            Time.timeScale = 20;
            Camera.main.enabled = false;
        }  
    }

    public void PlayAction(JObject message)
    {
        int action = message["action"].Value<int>();
        bool renderEpisode = message["render"].Value<bool>();

        int attackArmyCount = tv.frontLineUnits.Count + tv.rearLineUnits.Count;
        int enemyAttackArmyCount = enemytv.frontLineUnits.Count + enemytv.rearLineUnits.Count;


        //if (renderEpisode)
        //{
        //    Time.timeScale = 1;
        //    Camera.main.enabled = true;

        //}
        //else if (!renderEpisode)
        //{
        //    Time.timeScale = 20;
        //    Camera.main.enabled = false;
        //}

        //if (renderEpisode != renderEpisodeStarted)
        //{
        //    UpdateRenderSettings(renderEpisode);
        //}



        reward = 0;

        /* 0 : Do Nothing
         * 1 : Retreat
         * 2 : Defend
         * 3 : Advance
         * 4 : Spawn Miner
         * 5 : Spawn Swordsman
         * 6 : Spawn Archer
         */

        // These are rewards that are given based off the action
        if (action == 0) {
            if (tv.gold < gv.minerCost || tv.units >= gv.maxUnits)
            {
                reward += 5; // Small reward for waiting until resources are adequate
            }

            if (tv.gold > gv.minerCost && tv.units < gv.maxUnits)
            {
                reward -= 2; // Slight penalty for inaction when resources allow
            }
            if (tv.state == State.Defend && tv.frontLineUnits.Count > 0)
            {
                reward += 2; // Reward for holding a strong defensive position
            }
        }
        else if (action == 1)
        {
            if (tv.state == State.Retreat) // discourage clicking same state
            {
                reward -= 10;
            }
            if (tv.units <= 0) // discourage to retreat if there are no units
            {
                reward -= 10;
            }
            if(enemytv.state != State.Advance)
            {
                reward -= 10;
            }
            if(attackArmyCount < enemyAttackArmyCount)
            {
                reward += 50;
            }
            tv.state = State.Retreat;
        }
        else if (action == 2)
        {
            if (tv.state == State.Defend) // discourage clicking same state
            {
                reward -= 10;
            }
            tv.state = State.Defend;
        }
        else if (action == 3)
        {
            if (tv.state == State.Advance) // discourage clicking same state
            {
                reward -= 10;
            }
            
            if (tv.frontLineUnits.Count == 0 && tv.rearLineUnits.Count ==0) // discourage to advance if there are no attack units
            {
                reward -= 10;
            }

            if(enemytv.state == State.Retreat && (attackArmyCount>enemyAttackArmyCount)) // encorage attack if you have more units when they are retreating
            {
                reward += 50;
            }

            
            tv.state = State.Advance;
        }
        else if (action == 4)
        {
            
            if (tv.spawn.SpawnUnit(gv.miner))
            {
                reward += 5;
            }
            else
            {
                reward -= 5;
            }

        }
        else if (action == 5)
        {
            if (tv.spawn.SpawnUnit(gv.swordsman))
            {
                reward += 20;
            }
            else
            {
                reward -= 20;
            }
        }
        else if (action == 6)
        {
            if (tv.spawn.SpawnUnit(gv.archer))
            {
                reward += 20;
            }
            else
            {
                reward -= 20;
            }
        }
        else
        {
            Debug.LogWarning("Unknown action received: " + action);
            reward -= 5;
        }



        // these are rewards that are not action based but rather what is happening in the game such as if we lose units or do damage to tower

        ApplyRewards();


        //Debug.Log($"messaged recieved by agent {agentId}, and has been processed");
        SendObservation();
        
    }
    private void ApplyRewards()
    {
        if (tv.gathererUnits.Count > tv.goldList.Count * 2 || tv.gathererUnits.Count < 0) // we do not need more miners then there are gold options its not efficient and we cant have 0 miners as we need miners
        {
            reward -= 100;
        }

        if (tv.gold > 2000) // discourage passive play
        {
            reward -= 100;
        }

        if (tv.gold > 1000)
        {
            reward -= 30;
        }
        if (tv.gold < 1000 && tv.gathererUnits.Count > 0)
        {
            reward += 5; // Encourage maintaining active miners
        }


        if (tv.health < _towerHealth) // punish for getting tower hit
        {
            reward -= 1 * (_towerHealth - tv.health);
        }
        if (tv.units < _unitCount) // puinish for losing units
        {
            reward -= 10 * (_unitCount - tv.units);
        }


        if (enemytv.health < _enemyTowerHealth) //reward for attacking tower and killing their units
        {
            reward += 1 * (_enemyTowerHealth - enemytv.health);
        }
        if (enemytv.units < _unitCount)
        {
            reward += 5 * (_enemyUnitCount - enemytv.units);
        }


        if (tv.isDead)
        {
            reward -= 200;
        }
        if (enemytv.isDead)
        {
            reward += 200;
        }

        _towerHealth = tv.health;
        _unitCount = tv.units;
        _enemyTowerHealth = enemytv.health;
        _enemyUnitCount = enemytv.units;
    }

    public void SendObservation()
    {
        JObject newState = GetState();
        
        if (gv.gameOver) Debug.Log($"agentId: {agentId} : Team dead : {tv.isDead}  and Enemy Team : {enemytv.isDead}");

        //// Update global gameOver only if any agent sees game over
        //if (done && !gv.gameOver)
        //{
        //    gv.gameOver = true;  // Set global gameOver flag
        //}

        // Prepare the message to send back to Python
        JObject response = new JObject
        {
            ["agent_id"] = agentId,
            ["state"] = newState,
            ["reward"] = reward,
            ["done"] = gv.gameOver
        };

        
        SendToPython(response);


        // After sending the observation, increment end-of-episode count if gameOver is true
        if (gv.gameOver)
        {
            gv.endOfEpisodeCount++;

            // If both agents have confirmed gameOver, reset the environment
            if (gv.endOfEpisodeCount >= 2)
            {
                gv.ResetEnvironment();
               
            }
        }
    }

    public JObject GetState()
    {
        JObject state = new JObject
        {
            ["gold"] = tv.gold,
            ["health"] = tv.health,
            ["miners"] = tv.gathererUnits.Count,
            ["swordsmen"] = tv.frontLineUnits.Count,
            ["archers"] = tv.rearLineUnits.Count,
            ["stateValue"] = (int)tv.state,
            ["nearby_resources_available"] = tv.goldList.Count,

            ["enemy_health"] = enemytv.health,
            ["enemy_miners"] = enemytv.gathererUnits.Count / 10.0f,
            ["enemy_swordsmen"] = enemytv.frontLineUnits.Count,
            ["enemy_archers"] = enemytv.rearLineUnits.Count,
            ["enemies_in_vicinity"] = tv.enemiesInVicinity.Count,
            ["episode_time"] = gv.time
        };
        return state;
    }

    public async void SendToPython(JObject message)
    {
        try
        {
            // Convert JSON object to string
            string jsonString = message.ToString();

            // Send JSON string to Python
            byte[] data = Encoding.ASCII.GetBytes(jsonString);
            await stream.WriteAsync(data, 0, data.Length);
            //Debug.Log("Sent to Python: " + jsonString);
        }
        catch (Exception e)
        {
            Debug.LogError("Send error: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }
}
