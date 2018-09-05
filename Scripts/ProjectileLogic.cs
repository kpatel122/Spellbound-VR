using UnityEngine;
using System.Collections;

public class ProjectileLogic : MonoBehaviour {

	public int Damage = 20;

	GameLogic GameLogicScript;
	GameObject Player;
	GameObject Terrain;

	void GetComponents()
	{
		Player = GameObject.FindGameObjectWithTag ("Player");
		Terrain =  GameObject.FindGameObjectWithTag ("Terrain");
		GameLogicScript = GameObject.FindGameObjectWithTag ("GameLogic").GetComponent<GameLogic> ();
	}

	void Awake()
	{
		GetComponents ();
	}

	void OnCollisionEnter(Collision collisionInfo) {
		Debug.Log("OnCollisionEnter !!");

		
		if (collisionInfo.gameObject == Player ) { //attach to plaftorn
			GameLogicScript.PlayerDamaged(Damage);
		}

		if (collisionInfo.gameObject != Terrain)
			Destroy (gameObject);
		else {
			Destroy (gameObject, 3); //destroy on terrain after 3 seconds
			Debug.Log("DESTROYED!!!!");
		}
			
		
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
