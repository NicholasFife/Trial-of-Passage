using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.InputSystem;

//This script sorts whatever the player's current action BUTTON is
//It works more or less like the simple state machine we were shown a few 
//Classes ago. Except states are sorted by priority when they attempt to trigger.
public class Control_Actions : MonoBehaviour
{
    private Text buttonText;
    private CharacterController charcntl;
    public Control_Player cntl;
    public Control_Shield cntlShield;
    public Sword swordcntl;
    private Camera_Perspectives camPer;
    private GameObject footPivot;
    private Quaternion rotFoot;
    private Control_Inventory inv;

    public LayerMask obMask;

    public GameObject selected; //The object that currently has the Action Button's attention
    public GameObject itemInHands; //What they're carrying, be it sword or rock
    public GameObject CarryHolder; //This is the empty game object that we attach the rock or whatever to so we can move it around with animations.
    public GameObject BlockHolder;

    public GameObject bomb;

    private Animator anim;
    private float throwPower = 1;

    public bool buttonReady = true; //Controls if the button is ready to be pressed - set to false when action is taken
    public float ButtonDelay = .1f; //Cooldown time before button is usable again
    public float ButtonCountdown; //The variable used for the timer
    public bool buttonCooling = false; //Controls if the cooldown has started or note - starts *after* action is done to prevent bugs.
    public bool isPushing = false; //Tracks if the player is pushing an object
    public bool isClimbing = false;
    public bool willJumpAtk = false;

    private Gamepad gamepad;
    private Keyboard keyboard;

    public AudioSource a1;//Roll
    public AudioSource a2;//Pickup
    public AudioSource a3;//Rock Thunk
    public AudioSource a4;//Climb Grunt

    public enum ButtonState
    {
        
        Carry, //This is basically pickup named it different to avoid confusion
        isCarrying, //This state transitions to throwing or dropping the held object
        //Fall,
        //Drop,
        //Dive,
        //Crawl,
        Push,
        Climb,
        Roll,
        PutAway, //Set up for sword now. Need to adjust so it can put away any piece of equipment.
        Open, 
        ZJump,
        DrawBomb,

        None, //Ready for any actionbutton state to take over

        NUM_STATES
    }

    //Priorities for different button functions higher number = higher priority
    //Priorities between 1 and 5, because "None" is 0
    int putAwayPrior = 1;
    int pickupPrior = 2;
    int rollPrior = 3;
    int openPrior = 5;
    int pushPrior = 5;
    int climbPrior = 6;
    int bombPrior = 6;
    int zJumpPrior = 4;

    public ButtonState curState; //holds the current state
    public int curPrior; //holds the priority of the current state 0:none to 6:high

    //Initializes the dictionary
    public Dictionary<ButtonState, Action> asm = new Dictionary<ButtonState, Action>();
    
    // Start is called before the first frame update
    void Start()
    {
        //Fills the dictionary with the states and function associated with that state
        asm.Add(ButtonState.Carry, new Action(Carry));
        asm.Add(ButtonState.isCarrying, new Action(isCarrying));
        asm.Add(ButtonState.Roll, new Action(Roll));
        asm.Add(ButtonState.PutAway, new Action(PutAway));
        asm.Add(ButtonState.Open, new Action(Open));
        asm.Add(ButtonState.None, new Action(None));
        asm.Add(ButtonState.Push, new Action(Push));
        asm.Add(ButtonState.Climb, new Action(Climb));
        asm.Add(ButtonState.DrawBomb, new Action(DrawBomb));
        asm.Add(ButtonState.ZJump, new Action(Zjump));
        
        curState = ButtonState.None;//Sets the beginning state to None

        anim = GetComponent<Animator>();
        cntl = GetComponent<Control_Player>();
        cntlShield = GetComponentInChildren<Control_Shield>();
        buttonText = FindObjectOfType<UI_ActionText>().GetComponent<Text>();
        camPer = transform.parent.GetComponentInChildren<Camera_Perspectives>();
        footPivot = GetComponentInChildren<PivotIdent>().gameObject;
        charcntl = GetComponent<CharacterController>();
        inv = charcntl.GetComponent<Control_Inventory>();
        gamepad = Gamepad.current;
        keyboard = Keyboard.current;
    }

    void Update()
    {
        if (buttonCooling)
        {
            ButtonCool();
        }
        UpdateButtonText();

        if(anim.GetBool("isRolling") == true)
        {
            cntl.ApplyGravity();
        }

        if(anim.GetBool("isCarrying") == true)
        {
            selected.transform.localRotation = Quaternion.Euler(0, 0, 0);
            selected.transform.localPosition = new Vector3(0, 0, 0);
        }
    }

