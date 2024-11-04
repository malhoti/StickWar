using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ArmySpawn : MonoBehaviour
{
    
    public GlobalVariables gv;
    public TeamVariables tv;
    public int maxUnits = 20;

    
    public float horizontal;
    public float vertical;

    [Header("Spawn Cooldown")]
    public float spawnCooldown; // Cooldown time in seconds for spawning
    private float spawnCooldownTimer = 0.0f; // Timer to track cooldown
    private bool onCooldown = false; // Flag to indicate if you are on cooldown for spamming spawning units

    [Header("Debug")]
    public GameObject armySoldier;
    public bool testOutSpawn;
    // Start is called before the first frame update




    void Start()
    {
        tv = GetComponentInParent<TeamVariables>();
        spawnCooldown = gv.spawnCooldown;
        if(testOutSpawn)
        testSpawnUnit();
    }

    // Update is called once per frame
    void Update()
    {
        if (onCooldown)
        {
            spawnCooldownTimer -= Time.deltaTime;
            if (spawnCooldownTimer <= 0)
            {
                onCooldown = false;
                spawnCooldownTimer = 0;
            }
        }
    }

    

    public bool SpawnUnit(GameObject unit)
    {
        if (CanSpawn(unit))
        {
            float halfHeight = vertical / 2;
            float halfWidth = horizontal / 2;
            
            Vector2 spawnPosition = new Vector2(
                UnityEngine.Random.Range(transform.position.x - halfWidth, transform.position.x + halfWidth),
                UnityEngine.Random.Range(transform.position.y - halfHeight, transform.position.y + halfHeight));

            GameObject spawnedUnit = Instantiate(unit, spawnPosition, Quaternion.identity, tv.transform);
            tv.units++;

            if (unit.GetComponent<Miner>() != null)
            {
                tv.gathererUnits.Add(spawnedUnit);
            }
            if (unit.GetComponent<Swordsman>() != null)
            {
                tv.frontLineUnits.Add(spawnedUnit);
            }
            if (unit.GetComponent<Archer>() != null)
            {
                tv.rearLineUnits.Add(spawnedUnit);
            }

            onCooldown = true;
            spawnCooldownTimer = spawnCooldown;

            return true;
        }
        else
        {
            //Debug.Log("youre too broke");
            return false;
        }

    }
    bool CanSpawn(GameObject unit) 
    {
        //Debug.Log($"tv: {tv}, gv: {gv}, onCooldown: {onCooldown}, team {tv.team}");
        if (tv.units >= gv.maxUnits || onCooldown)
        {
            return false;
        }
        //Debug.Log((gv.swordsmanCost, tv.gold));
        if (unit == gv.miner )
        {
            if (tv.gold >= gv.minerCost)
            {
                tv.gold -= gv.minerCost;
                return true;
            }
                
        }
        if (unit == gv.swordsman)
        {
            if (tv.gold >= gv.swordsmanCost)
            {
                tv.gold -= gv.swordsmanCost;
                return true;
            }
        }
        if (unit == gv.archer)
        {
            if (tv.gold >= gv.archerCost)
            {
                tv.gold -= gv.archerCost;
                return true;
            } 
        }
        return false;
    }

    
    // debugging
    void testSpawnUnit()
    {
        float halfHeight = vertical / 2;
        float halfWidth = horizontal / 2;
        int currentUnits = 0;
        while (currentUnits < maxUnits)
        {
            Vector2 spawnPosition = new Vector2(
                UnityEngine.Random.Range(transform.position.x - halfWidth, transform.position.x + halfWidth),
                UnityEngine.Random.Range(transform.position.y - halfHeight, transform.position.y + halfHeight));

            GameObject spawnedUnit = Instantiate(armySoldier, spawnPosition, Quaternion.identity, tv.transform);
            currentUnits++;
            tv.frontLineUnits.Add(spawnedUnit);
        }
    }

    // Testing to see the spawn area
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(horizontal, vertical));
    }
}
