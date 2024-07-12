using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : DamageUnit
{
    public float fireRate;
    public float arrowSpeed;
    [Header("Arrow")]
    public GameObject arrowPrefab;
    public Transform shootPoint;
    

    [Header("Debugging")]
    public int index;
    public int row;
    


    // Start is called before the first frame update
    

    public override void GetPositionInFormation()
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
            targetLocation = new Vector2(startPos.x + x, startPos.y + y);
        }
        else
        {
            //Debug.Log(y);
            targetLocation = new Vector2(targetLocation.x, startPos.y + y);
        }
    }

    public override void DecideEnemy()
    {
        float closestdistance = Mathf.Infinity;
        targetUnit = null;
        targetLocation = Vector2.zero;

        foreach (Unit enemy in targetUnits)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestdistance)
            {
                closestdistance = distance;
                targetUnit = enemy;

            }

        }
        targetLocation = new Vector2(transform.position.x,targetUnit.transform.position.y+targetUnit.attackOffset.y);
    }

    public override IEnumerator AttackAnimation()
    {
        isCoroutineRunning = true;
        Unit initialTargetUnit = targetUnit;

        while (isAttacking)
        {
            anim.Play("Attack");
            yield return new WaitForSeconds(fireRate);

            if (!gameObject.activeSelf)
            {
                yield break;
            }
            if (targetUnit == initialTargetUnit)
            {
                if (targetUnit.alive)
                {
                    ShootArrow();

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

    private void ShootArrow()
    {
        Debug.Log("i shot");
        // Calculate direction towards the target
        Vector2 direction = (targetUnit.transform.position - shootPoint.position).normalized;
        if (direction.x < 0)
        {
            shootPoint.localPosition= new Vector2(shootPoint.localPosition.x * -1,shootPoint.localPosition.y);
        }
        // Instantiate the arrow at the shoot point
        GameObject arrowInstance = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity,shootPoint);

        

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
        alive = false;
        targetLocation = transform.position;// stand where you are
        tv.rearLineUnits.Remove(gameObject);
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

}
