using System.Collections;
using System.Collections.Generic;

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
    public int health;
    public int maxHealth;

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

    [Header("Debugging")]
    public Collider2D[] colliders;


    public virtual void Start()
    {
        alive = true;
        health = maxHealth;
        healthBar = GetComponentInChildren<HealthBar>();
    }

    public virtual void Update()
    {
        healthBar.UpdateHealthBar(health,maxHealth);
    }
    public virtual void FixedUpdate()
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
        anim.Play("Walk");
        GetComponent<SpriteRenderer>().flipX = flip;
        rb.MovePosition(rb.position + direction * (moveSpeed * Time.fixedDeltaTime));
    }
    

    public virtual void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            
            Die();
        }
    }

    protected virtual void Die()
    {
        alive = false;
        targetLocation = transform.position;// stand where you are
        gameObject.SetActive(false);
    }


    public virtual IEnumerator DeathAnimation()
    {
        anim.Play("Dead");
        yield return new WaitForSeconds(3);
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
   

