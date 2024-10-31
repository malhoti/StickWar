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

    private int reward;
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
   

    public void ProcessMessage(JObject message)
    {
        int action = message["action"].Value<int>();

        switch (action)
        {
            case 1:
                tv.state = State.Retreat;
                break;
            case 2:
                tv.state = State.Defend;
                break;
            case 3:
                tv.state = State.Advance;
                break;
            default:
                Debug.LogWarning("Unknown action received: " + action);
                break;
        }
        // we will process the message and action to take here


        Debug.Log("messaged recieved by agent, and has been processed");
        PrepareResponse();
        
    }

    void CalculateReward()
    {

    }

    void PrepareResponse()
    {

        JObject message = new JObject
        {
            ["agentId"] = agentId,
            ["state"] = new JObject
            {
                ["gold"] = tv.gold,
                ["health"] = tv.health,
                ["miners"] = tv.gathererUnits.Count,
                ["swordsmen"] = tv.frontLineUnits.Count,
                ["archers"] = tv.rearLineUnits.Count,
                ["stateValue"] = (int)tv.state,
                ["enemies_in_vicinity"] = tv.enemiesInVicinity.Count
            },
            ["reward"] = reward,
            ["done"] = tv.isDead
        };

        PythonController.Instance.SendToPython(message);  
    }
}
