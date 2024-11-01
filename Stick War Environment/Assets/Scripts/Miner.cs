using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting.Antlr3.Runtime.Tree;
//using UnityEditor.Experimental.GraphView;
//using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//using static UnityEditor.PlayerSettings;

public class Miner : Unit
{
    [Header("Unit Specific Attributes")]

    public StorageBar storageBar;

    [Header("")]

    public int maxStorage;
    public int storage;

    [Header("Debugging")]
    public Gold targetGold;
    public int miningPosition;   
    public bool isMining;
    public bool isUnloading;
    

    // Start is called before the first frame update
     public  void Awake()
    {
        Variation();
        isMining = false;
        isUnloading = false;
        flip = false;
        gv = FindObjectOfType<GlobalVariables>().GetComponent<GlobalVariables>();
        tv = GetComponentInParent<TeamVariables>();
        rb = GetComponent<Rigidbody2D>();
        storageBar = GetComponentInChildren<StorageBar>();

        anim = GetComponentInChildren<Animator>();
        FindNextTask();
        anim.Play("Walk");
        //targetLocation = transform.position;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        if (!alive) return;
        switch (tv.state)
        {
            case State.Retreat:
                Retreat();break;
            case State.Defend:
                Gather(); break;
            case State.Advance:
                Gather(); break;
        }
        
        storageBar.UpdateStorageBar(storage, maxStorage, flip);
    }

    



    public void Variation()
    {
        float sizeVariation = Random.Range(-0.05f, 0.05f);
        float speedVariation = Random.Range(-0.1f, 0.1f);
        transform.localScale = new Vector2(transform.localScale.x + sizeVariation, transform.localScale.y + sizeVariation);
        moveSpeed += speedVariation;
        anim.speed = Random.Range(0.95f, 1.05f);
    }

    /// <summary>
    /// It will locate a task for the Miner to go to
    /// </summary>
    public void FindNextTask()
    {
        List<Gold> goldOres = tv.goldList;
        float closestDistance = Mathf.Infinity;
        targetGold = null;
        miningPosition = 0;
        targetLocation = Vector2.zero;

        if (goldOres.Count == 0)
        {
            //Debug.Log("i Could not find a gold");
            if (storage == 0)
            {
                Retreat();
            }
            else
            {
                //Debug.Log("There is no gold but I need to unload before going to retreat");
                targetLocation = tv.unloadPos.transform.position;
            }
            return;
        }

        foreach (Gold goldOre in goldOres)
        {
            float distance1 = Vector2.Distance(transform.position, goldOre.spot1.transform.position);
            float distance2 = Vector2.Distance(transform.position, goldOre.spot2.transform.position);

            if (goldOre.miningSpotsUsed < goldOre.maxMiningSpots)
            {
                if (goldOre.spot1Available && distance1 < closestDistance)
                {
                    closestDistance = distance1;
                    targetLocation = goldOre.spot1.transform.position;
                    miningPosition = 1;
                    targetGold = goldOre;
                }

                if (goldOre.spot2Available && distance2 < closestDistance)
                {
                    closestDistance = distance2;
                    targetLocation = goldOre.spot2.transform.position;
                    miningPosition = 2;
                    targetGold = goldOre;
                }
            }
        }

        // if gold is there but no available slots, go to retreat
        if (closestDistance == Mathf.Infinity)
        {
            targetLocation = tv.retreatPos.transform.position;
            return;
        }

        
        //Debug.Log("I Found a Gold");
        if (miningPosition == 1)
        {
            targetGold.spot1Available = false;
        }

        else if (miningPosition == 2)
        {
            targetGold.spot2Available = false;
        }

        targetGold.miningSpotsUsed++;
            
        
    }

    /// <summary>
    /// the Retreat state of the miner, when the team goes to retreat the miner will stay put
    /// </summary>
    public void Retreat()
    {
        // this resets all mining settings so that there is no problem when miners change state. it acts as fresh start
        targetLocation = tv.retreatPos.transform.position;

        if (targetGold)
        {
            targetGold.spot1Available = true;
            targetGold.spot2Available = true;
            targetGold.miningSpotsUsed = 0;
            targetGold = null;
            isMining = false;
            isUnloading = false;
        }

        if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
        {
            anim.Play("Idle");
            //flip = false;
        }
    }


    // The Gather state of the miner, when the team is in defend or attack state the miner will Gather recources
    public void Gather()
    {
        ///The gather state///
        Vector2 currentPos = transform.position;
        Vector2 retreatPos = tv.retreatPos.transform.position;
        Vector2 unloadPos = tv.unloadPos.transform.position;
        float distanceToTarget = Vector2.Distance(currentPos, targetLocation);
        float distanceToUnloadPos = Vector2.Distance(currentPos, unloadPos);


        if (distanceToTarget < 0.2f)
        {
            
            HandleAtTargetPosition(retreatPos, unloadPos);
        }
        else
        {
            HandleNotAtTargetPosition(distanceToUnloadPos,distanceToTarget, unloadPos, retreatPos);
        }
    }

