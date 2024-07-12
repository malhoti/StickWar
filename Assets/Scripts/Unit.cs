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

    [Header("Attributes")]
    public float moveSpeed;
    public int health;

    [Header("States")]
    public Vector2 targetLocation;
    public bool flip;

    [Header("Bounds")]
    public float detectionWidth;
    public float detectionHeight;
    public Vector2 detectionOffset;
    

    public float attackWidth;
    public float attackHeight;
    public Vector2 attackOffset;
    

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
        // Handle death logic here
        gameObject.SetActive(false);
    }


    

    

   
    /// <summary>
    /// This returns a list of Units that it detects
    /// </summary>
    /// <returns></returns>
    public List<Unit> FindEnemies()
    {
        //Debug.Log("hi");
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position + (Vector3)detectionOffset, new Vector2(detectionWidth, detectionHeight), 0);

        var list = new List<Unit>();
        //Debug.Log(colliders.Length);

        foreach (var collider in colliders)
        {
            
            Unit enemy = collider.gameObject.GetComponent<Unit>();
            if (enemy != null)
            {
                if (enemy.tv.team != tv.team)
                {
                    
                    list.Add(enemy);

                }
            }

        }


        return list;
    }



    public bool IsTargetWithinAttackRange(Unit target)
    {

        Bounds attackBounds = new Bounds(transform.position + (Vector3) attackOffset, new Vector3(attackWidth, attackHeight, 0));

        Bounds targetAttackBounds = new Bounds(target.transform.position + (Vector3)target.attackOffset, new Vector3(target.attackWidth, target.attackHeight, 0));

        // Check if the target's position is within the bounds
        if (attackBounds.Intersects(targetAttackBounds))
        {
            Debug.Log("Target is within attack box");
            Debug.Log("im in range");
            return true;
            // Perform attack or other actions here
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)detectionOffset, new Vector3(detectionWidth, detectionHeight));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)attackOffset, new Vector3(attackWidth, attackHeight));
    }
}
   

