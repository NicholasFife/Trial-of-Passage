using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Stationary : Entity_Enemy
{
    public bool isImmature;

    public Material immature;
    
    private GameObject fov;

    private float coolMax = 2;
    private float coolCur;
    private bool cooling = false;

    public AudioSource flailHit;
    public AudioSource flailDeath;

    public SpawnOnDead spawn;

    // Start is called before the first frame update
    public override void Start()
    {
        var flailSounds = GetComponents<AudioSource>();
        flailHit = flailSounds[0];
        flailDeath = flailSounds[1];

        if (!isImmature)
        {
            myStats = new stats(6, 1, 2, true);
            base.Start();
        }
        else
        {
            matDefault = immature;
            for (int i = 0; i <= mr.Length - 1; i++)
            {
                mr[i].material = matDefault;
            }
            myStats = new stats(3, 1, 1, true);
            fov = transform.parent.GetComponentInChildren<EnemyFOVController>().gameObject;
            fov.gameObject.SetActive(false);
            base.Start();
        }
    }

    // Update is called once per frame
    public override void Update()
    {
        if(cooling)
        { coolAttack(); }

        //Controls Icon bools
        if (myStats.timedInvincible)
        {
            noSworddmg = true;
            noSlingshotdmg = true;
            noBombdmg = true;
        }

        else
        {
            noSworddmg = false;
            noSlingshotdmg = false;
            noBombdmg = false;
        }

            base.Update();
        
    }
    //Purposefully overriding Patrol and Search into Idle because the stationary enemy cannot chase the player.
    public override void EnterSearch()
    {
        EnterIdle();
    }
    public override void EnterPatrol()
    {
        EnterIdle();
    }
    public override void EnterIdle()
    {
        if (myStats.curHearts > 0)
        {
            anim.SetBool("seePlayer", false);
            base.EnterIdle();
        }
    }

    //In this case "Pursue" is really more "awake" but it has been repurposed
    //for the Flailer's needs.
    public override void EnterPursue(GameObject other)
    {
        if (curState != EnemyStates.Pursue)
        {
            coolAttackReset();
            anim.SetBool("seePlayer", true);
            base.EnterPursue(other);
        }
    }

    public override void StatePursue()
    {

        if (CheckLOS(target) == true)
        {
            if (GetComponent<Entity_Enemy>() && !cooling)
            {
                anim.SetBool("attackReady", true);
            }
            else
            {
                anim.SetBool("seePlayer", true);
            }
        }
        else
        { EnterIdle(); }
    }

    public override void EnterAttack()
    {
        //Debug.Log("Enter Attack called on Enemy_Stationary, there is a bug in the code");
        EnterIdle();
    }

    private void coolAttackReset()
    {
        coolCur = coolMax;
        cooling = true;
        anim.SetBool("attackReady", false);
    }

    public void attackDone()
    {
        anim.SetBool("attackReady", false);
        coolCur = coolMax;
        cooling = true;
    }

    private void coolAttack()
    {
        if (coolCur >= 0)
        { coolCur -= Time.deltaTime; }
        else
        {
            cooling = false;
            anim.SetBool("attackReady", true);
        }
    }

    public override void Damaged(int i)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            if (!gameObject.activeSelf) return;
            if (!isStunned)
            { Flinch(); }

            int tempdam = i;
            myStats.curHearts -= tempdam;
            if (myStats.curHearts <= 0)
            { Die(); }

            if (anim.GetBool("isDead") == false)
            {
                anim.Play("Flailer Flinch");
                flailHit.Play();
            }
            anim.SetBool("isFlinching", true);
        }
    }

    public override void Die()
    {
        //Turns off all the mesh colliders to prevent collision damage
        //And to prevent the player from being able to use the dying enemy to jump
        MeshCollider[] parts;
        parts = GetComponentsInChildren<MeshCollider>();
        for (int i = 0; i<= parts.Length-1; i++)
        {
            parts[i].enabled = false;
        }

        EnterIdle();
        flailDeath.Play();
        anim.SetBool("isDead", true);

        base.Die();
    }

    public void Disappear()
    {
        Destroy(gameObject.transform.parent.gameObject);
        spawn.GetComponent<SpawnOnDead>().Spawn();
    }

}
