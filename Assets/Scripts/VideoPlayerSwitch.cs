using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerSwitch : MonoBehaviour {

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;

    public float audioClipVolumeDimmer;

    // Use this for initialization
    void Start () {

        videoPlayer = GameObject.Find("loewe_butt").GetComponent<VideoPlayer>();
        audioSource = GameObject.Find("radio").GetComponent<AudioSource>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SwitchVideo()
    {
        videoPlayer.Play();
        Debug.Log("PLAY");
        //audioSource.volume = audioClipVolumeDimmer;
        StartCoroutine(FadeOut(audioSource, 20f));
        videoPlayer.loopPointReached += RecoverVolume;
    }

    void RecoverVolume(VideoPlayer vp)
    {
        //audioSource.volume = 1;
        StartCoroutine(FadeIn(audioSource, 40f));
    }


    public  IEnumerator FadeOut(AudioSource audioSource, float FadeTime)
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > audioClipVolumeDimmer)
        {
            audioSource.volume -= startVolume * Time.deltaTime / FadeTime;
            yield return null;
        }
        //audioSource.Stop();
    }
    public  IEnumerator FadeIn(AudioSource audioSource, float FadeTime)
    {
        //audioSource.Play();
        audioSource.volume = audioClipVolumeDimmer;
        while (audioSource.volume < 1)
        {
            audioSource.volume += Time.deltaTime / FadeTime;
            yield return null;
        }
    }

}
