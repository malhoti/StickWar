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
    // Start is called before the first frame update
    void Start()
    {
        tv = gameObject.GetComponent<TeamVariables>();
        agentId = tv.team;
        StartCoroutine(WaitForPythonControllerAndSend());
    }

    IEnumerator WaitForPythonControllerAndSend()
    {
        while (PythonController.Instance.stream == null)
        {
            Debug.Log("Waiting for PythonController instance to become ready...");
            yield return null; // Wait for the next frame
        }

        // Send the initial message
        PrepareResponse();
    }
   

    public void ProcessMessage(string message)
    {
        JObject response = JObject.Parse(message);
        // we will process the message and action to take here
        Debug.Log("messaged recieved by agent, and has been processed");
        PrepareResponse();
        
    }

    void PrepareResponse()
    {
        if (PythonController.Instance == null)
        {
            Debug.LogWarning("PythonController instance is not ready yet.");
            return;
        }
        JObject message = new JObject
        {
            ["agentId"] = agentId,
            ["gold"] = tv.gold,
            ["health"] = tv.health,
            ["miners"] = tv.gathererUnits.Count,
            ["swordsmen"] = tv.frontLineUnits.Count,
            ["archers"] = tv.rearLineUnits.Count,
            ["state"] = (int)tv.state,
            ["enemies_in_vicinity"] = tv.enemiesInVicinity.Count
        };
        
        PythonController.Instance.SendToPython(message);

        
    }
}
