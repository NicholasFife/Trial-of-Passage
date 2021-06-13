using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity;

//This script has been updated to accomodate the new input API with or without a gamepad.
public class Control_Zlock : MonoBehaviour
{
    private Gamepad gamepad;
    private Keyboard keyboard;
    
    private Control_Player player;
    private Entity_Player enplay;
    private Control_Actions ActionButton;
    private GameObject SwordBody;
    private ZTarget[] Lockables;
    public LayerMask obLayer;
    [SerializeField]
    private GameObject targeted;
    private int arrayLoc;
    public bool targeting;
    private GameObject reticle;
    private Camera cam;
    private Camera_Perspectives camPer;
    public ZSling zShot;
    public GameObject tShot;
    public GameObject zShotOb;
    private Control_Inventory inv;
    Animator anim;
    

    public float savedDistance;
    public float testDistance;

    public float maxDistance = 40f;

    bool SecondPass;
    
    // Start is called before the first frame update
    void Awake()
    {
        Lockables = FindObjectsOfType<ZTarget>();
        //reticle = FindObjectOfType<Reticle>().gameObject;
        //reticle.SetActive(false);
        cam = FindObjectOfType<Camera>();
        camPer = FindObjectOfType<Camera_Perspectives>();
        SwordBody = GetComponentInChildren<Sword>().gameObject;
        ActionButton = GetComponent<Control_Actions>();
        zShotOb = zShot.gameObject;
        zShotOb.SetActive(false);
        anim = GetComponent<Animator>();
        player = GetComponent<Control_Player>();
        inv = GetComponent<Control_Inventory>();
        gamepad = Gamepad.current;
        keyboard = Keyboard.current;
        enplay = GetComponent<Entity_Player>();
    }

    // Update is called once per frame
    void Update()
    {
        //prevent Zlock while pushing or in first person mode
        if (ActionButton.curState == Control_Actions.ButtonState.Push && ActionButton.itemInHands == ActionButton.selected)
        { return; }
        if (camPer.isFirst)
        { return; }
        
        if ((keyboard.tabKey.wasPressedThisFrame || gamepad?.leftShoulder.wasPressedThisFrame == true) && !player.stunned && anim.GetBool("isRolling") == false)
        {
            //Debug.Log("L1 was pressed. Creating Target Array");
            CreateTargetArray();
            camPer.StartZ(true);
            

        }
        else if (keyboard.tabKey.wasReleasedThisFrame || player.stunned || gamepad?.leftShoulder.wasReleasedThisFrame == true)
        {
            if (targeted != null)
            { targeted?.GetComponentInParent<Entity_Enemy>()?.TellZTarget(false); }

            targeted = null;
            testDistance = maxDistance;
            //reticle.SetActive(false);
            if (zShotOb.activeInHierarchy)
            {
                zShotOb.gameObject.SetActive(false); //turns the slingshot off
                anim.SetBool("isPistol", false);
                ActionButton.itemInHands = null;
            }

        }


        if (keyboard.eKey.wasPressedThisFrame && keyboard.tabKey.isPressed || gamepad?.leftShoulder.isPressed == true && gamepad?.leftTrigger.wasPressedThisFrame == true)
        {
            //Debug.Log("Next Target is called from update");
            NextTarget();
        }
        if (targeted != null && targeted.activeInHierarchy)
        {
            FaceTarget();
        }


        if ((keyboard.numpad5Key.wasPressedThisFrame || gamepad?.dpad.right.wasPressedThisFrame == true || gamepad?.rightTrigger.wasPressedThisFrame == true) && targeted != null && inv.GetAmmo() > 0 || (keyboard.numpad6Key.isPressed || gamepad?.dpad.down.isPressed == true) && targeted != null && inv.GetAmmo() > 2 && inv.CheckTriple())
        {

            if (ActionButton.itemInHands != zShotOb)
            {
                if (ActionButton.curState == Control_Actions.ButtonState.isCarrying)
                { ActionButton.CarryDetach(); }//Detaches rock if it is there.
                if (ActionButton.itemInHands == SwordBody)
                { ActionButton.EmptyHands(); }//puts away sword and shield if they're there.
                Debug.Log("Turning on ZPistol");

                zShotOb.gameObject.SetActive(true); //turns the slingshot on
                anim.SetBool("isPistol", true);
                ActionButton.itemInHands = zShotOb;
            }
            else
            {
                Debug.Log("Zshot is readying");
                zShot.Shoot(targeted);
            }
            if (keyboard.numpad5Key.wasPressedThisFrame || gamepad?.dpad.right.wasPressedThisFrame == true || gamepad?.rightTrigger.wasPressedThisFrame == true)
            {
                //tShot.SetActive(false);
                zShot.tOn = false;
            }
            else if (keyboard.numpad6Key.wasPressedThisFrame || gamepad?.dpad.down.wasPressedThisFrame == true)
            {
                //tShot.SetActive(true);
                zShot.tOn = true;
            }

        }

    }
    
