/*
Made with work from:
Brackeys. (2020, May 24). THIRD PERSON MOVEMENT in Unity [Video File]. Retrieved from https://www.youtube.com/watch?v=4HpC--2iowE

Brackeys. (2019, October 27). FIRST PERSON MOVEMENT in Unity - FPS Controller [Video File]. Retrieved from https://youtu.be/_QajrabyTJc
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

//Updated for new input system with sorting for if a gamepad is connected.
public class Control_Player : MonoBehaviour
{
    private Gamepad gamepad;
    private Keyboard keyboard;

    private CharacterController player;
    private Animator anim;

    //Gravity and jumping
    private float gravity = -20f;
    public Vector3 moveY;
    public Transform groundCheck;
    public float groundDistance = .05f;
    public LayerMask comboMask;
    public bool onGround = true;
    public float jumpHeight = 1f;
    public PoleSwing pole;
    //Jump cooldown is disabled by setting max to 0. This should be removed entirely if we like how it feels after a few days. 
    private bool isJumpCool;
    private float jumpCoolMax = 0;
    private float jumpCoolCount;

    private Vector3 lastPos;
    private Vector3 curPos;
    private Vector3 momentum;

    private Camera_Rotate cam;
    private Camera_Perspectives camChanger;
    private Transform camPos;
    private Control_Actions ActionButton;
    private float moveSpeed = 9;

    private float rotY = 0;
    private Vector3 CameraPosition;
    public bool isFirstPerson;

    private float turnSmoothTime = 0.05f;
    float turnsmoothVelocity;

    public bool busy; //is the character already doing something with their hands?
    private bool isWalking;

    public bool stunned = false;
    private float stunTime;
    private float stunTimeMax = 1;

    public bool zLocked = false; //When Tab is pressed, camera returns to behind the player & player movements should be locked

    private Control_Inventory inv;

    //Items list
    private GameObject SwordBody;
    private Sword swordScript;
    private Control_Shield cntlShield;
    public Slingshot sShot;
    public GameObject tShot;
    public bool isTriple;
    public GameObject bomb; //just used to identify if this is what's in player's hands 
    public bool flutterFalling = false;

    //Audio list
    public AudioSource a1;
    public AudioSource a2;
    public AudioSource a3;
    public AudioSource a4; //Jump sound

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<CharacterController>();
        inv = GetComponent<Control_Inventory>();
        anim = GetComponent<Animator>();
        cam = FindObjectOfType<Camera_Rotate>();
        camPos = cam.transform;
        camChanger = cam.GetComponent<Camera_Perspectives>();
        swordScript = GetComponentInChildren<Sword>();
        ActionButton = GetComponent<Control_Actions>();
        SwordBody = swordScript.gameObject;
        SwordBody.SetActive(false);
        cntlShield = FindObjectOfType<Control_Shield>();
        //stores player starting rotation
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.y;

        var swordSounds = GetComponents<AudioSource>();
        a1 = swordSounds[0];
        a2 = swordSounds[1];
        a3 = swordSounds[2];

        if (Gamepad.current != null)
        {
            gamepad = Gamepad.current;
            Debug.Log("Gamepad : " + Gamepad.current.name);
        }
        keyboard = Keyboard.current;
    }

    // Update is called once per frame
    void Update()
    {

        if (isJumpCool)
        { CoolJump(); }

        if (anim.GetBool("isClimbing"))
        { return; }

        if (anim.GetBool("isSpinning") || anim.GetBool("isSpinJump"))
        { return; }

        if ((ActionButton.curState == Control_Actions.ButtonState.Push && ActionButton.itemInHands == ActionButton.selected) || (ActionButton.curState == Control_Actions.ButtonState.Climb && ActionButton.itemInHands == ActionButton.selected))
        { return; }


        if ((keyboard.tabKey.wasPressedThisFrame || gamepad?.leftShoulder.wasPressedThisFrame == true) && zLocked == false && !isFirstPerson && !stunned && !busy && anim.GetBool("isRolling") == false)
        {
            anim.SetBool("isWalking", false);
            anim.SetBool("isLockStepping", true);
            zLocked = true;
            //Debug.Log("I'm zlocked");
        }
        else if ((keyboard.tabKey.wasReleasedThisFrame || gamepad?.leftShoulder.wasReleasedThisFrame == true) && zLocked == true)
        {
            if (ActionButton.curState == Control_Actions.ButtonState.ZJump)
            {
                ActionButton.ForceReadyNone();
            }
            anim.SetBool("isLockStepping", false);
            zLocked = false;
            //Debug.Log("I'm not zlocked");
            camChanger.StartThird();
        }
        else if (stunned && zLocked == true)
        {
            anim.SetBool("isLockStepping", false);
            zLocked = false;
            //Debug.Log("I'm not zlocked");
            camChanger.StartThird();
        }

        if (!isFirstPerson) //1st person camera controls found on Camera_Perspectives.cs
        {

            if (!stunned && !busy)
            {
                WASDmove();//handles player movement input
                Hands();//handles player action input
            }

            else
            { StunCount(); }
        }
        if ((keyboard.numpad3Key.wasPressedThisFrame || gamepad?.buttonWest.wasPressedThisFrame == true) && isFirstPerson == true)
        {
            isFirstPerson = false;
            camChanger.StartThird();
        }
    }

    public void NotOnGround()
    {
        onGround = false;
    }

    public bool AmIOnGround()
    { return onGround; }

    void StartJumpCool()
    {
        isJumpCool = true;
        jumpCoolCount = jumpCoolMax;
    }

    void CoolJump()
    {
        if (jumpCoolCount >= 0)
        { jumpCoolCount -= Time.deltaTime; }
        else
        { isJumpCool = false; }
    }

    public void ApplyGravity()
    {
        if (flutterFalling)
        { return; }
        if (onGround && moveY.y < 0) //Drags player that tiny bit further down to touch the ground while stabilizing their grounded downward velocity.
        {
            moveY.y = -2f;
        }

        if (Physics.CheckSphere(groundCheck.position, groundDistance, comboMask, QueryTriggerInteraction.Ignore))//If you're touching the ground
        {
            if (onGround) //You are not jumping and were not jumping
            { onGround = true; }
            else if (!onGround && moveY.y < 0)
            {
                anim.SetBool("isJump", false);
                onGround = true;
                StartJumpCool();
                ActionButton.StartCool();
            }

        }
        else//If you were already jumping
        {
            onGround = false;
        }
        if (onGround == true && !isJumpCool && anim.GetBool("isRolling") == false)//if you've just left the ground
        {
            if ((keyboard.spaceKey.wasPressedThisFrame || gamepad?.buttonNorth.wasPressedThisFrame == true) && ActionButton.curState != Control_Actions.ButtonState.ZJump)
            {
                
                Jump();
                onGround = false;
            }
            else if (keyboard.spaceKey.wasPressedThisFrame || gamepad?.buttonNorth.wasPressedThisFrame == true) //Action button is Zjump
            {
                ActionButton.ButtonPressed();
            }

        }

        if (!onGround && ActionButton.itemInHands != null && ActionButton.itemInHands.GetComponent<FlutterFall>() && moveY.y < 0)
        {
            Debug.Log("Activating Flutterfall");
            flutterFalling = true;
            return;
        }


        moveY.y += gravity * Time.deltaTime;
        player.Move(moveY * Time.deltaTime);
        
    }

    public void Jump()
    {
        moveY.y = Mathf.Sqrt(jumpHeight * -1.5f * gravity);
        anim.SetBool("isJump", true);
        a4.Play();
        onGround = false;
    }

    //Movement Controls here
    void WASDmove()
    {
        lastPos = transform.position;


        if (!flutterFalling)
        {
            ApplyGravity();
        }
        float moveX = 0;
        float moveZ = 0;
        float str = 0;

        //Takes player input and stores it
        if (gamepad != null)
        {
            moveX = gamepad.leftStick.x.ReadValue();
            moveZ = gamepad.leftStick.y.ReadValue();
            str = gamepad.leftStick.EvaluateMagnitude();
        }

        //Keyboard Input
        if (keyboard.wKey.isPressed)
        { str = 1; moveZ = 1; }
        else if (keyboard.sKey.isPressed)
        { str = 1; moveZ = -1; }
        if (keyboard.dKey.isPressed)
        { str = 1; moveX = 1; }
        else if (keyboard.aKey.isPressed)
        { str = 1; moveX = -1; }

        //Send values to blend
        if (gamepad.leftShoulder.isPressed)
        { anim.SetBool("isLockStepping", true); }
        else
        { anim.SetBool("isLockStepping", false); }

        anim.SetFloat("ZMove", moveZ);
        anim.SetFloat("XMove", moveX);
        anim.SetFloat("inputSTR", str);
        Vector3 moveMe = new Vector3(moveX, 0, moveZ);
        //Debug.Log("Combined movement input Vector3: " + moveMe);

        if (moveMe.magnitude >= 0.1f && anim.GetBool("isUp") == false || moveMe.magnitude >= 0.1f && zLocked)
        {
            //Stops camera from rotating if you're moving toward it. Causes it to rotate otherwise
            if (moveMe.z < 0 && moveMe.x > -.1 && moveMe.x < .1)
            { cam.RotateCam(false); }
            else
            { cam.RotateCam(true); }

            if (!zLocked && anim.GetBool("isRolling") == false) //Normal player movement
            {
                //Controls player rotation
                float targetRotation = Mathf.Atan2(moveMe.x, moveMe.z) * Mathf.Rad2Deg + camPos.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnsmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                //Controls player movement
                Vector3 moveDir = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
                player.Move(moveDir * moveSpeed * Time.deltaTime * str);

                if (onGround)
                { anim.SetBool("isWalking", true); }
                else
                { anim.SetBool("isWalking", false); }
            }

            else if (anim.GetBool("isRolling") == true)
            {
                //Controls player rotation
                float targetRotation = Mathf.Atan2(moveMe.x, moveMe.z) * Mathf.Rad2Deg + camPos.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnsmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                //Controls player movement
                Vector3 moveDir = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
                player.Move(moveDir * moveSpeed * Time.deltaTime * str * 1.5f);

                if (onGround)
                { anim.SetBool("isWalking", true); }
                else
                { anim.SetBool("isWalking", false); }
            }

            else //Tab is held down to focus camera forwards
            {
                float targetRotation = Mathf.Atan2(moveMe.x, moveMe.z) * Mathf.Rad2Deg + player.transform.eulerAngles.y;
                Vector3 moveDir = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
                if (anim.GetBool("isUp") == false)
                { player.Move(moveDir * moveSpeed * Time.deltaTime); }
                else
                {
                    player.Move(moveDir * moveSpeed * Time.deltaTime / 2);
                }
                if (ActionButton.curState != Control_Actions.ButtonState.ZJump)
                { ActionButton.EnterZjump(); }

                if (onGround)
                { anim.SetBool("isWalking", true); }
                else
                { anim.SetBool("isWalking", false); }
            }

            //Controls Action button
            if (ActionButton.curState != Control_Actions.ButtonState.Roll)
            { ActionButton.TryReadyRoll(); }

        }
        //Stops the camera from rotating around the player when they aren't moving
        else
        {
            cam.RotateCam(false);
            anim.SetBool("isWalking", false);

            //Resets action button
            if (ActionButton.curState == Control_Actions.ButtonState.Roll)
            { ActionButton.ForceReadyNone(); }
        }

        curPos = transform.position;
    }

    void RollMove()
    { }

    //Player actions triggered from here
    void Hands()
    {

        if (!onGround || anim.GetBool("isRolling") == true)
        { return; }
        if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame || gamepad?.buttonEast.wasPressedThisFrame == true || gamepad?.buttonWest.wasPressedThisFrame == true)
        {
            if (keyboard.numpad1Key.wasPressedThisFrame || gamepad?.buttonEast.wasPressedThisFrame == true)
            {
                Debug.Log("Sword button pressed");
                DrawSword();
                Debug.Log("DrawSword() has run");
                SwingSword();
                Debug.Log("SwingSword() has run");
                //moved playing sound effect to sword.cs

            }
            if ((keyboard.numpad3Key.wasPressedThisFrame || gamepad?.buttonWest.wasPressedThisFrame == true) && anim.GetBool("isSwordChop") == false)
            {
                cntlShield.RaiseShield();
            }
        }
        else if (keyboard.numpad2Key.wasPressedThisFrame || gamepad?.buttonSouth.wasPressedThisFrame == true)
        {
            ActionButton.ButtonPressed();
        }
        //Activates slingshot and sets state.
        else if ((keyboard.numpad5Key.wasPressedThisFrame && !keyboard.tabKey.isPressed) || (gamepad?.dpad.right.wasPressedThisFrame == true && gamepad?.leftShoulder.isPressed == false) || (gamepad?.rightTrigger.wasPressedThisFrame == true && gamepad?.leftShoulder.isPressed == false))
        {
            if (ActionButton.curState == Control_Actions.ButtonState.isCarrying)
            { ActionButton.CarryDetach(); }//Detaches rock if it is there.

            if (ActionButton.itemInHands == SwordBody)
            { ActionButton.EmptyHands(); }//puts away sword and shield if they're there.
            anim.SetBool("isWalking", false);
            sShot.gameObject.SetActive(true); //turns the slingshot on
            isFirstPerson = true; //DISABLES NORMAL INPUT - CAM HANDLED IN CAMERA_PERSPECTIVES SHOOTING AND REVERT HANDLED IN SLINGSHOT
            ActionButton.curState = Control_Actions.ButtonState.PutAway; //Sets action button state to put away
            ActionButton.PriorOverride(); //sets priority of state to 5
            camChanger.StartFirst();
            //The action button should now be locked to a state that will put away the slingshot when pressed. 
            //Any buttons that we want to revert the player back to normal state 
            //can be added to the slingshot function where this is handeled
            //as the only time they'll receive input is when the player is in this mode and wants to revert back
        }
        //same as above but for the tripleshot instead
        else if ((keyboard.numpad6Key.wasPressedThisFrame && !keyboard.tabKey.isPressed) && inv.CheckTriple() || (gamepad?.dpad.down.isPressed == true && gamepad?.leftShoulder.isPressed == false) && inv.CheckTriple())
        {
            if (ActionButton.curState == Control_Actions.ButtonState.isCarrying)
            { ActionButton.CarryDetach(); }//Detaches rock if it is there.

            if (ActionButton.itemInHands == SwordBody)
            { ActionButton.EmptyHands(); }//puts away sword and shield if they're there.
            anim.SetBool("isWalking", false);
            sShot.gameObject.SetActive(true); //turns the slingshot on
            isFirstPerson = true; //DISABLES NORMAL INPUT - CAM HANDLED IN CAMERA_PERSPECTIVES SHOOTING AND REVERT HANDLED IN SLINGSHOT
            ActionButton.curState = Control_Actions.ButtonState.PutAway; //Sets action button state to put away
            ActionButton.PriorOverride(); //sets priority of state to 5
            camChanger.StartFirst();
            //The action button should now be locked to a state that will put away the slingshot when pressed. 
            //Any buttons that we want to revert the player back to normal state 
            //can be added to the slingshot function where this is handeled
            //as the only time they'll receive input is when the player is in this mode and wants to revert back
        }
        else if (keyboard.numpad4Key.wasPressedThisFrame || gamepad?.dpad.left.wasPressedThisFrame == true || gamepad?.rightShoulder.wasPressedThisFrame == true)
        {
            //Debug.Log("Button for bombs was pressed");
            if (ActionButton.curState == Control_Actions.ButtonState.isCarrying)
            {
                ActionButton.ButtonPressed();
                return;
            }

            if (inv.GetBombs() > 0 && ActionButton.curState != Control_Actions.ButtonState.DrawBomb)
            {
                ActionButton.DrawBomb();
                //inv.MinusBomb();
            }
            else
            {
                //play error sound for no bombs left
            }
        }

        if (!keyboard.numpad3Key.isPressed && (gamepad == null || gamepad?.buttonWest.isPressed == false))
        {
            cntlShield.LowerShield();
        }
        

    }

    public void DrawSword()
    {
        //Carrying a rock? - have to check separately because detaching is a different process from setting inactive.
        if (ActionButton.itemInHands != null && ActionButton.itemInHands.GetComponentInChildren<CarryMe>())
        {
            //Debug.Log("Dropping carried item");
            DropHands();
        }
        //This check is for equipment pieces. 
        //OTHER THINGS THAT ONLY TEMPORARILY ATTACH MUST BE ACCOUNTED FOR ABOVE 
        //OR DISABLE THIS Hands() METHOD
        else if (ActionButton.itemInHands != null && SwordBody)
        {
            ActionButton.itemInHands.SetActive(false);
            ActionButton.itemInHands = null;
        }
        if (!SwordBody.activeSelf)
        {
            //Debug.Log("swinging sword");
            SwordBody.SetActive(true);
            cntlShield.PullShield();
            ActionButton.itemInHands = SwordBody;
        }
    }
    //Only handles initial swing. Sword.cs looks for further inputs while this script is marked busy.
    public void SwingSword()
    {
        swordScript.Start_Swing();
        cntlShield.LowerShield();
    }

    public void StartGuard()
    {
        cntlShield.Blocking();
    }

    public void SlingRevert()
    {
        sShot.gameObject.SetActive(false); //turns the slingshot on
        isFirstPerson = false; //DISABLES NORMAL INPUT - CAM HANDLED IN CAMERA_PERSPECTIVES SHOOTING AND REVERT HANDLED IN SLINGSHOT
        ActionButton.ForceReadyNone();
    }


    //Functions as an override to prevent input from player
    //Animation states make this mostly obsolete.
    public void ToggleBusy()
    { busy = !busy; }
    public void SetBusy(bool b)
    { busy = b; }
    
    //Pass throughs so animation can indirectly call methods on Sword.cs
    public void EndSwingOne()
    { swordScript.End_SwingOne(); }
    public void EndSwingTwo()
    { swordScript.End_SwingTwo(); }
    public void EndSwingThree()
    { swordScript.End_SwingThree(); }

    public void SwordDangerOn()
    {
        swordScript.isDangerOn();
    }


    public void SwordDone()
    {
        SetBusy(false);
    }

    //FLINCH METHODS
    //Call to stun for default time
    public void Flinch()
    {
        if (isFirstPerson)
        { camChanger.StartThird(); }

        cntlShield.LowerShield();
        DropHands();
        
        if (anim.GetBool("isRolling") == true)
        {
            anim.SetBool("isRolling", false);
            anim.Play("Idle_Player");

            Quaternion stand = Quaternion.Euler(0, transform.rotation.y, 0);
            transform.rotation = stand;
        }
        anim.Play("NewFlinch");
        anim.Play("NewFlinch", 1);
        stunTime = stunTimeMax;
        stunned = true;
        busy = false;
        ActionButton.StartCool();
    }
    
    //NOT COMPLETE
    //Call to stun for custom time
    public void Flinch(float t)
    {
        if (isFirstPerson)
        { camChanger.StartThird(); }
        if (anim.GetBool("isRolling") == true)
        {
            anim.SetBool("isRolling", false);

            Quaternion stand = Quaternion.Euler(0, transform.rotation.y, 0);
            transform.rotation = stand;
        }
        DropHands();
        anim.SetBool("isCarrying", false);
        //Need to further customize animations to implement this.
        stunTime = t;
        stunned = true;
    }


    public void DropHands()
    {
        if (ActionButton.itemInHands != null && ActionButton.itemInHands != SwordBody)
        { ActionButton.CarryDetach(); }
        anim.SetBool("isPistol", false);
        ActionButton.ForceReadyNone();
        anim.SetBool("isBomb", false);
        anim.SetBool("isCarrying", false);
    }

    //Called from Flinch animation to reset bool
    public void unFlinch()
    {
    //    anim.SetBool("isFlinching", false);
    }
    private void StunCount()
    {
        if (stunTime >= 0)
        {
            stunTime -= Time.deltaTime;
        }
        else
        { stunned = false; }
    }

    public void FaceIt(GameObject a)
    {
        Vector3 facehere = new Vector3(a.transform.position.x, transform.position.y, a.transform.position.z);
        transform.LookAt(facehere);
    }

    public void JumpFinish()
    {
        anim.SetBool("isSpinJump", false);
        anim.SetBool("isSpinning", false);
        //pole.toggleActivated();
        player.enabled = true;
        SetBusy(false);

    }

}
