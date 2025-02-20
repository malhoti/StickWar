using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEditor.VersionControl;

public class PythonConnection : MonoBehaviour
{
    public static PythonConnection Instance { get; private set; }

    private GlobalVariables gv;

    // TCP connection info
    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;
    private string serverIP = "127.0.0.1";
    private int serverPort = 5000;

    // Frequency (in seconds) for sending aggregated observations
    public float sendInterval = 0.5f;
    
    // Registered agents by their agentId
    public Dictionary<int, RLAgent> agents = new Dictionary<int, RLAgent>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    void Start()
    {
        gv = GetComponent<GlobalVariables>();
        // add agentst to the dictionary
        foreach (RLAgent agent in FindObjectsOfType<RLAgent>())
        {
            if (!agents.ContainsKey(agent.agentId))
            {
                agents.Add(agent.agentId, agent);
            }
            else
            {
                Debug.LogWarning($"Duplicate agentId {agent.agentId} found. Skipping duplicate.");
            }
        }

        StartCoroutine(ConnectToServer());
        
    }


    IEnumerator ConnectToServer()
    {
        
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connected to Python server.");
        }
        catch (Exception e)
        {
            Debug.LogError("CentralizedCommManager: Connection error: " + e.Message);
        }
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(SendAggregatedObservations());
        StartCoroutine(ReceiveFromServer());

    }

    IEnumerator SendAggregatedObservations()
    {
        
    
        yield return new WaitForSeconds(sendInterval);

        // Build the aggregated JSON message
        JObject aggregatedMsg = new JObject();
        aggregatedMsg["type"] = "state";
        JObject agentsObj = new JObject();

        foreach (var kvp in agents)
        {
            int id = kvp.Key;
            RLAgent agent = kvp.Value;
            JObject agentObs = new JObject
            {
                ["state"] = agent.GetState(),        // Agent's observation (as a JObject)
                ["reward"] = agent.reward,             // Current reward
                ["done"] = agent.gv.gameOver,    // Terminal flag
                             
            };
            // Use the agent ID (as a string) as key
            agentsObj[id.ToString()] = agentObs;
        }
        aggregatedMsg["agents"] = agentsObj;

        string jsonStr = aggregatedMsg.ToString(Newtonsoft.Json.Formatting.None);
        byte[] data = Encoding.ASCII.GetBytes(jsonStr + "\n"); // newline-delimited
        Debug.Log($"Sending: { jsonStr}");
        try
        {
            if (stream != null && stream.CanWrite)
            {
                stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("SendAggregatedObservations error: " + e.Message);
        }   
    }

    IEnumerator ReceiveFromServer()
    {


        byte[] buffer = new byte[1024];
        StringBuilder sb = new StringBuilder();
        while (isConnected)
        {
            if (stream != null && stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    string content = sb.ToString();
                    // Check if a full message (newline terminated) is available
                    if (content.Contains("\n"))
                    {
                        Debug.Log($"Recieved: {content}");
                        string[] messages = content.Split('\n');
                        // Process all complete messages except the last incomplete fragment
                        for (int i = 0; i < messages.Length - 1; i++)
                        {
                            ProcessServerMessage(messages[i]);
                        }
                        sb.Clear();
                        sb.Append(messages[messages.Length - 1]);
                    }
                }
            }
            yield return null;
        }
    }

    void ProcessServerMessage(string message)
    {
        
        try
        {
            JObject response = JObject.Parse(message);
            // Expected response format:
            // { "type": "action", "agents": { "1": { "action": <int>, "render": <bool>, ... }, ... } }
            if (response["type"]?.ToString() == "action")
            {
                JObject agentsActions = (JObject)response["agents"];
                foreach (var pair in agentsActions)
                {
                    
                    int id = int.Parse(pair.Key);
                    JObject actionData = (JObject)pair.Value;
                    if (actionData.ContainsKey("reset_ack") && actionData["reset_ack"].Value<bool>() == true)
                    {
                        foreach (RLAgent agent in agents.Values)
                        {
                            agent.ResetAgentEnv();
                        }
                        gv.ResetEnvironment(); // call your reset function
                        break; // exit the loop since a reset ack has been processed
                    }
                    else
                    {
                        int action = actionData["action"].Value<int>();
                        bool maxStepsReached = actionData["maxStepsReached"].Value<bool>();
                        int step = actionData["step"].Value<int>();
                        // Optionally extract other fields such as "render", "step", etc.
                        if (agents.ContainsKey(id))
                        {
                            agents[id].PlayAction(action, step, maxStepsReached);
                        }
                    }
                }
                StartCoroutine(SendAggregatedObservations());
            }
        }
        catch (Exception e)
        {
            Debug.LogError("ProcessServerMessage error: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
        isConnected = false;
    }
}

