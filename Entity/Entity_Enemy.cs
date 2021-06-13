using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

//Creating a new enemy... I'm probably going to forget stuff. But this should get a good chunk of it done.
//You don't actually have to do most of these steps in order but *shrug*. 

//Step 1: Create an empty that I'll call [whatever]Holder. (This is because we need a layer of separation
//between the Fields of view and the Entity script to prevent triggers from interacting improperly.)

//Step 2: Add a Nav Mesh Agent and Animator to [whatever]Holder.

//Step 3: Create a new empty that I'll call [whatever]Controller.

//Step 4: [whatever]Controller will contain: Enemy_[whatever].cs, a rigid body (Use Gravity = false, 
//is Kinematic = true, frozen rotation on X and Z axis), your audio Source(s), and a capsule collider 
//(isTrigger = true) to detect when it reaches a waypoint.

//Step 5: If you haven't already, create Enemy_[whatever].cs and go to the line under the usings
//where it says public class Enemy_[whatever] : MonoBehavior and change MonoBehavior to Entity_Enemy

//Step 6: Please use Entity_Blocker as a reference for how to set this script up. Most methods have
//code notes to explain what they are used for and should be fairly easy to modify as necessary.

//Step 7: Build the enemy's body as children of [whatever]Controller. 

//Step 8: Go ahead and set up the animations. If you're copying behaviors from Enemy_Blocker then recycle the 
//Animator bools for your own animations. 

//Step 9: Create your weapon and attach a new script to it. Please reference Axe.cs on the Blocker for format. 
//The weapon requires Enemy_[whatever] to call SetDamage and pass in myStats.atkDam.

//Step 10: Set up a trigger collider on the weapon on the part of it you want to be able to deal damage to the player.
//And then disable the trigger collider. If you start with a copy of Enemy_Blocker then when it attacks the collider
//will be turned on. 

//Step 11: Any bodyparts of your enemy that you want to be able to damage the player need to have a trigger 
//collider on them. This can be as simple as one part with a capsule/box/sphere collider or as complex as a bunch 
//of different ones. Add ChildCollision.cs to those bodyparts so that they inform Enemy_[whatever] of collisions.

//Step 12: Create a field of vision for the enemy in the size and shape you want. Make its collider a trigger and
//replace the material with GhostBox.mat. Once you have it placed how you want it you can go ahead and turn off the
//renderer so it's invisible.

//Step 13: Place IgnoreCollision.cs on the field of vision so other scripts' OnTriggerEnter can look for and ignore it.

//Step 14: Place EnemyFOVController.cs on it. This script will cause the enemy to try to enter the Pursuit state when
//it detects the player. 
//Note: You can create multiple fields of view and attach them this way if you say... wanted a multi-headed creature.

//Step 15: Create an empty name it TARGET and make it a child of [whatever]Body. Move it so it's right where you
//you would want the slingshot to hit. Place ZTarget.cs on the empty. 

//Step 16: IF you ahven't already, customize the behaviors you copied from Enemy_Blocker. 

//Now it's time for the animations. I'm not going to do a step-by-step of the animations. 
//However, here's a list of the animation events I had set up on Enemy_Blocker. 
//Note: I actually have the animation events pointing to BlockerPassthrough.cs because it keeps the list
//of possible public events to call pretty short. And BlockPassthrough.cs calls them on Enemy_Blocker. 

//Animation events on Blocker list:
//Attack animation calls EndAttack 1/60 sec before the animation ends to reset animation bools and turn the weapon's collider off.
//Dead animation calls Disappear at 1/60 sec before the animation ends to make the enemy disappear.
//Flinch calls StartInvincible() at the end.

//******************************NEW!!!!*****************************
//Adding the HealthBar and Targeting reticle to new enemy:
//STEP 1: Drag the prefab EnemyHUD onto the enemy or its parent object.
//Step 2: You may have to reposition or resize the healthbar and targeting reticle. The canvas component of EnemyHUD should already be on
//Reposition or resize the Slider and Image if needed. Then turn the Canvas component back off. NOTE: You still need to do Step 15 above as well.
//Step 3: Test and make sure it works. 


public class Entity_Enemy : Entity
{
    private int arraySlot;
    public GameMaster gm;
    public Canvas myCanvas;
    public Slider healthBar;
    
    //States first get declared here
    public enum EnemyStates
    {
        Idle,
        Patrol,
        Search,
        Pursue,
        Attack,

        NUM_STATES
    }
    [SerializeField]
    public EnemyStates curState; //holds reference to current state
    //declares the way the dictionary is formatted, associating a state with a method
    public Dictionary<EnemyStates, Action> esm = new Dictionary<EnemyStates, Action>();

