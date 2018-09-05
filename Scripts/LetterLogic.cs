using UnityEngine;
using System.Collections;

using UnityEngine.UI;



public class LetterLogic : MonoBehaviour {

	GameObject GameLogic;


	// Use this for initialization
	void Start () {
		GameLogic = GameObject.FindGameObjectWithTag ("GameLogic");

		Debug.Assert (GameLogic != null); //there must be  game logic script

		//set the text that gets sent to gamelogic the same as what has been entered in text mesh
		string t = this.GetComponent<TextMesh> ().text; 
		this.GetComponent<Text> ().text = t;
	
	}
	
	// Update is called once per frame
	void Update () {


	
	}

	void OnTriggerEnter(Collider other)
	{

		if (other.gameObject.tag == "Player") {
			GameLogic.SendMessage ("Collected", this.GetComponent<Text> ().text);
			Destroy (this.gameObject);
		}



	}
}