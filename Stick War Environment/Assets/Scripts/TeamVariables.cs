using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State
{
    Retreat = 0,
    Defend = 1,
    Advance = 2
}

public class TeamVariables : MonoBehaviour

{
    public GlobalVariables gv;

    [Header("Team Number")]
    public int team;

    [Header("")]
    public Transform retreatPos;
    public Transform unloadPos;
    public Transform defendPos;
    public Transform towerArcherPos;
    public Transform defendMaxPos;
    public ArmySpawn spawn;


    [Header("Variables")]
    public int gold;
    public int health;
    public int units;
    public List<Gold> goldList;
    public List<GameObject> gathererUnits;
    public List<GameObject> frontLineUnits;
    public List<GameObject> rearLineUnits;
    public List<Unit> enemiesInVicinity;
    public State state;
    public bool isDead;

    // Start is called before the first frame update
    void Awake()
    {
        team =  (gameObject.name == "Team1") ? 1 : 2;
        
        state = State.Defend;
        isDead = false;
        StartCoroutine(PassiveGoldCoroutine());      
    }

    private void Start()
    {
        gv = FindObjectOfType<GlobalVariables>();   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
   

    IEnumerator PassiveGoldCoroutine()
    {
        while (true) // Infinite loop
        {
            yield return new WaitForSeconds(3f);
            gold += gv.passiveGoldRate;
            
        }
    }
}
