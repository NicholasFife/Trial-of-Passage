using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Entity_Player : Entity
{
    private GameMaster gm;
    public int maxHp = 12;

    private Control_Player cntl;

    private float flinchTime = 1;

    Image[] hearts;
    Image[] empties;

    public Control_Player conPlay;
    public GameOverScreen gmScreen;
    public PlayerPos playerPos;

    public AudioSource lowHealth;
    public AudioClip healthBeep;
    private bool hasPlayed = false;

    public GameObject playerAvatar;

    // Start is called before the first frame update
    public void Awake()
    {
        myStats = new stats(maxHp);

        hearts = FindObjectOfType<HeartsHolder>().GetComponentsInChildren<Image>();
        //Debug.Log("Hearts Array length is " + hearts.Length);
        //Debug.Log("Hearts root is " + hearts[0].transform.parent.name);

        empties = FindObjectOfType<EmptyHeartHolder>().GetComponentsInChildren<Image>();
        /*for (int i = 0; i <= hearts.Length - 1; i++)
        {
            Debug.Log("Reassigning slot " + i + " away from " + hearts[i].name);
            hearts[i] = transform.parent.GetChild(i).GetComponent<Image>();
            Debug.Log("Assigned Heart number " + i + " to " + hearts[i].name);
        }

        
        for (int i = 0; i <= empties.Length - 1; i++)
        { empties[i] = transform.parent.GetChild(i).GetComponent<EmptyHeart>(); }*/

        gm = FindObjectOfType<GameMaster>();
    }

    void Start()
    {
        if (gm.NextLevel())
        {
            gm.SaveNewLevel();
        }
        if (gm.DidReset())
        {
            Debug.Log("Player informed of reset");
            //Set player variables from checkpoint
            gm.PlayerCheckpoint(gameObject);
        }

        cntl = GetComponent<Control_Player>();
        playerPos = GetComponent<PlayerPos>();
        //lowHealth = GetComponent<AudioSource>();
    }

    void FixedUpdate()
    {
        UpdateHearts();
    }

    void UpdateHearts()
    {
        for (int i = 0; i < empties.Length; i++)
        {
            if (i < myStats.maxHearts)
            {
                empties[i].gameObject.SetActive(true);
                //Debug.Log("Setting heart empty number " + i + " to active");
            }
            else
            {
                empties[i].gameObject.SetActive(false);
                //Debug.Log("Setting heart empty number " + i + " to inactive");
            }
                
        }

        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < myStats.curHearts && hearts[i].gameObject.activeInHierarchy == false)
            {
                hearts[i].gameObject.SetActive(true);
                //Debug.Log("Setting heart quarter" + i + " to true");
            }
            else if (i >= myStats.curHearts && hearts[i].gameObject.activeInHierarchy == true)
            {
                hearts[i].gameObject.SetActive(false);
                //Debug.Log("Setting heart quarter" + i + " to false");
            }
        }

        if(myStats.curHearts <= 4 && hasPlayed == false)
        {
            hasPlayed = true;
            StartCoroutine(LowHealthBeep());
        }
        else
        {
            return;
        }
    }

    public override void Damaged(int i)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            Flinch();
            conPlay.a2.Play();
            base.Damaged(i);

        }

    }
    public override void Damaged(int i, float t)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            Flinch();
            conPlay.a2.Play();
            base.Damaged(i, t);
            
        }
        //conPlay.a2.Play();
    }

    private void Flinch()
    {
        if(cntl == null)
        { cntl = GetComponent<Control_Player>(); }
        cntl.Flinch();
    }

    //NOTE - Flinch animation is 1 second, would need a new animation
    //or to scale the playback speed of the current animation
    //fall (very short animation), prone (time adjusted for total stun time, 
    private void Flinch(float t)
    {
        cntl.Flinch(t);
    }
    public override void Die()
    {
        GetComponent<Animator>().Play("New_Death");
        //gmScreen.gameObject.SetActive(true);
        Cursor.visible = true;
        conPlay.a3.Play();
    }
    public void StaticDead()
    {
        //Scene scene = SceneManager.GetActiveScene();
        //SceneManager.LoadScene(scene.name);
        gm.LoadCheckpoint();

    }

    public override void eDamaged(int i)
    {
        if (!myStats.invincible && !myStats.timedInvincible)
        {
            conPlay.a2.Play();
            Debug.Log(gameObject.name + "'s invincibility status is " + myStats.timedInvincible + "and they took damage.");
            if (!gameObject.activeSelf) return;
            int tempdam = i;
            StartTimedInvincibility();
            myStats.curHearts -= tempdam;
            if (myStats.curHearts <= 0)
            { Die(); }
        }
        base.eDamaged(i);
    }

    IEnumerator LowHealthBeep()
    {
        //AudioSource audio = GetComponent<AudioSource>();

        while(myStats.curHearts <= 3)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            //audio.clip = healthBeep;
            lowHealth.Play();
            hasPlayed = false;
        }
    }

    public void AddOneHeart()
    {
        myStats.maxHearts++; //increment value of maximum hearts
        myStats.curHearts = myStats.maxHearts * 4; //heal the player
    }

   
}
