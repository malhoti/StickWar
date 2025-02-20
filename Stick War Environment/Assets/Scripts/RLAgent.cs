//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Newtonsoft.Json.Linq;
//using System.IO;
//using System.Net.Sockets;
//using System;
//using System.Collections.Concurrent;
//using System.Text;
//using System.Threading;


//public class CSVLogger
//{
//    private StreamWriter writer;

//    public CSVLogger(string filename)
//    {
//        writer = new StreamWriter(filename, false); // overwrite existing file
//        writer.WriteLine("Step,Description,RewardChange");
//    }

//    public void Log(int step, string description, float rewardChange)
//    {
//        writer.WriteLine($"{step},{description},{rewardChange}");
//        writer.Flush();
//    }

//    public void Close()
//    {
//        writer.Close();
//    }
//}

//public class RLAgent : MonoBehaviour
//{
//    private TcpClient client;
//    public NetworkStream stream;
//    // This queue holds incoming messages from Python in a thread‑safe way.
//    private ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();
//    private bool isConnected = false;

//    // Frequency (in seconds) for sending observations.
//    private float sendInterval = 1f;

//    public int agentId;
//    public TeamVariables tv;
//    public TeamVariables enemytv;
//    public GlobalVariables gv;

//    public float reward;

//    private int _towerHealth;
//    private int _enemyTowerHealth;
//    private int _unitCount;
//    private int _enemyUnitCount;

//    private CSVLogger rewardLogger; 

//    // A flag to indicate if this agent has already reported the terminal state.
//    public bool hasReported = false;
//    // A flag to indicate if the episode is over (set by game logic in Unity)
//    public bool episodeIsTerminal = false;
//    private bool resetAckReceived = false;

//    private void Start()
//    {
//        gv = FindObjectOfType<GlobalVariables>();
//        tv = GetComponent<TeamVariables>();
//        agentId = tv.team;

//        rewardLogger = new CSVLogger($"RewardLog{agentId}.csv");
//        // Initialize tower and unit tracking.
//        if (tv.team == 1)
//        {
//            enemytv = gv.team2;
//            _towerHealth = tv.health;
//            _unitCount = tv.units;
//            _enemyTowerHealth = gv.team2.health;
//            _enemyUnitCount = gv.team2.units;
//        }
//        else if (tv.team == 2)
//        {
//            enemytv = gv.team1;
//            _towerHealth = tv.health;
//            _unitCount = tv.units;
//            _enemyTowerHealth = gv.team1.health;
//            _enemyUnitCount = gv.team1.units;
//        }
//        // Start the network connection and communication loop using coroutines.
//        StartCoroutine(ConnectAndCommunicateWithServer());
//        // Also start a coroutine to process messages from the queue.
//        StartCoroutine(ProcessIncomingMessages());
//    }

//    IEnumerator ConnectAndCommunicateWithServer()
//    {
//        // Attempt connection
//        try
//        {
//            client = new TcpClient("127.0.0.1", 5000);
//            stream = client.GetStream();
//            isConnected = true;
//            Debug.Log($"Agent {agentId} connected to Python server.");
//            gv.totalAgent++;
//        }
//        catch (Exception e)
//        {
//            Debug.LogError("Connection error: " + e.Message);
//            yield break;
//        }

//        // Wait briefly, then send the initial observation.
//        yield return new WaitForSeconds(0.1f);
//        // Use our protocol coroutine to send a normal observation.
//        StartCoroutine(SendObservationProtocol());

//        // Start a loop that continuously receives data.
//        while (isConnected)
//        {
//            yield return StartCoroutine(ReceiveFromPython());
//            yield return new WaitForSeconds(sendInterval);
//        }
//    }
//    // The protocol for sending observations. This handles both normal and terminal cases.
//    IEnumerator SendObservationProtocol()
//    {
//        // If this is a terminal episode and we haven’t reported it yet:

//        // if episode is terminal = python is making the environment do a reset, ususally because max steps of an episode has been reached
//        bool terminalCondition = episodeIsTerminal || gv.gameOver;


//        if (terminalCondition && !hasReported)
//        {
//            // Terminal message: send done = true.
//            JObject terminalMsg = new JObject
//            {
//                ["agent_id"] = agentId,
//                ["state"] = GetState(),
//                ["reward"] = reward,
//                ["done"] = true
//            };
//            SendToPython(terminalMsg);
//            Debug.Log($"Agent {agentId} sent terminal message.");
//            hasReported = true;
//            // Here, we assume that once all agents have terminated, Unity resets the environment.
//            // Unity must then send a reset acknowledgement (or a new initial state) back.

//            yield return StartCoroutine(WaitForResetAck());



//            // After resetting, send a new initial observation with done = false.
//            JObject initMsg = new JObject
//            {
//                ["agent_id"] = agentId,
//                ["state"] = GetState(),
//                ["reward"] = 0,
//                ["done"] = false
//            };
//            SendToPython(initMsg);
//            Debug.Log($"Agent {agentId} sent new initial state after reset.");

//            episodeIsTerminal = false;  // This is where you reset the flag!
//            hasReported = false;
//        }
//        else if (!episodeIsTerminal)
//        {
//            // Normal observation.
//            JObject normalMsg = new JObject
//            {
//                ["agent_id"] = agentId,
//                ["state"] = GetState(),
//                ["reward"] = reward,
//                ["done"] = false
//            };
//            SendToPython(normalMsg);
//            //Debug.Log($"Agent {agentId} sent normal observation.");
//        }
//        yield return null;
//    }


//    IEnumerator ProcessIncomingMessages()
//    {
//        while (isConnected)
//        {
//            if (receivedMessages.TryDequeue(out string messageString))
//            {
//                try
//                {
//                    Debug.Log(messageString);
//                    JObject message = JObject.Parse(messageString);
//                    // If the message is a reset acknowledgement, handle that.
//                    if (message.ContainsKey("reset_ack") && message["reset_ack"].Value<bool>() == true)
//                    {
//                        Debug.Log($"Agent {agentId} received reset ack.");

//                        resetAckReceived = true;

//                        // After reset ack, send new initial observation.
//                        StartCoroutine(SendObservationProtocol());

//                    }
//                    else
//                    {
//                        // Otherwise, process the message normally.
//                        PlayAction(message);
//                        // After processing, send the next observation.
//                        StartCoroutine(SendObservationProtocol());
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError("Error processing message: " + ex.Message);

//                    Debug.LogError(messageString);
//                }
//            }
//            yield return null;
//        }
//    }

//    IEnumerator WaitForResetAck()
//    {
//        Debug.Log($"Agent {agentId} waiting for reset ack from Python...");
//        // Wait until the local reset ack is received.
//        while (!resetAckReceived)
//        {
//            yield return null;
//        }
//        resetAckReceived = false; // clear local flag

//        // Atomically increment the global terminal counter.
//        //int count = Interlocked.Increment(ref gv.terminalAgentCount);
//        gv.terminalAgentCount++;


//        // If this agent is the last one to report terminal, it will perform the reset.
//        if (gv.terminalAgentCount == gv.totalAgent)
//        {

//            Debug.Log($"Agent {agentId} is the last to report terminal. Performing environment reset.");
//            gv.ResetEnvironment();
//            gv.newEpisodeReady = true;
//            // Reset the terminal counter so that next episode can start fresh.
//            //Interlocked.Exchange(ref gv.terminalAgentCount, 0);
//            gv.terminalAgentCount = 0;
//        }
//        else
//        {
//            Debug.Log($"Agent {agentId} waiting for environment to be reset by another agent.");
//            // Wait until the reset has been performed.
//            while (!gv.newEpisodeReady)
//            {
//                yield return null;
//            }
//            gv.newEpisodeReady = false;
//        }
//        ResetAgentEnv();
//        Debug.Log($"Agent {agentId} proceeding with new episode.");

