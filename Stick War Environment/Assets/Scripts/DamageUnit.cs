using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DamageUnit : Unit
{


    public int attackDamage;
    public float attackSpeed;
    public Unit targetUnit;
    public List<Unit> targetUnits;


    public bool isAttacking;
    public bool isCoroutineRunning = false;
    public bool reachedDefendMaxPos = false;

    public override void Start()
    {
        base.Start();
        anim.Play("Walk");
        targetLocation = transform.position;
        Variation();
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        if(!alive) return;
        switch (tv.state)
        {
            case State.Retreat:
                Retreat(); break;
            case State.Defend:
                Defend(); break;
            case State.Advance:
                Advance();
                break;
        }
    }
    
    public void Variation()
    {
        float sizeVariation = Random.Range(-0.1f, 0.1f);
        float speedVariation = Random.Range(-0.15f, 0.15f);
        transform.localScale = new Vector2(transform.localScale.x + sizeVariation, transform.localScale.y + sizeVariation);
        moveSpeed = moveSpeed + speedVariation;
        anim.speed = Random.Range(0.95f, 1.05f);
    }
    public virtual void Retreat()
    {
        ResetValues();
        targetLocation.x = tv.retreatPos.transform.position.x;
        if (Vector2.Distance(transform.position, targetLocation) < 1f)
        {
            anim.Play("Idle");
        }
    }

    /// <summary>
    /// Decides where in the formation it will stand, it will vary for different units
    /// </summary>
    public virtual Vector2 GetPositionInFormation()
    {
        return new Vector2();
    }


    public void Defend()
    {
        // whilst defending, units will always scout for enemies
        targetUnits = FindEnemies();
        

        // this checks if unit reaches the max range it can chase enemies and flags it
        if (((tv.team == 1 && transform.position.x > tv.defendMaxPos.position.x) || (tv.team == 2 && transform.position.x < tv.defendMaxPos.position.x)))
        {
            reachedDefendMaxPos = true;
        }


        // if unit doesnt have a target or it reached the end, it will go back to the formation
        if (targetUnits.Count == 0 || reachedDefendMaxPos)
        {
            
            targetLocation = GetPositionInFormation();
            if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
            {
               
                ResetValues();
                reachedDefendMaxPos = false;
                anim.Play("Idle");
                flip = tv.team != 1;
            }
            return;
        }

     

        // if it does detect enemies, and it isnt currently attacking, then decide for which enemy to go to 
        if (!isAttacking)
        {

            DecideEnemy();
            if (IsTargetWithinAttackRange())
            {
                Debug.Log("hello");
                targetLocation = transform.position; // if you are attacking stand still
                isAttacking = true;
            }
        }
        else
        {
            Attack();
        }



    }

    public void Advance()
    {

        // when advancing, unless it is in the middle of attacking, it will always search for enemies, if it doesnt find any , march forward until it finds an enemy or the enemy tower
        if (!isAttacking)
        {
            targetUnits = FindEnemies();
            DecideEnemy();

            if (targetUnit == null)
            {
                March();
                return;
            }

            
            if (IsTargetWithinAttackRange())
            {
                
                targetLocation = transform.position; // if you are attacking stand still
                isAttacking = true;
            }
        }
        else
        {
            Attack();
        }
    }

    

    public void March()
    {

        if (tv.team == 1)
        {
            if ((targetLocation.x - transform.position.x) < positionThreshold)
            {
                targetLocation.x = transform.position.x + 5;
                //GetPositionInFormation();
            }
        }
        else
        {
            if ((transform.position.x - targetLocation.x) < positionThreshold)
            {
                targetLocation.x = transform.position.x - 5;
                //GetPositionInFormation();
            }
        }



    }
    /// <summary>
    /// Decide which enemy to attack from the list of targets, this will be different for each type of unit, and sets the targetlocation
    /// </summary>
    public virtual void DecideEnemy()
    {
        // checks which unit is closest, wont target enemies that are retreating
        targetUnit = targetUnits.OrderBy(unit => Vector2.Distance(transform.position, unit.transform.position)).FirstOrDefault();
        Unit enemyTower = targetUnits.Where(unit => unit is Tower).FirstOrDefault();
        if (targetUnit != null)
        {
            if (targetUnit.tv.state == State.Retreat && enemyTower == null)
            {
                targetUnit = null;
                return;
            }
            if (targetUnit.tv.state == State.Retreat && enemyTower != null)
            {
                targetUnit = enemyTower;
                
            }
            
            targetLocation = new Vector2(targetUnit.transform.position.x, targetUnit.transform.position.y + targetUnit.attackOffset.y);

        }
    }

    public bool IsTargetWithinAttackRange()
    {
        Vector3 attackPosition = transform.position + (Vector3)attackOffset;
        //Vector3 attackSize = new Vector2(Mathf.Max(attackWidth,targetUnit.GetComponent<SpriteRenderer>().bounds.size.x), attackHeight);
        Vector3 attackSize = new Vector2(attackWidth, attackHeight);

        
        Vector3 targetAttackPosition = targetUnit.transform.position + (Vector3)targetUnit.attackOffset;
        

        
        Bounds attackBounds = new Bounds(attackPosition, attackSize);
        Bounds targetAttackBounds = new Bounds(targetAttackPosition, attackSize);
       

        // Check if the target's position is within the bounds
        if (attackBounds.Intersects(targetAttackBounds))
        {
            isAttacking = true;
            return true;
            // Perform attack or other actions here
        }
        isAttacking = false;
        return false;
    }

    public virtual void Attack()
    {

        if (!isCoroutineRunning)
        {
            StartCoroutine(AttackAnimation());
        }
    }

    public virtual IEnumerator AttackAnimation()
    {
        isCoroutineRunning = true;
        Unit initialTargetUnit = targetUnit;

        while (isAttacking)
        {
            anim.Play("Attack");
            yield return new WaitForSeconds(attackSpeed);

            if (!gameObject.activeSelf)
            {
                yield break;
            }

            // IMPORTANT - whilst it is attacking, it checks if the targetUnit is the same as the 'firerate' seoncds ago, because targetUnit can change by other places of the code , if archer decode to change enemy for a reason
            // we also have to check if the target is within range at all times, otherwise we will be attacking a unit outside the range, 
            // we also check if the unit is also alive
            if (targetUnit == initialTargetUnit && IsTargetWithinAttackRange() && targetUnit.alive)
            {

                targetUnit.TakeDamage(attackDamage);
            }
            else
            {
                ResetValues();
            }

        }
        isCoroutineRunning = false;
        yield return null;
        
    }

    /// <summary>
    /// This function resets all variables that are linked to attacking, such as target units and sets attacking to false
    /// </summary>
    public void ResetValues()
    {
        targetUnits = null;
        targetUnit = null;
        isAttacking = false;
        isCoroutineRunning = false;
        //anim.Play("Idle");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)detectionOffset, new Vector3(detectionWidth, detectionHeight));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)attackOffset, new Vector3(attackWidth, attackHeight));
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(targetLocation, new Vector3(1, 1));
        Gizmos.color = Color.magenta;
        Vector3 attackPosition = transform.position + (Vector3)attackOffset;
        Vector3 targetAttackPosition = targetUnit.transform.position + (Vector3)targetUnit.attackOffset;
        Vector3 attackSize = new Vector2(attackWidth, attackHeight);
        





        Bounds attackBounds = new Bounds(attackPosition, attackSize);
        Bounds targetAttackBounds = new Bounds(targetAttackPosition, attackSize);

        Gizmos.DrawWireCube(targetAttackPosition, attackSize);
    }


}
