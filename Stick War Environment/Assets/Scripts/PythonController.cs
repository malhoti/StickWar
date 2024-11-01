using UnityEngine;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

public class PythonController : MonoBehaviour
{
    public static PythonController Instance;

    private TcpClient client;
    public NetworkStream stream;
    private ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();

    public List<RLAgent> rlagents= new List<RLAgent>();

    public bool serverRequirement = true;

    private float timer = 0;
    private float sendInterval = 0.1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep the instance across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    void Start()
    {
        if (serverRequirement)
        {
            rlagents = new List<RLAgent>(FindObjectsOfType<RLAgent>());
            ConnectToServer();

            // Start receiving messages from Python
            ReceiveFromPython();

        }
            
    }
    

    


    void Update()
    {
        
        if (serverRequirement)
        {
            timer += Time.deltaTime;
            if (timer > sendInterval)
            {
                while (receivedMessages.TryDequeue(out string message))
                {
                    JObject response = JObject.Parse(message);

                   

                    int agentId = (int)response["agent_id"];

                    Debug.Log($"this is agent id:{agentId}");

                    RLAgent targetAgent = rlagents.FirstOrDefault(a => a.agentId == agentId);
                    if (targetAgent != null) {
                        
                        targetAgent.PlayAction(response);
                    }
                    //Debug.Log($"Processed message from {client.Client.RemoteEndPoint}: {response["message"]}");
                    
                }
                timer = 0;
            }
        }

    }


    void ConnectToServer()
    {
        try
        {
            client = new TcpClient("192.168.1.206", 5000);  // Connect to the Python server
            stream = client.GetStream();

            
            Debug.Log($"Connected to Python server at {client.Client.RemoteEndPoint}.");
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
        }
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
            Debug.Log("Sent to Python: " + jsonString);
        }
        catch (Exception e)
        {
            Debug.LogError("Send error: " + e.Message);
        }
    }

    async void ReceiveFromPython()
    {
        byte[] data = new byte[256];
        
        while (stream != null && stream.CanRead)
        {
            try
            {

                int bytes = await stream.ReadAsync(data, 0, data.Length);
                string response = Encoding.ASCII.GetString(data, 0, bytes);
                Debug.Log($"Messaged sent by Python: {response}");
                receivedMessages.Enqueue(response);

              
            }
            catch (Exception e)
            {
                Debug.LogError("JSON Parsing error: " + e.Message);
                break;
            }
        }
        


    }

    void OnApplicationQuit()
    {
        stream.Close();
        client.Close();
    }
}