//        yield return null;


//    }





//    // Coroutine to asynchronously receive messages and enqueue them.
//    IEnumerator ReceiveFromPython()
//    {
//        if (stream == null || !stream.CanRead)
//            yield break;

//        byte[] buffer = new byte[256];
//        int bytesRead = 0;
//        var readAsync = stream.ReadAsync(buffer, 0, buffer.Length);
//        while (!readAsync.IsCompleted)
//            yield return null;
//        try
//        {
//            bytesRead = readAsync.Result;
//        }
//        catch (Exception e)
//        {
//            Debug.LogError("Receive error: " + e.Message);
//            isConnected = false;
//            yield break;
//        }
//        if (bytesRead > 0)
//        {
//            string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);
//            // Enqueue the received message for processing.

//            receivedMessages.Enqueue(msg);
//            // Debug.Log($"Agent {agentId} received: {msg}");
//        }
//    }

//    // Coroutine to process incoming messages from Python.



//    // Process the action message received from Python.
//    public void PlayAction(JObject message)
//    {
//        int action = message["action"].Value<int>();
//        bool renderEpisode = message["render"].Value<bool>();
//        bool maxStepsReached = message["maxStepsReached"].Value<bool>();
//        int step = message["step"].Value<int>();
//        if (message.ContainsKey("maxStepsReached") && maxStepsReached)
//        {
//            Debug.Log($"Agent {agentId}: Max steps reached. Ending episode.");
//            // Set the terminal flag so our protocol sends a terminal observation.
//            episodeIsTerminal = true;
//            gv.gameOver = true;
//        }

//        // Compute rewards and update game state based on the received action.
//        // (This block is your game logic; adjust as needed.)
//        reward = 0;
//        int attackArmyCount = tv.frontLineUnits.Count + tv.rearLineUnits.Count;
//        int enemyAttackArmyCount = enemytv.frontLineUnits.Count + enemytv.rearLineUnits.Count;


//        /* 0 : Do Nothing
////         * 1 : Retreat
////         * 2 : Defend
////         * 3 : Advance
////         * 4 : Spawn Miner
////         * 5 : Spawn Swordsman
////         * 6 : Spawn Archer
////         */

//        switch (action)
//        {
//            case 0: // Do Nothing
//                    // If not enough gold for spawning or already at max units, a little patience is rewarded.
//                if (tv.gold < gv.minerCost || tv.units >= gv.maxUnits)
//                    reward += 1;
//                else
//                    reward -= 1;  // If resources are available and nothing is done, slight penalty.
//                                  // If already defending with some units, a slight positive bonus.
//                if (tv.state == State.Defend && tv.frontLineUnits.Count > 0)
//                    reward += 1;
//                break;
//            case 1: // Retreat
//                if (tv.state == State.Retreat)
//                    reward -= 5; // Penalize repeating the same retreat command.
//                if (tv.units <= 0)
//                    reward -= 5; // If no units to retreat, penalize.
//                if (enemytv.state != State.Advance)
//                    reward -= 5; // Retreat might be less appropriate if the enemy is not advancing.
//                if (attackArmyCount < enemyAttackArmyCount)
//                    reward += 5; // Reward retreat if you are outnumbered.
//                tv.state = State.Retreat;
//                break;
//            case 2: // Defend
//                if (tv.state == State.Defend)
//                    reward -= 5; // Avoid spamming the same command.
//                tv.state = State.Defend;
//                break;
//            case 3: // Advance
//                if (tv.state == State.Advance)
//                    reward -= 5; // Penalize repeated advancing.
//                if (tv.frontLineUnits.Count == 0 && tv.rearLineUnits.Count == 0)
//                    reward -= 5; // Advancing without units is bad.
//                if (enemytv.state == State.Retreat && (attackArmyCount > enemyAttackArmyCount))
//                    reward += 10; // Encourage advancing when you have a numerical advantage.
//                tv.state = State.Advance;
//                break;
//            case 4: // Spawn Miner
//                if (tv.spawn.SpawnUnit(gv.miner))
//                    reward += 2; // Moderate positive reward.
//                else
//                    reward -= 2; // Moderate penalty if failed.
//                break;
//            case 5: // Spawn Swordsman
//                if (tv.spawn.SpawnUnit(gv.swordsman))
//                    reward += 5; // Reward successful spawning.
//                else
//                    reward -= 5;
//                break;
//            case 6: // Spawn Archer
//                if (tv.spawn.SpawnUnit(gv.archer))
//                    reward += 5;
//                else
//                    reward -= 5;
//                break;
//            default:
//                Debug.LogWarning("Unknown action received: " + action);
//                reward -= 5;
//                break;
//        }

