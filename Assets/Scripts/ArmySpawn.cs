using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmySpawn : MonoBehaviour
{
    public GameObject armySoldier;
    public GlobalVariables gv;
    public TeamVariables tv;
    public int maxUnits = 20;

    
    public float horizontal;
    public float vertical;

    public bool testOutSpawn;
    // Start is called before the first frame update




    void Start()
    {
        tv = GetComponentInParent<TeamVariables>();
        if(testOutSpawn)
        testSpawnUnit();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void testSpawnUnit()
    {
        float halfHeight = vertical / 2;
        float halfWidth = horizontal / 2;
        int currentUnits = 0;
        while (currentUnits < maxUnits)
        {
            Vector2 spawnPosition = new Vector2(
                Random.Range(transform.position.x - halfWidth, transform.position.x + halfWidth),
                Random.Range(transform.position.y - halfHeight, transform.position.y + halfHeight));

            GameObject spawnedUnit = Instantiate(armySoldier, spawnPosition, Quaternion.identity, tv.transform);
            currentUnits++;
            tv.frontLineUnits.Add(spawnedUnit);
        }
    }

    public void SpawnUnit(GameObject unit)
    {
        float halfHeight = vertical / 2;
        float halfWidth = horizontal / 2;
        if (tv.units < gv.maxUnits)
        {
            Vector2 spawnPosition = new Vector2(
                Random.Range(transform.position.x - halfWidth, transform.position.x + halfWidth),
                Random.Range(transform.position.y - halfHeight, transform.position.y + halfHeight));

            GameObject spawnedUnit = Instantiate(unit, spawnPosition, Quaternion.identity,tv.transform);
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


        }

    }

    // Testing to see the spawn area
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(horizontal, vertical));
    }
}
