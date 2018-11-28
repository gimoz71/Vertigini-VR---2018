using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class scareTrigger : MonoBehaviour {

	public LandingSpotController _landingSpotController;

	void OnTriggerEnter (Collider other) 
	{
		if (other.name == "Scare")
		{
			_landingSpotController.ScareAll();
		}
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}
}