    //Declare variables referencing parts of the enemy
    public Animator anim;
    
    //Object of the pursue and attack state
    public GameObject target;

    //Player's Lock On
    public bool informDeath; //if true tells Control_ZTarget when this entity dies
    public Control_Zlock ztarget; //Auto-assigned in Start()
    private GameObject myZtargetOb;

    //Check this before dealing damage to allow shield to block collision damage!
    public bool isBlocked = false;

    //Handles Flinch 
    public bool isStunned;
    public float stunTime;
    public float stunTimeMax = 1f;

    //Handles maximum time enemy will search for a target before cycling to idle then patrol.
    public float searchTime;
    public float searchTimeMax = 3;

    //Icons as gameObjects
    public GameObject YesSword;
    public GameObject NoSword;
    public GameObject YesSlingshot;
    public GameObject NoSlingshot;
    public GameObject YesBomb;
    public GameObject NoBomb;
    

    //These control the Z-Target HUD elements. Update these variables when coding enemy immunity so the HUD accurately reflects.
    public bool noSworddmg;
    public bool noSlingshotdmg;
    public bool noBombdmg; //currently there are no enemies that have bomb immunity
    
    public virtual void Start()
    {
        //Declare states from Enum here to associate with a function
        esm.Add(EnemyStates.Idle, new Action(StateIdle));
        esm.Add(EnemyStates.Patrol, new Action(StatePatrol));
        esm.Add(EnemyStates.Search, new Action(StateSearch));
        esm.Add(EnemyStates.Pursue, new Action(StatePursue));
        esm.Add(EnemyStates.Attack, new Action(StateAttack));

        //Used for objects where their Entity script is on the parent object
        if (GetComponentInChildren<Canvas>())
        {
            myCanvas = GetComponentInChildren<Canvas>();

            //sets up healthbar
            healthBar = myCanvas.GetComponentInChildren<Slider>();
            healthBar.maxValue = myStats.maxHearts;
            healthBar.value = myStats.curHearts;

            //Sets up icons
            YesSword = myCanvas.transform.GetChild(1).GetChild(0).gameObject;
            NoSword = myCanvas.transform.GetChild(1).GetChild(1).gameObject;
            YesSlingshot = myCanvas.transform.GetChild(1).GetChild(2).gameObject;
            NoSlingshot = myCanvas.transform.GetChild(1).GetChild(3).gameObject;
            YesBomb = myCanvas.transform.GetChild(1).GetChild(4).gameObject;
            NoBomb = myCanvas.transform.GetChild(1).GetChild(5).gameObject;
        }
        //used for enemies where there is a parent "holder" object
        else if (transform.parent.GetComponentInChildren<Canvas>())
        {
            myCanvas = transform.parent.GetComponentInChildren<Canvas>();
            
            //sets up healthbar
            healthBar = myCanvas.GetComponentInChildren<Slider>();
            healthBar.maxValue = myStats.maxHearts;
            healthBar.value = myStats.curHearts;

            //Sets up icons
            YesSword = myCanvas.transform.GetChild(1).GetChild(0).gameObject;
            NoSword = myCanvas.transform.GetChild(1).GetChild(1).gameObject;
            YesSlingshot = myCanvas.transform.GetChild(1).GetChild(2).gameObject;
            NoSlingshot = myCanvas.transform.GetChild(1).GetChild(3).gameObject;
            YesBomb = myCanvas.transform.GetChild(1).GetChild(4).gameObject;
            NoBomb = myCanvas.transform.GetChild(1).GetChild(5).gameObject;
        }

        anim = GetComponentInParent<Animator>();
        ztarget = FindObjectOfType<Control_Zlock>();
        gm = FindObjectOfType<GameMaster>();
        if (GetComponentInChildren<ZTarget>())
        {
            myZtargetOb = GetComponentInChildren<ZTarget>().gameObject;
        }

        EnterIdle(); //Beginning State for all Enemies
    }
    
    public override void Update()
    {
        if(myCanvas != null && informDeath)
        {
            if(ztarget.getTarget() != myZtargetOb)
            {
                TellZTarget(false);
                return;
            }

            updateHealthBar();
            updateIcons();
        }


        if (!isStunned && myStats.curHearts >0)
        {
            esm[curState].Invoke(); //Runs the method handling curState's behavior
        }
        else
        {
            //Debug.Log("Stun counting down");
            StunCount(); } //Otherwise, countdown the stun

        base.Update(); //Base.Update (Entity.cs) handles the invicibility timer
    }


