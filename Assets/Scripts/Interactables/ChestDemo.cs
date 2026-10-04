using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestDemo : MonoBehaviour {
    private static readonly int CloseHash = Animator.StringToHash("close");
    private static readonly int OpenHash = Animator.StringToHash("open");

    //This script goes on the ChestComplete prefab;

    public Animator chestAnim; //Animator for the chest;

	// Use this for initialization
	void Awake ()
    {
        //get the Animator component from the chest;
        chestAnim = GetComponent<Animator>();
        //start opening and closing the chest for demo purposes;
        StartCoroutine(OpenCloseChest());
	}


    IEnumerator OpenCloseChest()
    {
        //play open animation;
        chestAnim.SetTrigger(OpenHash);
        //wait 2 seconds;
        yield return new WaitForSeconds(2);
        //play close animation;
        chestAnim.SetTrigger(CloseHash);
        //wait 2 seconds;
        yield return new WaitForSeconds(2);
        //Do it again;
        StartCoroutine(OpenCloseChest());

    }
}
