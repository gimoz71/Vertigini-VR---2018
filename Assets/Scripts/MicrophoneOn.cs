using UnityEngine;
using System.Collections;
[RequireComponent (typeof(AudioSource))]

public class MicrophoneOn : MonoBehaviour {

	void Start ()
	{
		AudioSource audio = GetComponent<AudioSource>();
		audio.clip = Microphone.Start(null, true, 100, 44100);
		audio.loop = true;
		while (!(Microphone.GetPosition(null) > 0)){}
		Debug.Log("start playing... position is " + Microphone.GetPosition(null));
		audio.Play();
	}
}