    public void FirstTarget()
    {
        savedDistance = maxDistance;

        for (int i = 0; i <= Lockables.Length - 1; i++)
        {
            Debug.Log("Checking Lockables[" + i + "]");
            RaycastHit hit;
            testDistance = Vector3.Distance(transform.position, Lockables[i].transform.position);

            if (testDistance < savedDistance && !Physics.Linecast(transform.position, Lockables[i].transform.position, out hit, obLayer) && Lockables[i].transform.parent.GetComponent<Renderer>().isVisible)
            {
                targeted = Lockables[i].gameObject;
                savedDistance = testDistance;
                arrayLoc = i;
                targeted.GetComponentInParent<Entity_Enemy>().TellZTarget(true);
                camPer.SetCurTarget(targeted.transform);
                //return;
            }
        }
        //Debug.Log("No valid lock-on Target found");
    }
    public void NextTarget()
    {
        arrayLoc++;
        if (arrayLoc > Lockables.Length -1)
        { arrayLoc = 0; }
        for (int i = arrayLoc; i <= Lockables.Length - 1; i++)
        {
            //Debug.Log("Checking Lockables[" + i + "]");
            RaycastHit hit;
            if (Lockables[i] != null)
            {
                testDistance = Vector3.Distance(transform.position, Lockables[i].transform.position);

                if (!Physics.Linecast(transform.position, Lockables[i].transform.position, out hit, obLayer) && Lockables[i].transform.parent.GetComponent<Renderer>().isVisible)
                {
                    SecondPass = false;
                    targeted = Lockables[i].gameObject;
                    arrayLoc = i;
                    targeted.GetComponentInParent<Entity_Enemy>().TellZTarget(true);
                    camPer.SetCurTarget(targeted.transform);
                    return;
                }
            }
        }

        if (!SecondPass)
        {
            SecondPass = true;
            CreateTargetArray();
            NextTarget();
        }
    }
    //Makes sure the player gameObject is always facing toward the targeted gameObject on the x and z axis
    public void FaceTarget()
    {
        if (enplay.CheckLOS(targeted) == false)
        {
            NextTarget();
        }
        Vector3 rotPlayer = new Vector3(targeted.transform.position.x, transform.position.y, targeted.transform.position.z);
        Quaternion Zlook = Quaternion.Euler(rotPlayer.x, rotPlayer.y, rotPlayer.z);
        Quaternion smoothedLook = Quaternion.Lerp(transform.rotation, Zlook, 5 * Time.deltaTime);
        transform.LookAt(rotPlayer);
    }

    //This should be called when first locking on, and every time the locked-on enemy is killed. 
    public void CreateTargetArray()
    {
        camPer.ReleaseCurTarget();
        //Debug.Log("Creating Target Array");
        if (targeted != null)
        { targeted.GetComponentInParent<Entity_Enemy>()?.TellZTarget(false); }

        Lockables = FindObjectsOfType<ZTarget>();
        //FirstTarget(); Moving... somewhere else. Probably call from the camera itself
    }

    //Building in support for older scripts
    public void RefreshTargets()
    {
        CreateTargetArray();
    }
    //returns the transform of the currently targeted gameObject
    public Transform GetTransform()
    {
        return targeted.transform;
    }
    //returns if the player is currently targeting something
    public bool isTargeting()
    {
        if(targeted != null)
        { return true; }
        return false;
    }
    //returns the targeted object
    public GameObject getTarget()
    {
        return targeted;
    }
}
