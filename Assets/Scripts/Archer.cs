using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : Unit
{
    [Header("Unit Specific Attributes")]

    public int attackDamage;
    public Unit targetUnit;
    public List<Unit> targetUnits;


    public bool isAttacking;

    [Header("Debugging")]
    public int index;
    public int row;
    bool isCoroutineRunning = false;


    // Start is called before the first frame update
    void Awake()
    {
        flip = false;
        gv = FindObjectOfType<GlobalVariables>().GetComponent<GlobalVariables>();
        rb = GetComponent<Rigidbody2D>();
        tv = GetComponentInParent<TeamVariables>();
        anim = GetComponentInChildren<Animator>();

        anim.Play("ArcherWalk");
        targetLocation = transform.position;
        Variation();
    }

    // Update is called once per frame
    void Update()
    {
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
    private void FixedUpdate()
    {
        Vector2 direction = (targetLocation - (Vector2)transform.position).normalized;

        if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
        {
            GetComponent<SpriteRenderer>().flipX = flip;
            return;
        }
        // is moving
        else if (direction.x > 0)
        {

            flip = false;
        }
        else if (direction.x < 0)
        {

            flip = true;
        }
        anim.Play("ArcherWalk");
        GetComponent<SpriteRenderer>().flipX = flip;
        rb.MovePosition(rb.position + direction * (moveSpeed * Time.fixedDeltaTime));
    }

    public void Variation()
    {
        float sizeVariation = Random.Range(-0.1f, 0.1f);
        float speedVariation = Random.Range(-0.15f, 0.15f);
        transform.localScale = new Vector2(transform.localScale.x + sizeVariation, transform.localScale.y + sizeVariation);
        moveSpeed = moveSpeed + speedVariation;
        anim.speed = Random.Range(0.95f, 1.05f);
    }
    public void Retreat()
    {
        targetLocation = tv.retreatPos.transform.position;
        if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
        {
            anim.Play("ArcherIdle");
        }
    }

    public void GetPositionInFormation()
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

    public void Defend()
    {
        GetPositionInFormation();
        // if swordsman is at the defend location
        if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
        {
            anim.Play("ArcherIdle");
            flip = (tv.team != 1);
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

            DecideEnemy(targetUnits);
            if (IsTargetWithinAttackRange(targetUnit))
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


    // When the Units are advancing they March forward
    void March()
    {
        if (tv.team == 1)
        {
            if ((targetLocation.x - transform.position.x) < 1f)
            {
                targetLocation.x = transform.position.x + 5;
                GetPositionInFormation();
            }
        }
        else
        {
            if ((transform.position.x - targetLocation.x) < 1f)
            {
                targetLocation.x = transform.position.x - 5;
                GetPositionInFormation();
            }
        }



    }


    /// <summary>
    /// Decide which enemy to attack from the list of targets, this will be different for each type of unit
    /// </summary>
    /// <param name="enemies"></param>
    public void DecideEnemy(List<Unit> enemies)
    {
        float closestdistance = Mathf.Infinity;
        targetUnit = null;
        targetLocation = Vector2.zero;

        foreach (Unit enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestdistance)
            {
                closestdistance = distance;
                targetUnit = enemy;

            }

        }
        targetLocation = targetUnit.transform.position;
    }

    void Attack()
    {
        if (!isCoroutineRunning)
        {
            StartCoroutine(AttackAnimation());
        }
    }

    public IEnumerator AttackAnimation()
    {
        isCoroutineRunning = true;
        Unit initialTargetUnit = targetUnit;

        while (isAttacking)
        {
            anim.Play("ArcherAttack");
            yield return new WaitForSeconds(1);

            if (!gameObject.activeSelf)
            {
                yield break;
            }
            if (targetUnit == initialTargetUnit)
            {
                if (targetUnit.gameObject.activeSelf)
                {
                    targetUnit.TakeDamage(attackDamage);

                }
                else
                {

                    isAttacking = false;
                    anim.Play("ArcherWalk");
                }
            }
        }
        isCoroutineRunning = false;
        yield return null;
    }

    protected override void Die()
    {
        tv.frontLineUnits.Remove(gameObject);
        gameObject.SetActive(false);
    }
    // Testing functions
    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float moveInputV = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector2(moveInput * moveSpeed, moveInputV * moveSpeed);
    }

}
