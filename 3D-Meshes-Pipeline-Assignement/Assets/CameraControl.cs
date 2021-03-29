using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed;
    public float rotSpeed;
    public float maxVelocity;
    public float drag;

    Vector3 rotation;
    Vector3 velocity;
    Vector3 mousePos;
    Vector3 deltaMouse;

    // Update is called once per frame
    void Update()
    {
        //Move around using the wsad keys
        if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
        {
            velocity += moveSpeed * transform.forward;
        }
        if (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
        {
            velocity -= moveSpeed * transform.forward;
        }
        if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
        {
            velocity += moveSpeed * transform.right;
        }
        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            velocity -= moveSpeed * transform.right;
        }

        //Rotate the camera with left mouse drag
        if (Input.GetMouseButton(1))
        {
            //Calculate the delta mouse by substracting the previous mouse position form the current mouse position
            deltaMouse = Input.mousePosition - mousePos;
            //Rotate the camera based on deltaMouse and rotation speed
            rotation = new Vector3(-deltaMouse.y * rotSpeed, deltaMouse.x * rotSpeed, 0);
            //Apply the rotation
            if (rotation != Vector3.zero)
            {
                transform.eulerAngles = transform.eulerAngles + rotation;
            }

            //Prevent the z angle from changing
            if (transform.eulerAngles.z != 0)
            {
                transform.eulerAngles -= new Vector3(0, 0, transform.eulerAngles.z);
            }
        }
        //Always keep track of the mouse position for calculating the delta
        mousePos = Input.mousePosition;

        //When the vector distance is bigger then 1 normalize it
        if (velocity.magnitude > 1)
        {
            //Create a max speed using the normalized vector distance
            velocity.Normalize();
        }
        //Because the total velocity is 1 or smaller multiply this by the desired speed 
        transform.position += velocity * maxVelocity;

        //Have drag to slow down and stop
        velocity *= drag;

        //Prevent micromovement
        if (velocity.magnitude < 0.04f)
        {
            velocity = Vector3.zero;
        }
    }
}
