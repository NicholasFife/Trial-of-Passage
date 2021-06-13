using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control_Shield : MonoBehaviour
{
    private Animator anim;
    public bool blocking;
    public Control_Actions actionButton;
    public Collider ShieldCollider;

    public GameObject Shield;

    // Start is called before the first frame update
    void Start()
    {
        anim = transform.parent.GetComponentInParent<Animator>();
        actionButton = FindObjectOfType<Control_Actions>();
        ShieldCollider.enabled = false;
        Shield = transform.GetChild(0).gameObject;
        Shield.SetActive(false);
    }
    void Update()
    {
        if (Shield.activeSelf == true && !anim.GetBool("isUp") == true)
        { actionButton.TryReadyPutAway(); }
        if(anim.GetBool("isUp") == false)
        { ShieldCollider.enabled = false; }
    }
    
    public void PullShield()
    {
        Shield.SetActive(true);
    }
    public void PutAwayShield()
    {
        Shield.SetActive(false);
    }
    public void RaiseShield()
    {
        Debug.Log("RaiseShield Called to animator on " + anim.gameObject.name);
        Shield.SetActive(true);
        ShieldCollider.enabled = true;
        blocking = true;
        anim.SetBool("isUp", true);
    }
    public void Blocking()
    {
        ShieldCollider.enabled = true;
        blocking = true;
    }
    public void LowerShield()
    {
        anim.SetBool("isUp", false);
        ShieldCollider.enabled = false;
        blocking = false;
    }
    public bool CheckBlock()
    { return blocking; }
}
