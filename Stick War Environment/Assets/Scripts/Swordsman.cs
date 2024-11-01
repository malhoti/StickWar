using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
//using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class Swordsman : DamageUnit
{
    [Header("Debugging")]
    public int index;
    public int row;
    

   

    public override Vector2 GetPositionInFormation()
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
            return new Vector2(startPos.x + x, startPos.y + y);
        }
        else
        {
            
            return new Vector2(targetLocation.x, startPos.y + y);
        }
    }


    protected override void Die()
    {
        base.Die();
        tv.frontLineUnits.Remove(gameObject);
        StopAllCoroutines();
        StartCoroutine(DeathAnimation());
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