//        // Additional reward logic.
//        ApplyRewards(step);

//        Debug.Log($"Agent {agentId}: {reward} given for step :{step}.");
//    }

//    private async void SendToPython(JObject message)
//    {
//        try
//        {
//            string jsonString = message.ToString();
//            byte[] data = Encoding.ASCII.GetBytes(jsonString);
//            await stream.WriteAsync(data, 0, data.Length);
//            // Optionally, log the sent message.
//            //Debug.Log("Sent to Python: " + jsonString);
//        }
//        catch (Exception e)
//        {
//            Debug.LogError("Send error: " + e.Message);
//        }
//    }

//    public JObject GetState()
//    {
//        JObject state = new JObject
//        {
//            ["gold"] = tv.gold,
//            ["health"] = tv.health,
//            ["miners"] = tv.gathererUnits.Count,
//            ["swordsmen"] = tv.frontLineUnits.Count,
//            ["archers"] = tv.rearLineUnits.Count,
//            ["stateValue"] = (int)tv.state,
//            ["nearby_resources_available"] = tv.goldList.Count,
//            ["enemy_health"] = enemytv.health,
//            ["enemy_miners"] = enemytv.gathererUnits.Count / 10.0f,
//            ["enemy_swordsmen"] = enemytv.frontLineUnits.Count,
//            ["enemy_archers"] = enemytv.rearLineUnits.Count,
//            ["enemies_in_vicinity"] = tv.enemiesInVicinity.Count,
//            ["episode_time"] = gv.time
//        };
//        return state;
//    }

//    private void ApplyRewards(int step)
//    {
//        // Log and apply each reward component.
//        // If there are too many gatherers relative to available gold sources.
//        if (tv.gathererUnits.Count > tv.goldList.Count * 2)
//        {
//            float change = -50;
//            reward += change;
//            rewardLogger.Log(step, "Too many gatherers relative to gold sources", change);
//        }

//        // Discourage hoarding too much gold.
//        if (tv.gold > 2000)
//        {
//            float change = -50;
//            reward += change;
//            rewardLogger.Log(step, "Gold > 2000 (hoarding penalty)", change);
//        }
//        else if (tv.gold > 1000)
//        {
//            float change = -20;
//            reward += change;
//            rewardLogger.Log(step, "Gold > 1000", change);
//        }
//        else if (tv.gold < 1000 && tv.gathererUnits.Count > 0)
//        {
//            float change = 2;
//            reward += change;
//            rewardLogger.Log(step, "Gold < 1000 and active gatherers", change);
//        }

//        // Penalize losing tower health (scaled: 1 reward per 10 health lost).
//        if (tv.health < _towerHealth)
//        {
//            float change = (_towerHealth - tv.health) / 10f;
//            reward -= change;
//            rewardLogger.Log(step, "Lost tower health", -change);
//        }

//        // Penalize losing units moderately.
//        if (tv.units < _unitCount)
//        {
//            float change = 5 * (_unitCount - tv.units);
//            reward -= change;
//            rewardLogger.Log(step, "Lost units", -change);
//        }

