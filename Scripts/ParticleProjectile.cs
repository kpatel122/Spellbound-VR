using UnityEngine;
using System.Collections;

public class ParticleProjectile : MonoBehaviour {

	public ParticleSystem ps;
	public int Damage = 20;
	public float bounciness = 0.5f;

	GameLogic GameLogicScript;
	GameObject Player;
	GameObject Terrain;
	SphereCollider SCollider;

	void GetComponents()
	{
		Player = GameObject.FindGameObjectWithTag ("Player");
		Terrain =  GameObject.FindGameObjectWithTag ("Terrain");
		GameLogicScript = GameObject.FindGameObjectWithTag ("GameLogic").GetComponent<GameLogic> ();
		SCollider = gameObject.GetComponent<SphereCollider> ();
	}

	void Awake()
	{
		GetComponents ();
	}


	// Use this for initialization
	void Start () {

		SCollider.material.bounciness = bounciness;

		ParticleSystem go = Instantiate (ps);
		go.Stop ();
		go.transform.parent = transform;
		go.transform.localPosition = Vector3.zero;
 


	}

	public void TurnOnCollider()
	{

		SCollider.enabled = true;

	}

	void OnCollisionEnter(Collision collisionInfo) {
		if (collisionInfo.gameObject == Player ) { //attach to plaftorn
			GameLogicScript.PlayerDamaged(Damage);
		}

		if (collisionInfo.gameObject != Terrain)
			Destroy (gameObject);
		else {
			Destroy (gameObject, 3); //destroy on terrain after 3 seconds
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
