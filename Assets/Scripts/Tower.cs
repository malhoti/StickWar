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
        gv = FindObjectOfType<GlobalVariables>().GetComponent<GlobalVariables>();
        tv = GetComponentInParent<TeamVariables>();
        anim = GetComponentInChildren<Animator>();
        alive = true;
    }

    // Update is called once per frame
    public override void Update()
    {
        targetUnits = FindEnemies();
        
    }
    public override void FixedUpdate()
    {
        
    }
}
