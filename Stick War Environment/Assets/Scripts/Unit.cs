using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
//using UnityEditor.SceneManagement;
using UnityEngine;


public class Unit : MonoBehaviour
{
    [Header("Components")]
    public GlobalVariables gv;
    public TeamVariables tv;
    public Rigidbody2D rb;
    public Animator anim;
    public HealthBar healthBar;
    

    [Header("Attributes")]
    public float moveSpeed;
    public float health;
    private float maxHealth;

    [Header("States")]
    public Vector2 targetLocation;
    public bool flip;
    public bool alive = true;

    [Header("Bounds")]
    public float detectionWidth;
    public float detectionHeight;
    public Vector2 detectionOffset;
    

    public float attackWidth;
    public float attackHeight;
    public Vector2 attackOffset;

    public float positionThreshold;

    [Header("Debugging")]
    public Collider2D[] colliders;

    
    
    public virtual void Start()
    {
        alive = true;
        maxHealth = health;
        flip = false;
        gv = FindObjectOfType<GlobalVariables>().GetComponent<GlobalVariables>();
        rb = GetComponent<Rigidbody2D>();
        tv = GetComponentInParent<TeamVariables>();
        anim = GetComponent<Animator>();
        healthBar = GetComponentInChildren<HealthBar>();
        positionThreshold = 1f;
        
    }

    public virtual void Update()
    {
        
        healthBar.UpdateHealthBar(health,maxHealth);
    }
    public virtual void FixedUpdate()
    {
        Vector2 direction = (targetLocation - (Vector2)transform.position).normalized;


        if (Vector2.Distance(transform.position, targetLocation) < positionThreshold)
        {
            transform.position = targetLocation;
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
        anim.Play("Walk");
        GetComponent<SpriteRenderer>().flipX = flip;
        rb.MovePosition(rb.position + direction * (moveSpeed * Time.fixedDeltaTime));
    }
    

    public virtual void TakeDamage(int damage)
    {

        health -= damage;
        if (health <= 0)
        {
            if (alive) Die();
        }
    }

    protected virtual void Die()
    {
        alive = false;
        targetLocation = transform.position;// stand where you are
        tv.units--;
        anim.Play("Dead");
        //gameObject.SetActive(false);
    }


    public void DeathAnimation()
    {
        
        
        Destroy(gameObject);
    }
    

    /// <summary>
    /// This returns a list of Units that it detects
    /// </summary>
    /// <returns></returns>
    public List<Unit> FindEnemies()
    {
        colliders = Physics2D.OverlapBoxAll(transform.position + (Vector3)detectionOffset, new Vector2(detectionWidth, detectionHeight), 0);

        var list = new List<Unit>();
        

        foreach (var collider in colliders)
        {
            Unit enemy = collider.gameObject.GetComponent<Unit>();
            if (enemy == null || !enemy.alive || enemy.tv.team == tv.team)
            {
                continue;
            }
            list.Add(enemy);
        }
        return list;
    }

    



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)detectionOffset, new Vector3(detectionWidth, detectionHeight));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)attackOffset, new Vector3(attackWidth, attackHeight));
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(targetLocation, new Vector3(1, 1));
    }
}
   

