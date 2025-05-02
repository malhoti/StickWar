using Newtonsoft.Json.Linq;
using System.Text;
using UnityEngine;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections;


public class PythonConnectionHardcodedAI : MonoBehaviour
{
    public static PythonConnectionHardcodedAI Instance { get; private set; }

    private GlobalVariables gv;

   
    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;
    private string serverIP = "127.0.0.1";
    private int serverPort = 5000;

    
    

    // Registered agents by their agentId
    public Dictionary<int, TeamVariables> teams = new Dictionary<int, TeamVariables>();

    void Start()
    {
        gv = GetComponent<GlobalVariables>();
        // add agentst to the dictionary
        foreach (TeamVariables team in FindObjectsOfType<TeamVariables>())
        {
            if (!teams.ContainsKey(team.team))
            {
                if (team.enabled) teams.Add(team.team, team);

            }
            else
            {
                Debug.LogWarning($"Duplicate agentId {team.team} found. Skipping duplicate.");
            }
        }

        if (!(teams.Count == 0))
            StartCoroutine(ConnectToServer());
    
    }

    void Update()
    {
        // When the game ends, send the match result once
        if (gv.gameOver)
        {
            SendMatchResult();
            gv.ResetEnvironment();
        }
    }

    private void SendMatchResult()
    {
        // Compute result
        bool team1Dead = gv.team1.isDead;
        bool team2Dead = gv.team2.isDead;
        int result;
        if (!team1Dead && !team2Dead)
        {
            result = 2; // draw
        }
        else if (gv.team1)
        {
            result = team2Dead ? 0 : 1;
        }
        else // team 2
        {
            result = team1Dead ? 0 : 1;
        }

        // Build JSON
        var msg = new JObject
        {
            ["type"] = "match_result",
            ["agent1"] = "HARDAI",   // or gv.myTeamId.ToString()
            ["agent2"] = "HUMAN",   // or the opponent’s identifier
            ["result"] = result
        };
        string json = msg.ToString(Newtonsoft.Json.Formatting.None) + "\n";
        print(json);
        // Send over TCP
        if (stream != null && stream.CanWrite)
        {
            byte[] data = Encoding.ASCII.GetBytes(json);
            try { stream.Write(data, 0, data.Length); }
            catch (System.Exception e)
            { Debug.LogError("SendMatchResult error: " + e); }
        }
    }

    IEnumerator ConnectToServer()
    {
        client = new TcpClient(serverIP, serverPort);
        stream = client.GetStream();
        yield return null;
    }
}
