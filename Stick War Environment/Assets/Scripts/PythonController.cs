using UnityEngine;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

public class PythonController : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;

    private float timer = 0;
    private float sendInterval = 1;

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > sendInterval)
        {
            SendToPython("Im from Unity");

            // Receive the action and pass it to AgentBehavior
            string action = ReceiveFromPython();
            timer = 0;
        }

    }


    void ConnectToServer()
    {
        try
        {
            client = new TcpClient("192.168.1.206", 5000);  // Connect to the Python server
            stream = client.GetStream();

            // Retrieve the remote endpoint (server address) and log it
            Debug.Log($"Connected to Python server at {client.Client.RemoteEndPoint}.");
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
        }
    }


    void SendToPython(string message)
    {
        try
        {
            // Create a JSON object
            JObject jsonMessage = new JObject
            {
                ["message"] = message
            };

            // Convert JSON object to string
            string jsonString = jsonMessage.ToString();

            // Send JSON string to Python
            byte[] data = Encoding.ASCII.GetBytes(jsonString);
            stream.Write(data, 0, data.Length);
            //Debug.Log("Sent to Python: " + jsonString);
        }
        catch (Exception e)
        {
            Debug.LogError("Send error: " + e.Message);
        }
    }

    string ReceiveFromPython()
    {
        byte[] data = new byte[256];
        int bytes = stream.Read(data, 0, data.Length);
        try
        {
            JObject response = JObject.Parse(Encoding.ASCII.GetString(data, 0, bytes));
            Debug.Log($"Messaged recieved from {client.Client.RemoteEndPoint}: {response["message"]}");
            return response.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError("JSON Parsing error: " + e.Message);
        }
        return null;

    }

    void OnApplicationQuit()
    {
        stream.Close();
        client.Close();
    }
}
