using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gravityTrigger : MonoBehaviour {
    
	void OnTriggerEnter (Collider other) 
	{
		if(other.tag == "box")
		{
			GameObject.Find("SteamVRObjects").GetComponent<Rigidbody>().useGravity = true;
			GameObject.Find("SteamVRObjects").GetComponent<Rigidbody>().isKinematic = false;
			GameObject.Find("SteamVRObjects").GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
		}
		if(other.name == "Terrain")
		{
			GameObject.Find("SteamVRObjects").GetComponent<Rigidbody>().useGravity = false;
			GameObject.Find("SteamVRObjects").GetComponent<Rigidbody>().isKinematic = true;
			//Application.LoadLevel ("start");
			SceneManager.LoadScene ("start");
		}
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