    //Following methods handle flinching
    //It should be possible to override the normal flinch
    //by doing the following:
    //Step 1: Don't call Flinch(), use a new function to set the stunTime
    //to anything longer than the time you actually want, then set isStunned to true.
    //Step2: Make your animation and create an animation event near the end of the
    //animation. Have that event call UnFlinch(). 

   public virtual void updateHealthBar()
    {
        healthBar.maxValue = myStats.maxHearts;
        healthBar.value = myStats.curHearts;
    }

    private void updateIcons()
    {
        //Checks to see if the right UI icon is displayed. No check for the other icon should be necessary since they're turned on/off together.
        if (noSworddmg == YesSword.activeInHierarchy)
        {
            //uses its current status to flip the bool
            YesSword.SetActive(!noSworddmg);
            NoSword.SetActive(noSworddmg);
        }
        if (noSlingshotdmg == YesSlingshot.activeInHierarchy)
        {
            YesSlingshot.SetActive(!noSlingshotdmg);
            NoSlingshot.SetActive(noSlingshotdmg);
        }
        if (noBombdmg == YesBomb.activeInHierarchy)
        {
            YesBomb.SetActive(!noBombdmg);
            NoBomb.SetActive(noBombdmg);
        }
    }

    //This begins the flinch
    public virtual void Flinch()
    {
        stunTime = stunTimeMax;
        isStunned = true;
    }
    //This is called on Update to countdown the flinch
    public virtual void StunCount()
    {
        if (stunTime >= 0)
        {
            stunTime -= Time.deltaTime;
        }
        else
        {
            isStunned = false;
            UnFlinch();
        }
    }
    //This ends the flinch.
    public virtual void UnFlinch()
    {
        isStunned = false;
        anim.SetBool("isFlinching", false); //turn off flinch animation
        StartTimedInvincibility();
    }

    //This will inform the GameMaster of its death, and tell Z-target
    //to pick a new target if this object is the one currently targetd.
    public override void Die()
    {
        //Debug.Log(arraySlot + " registered Die()");
        gm.UpdateDefeats(arraySlot);

        if (informDeath)
        { ztarget.NextTarget(); }
        //Debug.Log("Telling Control_Zlock that I died!");
    }

    //Called by ChildCollision to pass along collision info for collision damage
    public virtual void collisiondetected(Collider other)
    {
        if (myStats.curHearts > 0 && isBlocked == false)
        {
            if (!other.GetComponent<Item>() && !other.GetComponent<CarryMe>()) //anything attached to the player that doesn't count as part of them needs this item script (or no collider)
            {
                if (other.GetComponentInParent<Entity_Player>())
                {
                    other.GetComponentInParent<Entity_Player>().Damaged(myStats.colDam);
                }
            }
        }
    }

    //Called by ShieldDetector to let the enemy know they can't hurt the player right now
    public virtual void Blocked(bool a)
    {
        isBlocked = a;
        //Debug.Log("isBlocked was set to " + a);
    }

    //State Machine below here
    //Most of state machine behaviors should be handled with an override of
    //the following states. They're only declared here to initialize them so they're available to be overridden.
    //Except for search, which includes a countdown before resetting the state to idle.
    public virtual void EnterIdle()
    {
        curState = EnemyStates.Idle;
        target = null;
    }
    public virtual void StateIdle()
    { }

    public virtual void EnterPatrol()
    {
        target = null;
        curState = EnemyStates.Patrol;
    }
    public virtual void StatePatrol()
    { }

    public virtual void EnterSearch()
    {
        searchTime = searchTimeMax;
        curState = EnemyStates.Search;
    }
    public virtual void StateSearch()
    {
        if (searchTime >= 0)
        {
            searchTime -= Time.deltaTime;
        }
        else
        {
            EnterIdle();
        }
    }



    public virtual void EnterPursue(GameObject other)
    {
        target = other;
        curState = EnemyStates.Pursue;
    }
    public virtual void StatePursue()
    { }

    public virtual void EnterAttack()
    {
        curState = EnemyStates.Attack;
    }
    public virtual void StateAttack()
    { }

    //Called by Control_Ztarget so this script will know to inform it of death.
    public virtual void TellZTarget(bool t)
    {
        informDeath = t;
        myCanvas.enabled = t;
    }
    //Called by GameMaster so this script knows how to identify itself when informing 
    //GameMaster of its death.
    public virtual void AssignArraySlot(int i)
    {
        //Debug.Log(gameObject.name + " was assigned array slot " + i);
        arraySlot = i;
    }
}
