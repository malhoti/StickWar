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
}
   

