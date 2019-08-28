using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonToggleAnim : MonoBehaviour
{
    private bool toggleState = true;
    private bool toggleStart = true;

    public Animator curtain1;
    public Animator curtain2;
    public Animator curtain3;
    public Animator curtain4;

    // Use this for initialization
    void Start()
    {
        curtain1.speed = 1;
        curtain2.speed = 1;
        curtain3.speed = 1;
        curtain4.speed = 1;
    }


    public void ToggleState()
    {
        if (toggleStart == true)
        {
            curtain1.SetTrigger("idle");
            curtain2.SetTrigger("idle");
            curtain3.SetTrigger("idle");
            curtain4.SetTrigger("idle");
            toggleStart = false;
        }



        if (toggleState == true)
        {
            curtain1.ResetTrigger("apriti");
            curtain1.SetTrigger("chiuditi");

            curtain2.ResetTrigger("apriti");
            curtain2.SetTrigger("chiuditi");

            curtain3.ResetTrigger("apriti");
            curtain3.SetTrigger("chiuditi");

            curtain4.ResetTrigger("apriti");
            curtain4.SetTrigger("chiuditi");

            toggleState = false;
        }
        else
        {
            curtain1.SetTrigger("apriti");
            curtain1.ResetTrigger("chiuditi");

            curtain2.SetTrigger("apriti");
            curtain2.ResetTrigger("chiuditi");

            curtain3.SetTrigger("apriti");
            curtain3.ResetTrigger("chiuditi");

            curtain4.SetTrigger("apriti");
            curtain4.ResetTrigger("chiuditi");

            toggleState = true;
        }
    }
}
