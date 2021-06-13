using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Camera_Perspectives : MonoBehaviour
{
    float ManualSensitivity = 100f;
    public static float playerSensitivity = 1f;

    private Keyboard keyboard;
    private Gamepad gamepad;

    private GameObject player;
    private Control_Player playerController;
    private Control_Zlock zController;
    public Transform thirdPerson;
    public Transform firstPerson;
    public Transform zLocation;
    public Transform pivotAngle;
    public Transform Cam;
    private Camera_Distance thirdDistance;
    private Camera_Rotate thirdRotate;
    private Camera_Follow camFollow;
    public Slingshot sshot;
    public Camera ActualCam;

    bool FirstTargetDelay = false;

    public bool isFirst = false;
    public bool isZlock = false;
    private bool isObjectiveCam = false;
    private bool isEndCam = false;

    private Vector3 returnRot;
    private Vector3 returnPos;
    private Transform EndTran;
    private Quaternion firstCamRot;
    private Quaternion zCamRot;
    private Quaternion thirdCamRot;

    private Quaternion oRotation;
    private Vector3 oPosition;

    private Transform curTarget;

    private float rotY = 0; //holds the target rotation value for y
    private float rotX = 0; //holds the target rotation value for x

    private float senseX = 10;
    private float senseY = 10;

    private float baseSensitivity = 3; //We should set up a slider at some point and scale it between 1 and 10?

    private float clampAngleMin = -60;
    private float clampAngleMax = 40;

    private float smoothSpeed = 10;
    private float highlightSmooth = 2.5f;

    public bool HasFocusObject = false;
    public GameObject FocusObject;

    // Start is called before the first frame update
    void Start()
    {
        playerController = transform.parent.GetComponentInChildren<Control_Player>();
        player = playerController.gameObject;
        zController = player.GetComponent<Control_Zlock>();

        thirdDistance = GetComponentInChildren<Camera_Distance>();
        thirdRotate = GetComponent<Camera_Rotate>();
        camFollow = GetComponent<Camera_Follow>();
        ActualCam = thirdDistance.GetComponent<Camera>();
        firstCamRot = Quaternion.Euler(5, 0, 0);
        zCamRot = Quaternion.Euler(25, 0, 0);
        thirdCamRot = Quaternion.Euler(25, 0, 0);
        keyboard = Keyboard.current;
        gamepad = Gamepad.current;
    }

    // Update is called once per frame
    void Update()
    {
        if(isFirst)
        {
            FirstCamWASD();
            FirstRotate();
        }
        else if (isZlock)
        {
            if (curTarget != null)
            { TargetingZ(); }
        }
        else if (isObjectiveCam)
        {
            HighlightCam();
        }
        else if (isEndCam)
        {
            FollowAirship();
        }
        if (FirstTargetDelay)
        {
            FirstTargetDelay = false;
            zController.FirstTarget();
        }
    }

    void FixedUpdate()
    {

    }
    public void StartFirst()
    {
        //Debug.Log("entering first person");

        returnRot = Cam.localEulerAngles; //saves the camera rotation
        returnPos = Cam.position; //saves the camera position

        thirdDistance.enabled = false; //disables the script controlling camera distance
        thirdRotate.enabled = false; //disables the script controlling camera rotation

        Cam.position = firstPerson.position; //uses an empty gameobject as a reference point to set its new position
        Cam.localRotation = firstCamRot;

        Vector3 rot = player.transform.eulerAngles; //grab the player's rotation
                                                    //Set beginning values for rotation
        rotY = rot.y;
        rotX = rot.x;

        isFirst = true; //turns on this script's controls
        isZlock = false;

    }
    public void StartZ(bool islock)
    {
        isFirst = false;
        zLocation = FindObjectOfType<zpos>().gameObject.transform;
        //Debug.Log("entering Zlock position");
        returnRot = Cam.rotation.eulerAngles;
        returnPos = Cam.position;
        thirdDistance.StartZ();
        thirdRotate.enabled = false;
        transform.rotation = zLocation.rotation;
        Cam.position = zLocation.position;
        Cam.localRotation = zCamRot; //change pivot's rotation

        isZlock = true;

        if(islock == true)
        {
            FirstTargetDelay = true;
            
        }

    }
    public void StartThird()
    {
        //Debug.Log("entering third person");
        thirdDistance.enabled = true;
        camFollow.enabled = true;
        thirdDistance.Start3rd();
        thirdRotate.enabled = true;

        transform.rotation = pivotAngle.rotation;
        Cam.localRotation = thirdCamRot;
        Cam.position = thirdPerson.position;
        
        isFirst = false;
        isZlock = false;
        isObjectiveCam = false;

        if (sshot.isActiveAndEnabled)
        {
            sshot.PutAway();
        }
    }

    public void StartHighlight(GameObject o)
    {
        //Debug.Log("Entering Highlight Cam");

        isFirst = false;
        isZlock = false;
        isObjectiveCam = true;

        thirdDistance.enabled = false;
        thirdRotate.enabled = false;
        camFollow.enabled = false;

        oRotation = o.transform.rotation;
        oPosition = o.transform.position;
    }

    public void StartEndSequence(GameObject a)
    {
        FocusObject = a;
        Debug.Log("FocusObject set as: " + FocusObject);
        isFirst = false;
        isZlock = false;
        isObjectiveCam = false;
        isEndCam = true;

        thirdDistance.enabled = false; //disables the script controlling camera distance
        thirdRotate.enabled = false; //disables the script controlling camera rotation
        transform.rotation = pivotAngle.rotation;
        Cam.localRotation = thirdCamRot;
        Cam.position = thirdPerson.position;
    }

    public void StartEndSequence(GameObject a, Transform b)
    {
        FocusObject = a;
        Debug.Log("FocusObject set as: " + FocusObject);
        isFirst = false;
        isZlock = false;
        isObjectiveCam = false;
        isEndCam = true;

        thirdDistance.enabled = false; //disables the script controlling camera distance
        thirdRotate.enabled = false; //disables the script controlling camera rotation
        transform.rotation = pivotAngle.rotation;
        Cam.localRotation = thirdCamRot;
        EndTran = b;
    }

    private void HighlightCam()
    {
        Quaternion smoothedRotation = Quaternion.Lerp(Cam.rotation, oRotation, highlightSmooth * Time.deltaTime);

        Cam.rotation = smoothedRotation;

        Vector3 smoothedPosition = Vector3.Lerp(Cam.position, oPosition, highlightSmooth * Time.deltaTime);
        Cam.transform.position = smoothedPosition;
        
    }

    public void FirstCamWASD()
    {
        if (gamepad != null)
        {
            rotX += gamepad.rightStick.y.ReadValue() * -1 * Time.deltaTime * ManualSensitivity * playerSensitivity;
            rotY += gamepad.rightStick.x.ReadValue() * Time.deltaTime * ManualSensitivity * playerSensitivity;


            //keyboard support
            if (keyboard.wKey.isPressed)
            {
                rotX -= Time.deltaTime * senseX * baseSensitivity;
            }
            else if (keyboard.sKey.isPressed)
            {
                rotX += Time.deltaTime * senseX * baseSensitivity;
            }
            if (keyboard.aKey.isPressed)
            {
                rotY -= Time.deltaTime * senseY * baseSensitivity;
            }
            else if (keyboard.dKey.isPressed)
            {
                rotY += Time.deltaTime * senseY * baseSensitivity;
            }
        }
        else
        {
            if (keyboard.wKey.isPressed)
            {
                rotX -= Time.deltaTime * senseX * baseSensitivity;
            }
            else if (keyboard.sKey.isPressed)
            {
                rotX += Time.deltaTime * senseX * baseSensitivity;
            }
            if (keyboard.aKey.isPressed)
            {
                rotY -= Time.deltaTime * senseY * baseSensitivity;
            }
            else if (keyboard.dKey.isPressed)
            {
                rotY += Time.deltaTime * senseY * baseSensitivity;
            }
        }
    }

    private void FirstRotate()
    {
        rotX = Mathf.Clamp(rotX, clampAngleMin, clampAngleMax);

        Quaternion newRotation = Quaternion.Euler(rotX, rotY, 0);
        Quaternion smoothedRotation = Quaternion.Lerp(transform.rotation, newRotation, smoothSpeed * Time.deltaTime);
        
        transform.rotation = smoothedRotation;

        Quaternion playerRot = Quaternion.Euler(player.transform.eulerAngles.x, rotY, 0);
        player.transform.rotation = playerRot;

        Cam.position = firstPerson.position;
    }

    void FollowAirship()
    {

        Vector3 smoothedPosition = Vector3.Lerp(Cam.position, EndTran.position, highlightSmooth * Time.deltaTime * .5f);
        Cam.position = smoothedPosition;

        Quaternion smoothedRotation = Quaternion.Lerp(Cam.rotation, EndTran.rotation, highlightSmooth * Time.deltaTime * .5f);
        Cam.rotation = smoothedRotation;

    }

    private void TargetingZ()
    {
        transform.LookAt(curTarget);
    }
    
    public void SetSensitivity(float a)
    {
        playerSensitivity = a;
    }
    public float GetSensitivity()
    {
        return playerSensitivity;
    }

    public void SetCurTarget(Transform t)
    {
        curTarget = t;
    }
    public void ReleaseCurTarget()
    {
        curTarget = null;
    }


}
