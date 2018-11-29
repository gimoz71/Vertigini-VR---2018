using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;


public class restartScene : MonoBehaviour {

	public string loadScene;

	void OnTriggerEnter (Collider other) 
	{
		if(other.name == "restart")
		{
			//Application.LoadLevel ("ambient test");
			//SceneManager.LoadScene ("ambient test");
			SteamVR_LoadLevel.Begin(loadScene);
		}
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
