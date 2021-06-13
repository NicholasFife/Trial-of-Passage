using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script goes on the CameraPivot
public class Camera_Follow : MonoBehaviour
{
    private GameObject Player;

    private Vector3 moveLoc; //Holds the player's position for following
    private float moveSmoothSpeed = 10f; //Set camera follow speed between 0 (slow) and 1 (fast)
    Vector3 smoothedPosition;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Player = FindObjectOfType<Control_Player>().gameObject;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        
        FollowPlayer();
    }
    
    void FollowPlayer()
    {
        moveLoc = Player.transform.position;
        smoothedPosition = Vector3.Lerp(transform.position, moveLoc, moveSmoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }


}