    //Each state must be addressed here for button to update properly
    void UpdateButtonText()
    {
        if (curState == ButtonState.None)
        { buttonText.text = "None"; }
        else if (curState == ButtonState.Carry)
        { buttonText.text = "Pick Up"; }
        else if (curState == ButtonState.PutAway)
        { buttonText.text = "Put Away"; }
        else if (curState == ButtonState.Roll)
        { buttonText.text = "Roll"; }
        else if (curState == ButtonState.Open)
        { buttonText.text = "Open"; }
        else if (curState == ButtonState.Push)
        { buttonText.text = "Push"; }
        else if (curState == ButtonState.Climb)
        { buttonText.text = "Climb"; }
        else if (curState == ButtonState.isCarrying)
        {
            if (anim.GetBool("isWalking") == true)
            { buttonText.text = "Throw"; }
            else
            { buttonText.text = "Drop"; }
        }
        else if (curState == ButtonState.ZJump)
        { buttonText.text = "Jump"; }
        else
        {
            buttonText.text = "Fix Me";
            Debug.LogError("Action Button State not coded into UpdateButtonText() in Control_Actions.cs");
        }
    }

    public void ButtonPressed()
    {
        if (buttonReady && curState != ButtonState.None)
        {
            buttonReady = false;
            asm[curState].Invoke();
        }
    }
    public void StartCool()
    {
        buttonCooling = true;
        ButtonCountdown = ButtonDelay;
    }
    void ButtonCool()
    {
        if (ButtonCountdown >= 0)
        { ButtonCountdown -= Time.deltaTime; }
        else
        {
            buttonReady = true;
            buttonCooling = false;
        } 
    }
    public void EmptyHands()
    {
        if(itemInHands)
            itemInHands.SetActive(false); //puts away item in hands
        itemInHands = null; //purges variable
        cntlShield.PutAwayShield();
    }

    //Try functions are attempting to set the action button to that state
    //They should all follow the exact same format 
    //And be triggered from outside this script.
    //Action functions "Roll" "Carry" etc... are triggered on button press
    //and *do* the action. buttonReady is set to false in ButtonPressed method above.
    //End functions are generally triggered by an animation and serve to
    //Reset everything back to normal, initialize the cooldown, and ForceReadyNone to clear the state.
    public bool TryReadyRoll()
    {
        if (rollPrior > curPrior && !isPushing)
        {
            curState = ButtonState.Roll;
            curPrior = rollPrior;
            
            return true;
        }
        return false;
    }
    private void Roll()
    {
        anim.SetBool("isRolling", true);
        //camPer.StartThird();
        a1.Play();
    }
    public void EndRoll()
    {
        anim.SetBool("isRolling", false);
        cntl.SetBusy(false);
        StartCool();
        ForceReadyNone();
        
    }

    public bool TryReadyPutAway()
    {
        if (putAwayPrior > curPrior)
        {
            curState = ButtonState.PutAway;
            curPrior = putAwayPrior;
            return true;
        }
        return false;
    }
    private void PutAway()
    {
        if (itemInHands != null)
        {
            itemInHands.SetActive(false);
            itemInHands = null;
        }
        cntlShield.PutAwayShield();
        ForceReadyNone();
        StartCool();
    }

    public bool TryReadyOpen()
    {
        if (openPrior > curPrior)
        {
            curState = ButtonState.Open;
            curPrior = openPrior;
            return true;
        }
        return false;
    }
    private void Open()
    {
        bool tempdoor;

        if(selected.GetComponent<OpenChest>())
        {
            selected.GetComponent<OpenChest>().Open();
        }
        else
        {
            tempdoor = selected.GetComponent<Animator>().GetBool("isOpen");
            selected.GetComponent<Animator>().SetBool("isOpen", !tempdoor);
        }

        StartCool();
        ForceReadyNone();
    }

    public bool TryClimb()
    {
        if (climbPrior > curPrior)
        {
            curState = ButtonState.Climb;
            curPrior = climbPrior;
            return true;
        }
        return false;
    }

    public void Climb()
    {
        anim.SetBool("isWalking", false);
        //rotFoot = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 0);
        //footPivot.transform.localRotation = rotFoot;
        cntl.SetBusy(true);
        cntl.stunned = true;
        cntlShield.PutAwayShield();
        if (itemInHands != null)
        { EmptyHands(); }
        this.transform.rotation = selected.GetComponent<ClimbMe>().parentRotation;
        itemInHands = selected;
        //cntl.FaceIt(selected);
        cntl.GetComponent<CharacterController>().enabled = false;

        if(selected.GetComponent<FullClimb>())
            anim.SetBool("isClimbing", true);

        if (selected.GetComponent<MedClimb>())
            anim.SetBool("isClimbingMed", true);

        if (selected.GetComponent<SmallClimb>())
            anim.SetBool("isClimbingSmall", true);
        
        

        isClimbing = true;
        selected.GetComponent<ClimbMe>().toggleActivated();

        curPrior = 5;
        camPer.StartZ(false);
    }

