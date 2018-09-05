using UnityEngine;
using System.Collections;

public class Spinning : MonoBehaviour {

	public int Speed = 60;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void FixedUpdate()
	{
		transform.Rotate(0,Speed*Time.deltaTime,0);
	}
}
