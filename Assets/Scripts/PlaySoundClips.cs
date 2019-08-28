using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundClips : MonoBehaviour {

    public AudioClip[] clips;
    private AudioSource audioSource;
    int clipPick;
    private void Start()
    {
        audioSource = GameObject.Find("radio").GetComponent<AudioSource>();
        clipPick = 0;

        /*Debug.Log("CLIP NUMERO: " + clipPick);
        Debug.Log("CLIP QUANTITA': " + clips.Length);*/
    }

    public void LetsPlay()
    {
        /*int clipPick = Random.Range(0, clips.Length);*/
        if (clipPick < clips.Length-1)
        {
            clipPick =  clipPick+1;
        }
        else
        {
            clipPick = 0;
        }

        audioSource.clip = clips[clipPick];
        audioSource.Play();

        //Debug.Log("CLIP NUMERO: " + clipPick);
    }
}