    private void HandleAtTargetPosition(Vector2 retreatPos, Vector2 unloadPos) {
       
        // Miner is at the Retreat positon, will look for gold constantly 
        if (targetLocation == retreatPos)
        {
            //Debug.Log("At retreat looking for gold");
            Retreat();
            FindNextTask();
        }

        // Miner is at the unloading position
        else if (targetLocation == unloadPos)
        {
            Unload(); 
        }
        // Miner is at the gold mining position
        else if (targetLocation != retreatPos && targetLocation != unloadPos && targetGold)
        {
            Mine();   
        }
        // if the Gold ore runs out, this condition is met as the target location is still set to where the gold mine was but there is no target gold as it was destroyed
        else if (!targetGold)
        {
            //Debug.Log("Gold ore that I was mining ran out so now am looking for new gold");

            StopMining();
            FindNextTask();
        }

    }
    private void HandleNotAtTargetPosition(float distanceToUnloadPos, float distanceToTarget, Vector2 unloadPos, Vector2 retreatPos)
    {
        isUnloading = false;


        if (storage > 0 && storage < maxStorage)
        {
            // if we have any gold, we check to see if we have a target gold to go to, and see if its better to go unload or go mine that gold
            if (targetGold)
            {
                
                if (distanceToUnloadPos < distanceToTarget)
                {
                    //Debug.Log("Going to unload");
                    targetLocation = unloadPos;
                    StopMining();
                }
            }

            // if we dont have a target gold, then we see if unloading is better than going to the next task location
            // this will also be called when changing team states, from retreat to defend or attack, because when retreating all states are reset as well as target gold
            // so we see the best task and compare that too
            else
            {
                FindNextTask();
                distanceToTarget = Vector2.Distance(transform.position, targetLocation);

                if (distanceToUnloadPos < distanceToTarget)
                {
                    //Debug.Log("I have some gold left in my bag and the unload position is closer, so I am going there");
                    targetLocation = unloadPos;
                    StopMining();
                    targetGold = null;
                }
            }
        }
        else if (storage >= maxStorage)
        {
            targetLocation = unloadPos;
            StopMining();
        }

        else if (targetLocation == retreatPos || !targetGold)
        {
            
            FindNextTask();
        }
       
        
    }
    
    private void Mine()
    {
       
        anim.Play("Mine");
        flip = (tv.team ==1) ?(miningPosition == 1):(miningPosition !=1) ;

        if (tv.team == 1)
        {
            if (miningPosition == 1) flip = false;
            else flip = true;
        }
        else
        {
            if (miningPosition == 1) flip = true;
            else flip = true;
        }

        if (!isMining)
        {
            //Debug.Log("I am Mining");
            isMining = true;
            StartCoroutine(MineAnimation());
        }

        if (storage >= maxStorage)
        {
            //Debug.Log("Storage full");
            StopMining();
            targetLocation = tv.unloadPos.transform.position;
        }
        //else if (targetGold.capacity <= 0)
        //{
        //   // Debug.Log("Im a gold ore and i ran out of gold bye x");
        //    tv.goldList.Remove(targetGold);
        //    StopMining() ;
        //    FindNextTask();
        //}
    }
    private void Unload()
    {
      
        anim.Play("Idle");

        if (!isUnloading)
        {
            //Debug.Log("Unloading");
            isUnloading = true;
            //flip = true;
            StartCoroutine(UnloadAnimation());
        }

        if (storage == 0)
        {
            //Debug.Log("Storage empty");
            StopCoroutine(UnloadAnimation());
            isUnloading = false;
            FindNextTask();
        }
    }

    private void StopMining()
    {
        StopCoroutine(MineAnimation());
        isMining = false;
        if (targetGold) { 
            if (miningPosition == 1)
            {
                targetGold.spot1Available = true;
            } else
            {
                targetGold.spot2Available = true;
            }
            targetGold.miningSpotsUsed--;
            targetGold = null;
        } 
        
    }

   


    public IEnumerator MineAnimation()
    {
        Gold initialTargetGold = targetGold;

        while (isMining)
        {
            yield return new WaitForSeconds(1);

            if (storage >= maxStorage)
            {
                yield break;
            }

            if (targetGold == initialTargetGold)
            {
                if (targetGold)
                {
                    targetGold.capacity--;
                    storage++;
                }
                else
                {
                    isMining = false;
                    anim.Play("Walk");
                }
            }
        }
    }

    public IEnumerator UnloadAnimation()
    {
        yield return new WaitForSeconds(2);
        if (Vector2.Distance(transform.position, (Vector2)tv.unloadPos.transform.position) < 0.2f) { 
            tv.gold += storage * gv.valuePerGold;
            storage = 0;
        }
    }


    protected override void Die()
    {
        base.Die();
        if (targetGold)
        {
            if (miningPosition == 1)
            {
                targetGold.spot1Available = true;
            }
            else
            {
                targetGold.spot2Available = true;
            }
        }
        
        
        isMining = false;
        tv.gathererUnits.Remove(gameObject);
        StopAllCoroutines();
        StartCoroutine(DeathAnimation());
    }
    

    // Testing functions
    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float moveInputV = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector2(moveInput * moveSpeed, moveInputV * moveSpeed);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(targetLocation, new Vector3(1, 1));
       
    }
}
