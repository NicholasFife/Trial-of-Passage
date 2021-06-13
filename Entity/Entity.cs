using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public stats myStats = new stats();
    public LayerMask obstacles;
    public LayerMask player;
    public LayerMask enemies;

    //This bool is turned on automatically by HUDTrigger, causing the enemy to inform HUDTrigger.cs of the first time it is hit
    //so it can trigger the checkpoint.
    private bool isHitCheckpoint;
    private HUDTrigger thisHudTrigger;

    public class stats
    {
        //ALL public variables listed below are accessible by typing
        //myStats.[variable] ex: myStats.maxHearts
        //create your own constructor, or use one of the ones below
        //for any new entity
        public int maxHearts; //health, probably 2 or 4 = a full heart?
        public int curHearts; //current health
        public int colDam; //damage dealt to player on collision
        public int atkDam; //damage dealt to player by basic attack
        public bool hostile; //true = enemy && false = friendly
        public float stunTime; //how long the player should flinch 1 second by default
        public bool invincible = false; //semi-permanent invincibility
        public bool timedInvincible = false; //invincibility on a timer
        public float invincibleCountMax = 1;

        //default constructor (Only used if method found below isn't used to overwrite)
        public stats()
        {
            maxHearts = 4;
            curHearts = maxHearts;
            colDam = 1;
            hostile = false;
        }

        //Call this to manually set stats - heals player if called while injured
        public stats(int max) //For Player
        {
            maxHearts = max;
            Debug.Log("Player Max hearts set to " + maxHearts);
            curHearts = maxHearts * 4;
            Debug.Log("Player curhearts set to " + curHearts);
            invincibleCountMax = 2;
        }
        public stats(int max, int col, int atk, bool bad) //For enemies
        {
            maxHearts = max;
            curHearts = maxHearts;
            colDam = col;
            atkDam = atk;
            hostile = bad;
            stunTime = 1;
        }
    }

    //Assign all MeshRenderers that should flash while invincible in inspector
    //Assign default material and material to flash to while invincible.
    public float invincibileCount = 0;
    public MeshRenderer[] mr;
    public Material matDefault;
    public Material matHurt;
    public float colorSmooth = .1f;

    
    public virtual void Update()
    {
        if (myStats.timedInvincible && myStats.curHearts >0)
        {
            InvincibleColor();
            InvincibleTimer();
        }
    }

    //Use this version of Damaged, please!
    public virtual void Damaged(int i)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            //Debug.Log(gameObject.name + "'s invincibility status is " + myStats.timedInvincible + "and they took damage.");
            if (!gameObject.activeSelf) return;
            int tempdam = i;
            StartTimedInvincibility();
            myStats.curHearts -= tempdam;
            if (myStats.curHearts <= 0)
            { Die(); }
        }
    }
    //Stun is not handled on here, this method is obsolete
    public virtual void Damaged(int i, float t)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            Debug.Log(gameObject.name + "'s invincibility status is " + myStats.timedInvincible + "and they took damage.");
            if (!gameObject.activeSelf) return;
            int tempdam = i;
            StartTimedInvincibility();
            myStats.curHearts -= tempdam;
            if (myStats.curHearts <= 0)
            { Die(); }
        }
    }

    //Does nothing! It used to, but the functionality has been split 
    //for player and enemies into scripts inheriting from this.
    public virtual void Die()
    {
        if (!gameObject.activeSelf) return;
        //gameObject.SetActive(false);
    }

    //Called to receive a bool informing if object can be seen from current position.
    public virtual bool CheckLOS(GameObject other)
    {
        Vector3 toTarget = other.transform.position - transform.position;
        float distance = Vector3.Distance(other.transform.position, transform.position);
        RaycastHit hit;
        if(!Physics.Raycast(transform.position, toTarget, out hit, distance, obstacles))
        {
            return true;
        }
        return false;
    }
    //Manually turn invincibility off and on 
    //NOT on a timer.
    public virtual void ToggleInvincibility()
    {
        myStats.invincible = !myStats.invincible;
    }

    public virtual void StartTimedInvincibility()
    {
        if (isHitCheckpoint == true)
        {
            thisHudTrigger.EnemyWasHit();
            isHitCheckpoint = false;
        }

        invincibileCount = myStats.invincibleCountMax;
        myStats.timedInvincible = true;
    }
    public virtual void InvincibleTimer()
    {
        if (invincibileCount >= 0)
        { invincibileCount -= Time.deltaTime; }
        else
        {
            myStats.timedInvincible = false;
            for (int i = 0; i <= mr.Length - 1; i++)
            {
                mr[i].material = matDefault;
            }
        }
    }
    public virtual void InvincibleColor()
    {
        float smoothedMat = Mathf.PingPong(Time.time, colorSmooth) / colorSmooth;
        for (int i = 0; i <= mr.Length - 1; i++)
        {
            mr[i].material.Lerp(matDefault, matHurt, smoothedMat);
        }
    }

    //Called to deal damage without starting a flinch
    public virtual void eDamaged(int i)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            Debug.Log(gameObject.name + "'s invincibility status is " + myStats.timedInvincible + "and they took damage.");
            if (!gameObject.activeSelf) return;
            int tempdam = i;
            StartTimedInvincibility();
            myStats.curHearts -= tempdam;
            if (myStats.curHearts <= 0)
            { Die(); }
        }
    }

    public virtual void AmHitCheckpoint(HUDTrigger a)
    {
        isHitCheckpoint = true;
        thisHudTrigger = a;
    }
}
