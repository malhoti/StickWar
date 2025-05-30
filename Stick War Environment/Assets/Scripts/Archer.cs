using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Archer : DamageUnit
{
    
    public float arrowSpeed;
    [Header("Arrow")]
    public GameObject arrowPrefab;
    public UnityEngine.Transform shootPoint;
    
    
    [Header("Debugging")]
    public int index;
    public int row;
    public bool reachedPos;




    public override void Retreat()
    {
        
        // true for archers that can attack from garrison, only a maximum of how many units are allowed per column are allowed
        if (tv.rearLineUnits.IndexOf(gameObject) < gv.maxUnitsPerColumn)
        {

            // sets the the limit of archers so that they can go out of position when retreat state, they stay there, this prevents archers chasing units that are retreating
            targetLocation.x =tv.towerArcherPos.transform.position.x;

            targetUnits = FindEnemies();
            if (targetUnits.Count == 0)
            {
                GetPositionInRetreat();
                if (Vector2.Distance(transform.position, targetLocation) < 1f)
                {
                    anim.Play("Idle");
                    flip = tv.team != 1;
                }
                return;
            }

            if (!isAttacking)
            {
                targetUnit = null;
                
                DecideEnemy();
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

        

        else
        {
            targetLocation = tv.retreatPos.transform.position;
            if (Vector2.Distance(transform.position, targetLocation) < 1f)
            {
                anim.Play("Idle");
            }
        }

    }
    

    public virtual void GetPositionInRetreat()
    {
        index = tv.rearLineUnits.IndexOf(gameObject);
        int maxUnitsPerColumn = gv.maxUnitsPerColumn;
        float horizontalSpacing = gv.horizontalSpacing;
        Vector2 startPos = tv.towerArcherPos.position;

        int positionInColumn = index % maxUnitsPerColumn;
        if (positionInColumn % 2 == 0)
        {
            row = positionInColumn / 2;
        }
        else
        {
            row = -(positionInColumn / 2) - 1;
        }

        int unitsInColumn = Mathf.Min(tv.rearLineUnits.Count, maxUnitsPerColumn);
        float yOffset = (unitsInColumn % 2 == 0) ? horizontalSpacing / 2 : 0;
        float y = row * horizontalSpacing + yOffset;

        targetLocation = new Vector2(startPos.x, startPos.y + y);
    }
    public override Vector2 GetPositionInFormation()
    {
        index = tv.rearLineUnits.IndexOf(gameObject);
        int maxUnitsPerColumn = gv.maxUnitsPerColumn;
        float horizontalSpacing = gv.horizontalSpacing;
        float verticalSpacing = gv.verticalSpacing;
        Vector2 startPos = tv.defendPos.position;

        // how many columns does the front line take
        int frontLineColumns = tv.frontLineUnits.Count % maxUnitsPerColumn ==0 ? (tv.frontLineUnits.Count / maxUnitsPerColumn) : (tv.frontLineUnits.Count / maxUnitsPerColumn)+1;
        int column = index / maxUnitsPerColumn;
        
        int positionInColumn = index % maxUnitsPerColumn;
        if (positionInColumn % 2 == 0)
        {
            row = positionInColumn / 2;
        }
        else
        {
            row = -(positionInColumn / 2) - 1;
        }
        int unitsInColumn = Mathf.Min(tv.rearLineUnits.Count - column * maxUnitsPerColumn, maxUnitsPerColumn);
        float yOffset = (unitsInColumn % 2 == 0) ? horizontalSpacing / 2 : 0;
        float y = row * horizontalSpacing + yOffset;
        float x = (tv.team == 1) ? -(column+frontLineColumns) * verticalSpacing : (column + frontLineColumns) * verticalSpacing;
        if (tv.state != State.Advance)
        {
            return new Vector2(startPos.x + x, startPos.y + y);
        }
        else
        {
            //Debug.Log(y);
            return new Vector2(targetLocation.x, startPos.y + y);
        }
    }

    /// <summary>
    /// function which changes targetLocation and sets targetUnit to an enemy
    /// </summary>
    public override void DecideEnemy()
    {
        targetUnit = targetUnits.OrderBy(unit => Vector2.Distance(transform.position, unit.transform.position)).FirstOrDefault();
        if (targetUnit != null)
        {
            
            targetLocation = new Vector2(transform.position.x, targetUnit.transform.position.y + targetUnit.attackOffset.y);
        }
        
    }

    public override void Attack()
    {
        //targetLocation = transform.position;
        anim.Play("Attack");
    }

    

    /// <summary>
    /// Called by the animation event when the attack should deal damage.
    /// </summary>
    public override void OnAttackHit()
    {
        
        if (targetUnit != null && targetUnit.alive)
        {
            ShootArrow();
            // Optionally, add attack effects or sound here
        }
        else 
        {
            ResetValues();
        }
    }

    
    private void ShootArrow()
    {
        
        // Calculate direction towards the target
        Vector2 direction = (targetUnit.transform.position - shootPoint.position).normalized;
        if (direction.x < 0)
        {
            shootPoint.localPosition= new Vector2(shootPoint.localPosition.x * -1,shootPoint.localPosition.y);
        }
        // Instantiate the arrow at the shoot point
        GameObject arrowInstance = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity,shootPoint);

        arrowInstance.GetComponent<Arrow>().target = targetUnit;

        // Calculate the initial velocity for a parabolic trajectory
        Rigidbody2D rb = arrowInstance.GetComponent<Rigidbody2D>();
        Vector2 targetPosition = targetUnit.transform.position;
        Vector2 initialVelocity = CalculateInitialVelocity(shootPoint.position, targetPosition, arrowSpeed);

        // Set the initial velocity of the arrow
        rb.velocity = initialVelocity;
    }

    private Vector2 CalculateInitialVelocity(Vector2 startPosition, Vector2 targetPosition, float speed)
    {
        // Calculate the distance to the target
        Vector2 distance = targetPosition - startPosition;

        // Calculate the time to reach the target
        float time = distance.magnitude / speed;

        // Calculate the initial velocity components
        float vx = distance.x / time;
        float vy = distance.y / time + 0.5f * Mathf.Abs(Physics2D.gravity.y) * time;

        return new Vector2(vx, vy);
    }
    protected override void Die()
    {
        base.Die();
        tv.rearLineUnits.Remove(gameObject);
        
    }
    // Testing functions
    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float moveInputV = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector2(moveInput * moveSpeed, moveInputV * moveSpeed);
    }

}
