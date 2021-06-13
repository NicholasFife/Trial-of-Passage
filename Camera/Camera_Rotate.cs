using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//This script goes on the CameraPivot
public class Camera_Rotate : MonoBehaviour
{
    private Gamepad gamepad;
    private GameObject Player;
    private float rotY = 0; //holds the target rotation value for y
    private float rotX = 0; //holds the target rotation value for x

    private static float LookSense = 1f;//Sets the starting sensitivity it scales between .1 and 2;

    private float PlayerX; //should be set on start according to how the camera is positioned as "default" angle.
    private float PlayerY; //should update at runtime so camera has a reference for the y value to snap to.
    private float camSmoothSpeed = 1.0f;
    private bool rotCam;
    private float speedModifier = 1f;

    public bool useGamepad = true;
    private Animator anim;

    public bool useme = true;
    // Start is called before the first frame update
    void Start()
    {
        Player = FindObjectOfType<Control_Player>().gameObject;
        anim = transform.parent.GetChild(0).GetComponent<Animator>();
        Vector3 rot = transform.localRotation.eulerAngles;
        gamepad = Gamepad.current;
        //Set beginning rotation
        rotY = rot.y;
    }
    void Update()
    {
        if (rotCam)
        {
            CalculateWalkRotation();
            PlayerLook();
        }
        if (gamepad != null && useGamepad)
        {
            ManualLookRotation();
        }
    }
    void FixedUpdate()
    {
        
    }

    void ManualLookRotation()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        float rotX = rot.x;
        float rotY = rot.y;
        float lookX = 0;
        float lookY = 0;


        if (gamepad.rightStick.IsActuated())
        {
            lookX = gamepad.rightStick.y.ReadValue() * -1;
            lookY = gamepad.rightStick.x.ReadValue();
        }
        
        if(anim.GetBool("isRolling") == true)
        {
            lookX = lookX * .5f;
            lookY = lookY * .5f;
        }

        rotX += lookX * Time.deltaTime * 100 * LookSense;
        rotY += lookY * Time.deltaTime * 100 * LookSense;
        Quaternion newRotation = Quaternion.Euler(rotX, rotY, 0);
        //Quaternion smoothedRotation = Quaternion.Lerp(transform.rotation, newRotation, camSmoothSpeed * Time.deltaTime * speedModifier);
        //Quaternion finalSmoothed = Quaternion.Euler(smoothedRotation.x, smoothedRotation.y, 0);
        //Debug.Log("New manually set camera rotation is: " + finalSmoothed);
        transform.rotation = newRotation;
    }

    void CalculateWalkRotation()
    {
        PlayerY = Player.transform.eulerAngles.y;
        rotY = PlayerY;
    }

    //lerps camera rotation while player is moving
    public void PlayerLook()
    {
        
        if (anim.GetBool("isRolling") == false)
        {
            Quaternion newRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, rotY, transform.rotation.eulerAngles.z);
            Quaternion smoothedRotation = Quaternion.Lerp(transform.rotation, newRotation, camSmoothSpeed * Time.deltaTime * speedModifier);
            Quaternion finalSmoothed = Quaternion.Euler(smoothedRotation.x, smoothedRotation.y, 0);
            transform.rotation = smoothedRotation;
        }

    }

    //Called from Control_Player. True: Camera rotates on LateUpdate False: Camera does not rotate
    public void RotateCam(bool a)
    {
        rotCam = a;
    }

    public void SetLookSense(float a)
    {
        LookSense = a;
    }
    public float GetLookSense()
    {
        return LookSense;
    }
}
