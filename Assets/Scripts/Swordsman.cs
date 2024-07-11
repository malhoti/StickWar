using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class Swordsman : Unit
{
    [Header("Unit Specific Attributes")]

    public int attackDamage;
    public EnemyDetector enemyDetector;
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
        enemyDetector = GetComponent<EnemyDetector>();
        anim.Play("SwordsmanWalk");
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


        if (Vector2.Distance(transform.position, targetLocation) < 0.1f)
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
        anim.Play("SwordsmanWalk");
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
        if (Vector2.Distance(transform.position, targetLocation) < 0.1f)
        {
            anim.Play("SwordsmanIdle");
        }
    }

    public void GetPositionInFormation()
    {

        index = tv.frontLineUnits.IndexOf(gameObject);
        int maxUnitsPerColumn = gv.maxUnitsPerColumn;
        float horizontalSpacing = gv.horizontalSpacing;
        float verticalSpacing = gv.verticalSpacing;
        Vector2 startPos = tv.defendPos.position;


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
        int unitsInColumn = Mathf.Min(tv.frontLineUnits.Count - column * maxUnitsPerColumn, maxUnitsPerColumn);
        float yOffset = (unitsInColumn % 2 == 0) ? horizontalSpacing / 2 : 0;
        float y = row * horizontalSpacing + yOffset;
        float x = (tv.team == 1) ? -column * verticalSpacing : column * verticalSpacing;
        if (tv.state != State.Advance){
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
        if (Vector2.Distance(transform.position, targetLocation) < 0.1f)
        {
            anim.Play("SwordsmanIdle");
            flip = (tv.team != 1);
        }


    }

    public void Advance()
    {

        Vector2 currentPos = transform.position;

        float distanceToTarget = Vector2.Distance(currentPos, targetLocation);

        // when advancing, unless it is in the middle of attacking, it will always search for enemies, if it doesnt find any , march forward until it finds an enemy or the enemy tower
        if (!isAttacking) { 
        FindEnemies();

            if (targetUnits.Count <= 0)
            {
                March();
                return;
            }
            
            FindClosestEnemy(targetUnits);
            IsTargetWithinAttackRange();
        }
        else
        {
            Attack();
        }
    }
    

    void IsTargetWithinAttackRange()
    {
        
        if (enemyDetector.IsTargetWithinAttackRange(targetUnit))
        {
            targetLocation = transform.position; // if you are attacking stand still
            isAttacking = true;
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

    public void FindEnemies()
    {
        targetUnits = enemyDetector.FindEnemies();
    }
    

    public void FindClosestEnemy(List<Unit> enemies)
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

        if (!isCoroutineRunning) { 
            StartCoroutine(AttackAnimation());
        }
    }

    public IEnumerator AttackAnimation()
    {
        isCoroutineRunning = true;
        Unit initialTargetUnit = targetUnit;

        while (isAttacking)
        {
            anim.Play("SwordsmanAttack");
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
                    anim.Play("SwordsmanWalk");
                }
            }
        }
        isCoroutineRunning = false;
        yield return null;
    }
        
        // Testing functions
        void HandleMovement()
        {
            float moveInput = Input.GetAxisRaw("Horizontal");
            float moveInputV = Input.GetAxisRaw("Vertical");
            rb.velocity = new Vector2(moveInput * moveSpeed, moveInputV * moveSpeed);
        }


        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawWireCube(transform.position, new Vector3(detectionZone.size.x, detectionZone.size.y));
        //}

    
}
