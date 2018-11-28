using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveCar : MonoBehaviour {


	public string newPath = "";
	public string newLoop = "loop";
	public int newTime = 10;


	// Use this for initialization
	void Start () {
		iTween.Defaults.easeType = iTween.EaseType.linear;
		iTween.MoveTo(gameObject, iTween.Hash("path", iTweenPath.GetPath(newPath), "delay", 1, "time", newTime, "lookahead", .036, "looktime", .05 , "orienttopath", true, "loopType", newLoop));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}	
