using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    public Camera Camera;
    public GlobalVariables gv;

    public TeamVariables tv1;
    public TeamVariables tv2;

    public TMP_Text score1;
    public TMP_Text score2;

    public TMP_Text time;

    public Button retreat1;
    public Button retreat2;

    public Button defend1;
    public Button defend2;

    public Button attack1;
    public Button attack2;

    public Button miner1;
    public Button miner2;

    public Button swordsman1;
    public Button swordsman2;

    public Button archer1;
    public Button archer2;

    public Button moveToTeam1;
    public Button moveToTeam2;
    // Start is called before the first frame update
    void Start()
    {
        retreat1.onClick.AddListener(Retreat1Pressed);
        retreat2.onClick.AddListener(Retreat2Pressed);

        defend1.onClick.AddListener(Defend1Pressed);
        defend2.onClick.AddListener(Defend2Pressed);

        attack1.onClick.AddListener(Attack1Pressed);
        attack2.onClick.AddListener(Attack2Pressed);

        miner1.onClick.AddListener(Miner1Pressed);
        miner2.onClick.AddListener(Miner2Pressed);

        swordsman1.onClick.AddListener(Swordsman1Pressed);  
        swordsman2.onClick.AddListener(Swordsman2Pressed);

        archer1.onClick.AddListener(Archer1Pressed);
        archer2.onClick.AddListener(Archer2Pressed);

        moveToTeam1.onClick.AddListener(MoveToTeam1Pressed);
        moveToTeam2.onClick.AddListener(MoveToTeam2Pressed);

    }

    // Update is called once per frame
    void Update()
    {
        score1.text = ("Gold : "+ tv1.gold.ToString());
        score2.text = ("Gold : " + tv2.gold.ToString());


        int minutes = Mathf.FloorToInt(gv.time / 60f);
        int seconds = Mathf.FloorToInt(gv.time % 60f);
        time.text = $"Time: {minutes:0}:{seconds:00}";

    }
    void Retreat1Pressed()
    {
        tv1.state = State.Retreat;
    }
    void Retreat2Pressed()
    {
        tv2.state = State.Retreat;
    }
    void Defend1Pressed()
    {
        tv1.state = State.Defend;
    }
    
    void Defend2Pressed()
    {
        tv2.state = State.Defend;
    }

    void Attack1Pressed()
    {
        tv1.state = State.Advance;
    }

    void Attack2Pressed()
    {
        tv2.state = State.Advance;
    }
    void Miner1Pressed()
    {
       
        
        tv1.spawn.SpawnUnit(gv.miner);
        
        
    }
    void Miner2Pressed()
    {

        tv2.spawn.SpawnUnit(gv.miner);

    }
    void Swordsman1Pressed()
    {
        tv1.spawn.SpawnUnit(gv.swordsman);
        
    }
    
    void Swordsman2Pressed()
    {
        tv2.spawn.SpawnUnit(gv.swordsman);

    }

    void Archer1Pressed()
    {
        tv1.spawn.SpawnUnit(gv.archer);
    }

    void Archer2Pressed()
    {
        tv2.spawn.SpawnUnit(gv.archer);

    }

    void MoveToTeam1Pressed()
    {
        Camera.transform.position = new Vector3 (tv1.transform.position.x, Camera.transform.position.y, Camera.transform.position.z);

    }
    void MoveToTeam2Pressed()
    {
        Camera.transform.position = new Vector3(tv2.transform.position.x, Camera.transform.position.y, Camera.transform.position.z);

    }

}
