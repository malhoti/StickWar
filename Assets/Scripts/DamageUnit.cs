using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DamageUnit : Unit
{


    public int attackDamage;
    public Unit targetUnit;
    public List<Unit> targetUnits;


    public bool isAttacking;

    public bool isCoroutineRunning = false;
    public void Awake()
    {
        alive = true;
        flip = false;
        gv = FindObjectOfType<GlobalVariables>().GetComponent<GlobalVariables>();
        rb = GetComponent<Rigidbody2D>();
        tv = GetComponentInParent<TeamVariables>();
        anim = GetComponentInChildren<Animator>();

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
        targetLocation = tv.retreatPos.transform.position;
        if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
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
        targetUnits = FindEnemies();
        if (targetUnits.Count == 0) { 
            targetLocation = GetPositionInFormation();
            if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
            {
                anim.Play("Idle");
                flip = tv.team != 1;
            }
            return;
        }

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

            if (targetUnits.Count <= 0)
            {
                March();
                return;
            }

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

    public void AttackOrMoveToPosition(Vector2 location)
    {
        if (!isAttacking)
        {
            targetUnits = FindEnemies();

            if (targetUnits.Count <= 0)
            {
                targetLocation = location;
                return;
            }

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

    public void March()
    {

        if (tv.team == 1)
        {
            if ((targetLocation.x - transform.position.x) < 1f)
            {
                targetLocation.x = transform.position.x + 5;
                //GetPositionInFormation();
            }
        }
        else
        {
            if ((transform.position.x - targetLocation.x) < 1f)
            {
                targetLocation.x = transform.position.x - 5;
                //GetPositionInFormation();
            }
        }



    }
    /// <summary>
    /// Decide which enemy to attack from the list of targets, this will be different for each type of unit
    /// </summary>
    public virtual void DecideEnemy()
    {

    }

    public bool IsTargetWithinAttackRange()
    {
        Vector3 attackPosition = transform.position + (Vector3)attackOffset;
        Vector3 attackSize = new Vector2(Mathf.Max(attackWidth,targetUnit.GetComponent<SpriteRenderer>().bounds.size.x), attackHeight);

        
        Vector3 targetAttackPosition = targetUnit.transform.position + (Vector3)targetUnit.attackOffset;
        

        
        Bounds attackBounds = new Bounds(attackPosition, attackSize);
        Bounds targetAttackBounds = new Bounds(targetAttackPosition, attackSize);
       

        // Check if the target's position is within the bounds
        if (attackBounds.Intersects(targetAttackBounds))
        {
            return true;
            // Perform attack or other actions here
        }

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
            yield return new WaitForSeconds(1);

            if (!gameObject.activeSelf)
            {
                yield break;
            }
            if (targetUnit == initialTargetUnit)
            {
                if (targetUnit.alive)
                {
                    targetUnit.TakeDamage(attackDamage);

                }
                else
                {

                    isAttacking = false;
                    anim.Play("Walk");
                }
            }
        }
        isCoroutineRunning = false;
        yield return null;
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
        Vector3 attackSize = new Vector2(Mathf.Max(attackWidth, targetUnit.GetComponent<SpriteRenderer>().bounds.size.x), attackHeight);






        Bounds attackBounds = new Bounds(attackPosition, attackSize);
        Bounds targetAttackBounds = new Bounds(targetAttackPosition, attackSize);

        Gizmos.DrawWireCube(targetAttackPosition, attackSize);
    }


}
