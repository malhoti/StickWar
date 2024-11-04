using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gold : MonoBehaviour

{
    public int capacity = 20;
    public float capacityVariance;
    public int miningSpotsUsed = 0; //max 2
    public int maxMiningSpots = 2;

    public Transform spot1;
    public Transform spot2;

    public TeamVariables tv;
    private GoldSpawn goldSpawn;

    public bool spot1Available;
    public bool spot2Available;

    // Start is called before the first frame update
    void Awake()
    {
        tv = GetComponentInParent<TeamVariables>();
        goldSpawn = GetComponentInParent<GoldSpawn>();
        spot1 = transform.Find("MiningSpot1");
        spot2 = transform.Find("MiningSpot2");

        spot1Available = true;
        spot2Available = true;
        Variation();
        
    }
    public void Variation()
    { 
        capacity = Mathf.RoundToInt(capacity * Random.Range(1-(capacityVariance/100),1+ (capacityVariance / 100)));
    }

    // Update is called once per frame
    void Update()
    {
        if (capacity <= 0) {
            tv.goldList.Remove(this);
            Destroy();
        }
    }

    public void Destroy()
    {
        
        
        goldSpawn.currentGoldOres--;
        Destroy(gameObject);
    }
}
