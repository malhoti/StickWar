using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State
{
    Retreat,
    Defend,
    Advance
}

public class TeamVariables : MonoBehaviour
{
    [Header("Team Number")]
    public int team;

    [Header("")]
    public Transform retreatPos;
    public Transform unloadPos;
    public Transform defendPos;
    public Transform towerArcherPos;
    public int gold;

    public ArmySpawn spawn;

    public int units;

    public List<Gold> goldList;
    public List<GameObject> gathererUnits;
    public List<GameObject> frontLineUnits;
    
    public List<GameObject> rearLineUnits;

    

    public State state;
    // Start is called before the first frame update
    void Start()
    {
        team =  (gameObject.name == "Team1") ? 1 : 2;
        gold = 0;
        state = State.Defend;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