    public void EndClimb()
    {
        
        StartCool();
        selected.transform.parent = null;
        itemInHands = null;
        isClimbing = false;
        selected.GetComponent<ClimbMe>().toggleActivated();
        selected.GetComponent<ClimbMe>().lastActiveTrigger.GetComponent<RotateToFace>().inThisTrigger = false;
        anim.SetBool("isClimbing", false);
        anim.SetBool("isClimbingMed", false);
        anim.SetBool("isClimbingSmall", false);
        selected.GetComponent<ClimbMe>().inTrigger = false;
        selected.GetComponent<ClimbMe>().triggerTime = 0;
        cntl.GetComponent<CharacterController>().enabled = true;
        cntl.stunned = false;
        cntl.SetBusy(false);
        ForceReadyNone();
        camPer.StartThird();
    }

    public bool TryPush()
    {
        if (pushPrior > curPrior)
        {
            curState = ButtonState.Push;
            curPrior = pushPrior;
            return true;
        }
        return false;
    }

    public void Push()
    {
        curState = ButtonState.Push;
        isPushing = true;
        anim.SetBool("isWalking", false);
        //rotFoot = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 0);
        //footPivot.transform.localRotation = rotFoot;
        cntl.SetBusy(true);
        EmptyHands();
        //if (itemInHands != null)
        //{ EmptyHands(); }
        //cntl.FaceIt(selected); //turns to face the object
        this.transform.rotation = selected.GetComponent<Pushable>().parentRotation;
        //selected.transform.parent = BlockHolder.transform; //makes object the child of CarryHolder
        //selected.transform.rotation = new Quaternion (0,0,0,0);
        itemInHands = selected; //sets the picked up rock to what's in hands
        selected.GetComponent<Pushable>().toggleActivated();
        curPrior = 5; //Makes sure player can't try to interact with things they shouldn't. Maybe we can override this for talking to NPCs?
        //camPer.StartZ();
    }

    public void EndPush()
    {
        cntl.SetBusy(false);
        StartCool();
        //selected.transform.parent = null;
        itemInHands = null;
        isPushing = false;
        //selected = null;
        
        //Debug.Log(selected);
        selected.GetComponent<Pushable>().toggleActivated();
        ForceReadyNone();
        //camPer.StartThird();
    }
    public bool TryCarry()
    {
        if (pickupPrior > curPrior)
        {
            curState = ButtonState.Carry;
            curPrior = pickupPrior;
            return true;
        }
        return false;
    }


    private void Carry()
    {
        if (selected != null)
        {
            selected = selected.transform.parent.gameObject;

            a2.Play();
            cntl.SetBusy(true);
            cntlShield.PutAwayShield();
            if (itemInHands != null)
            { EmptyHands(); }
            cntl.FaceIt(selected); //turns to face the object
            selected.GetComponent<Rigidbody>().isKinematic = true; //sets object to kinematic
            selected.transform.parent = CarryHolder.transform; //makes object the child of CarryHolder
            selected.transform.position = new Vector3(0, 0, 0);
            selected.transform.rotation = Quaternion.Euler(0, 0, 0);
            itemInHands = selected; //sets the picked up rock to what's in hands
            selected.transform.localPosition = new Vector3(0, 0, 0); //sets its transform to center it on CarryHolder
            selected.GetComponent<Rigidbody>().useGravity = false; //turns off gravity - only thing that moves it should now be it's parent's movement
            curPrior = 5; //Makes sure player can't try to interact with things they shouldn't. Maybe we can override this for talking to NPCs?
            curState = ButtonState.isCarrying;
            anim.SetBool("isWalking", false);
            anim.SetBool("isCarrying", true);
            if (selected.GetComponent<BreakWhenThrown>())
            { selected.GetComponent<BreakWhenThrown>().picked(); }
        }
        else
        { ForceReadyNone(); }

    }
    public void DrawBomb()
    {
        //Debug.Log("DrawBomb called comparing bomb priority: " + bombPrior + " to current priority: " + curPrior);
        if (bombPrior > curPrior)
        {
            curState = ButtonState.DrawBomb;
            curPrior = bombPrior;
            inv.MinusBomb();
        }
        else { return; }


        a2.Play();//pickup noise
        
        curPrior = 5; //Makes sure player can't try to interact with things they shouldn't. Maybe we can override this for talking to NPCs?
        
        //Clear hands, make sure player is in a state ready for drawing the bomb
        cntl.SetBusy(true);
        cntlShield.PutAwayShield();
        if (itemInHands != null)
        { EmptyHands(); }

        selected = Instantiate(bomb, CarryHolder.transform);
        selected.GetComponent<Rigidbody>().isKinematic = true; //sets object to kinematic
        itemInHands = selected; //sets the picked up rock to what's in hands
        selected.GetComponent<Rigidbody>().useGravity = false; //turns off gravity - only thing that moves it should now be it's parent's movement
        anim.SetBool("isWalking", false);
        anim.SetBool("isBomb", true);
        anim.SetBool("isCarrying", true);
        curState = ButtonState.isCarrying;
    }

