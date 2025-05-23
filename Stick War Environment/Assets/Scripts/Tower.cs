using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Tower : Unit
{

    
    public List<Unit> targetUnits;
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        health = gv.maxHealth;
        
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        tv.enemiesInVicinity = FindEnemies();
        
        
    }
    public override void FixedUpdate()
    {
        
    }
    public override void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Debug.Log("tower dead");
            tv.health = 0;
            tv.isDead = true;
            gameObject.SetActive(false);
            Die();
        }
        tv.health = health;
    }
    
}
