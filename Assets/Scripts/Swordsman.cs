using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Swordsman : Unit
{
    [Header("Unit Specific Attributes")]
    public int attackDamage;



    [Header("Debugging")]
    public int index;
    public int row;





    // Start is called before the first frame update
    void Start()
    {


        flip = false;
        gv = FindObjectOfType<GlobalVariables>().GetComponent<GlobalVariables>();
        rb = GetComponent<Rigidbody2D>();
        tv = GetComponentInParent<TeamVariables>();
        anim = GetComponentInChildren<Animator>();
        anim.Play("SwordsmanWalk");
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
            case State.Attack:
                Attack();
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
        if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
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
        targetLocation = new Vector2(startPos.x + x, startPos.y + y);


    }

    public void Defend()
    {
        GetPositionInFormation();
        // if swordsman is at the defend location
        if (Vector2.Distance(transform.position, targetLocation) < 0.2f)
        {
            anim.Play("SwordsmanIdle");
            flip = (tv.team == 1) ? false : true;
        }


    }

    public void Attack()
    {
        if (tv.team == 1) {
            targetLocation = new Vector2(targetLocation.x + 10, targetLocation.y);
        }
        else
        {
            targetLocation = new Vector2(targetLocation.x - 10, targetLocation.y);
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        Unit enemy = collision.gameObject.GetComponent<Unit>();
        if (enemy != null)
        {
            if (enemy.tv.team != tv.team)
            {
                Debug.Log("I see another enemy");
            }
        }




    }
        // Testing functions
    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float moveInputV = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector2(moveInput * moveSpeed, moveInputV * moveSpeed);
    }

    
}
