using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class GoldSpawn : MonoBehaviour
{
    public TeamVariables tv;
    public GameObject goldPrefab;
    public int maxGoldOres;
    public float horizontal;
    public float vertical;

    public int currentGoldOres = 0;
    // Start is called before the first frame update
    void Awake()
    {
        tv = GetComponentInParent<TeamVariables>();
        
        
    }

    private void Start()
    {
        maxGoldOres = tv.gv.maxGoldOres;
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnGoldOres()
    {
        float halfHeight = vertical / 2;
        float halfWidth = horizontal / 2;

        
        while (currentGoldOres < maxGoldOres)
        {
            Vector2 spawnPosition = new Vector2(
                Random.Range(transform.position.x - halfWidth, transform.position.x + halfWidth),
                Random.Range(transform.position.y - halfHeight, transform.position.y + halfHeight));
  
            GameObject gold = Instantiate(goldPrefab, spawnPosition, Quaternion.identity,transform);
            currentGoldOres++;

            tv.goldList.Add(gold.GetComponent<Gold>());
        }

    }

    // Testing to see the spawn area
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(horizontal, vertical));
    }
}
