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
            tv.health = 0;
            tv.isDead = true;
            Die();
        }
        tv.health = health;
    }
    
}
