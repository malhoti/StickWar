using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public int damage;

    public TeamVariables tv;
    public Archer archer;
    Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        tv = GetComponentInParent<TeamVariables>();
        archer = FindObjectOfType<Archer>().GetComponent<Archer>();
        rb = GetComponent<Rigidbody2D>();

        damage = archer.attackDamage;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        Unit target = collision.GetComponent<Unit>();
        if (target != null)
        {
            if (target.tv.team != tv.team)
            {
                // Apply damage to the target
                target.TakeDamage(damage);

                // Destroy the arrow
                Destroy(gameObject);
            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (rb.velocity.x < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
        }
        else
        {
            GetComponent<SpriteRenderer>().flipX = false;
        }
    }
}
