using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f;  // Speed of camera movement

    public float dragSpeed = 1f;  // Speed of camera dragging

    private Vector3 dragOrigin;

    void Update()
    {
        // Horizontal movement
        float horizontalInput = Input.GetAxis("Horizontal");
        // Vertical movement
        

        // Calculate movement direction
        Vector3 moveDirection = new Vector3(horizontalInput, 0, 0).normalized;

       
        // Move camera
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);



        

    
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(0)) return;

        Vector3 pos = Camera.main.ScreenToViewportPoint(dragOrigin - Input.mousePosition);
        Vector3 move = new Vector3(pos.x * dragSpeed,0,0);

        // Adjust drag speed based on your preference
        transform.Translate(move, Space.World);
    }
}


