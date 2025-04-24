using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField]
    private Slider healthBar;
    public Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        healthBar = GetComponent<Slider>();
        Vector3 offset = transform.position;
    }

    // Update is called once per frame


    public void UpdateHealthBar(float health, float maxhealth)
    {
        if (health == maxhealth)
        {
            healthBar.gameObject.SetActive(false);
        }
        else
        {
            healthBar.gameObject.SetActive(true);
            healthBar.value = (float)health / maxhealth;
        }
        

      

    }
}

