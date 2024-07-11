using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class EnemyDetector : MonoBehaviour
{
    public float detectionWidth;
    public float detectionHeight;

    public float attackWidth;
    public float attackHeight;

    public TeamVariables tv;

    private void Awake()
    {
        tv = GetComponentInParent<TeamVariables>();
    }
    /// <summary>
    /// This returns a list of Units that it detects
    /// </summary>
    /// <returns></returns>
    public List<Unit> FindEnemies()
    {

        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, new Vector2(detectionWidth, detectionHeight), transform.rotation.eulerAngles.z);

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
        
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, new Vector2(attackWidth, attackHeight), transform.rotation.eulerAngles.z);

        foreach (var collider in colliders)
        {
            if (collider == target.GetComponent<Collider2D>()) return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {  
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionWidth, detectionHeight));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(attackWidth, attackHeight));
    }
}
