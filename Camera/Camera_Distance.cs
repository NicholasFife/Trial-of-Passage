/*
Made with work from:
Filmstorm. (2017, May 30). Free 3rd Person Camera Setup & Camera Collision Tutorial [Video File]. Retrieved from https://www.youtube.com/watch?v=LbDQHv9z-F0
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script goes on the Camera itself
public class Camera_Distance : MonoBehaviour
{
    public Transform thirdPersonT;
    public float thirdDistance;
    public Transform ZlockT;
    public float zDistance;



    public LayerMask obLayer; //set to obstacle & any other layers that shouldn't be allowed to block the player!
    float minDistance = 1.0f;
    float maxDistance = 8.0f;
    float smooth = 10;
    Vector3 zdirection;
    Vector3 thirdDirection;
    Vector3 direction;
    float distance;
    

    // Start is called before the first frame update
    void Start()
    {
        thirdDistance = Vector3.Distance(transform.position, transform.parent.position);
        //Debug.Log("thirdDistance = " + thirdDistance);
        zDistance = Vector3.Distance(transform.parent.position, ZlockT.position);
        Debug.Log("zDistance = " + zDistance);

        zdirection = ZlockT.transform.localPosition.normalized;
        //Debug.Log("zdirection = " + zdirection);
        thirdDirection = transform.localPosition.normalized;
        //Debug.Log("thirdDirection = " + thirdDirection);

        distance = thirdDistance;
        direction = thirdDirection;
    }

    void Update()
    {
        //Debug.Log("desired distance is: " + distance);
        Vector3 desiredCamPos = transform.parent.TransformPoint(direction * maxDistance);
        RaycastHit hit;

        if (Physics.Linecast(transform.parent.position, desiredCamPos, out hit, obLayer))
        {
            if (!hit.collider.GetComponent<CarryMe>())
            {
                distance = Mathf.Clamp((hit.distance * .9f), minDistance, maxDistance);
            }
            else
            { distance = maxDistance; }
        }
        else
        {
            distance = maxDistance;
        }
        transform.localPosition = direction * distance;
    }
    
    public void Start3rd()
    {
        maxDistance = thirdDistance;
        direction = thirdDirection;
    }
    public void StartZ()
    {
        zDistance = Vector3.Distance(transform.parent.position, ZlockT.position);
        maxDistance = zDistance;
        direction = zdirection;
    }
}
