using System;
using System.Net.Sockets;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public class RLAgent : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private float timeSinceLastAction = 0f;
    public float actionInterval = 0.5f;  // Interval between actions

    void Start()
    {
        client = new TcpClient("127.0.0.1", 5000);  // Replace with the correct IP and port
        stream = client.GetStream();
    }

    void Update()
    {
        timeSinceLastAction += Time.deltaTime;

        if (timeSinceLastAction >= actionInterval)
        {
            string observations = CollectObservations();
            float reward = CalculateReward();
            SendToPython(observations, reward);
            string action = ReceiveFromPython();
            ApplyAction(action);

            timeSinceLastAction = 0f;
        }
    }

    private string CollectObservations()
    {
        int gold = 100;  // Example observation
        int baseHealth = 100;  // Example observation
        return $"{{\"gold\": {gold}, \"baseHealth\": {baseHealth}}}";
    }

    private float CalculateReward()
    {
        float reward = 0f;
        if (successfullyDefendedBase) reward += 1.0f;
        if (goldCollected > 0) reward += 0.1f * goldCollected;
        if (unitLost) reward -= 0.5f;
        if (baseHealth < startingBaseHealth) reward -= 1.0f;
        return reward;
    }

    private void SendToPython(string observations, float reward)
    {
        string message = $"{{\"observations\": {observations}, \"reward\": {reward}}}";
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    private string ReceiveFromPython()
    {
        byte[] data = new byte[256];
        int bytes = stream.Read(data, 0, data.Length);
        return Encoding.ASCII.GetString(data, 0, bytes);
    }

    private void ApplyAction(string action)
    {
        // Parse and apply actions like spawning units or changing army state
    }

    void OnApplicationQuit()
    {
        stream.Close();
        client.Close();
    }
}