//        // Reward damaging the enemy tower (scaled).
//        if (enemytv.health < _enemyTowerHealth)
//        {
//            float change = (_enemyTowerHealth - enemytv.health) / 10f;
//            reward += change;
//            rewardLogger.Log(step, "Enemy tower health reduced", change);
//        }

//        // Reward enemy unit losses moderately.
//        if (enemytv.units < _enemyUnitCount)
//        {
//            float change = 2 * (_enemyUnitCount - enemytv.units);
//            reward += change;
//            rewardLogger.Log(step, "Enemy unit loss", change);
//        }

//        // Death penalties and rewards.
//        if (tv.isDead)
//        {
//            float change = 100;
//            reward -= change;
//            rewardLogger.Log(step, "Agent died", -change);
//        }
//        if (enemytv.isDead)
//        {
//            float change = 100;
//            reward += change;
//            rewardLogger.Log(step, "Enemy died", change);
//        }

//        // Encourage balanced force composition.
//        float desiredRatio = 0.5f;
//        float currentRatio = (float)tv.frontLineUnits.Count / (tv.frontLineUnits.Count + tv.rearLineUnits.Count + 1);
//        if (Mathf.Abs(currentRatio - desiredRatio) < 0.1f)
//        {
//            float change = 5;
//            reward += change;
//            rewardLogger.Log(step, "Balanced forces bonus", change);
//        }
//        else
//        {
//            float change = 5;
//            reward -= change;
//            rewardLogger.Log(step, "Imbalanced forces penalty", -change);
//        }

//        // Update baseline values for next step.
//        _towerHealth = tv.health;
//        _unitCount = tv.units;
//        _enemyTowerHealth = enemytv.health;
//        _enemyUnitCount = enemytv.units;
//    }


//    private void ApplyRewards()
//    {
//        // If there are too many gatherers relative to available gold sources, penalize moderately.
//        if (tv.gathererUnits.Count > tv.goldList.Count * 2 || tv.gathererUnits.Count < 0)
//            reward -= 50;

//        // Discourage hoarding too much gold.
//        if (tv.gold > 2000)
//            reward -= 50;
//        else if (tv.gold > 1000)
//            reward -= 20;
//        else if (tv.gold < 1000 && tv.gathererUnits.Count > 0)
//            reward += 2;

//        // Penalize losing tower health but scale down damage (e.g. subtract 1 reward unit per 10 health lost).
//        if (tv.health < _towerHealth)
//            reward -= (_towerHealth - tv.health) / 10;

//        // Penalize losing units moderately.
//        if (tv.units < _unitCount)
//            reward -= 5 * (_unitCount - tv.units);

//        // Reward damaging the enemy tower (scaled).
//        if (enemytv.health < _enemyTowerHealth)
//            reward += (_enemyTowerHealth - enemytv.health) / 10;

//        // Reward enemy unit losses moderately.
//        if (enemytv.units < _enemyUnitCount)
//            reward += 2 * (_enemyUnitCount - enemytv.units);

//        // Death penalties and rewards:
//        if (tv.isDead)
//            reward -= 100;
//        if (enemytv.isDead)
//            reward += 100;

//        float desiredRatio = 0.5f; // say you want 50% melee, 50% ranged, etc.
//        float currentRatio = (float)tv.frontLineUnits.Count / (tv.frontLineUnits.Count + tv.rearLineUnits.Count + 1);
//        if (Mathf.Abs(currentRatio - desiredRatio) < 0.1f)
//        {
//            reward += 5; // small bonus for balanced forces.
//        }
//        else
//        {
//            reward -= 5; // penalize if it's too skewed.
//        }

//        // Update baseline values for next step.
//        _towerHealth = tv.health;
//        _unitCount = tv.units;
//        _enemyTowerHealth = enemytv.health;
//        _enemyUnitCount = enemytv.units;

