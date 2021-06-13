using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy_Blocker : Entity_Enemy
{
    //Okay, it's not an axe anymore. Sue me.
    private Axe myAxe;
    private SphereCollider scAxe;
    private CapsuleCollider ccAxe;
    private BlockerShield myShield;
    public BoxCollider bshield;
    private bool bombed;
    public Transform eye;
    private HeadPivot HP;
    public GameObject LaserBall;
    public GameObject ShotPos;
    
    //Handles destination points
    private NavMeshAgent agent;
    private int wayIndex = 0;
    public GameObject[] myWayPoints;

    //Handles the idle count - bool not needed due to state machine
    private float idlemax = 1;
    private float idlecount;
    
    //Handles the attack cooldown - bool needed because this behavior happens outside one specific state
    private float atkCoolMax = 4f;
    private float atkCoolCount;
    private bool atkCooling;

    public SpawnOnDead spawn;

    //Audio Sources
    public AudioSource[] noise;
    
    public override void Start()
    {
        //Sets stats: Health, collision damage, attack damage, hostile
        myStats = new stats(9, 1, 2, true);
        myAxe = GetComponentInChildren<Axe>(); //get axe
        scAxe = myAxe.GetComponent<SphereCollider>(); //get sphere collider so I can turn it on and off and control if weapon's tip deals damage
        ccAxe = myAxe.GetComponent<CapsuleCollider>(); //get capsule collider so I can turn it on and off to control if the entire weapon deals damage.
        myAxe.SetDamage(myStats.atkDam); //tell axe what damage it deals
        myShield = GetComponentInChildren<BlockerShield>();
        bshield = myShield.GetComponent<BoxCollider>();
        agent = transform.parent.GetComponent<NavMeshAgent>();
        HP = GetComponentInChildren<HeadPivot>();
        //Handles setting up the state machine's states and basic "entity" construction
        base.Start();
    }
    
    public override void Update()
    {
        //esm Behavior is invoked in Entity_Enemy
        base.Update();

        if(agent.destination == agent.transform.position)
        { anim.SetBool("isWalking", false); }
        else
        { anim.SetBool("isWalking", true); }

        if(atkCooling)
        { CoolAttack(); }

        //Set Icon status bools
        if (myStats.timedInvincible)
        {
            noSworddmg = true;
            noSlingshotdmg = true;
            noBombdmg = true;
        }

        else
        {
            noSworddmg = anim.GetBool("isPursuit");
            noSlingshotdmg = anim.GetBool("isPursuit");
            noBombdmg = false;
        }
    }

    //Called to begin the idle state
    public override void EnterIdle()
    {
        anim.SetBool("isWalking", false);
        anim.SetBool("isPursuit", false);
        //Debug.Log("Entering Idle");
        //Selects the next waypoint
        agent.SetDestination(transform.position);
        //resets the idle timer
        idlecount = idlemax;
        HP.SetFollow(false);

        base.EnterIdle();
    }
    //Ran on update while in the idle state
    public override void StateIdle()
    {
        Idling();
    }
    //Counts the time until leaving idle
    private void Idling()
    {
        if (idlecount > 0)
        { idlecount -= Time.deltaTime; }
        else
        { EnterPatrol(); }
    }

    //Called to begin the Patrol State
    public override void EnterPatrol()
    {
        anim.SetBool("isWalking", true);
        anim.SetBool("isPursuit", false);
        agent.speed = 6;
        HP.SetFollow(false);
        //Debug.Log("Entering Patrol");
        //Begins movement to the currently set waypoint
        MoveToWayPoint();
        base.EnterPatrol();
    }
    //Called on update white in the patrol state
    //There is not actually any behavior associated with being in patrol.
    //Setting destination in done upon entering patrol.
    //Changing destination is done on collision with a waypoint.
    public override void StatePatrol()
    {
        base.StatePatrol();
    }
    
    //Picks next NavPoint
    private void pickNextWayPoint()
    {
        ++wayIndex;
        //Debug.Log("Setting new Waypoint index: " + wayIndex);
        if (wayIndex >= myWayPoints.Length)
        { wayIndex = 0; }
    }
    //Sets destination to selected NavPoint
    private void MoveToWayPoint()
    {
        //Debug.Log("Moving to waypoint index: " + wayIndex);
        agent.SetDestination(myWayPoints[wayIndex].transform.position); }

    //Recognizes when it's reached a navpoint so a new desination can be set and start idle
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("detected collision with: " + other.name);
        //Debug.Log("My state is " + curState);
        if (curState == EnemyStates.Patrol)
        {
            if (other.gameObject == myWayPoints[wayIndex])
            {
                pickNextWayPoint();
                EnterIdle();
            }
        }
    }
    
    //Starts pursuit animation
    //Increases their speed because I wanted normal walking speed to be different.
    //Lets Entity_Enemy handle setting target and actually change the state.
    public override void EnterPursue(GameObject other)
    {
        if(isStunned == true)
        { return; }

        HP.SetFollow(true);

        anim.SetBool("isPursuit", true);
        bshield.enabled = true;
        //Debug.Log("Entering Pursuit");
        agent.speed = 3.5f;
        base.EnterPursue(other);
    }

    //Makes sure the player hasn't broken line of sight
    //Checks the distance to the player and transitions to attack if 
    //distance requirements are met. Otherwise chases.
    public override void StatePursue()
    {
        if (!CheckLOS(target))
        {
            EnterSearch();
            return;
        }
        float distance = Vector3.Distance(transform.position, target.transform.position);



        //if they're close enough and attack isn't on cooldown
        if (!atkCooling && distance < 3)
        {
            agent.SetDestination(transform.position);
            Vector3 facehere = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
            transform.parent.LookAt(facehere); //enemy's last chance to aim before the attack. This is intentionally early to give the player a chance to dodge and attack.
            EnterAttack();
        }

        else if (distance > 3)
        {
            agent.SetDestination(target.transform.position);
        }


    }

    //Without any additional input, enemy will automatically move towards 
    //the player's last known position for X time, then leave the state.
    //
    public override void StateSearch()
    {
        base.StateSearch();
    }

    public override void EnterAttack()
    {
        Vector3 facehere = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z); //Store target position
        transform.parent.LookAt(facehere); //enemy's last chance to aim before the attack. This is intentionally early to give the player a chance to dodge and attack.
        HP.SetFollow(true);
        anim.SetBool("isPursuit", false);
        bshield.enabled = false;
        //Debug.Log("Entering Attack");
        //Debug.Log("Placeholder: I'm attacking now!");

        float distance = Vector3.Distance(transform.position, target.transform.position);

        //if they're close enough and attack isn't on cooldown
        if (distance < 3)
        {
            //Short Range Attacks
            int attacknum = Random.Range(1, 5);
            Debug.Log("Attack number " + attacknum);

            if (attacknum == 4)
            {
                anim.SetBool("isAttack2", true);
            }
            else
            {
                anim.SetBool("isAttack", true);
            }
        }

        base.EnterAttack();
    }
    public void BigDangerOn()
    {
        bshield.enabled = false;
        ccAxe.enabled = true;
    }
    public void SmallDangerOn()
    {
        bshield.enabled = false;
        scAxe.enabled = true;
    }

    void StartRangedAttack()
    {
        anim.Play("RangedAttack");
    }

    public void RangedAttack()
    {
        atkCoolCount = Random.Range(2, atkCoolMax);
        atkCooling = true;

        GameObject myLaser = Instantiate(LaserBall, ShotPos.transform.position, ShotPos.transform.rotation);

        myLaser.GetComponent<LaserProjectile>().SetTarget();
    }

    public override void StateAttack()
    {

    }

    //Called by the animation
    public void EndAttack()
    {
        anim.SetBool("isAttack", false); //End animation state
        anim.SetBool("isAttack2", false);
        if (target != null) //Begin pursuit if target hasn't been lost
        {
            EnterPursue(target);
        }
        //begin attack cooldown, change bool, and deactivate weapon collider



        atkCoolCount = Random.Range(1, atkCoolMax);
        atkCooling = true;
        scAxe.enabled = false;
        ccAxe.enabled = false;
        bshield.enabled = true;
    }

    private void CoolAttack()
    {
        if (atkCoolCount > 0)
        { atkCoolCount -= Time.deltaTime; }
        else
        { atkCooling = false; }
    }

    public override void Damaged(int i)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            if (!gameObject.activeSelf)
            {
                //Debug.Log("Damage blocked");
                noise[0].Play();
                return;
            }
            
            if (!isStunned) // || isStunned && bombed)
            {
                noise[1].Play();
                Flinch();//starts timer
                anim.Play("Flinch");
                anim.SetBool("isFlinching", true);//Pretty sure this is just a remnant of older implementation...
                anim.SetBool("isAttack", false);
                target = null;
                EnterIdle();
            }
            //Debug.Log("Damage NOT blocked");
            //Debug.Log("Blocker applying damage: " + i);
            int tempdam = i;
            myStats.curHearts -= tempdam;
            //Debug.Log("Block health is: " + myStats.curHearts);
            if (myStats.curHearts <= 0 && anim.GetBool("isDead") == false && bombed)
            { BigStunDie(); }

            else if (myStats.curHearts <= 0 && anim.GetBool("isDead") == false)
            { Die(); }
        }
    }

    public void GuardBreak()
    {
        stunTime = 6;
        isStunned = true;
        bombed = true;
        anim.SetBool("isWalking", false);
        anim.SetBool("isPursuit", false);
        noise[3].Play();
        anim.Play("BigFlinch");
        //Debug.Log("Entering Idle");
        //Selects the next waypoint
        agent.SetDestination(transform.position);



        base.EnterIdle();
    }

    public void EndBombed()
    {
        bombed = false;
    }

    public override void Flinch()
    {
        bshield.enabled = false;
        base.Flinch();
    }

    public override void UnFlinch()
    {
        bshield.enabled = true;
        anim.SetBool("isFlinching", false);
        StartTimedInvincibility();
        bombed = false;
    }

    public override void Die()
    {


        //Tells Z-target to lock on to a different enemy

        //Play dead noise
        anim.SetBool("isDead", true);

        //Turns off all the colliders to prevent collision damage
        Collider[] parts;
        parts = GetComponentsInChildren<Collider>();
        for (int i = 0; i <= parts.Length - 1; i++)
        {
            parts[i].enabled = false;
        }

        //Debug.Log("Blocker is dead");

        noise[2].Play();

        base.Die();
    }

    public void BigStunDie()
    {
        //Tells Z-target to lock on to a different enemy

        //Play dead noise
        anim.Play("BigDie");
        //Debug.Log("Blocker is dead");
        Collider[] parts;
        parts = GetComponentsInChildren<Collider>();
        for (int i = 0; i <= parts.Length - 1; i++)
        {
            parts[i].enabled = false;
        }

        noise[2].Play();

        base.Die();
    }

    //Called by death animation
    public void Disappear()
    { transform.parent.gameObject.SetActive(false);
        spawn.GetComponent<SpawnOnDead>().Spawn();
    }

    public override bool CheckLOS(GameObject other)
    {
        Vector3 toTarget = other.transform.position - transform.position;
        float distance = Vector3.Distance(other.transform.position, eye.position);
        RaycastHit hit;
        if (!Physics.Raycast(eye.position, toTarget, out hit, distance, obstacles))
        {
            return true;
        }
        return false;
    }

    //Called by ChildCollision
    public override void collisiondetected(Collider other)
    {
        /*if (myStats.curHearts > 0 && isBlocked == false)
        {
            if (!other.GetComponent<Item>()) //anything attached to the player that doesn't count as part of them needs this item script (or no collider)
            {
                if (other.GetComponentInParent<Entity_Player>())
                {
                    other.GetComponentInParent<Entity_Player>().Damaged(myStats.colDam);
                }
            }
        }*/
    }
}
