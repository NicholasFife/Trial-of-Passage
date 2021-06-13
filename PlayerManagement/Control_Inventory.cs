using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Use this to store all inventory variables
public class Control_Inventory : MonoBehaviour
{
    private int ammoSeeds = 20;
    private int bombs = 8;
    private static int goldOrbs;

    private Text ammoText;
    private Text bombText;
    public Slingshot sshot;
    public ZSling zshot;

    private bool hasTriple;

    public int maxBombs = 8;
    public int maxSeeds = 20;

    // Start is called before the first frame update
    void Start()
    {
        ammoText = FindObjectOfType<UI_Slingshottxt>().GetComponent<Text>();
        bombText = FindObjectOfType<UIBomb_Text>().GetComponent<Text>();
        if(ammoSeeds <= 0)
        { sshot.hasStone(false); }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInvUI();
    }

    private void UpdateInvUI()
    {
        ammoText.text = ammoSeeds.ToString();
        bombText.text = bombs.ToString();
    }

    public void TripleReceived()
    {
        hasTriple = true;
    }

    public bool CheckTriple()
    { return hasTriple; }

    public void MinusAmmo()
    {
        ammoSeeds--;
    }
    public void AddAmmo(int i)
    {
        ammoSeeds += i;
    }
    public int GetAmmo()
    { return ammoSeeds; }
    public void SetAmmo(int i)
    { ammoSeeds = i; }

    public void MinusBomb()
    { bombs--; }
    public void AddBombs(int i)
    { bombs += i; }
    public int GetBombs()
    { return bombs; }
    public void SetBombs(int i)
    { bombs = i; }

    public void SetMaxBombs(int b)
    {
        maxBombs = b;
    }

    public void SetMaxSeeds (int b)
    {
        maxSeeds = b;
    }
}