    public void FinishedPickup()
    {
        cntl.SetBusy(false);
        StartCool();
        anim.SetBool("isCarrying", false);
    }
    private void isCarrying()
    {
        Debug.Log("Setting held object rotation to all 0");
        float str = 0;
        if (gamepad != null)
        { str = gamepad.leftStick.EvaluateMagnitude(); }
        if (keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed|| keyboard.dKey.isPressed)
        { str = 1; }
        Debug.Log("str in isCarrying set to: " + str);
        //Weird dual-ready State where priority cannot be overridden (Carry() sets curPrior to 5)
        if (str > .3)
        {
            a1.Play();
            anim.SetBool("isThrowing", true);
            anim.SetBool("isCarrying", false);
            anim.SetBool("isBomb", false);
        }
        else
        {
            if (selected.GetComponent<BreakWhenThrown>())
            { selected.GetComponent<BreakWhenThrown>().unPicked(); }
            anim.SetBool("isDropping", true);
            anim.SetBool("isCarrying", false);
            anim.SetBool("isBomb", false);
        }
    }
    public void LightBomb()
    {
        if (selected.GetComponent<Bomb>())
        { selected.GetComponent<Bomb>().LightFuse(); }
    }

    public void CarryDetach()
    {
        if (selected != null)
        {
            selected.transform.parent = null;
            selected.GetComponentInParent<Rigidbody>().isKinematic = false;
            selected.GetComponentInParent<Rigidbody>().useGravity = true;
            itemInHands = null;
        }
    }
    public void Throw()
    {
        CarryDetach();
        if(selected.GetComponent<BreakWhenThrown>())
        {
            selected.GetComponent<BreakWhenThrown>().willBreak = true;
        }
        Vector3 Throwdir = GetComponent<CharacterController>().transform.forward;
        Throwdir.y += .3f;
        Throwdir = Throwdir.normalized;
        Debug.Log("throw direction is " + Throwdir);


        selected.GetComponent<Rigidbody>().AddForce(Throwdir * throwPower * 10, ForceMode.Impulse);
        anim.SetBool("isThrowing", false);
        StartCool();
        ForceReadyNone();
    }

    public bool EnterZjump()
    {
        if (zJumpPrior > curPrior)
        {
            curState = ButtonState.ZJump;
            curPrior = zJumpPrior;
            return true;
        }
        return false;
    }
    public void Zjump()
    {
        cntl.Jump();
        /*if (willJumpAtk)
        {
            cntl.DrawSword();
            cntlShield.PutAwayShield();
            charcntl.enabled = false;
            anim.SetBool("isJumpAtk", true);
            swordcntl.isDangerOn();
            swordcntl.dam = swordcntl.dam * 2;
        }*/
    }
    public void EndJumpAtk()
    {
        charcntl.enabled = true;
        anim.SetBool("isJumpAtk", false);
        swordcntl.isDangerOff();
        swordcntl.dam = swordcntl.dam / 2;
    }


    public void Thunk()
    {
        a3.Play();
    }

    public void ClimbGrunt()
    {
        a4.Play();
    }
    
    public void Drop()
    {
        LightBomb();
        CarryDetach();
        anim.SetBool("isDropping", false);
        StartCool();
        ForceReadyNone();
    }
    
    //Just empties the button state. 
    //Must be triggered when action is complete  
    //AND if button state conditions are no longer met
    //Ex: button is for open and you walk away from the door
    public void ForceReadyNone()
    {
        if (!buttonReady)
        { StartCool(); }
        curState = ButtonState.None;
        selected = null;
        curPrior = 0;
    }

    public void PriorOverride()
    { curPrior = 5; }

    private void None()
    {
        //Debug.Log("Placeholder: No action is ready");
    }
}