//    }
//    private void ResetAgentEnv()
//    {
//        if (tv.team == 1)
//        {
//            enemytv = gv.team2;
//            _towerHealth = tv.health;
//            _unitCount = tv.units;
//            _enemyTowerHealth = gv.team2.health;
//            _enemyUnitCount = gv.team2.units;
//        }
//        else if (tv.team == 2)
//        {
//            enemytv = gv.team1;
//            _towerHealth = tv.health;
//            _unitCount = tv.units;
//            _enemyTowerHealth = gv.team1.health;
//            _enemyUnitCount = gv.team1.units;
//        }
//    }

//    void OnApplicationQuit()
//    {
//        if (stream != null) stream.Close();
//        if (client != null) client.Close();
//        rewardLogger.Close();
//    }
//}
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
    public TeamVariables tv;
    public TeamVariables enemytv;
    public GlobalVariables gv;

    public float reward;

    private int _towerHealth;
    private int _enemyTowerHealth;
    private int _unitCount;
    private int _enemyUnitCount;

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
                break;
            case 1: // Retreat
                if (tv.state == State.Retreat)
                    reward -= 5; // Penalize repeating the same retreat command.
                if (tv.units <= 0)
                    reward -= 5; // If no units to retreat, penalize.
                if (enemytv.state != State.Advance)
                    reward -= 5; // Retreat might be less appropriate if the enemy is not advancing.
                if (attackArmyCount < enemyAttackArmyCount)
                    reward += 5; // Reward retreat if you are outnumbered.
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
                tv.state = State.Advance;
                break;
            case 4: // Spawn Miner
                if (tv.spawn.SpawnUnit(gv.miner))
                    reward += 2; // Moderate positive reward.
                else
                    reward -= 2; // Moderate penalty if failed.
                break;
            case 5: // Spawn Swordsman
                if (tv.spawn.SpawnUnit(gv.swordsman))
                    reward += 5; // Reward successful spawning.
                else
                    reward -= 5;
                break;
            case 6: // Spawn Archer
                if (tv.spawn.SpawnUnit(gv.archer))
                    reward += 5;
                else
                    reward -= 5;
                break;
            default:
                Debug.LogWarning("Unknown action received: " + action);
                reward -= 5;
                break;
        }

        // Additional reward logic.
        ApplyRewards(step);

        Debug.Log($"Agent {agentId}: {reward} given for step :{step}.");
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

    private void ApplyRewards(int step)
    {
        // Log and apply each reward component.
        // If there are too many gatherers relative to available gold sources.
        if (tv.gathererUnits.Count > tv.goldList.Count * 2)
        {
            float change = -50;
            reward += change;
            rewardLogger.Log(step, "Too many gatherers relative to gold sources", change);
        }

        // Discourage hoarding too much gold.
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

        // Penalize losing tower health (scaled: 1 reward per 10 health lost).
        if (tv.health < _towerHealth)
        {
            float change = (_towerHealth - tv.health) / 10f;
            reward -= change;
            rewardLogger.Log(step, "Lost tower health", -change);
        }

        // Penalize losing units moderately.
        if (tv.units < _unitCount)
        {
            float change = 5 * (_unitCount - tv.units);
            reward -= change;
            rewardLogger.Log(step, "Lost units", -change);
        }

        // Reward damaging the enemy tower (scaled).
        if (enemytv.health < _enemyTowerHealth)
        {
            float change = (_enemyTowerHealth - enemytv.health) / 10f;
            reward += change;
            rewardLogger.Log(step, "Enemy tower health reduced", change);
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
            float change = 100;
            reward -= change;
            rewardLogger.Log(step, "Agent died", -change);
        }
        if (enemytv.isDead)
        {
            float change = 100;
            reward += change;
            rewardLogger.Log(step, "Enemy died", change);
        }

        // Encourage balanced force composition.
        float desiredRatio = 0.5f;
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
    }

    //void OnApplicationQuit()
    //{
    //    if (stream != null) stream.Close();
    //    if (client != null) client.Close();
    //    rewardLogger.Close();
    //}
}
