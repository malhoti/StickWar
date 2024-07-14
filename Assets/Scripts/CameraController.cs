using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class CameraController : MonoBehaviour
{
    GlobalVariables gv;
    public float moveSpeed = 5f;  // Speed of camera movement

    public float dragSpeed = 1f;  // Speed of camera dragging

    private Vector3 dragOrigin;

    public float leftBound;
    public float rightBound;
    public float yposition;

    private Camera cam;

    private bool isDragging;

    void Start()
    {
        cam = Camera.main;
    }

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

        if (Input.GetMouseButton(0))
        {
            Vector3 pos = Camera.main.ScreenToViewportPoint(dragOrigin - Input.mousePosition);
            Vector3 move = new Vector3(pos.x * dragSpeed, 0, 0);

            // Adjust drag speed based on your preference
            transform.Translate(move, Space.World);
        }




        Vector3 clampedPosition = transform.position;

        // Calculate the camera's size in world units
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        // Clamp the camera's position to ensure its edges stay within bounds
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, leftBound + camWidth / 2, rightBound - camWidth / 2);
        clampedPosition.y = yposition;

        transform.position = clampedPosition;
    
    }

    
        

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(new Vector2(leftBound, 10), new Vector2(leftBound, -10));
        Gizmos.DrawLine(new Vector2(rightBound, 10), new Vector2(rightBound, -10));
        Gizmos.DrawLine(new Vector2(10, yposition), new Vector2(-10, yposition));

    }


}


