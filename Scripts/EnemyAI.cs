using UnityEngine;
using System.Collections;
using System;

public class EnemyAI : MonoBehaviour {

	private enum ESTATE
	{
		PATROLLING,
		BEGIN_CHASE,
		BEGIN_PATROL,
		CHASING,
		BEGIN_STRIKE,
		STRIKING,
		THROWING_PROJECTILES,
		BEGIN_THROW_PROJECTILES
	};


	private static class ANIM_STATES //cant use enums as ints in c# so using static class instead
	{
		public const int RESET_ANIM_VALUE = 1; //must match animator AnimValue
		public const int THROW_PROJECTILE_ANIM_VALE = 2; //must match animator AnimValue
		public const int STRIKE_ANIM_VALE = 3; //must match animator AnimValue
	}
 

 
	ESTATE OldState = ESTATE.BEGIN_PATROL;
	ESTATE State = ESTATE.PATROLLING; 
	ESTATE_STATUS StateStatus = ESTATE_STATUS.RUNNING;


	[Header("Chase Settings")]
	public int RangeForChase = 10;


	[Header("Strike Settings")]
	public int RangeForStrike = 5;
	public int ReachForStrike = 2; //after anim has played this gets added to the range for strike so plater can still get hit after backing away
	public int DamageForStrike = 0; //how much damage when the user is striked
	public float StrikeAnimDuration = 1f;

	[Header("Projectile Settings")]
	public int RangeForProjectile = 20;
	public int MinimumTimeBetweenProjectile = 4;
	public float DelayProjectileThrownAnim = 0.2f;
	public int TimePersuePlayerForProjectileWhenHidden = 4;
	public int ProjectileSpeed = 20; //TODO make this a function of distance to player
	public Rigidbody ProjectileRigidBody;

	/* These triggers must exist in the objets animater */
	[Header("Enemy Intelligence Settings")]
	public int FieldOfView = 50;

	const string ANIMATOR_ANIM_NAME = "AnimValue"; //the name of the main anim  variable in unity animator
	const string StrikeAnimNameInAnimator = "strike"; // the animator strike animation MUST be called 'strike'


	GameObject ProjectileOrigin;
	ParticleProjectile ParticleProjectile;

	float ProjectileTimer = 0f;
	float CantSeePlayerTimer = 0f;

	int ProjectileSeconds = 0;
	int CantSeePlayerSeconds = 0;


	float DistanceToPlayer;

	GameObject GameLogicObject;
	GameLogic GameLogicScript;

	Transform PlayerPosition;

	NavMeshAgent NavAgent;
	SWS.navMove WaypointScript;
	bool IsWayPointPaused = false; //not implemented in simple waypoint system

	Animator AnimController;

	bool AIStarted = false;
	bool StrikeAnimFinished = false;
	bool CallBackFunctionCalledOnce = false;

	public IEnumerator PlayOneShotAnimation (int AnimToPlay )
	{

		AnimController.SetInteger( ANIMATOR_ANIM_NAME, AnimToPlay ); //1st frame
		yield return null;
		AnimController.SetInteger( ANIMATOR_ANIM_NAME, ANIM_STATES.RESET_ANIM_VALUE ); //2nd frame
	}

	void GetProjectileOrigin()
	{
		foreach (Transform child in transform){
			if(child.gameObject.tag == "ProjectileOrigin"){
				ProjectileOrigin = child.gameObject;
			}
		}
	}



	void GetEnemyComponents()
	{
		NavAgent = GetComponent<NavMeshAgent> ();
		WaypointScript = GetComponent<SWS.navMove> ();
		GameLogicObject = GameObject.FindGameObjectWithTag ("GameLogic");
		PlayerPosition = GameObject.FindGameObjectWithTag ("Player").transform;
		AnimController = GetComponent<Animator> ();

		GameLogicScript = GameLogicObject.GetComponent<GameLogic> ();
		ParticleProjectile = GetComponentInChildren<ParticleProjectile> ();

		GetProjectileOrigin ();

		Debug.Assert (NavAgent != null, "No Navmesh agent assigned to enemy");
		Debug.Assert (WaypointScript != null, "No waypointsript assigned to enemy");
		Debug.Assert (GameLogicObject != null, "No GameLogic tag found in enemy");
		Debug.Assert (PlayerPosition != null, "Player position is null");
		Debug.Assert (AnimController != null, "No animator controller assigned");

		Debug.Assert (ParticleProjectile != null, "ParticleProjectile is null");
	}


	IEnumerator CallAfterWait (float seconds, Action Callback)
	{
		yield  return new WaitForSeconds (seconds);
		Callback ();
	}

	void CallBackAfterWait(float seconds, Action Callback)
	{
		StartCoroutine (CallAfterWait( seconds,Callback) );
	}

	void FireProjectile()
	{


		ParticleProjectile pe =  (ParticleProjectile)Instantiate(ParticleProjectile, ProjectileOrigin.transform.position, transform.rotation);
		pe.TurnOnCollider ();
		Rigidbody ProjectileClone = pe.GetComponent<Rigidbody> ();
	

		//new particle method
		ProjectileClone.transform.LookAt (PlayerPosition);
		ProjectileClone.velocity = ProjectileClone.transform.forward * ProjectileSpeed; //will always launch at player regardless of where the enemy faces
		ProjectileTimer = 0;


		/*
		Rigidbody ProjectileClone = (Rigidbody) Instantiate(ProjectileRigidBody, ProjectileOrigin.transform.position, transform.rotation);
		ProjectileClone.transform.LookAt (PlayerPosition);
		 

		ProjectileClone.velocity = ProjectileClone.transform.forward * ProjectileSpeed; //will always launch at player regardless of where the enemy faces
		ProjectileTimer = 0;
		*/
		 
	}

	void CalculateDistanceToPlayer()
	{
		DistanceToPlayer = Vector3.Distance(PlayerPosition.position, transform.position);
	 
	}

	bool CanSeePlayer()
	{

		RaycastHit hit;
		Vector3 rayDirection = PlayerPosition.position - transform.position;
	 
	 
		if((Vector3.Angle(rayDirection, transform.forward)) < FieldOfView){ // Detect if player is within the field of view
			if (Physics.Raycast (transform.position, rayDirection, out hit)) {

				if (hit.transform.tag == "Player") {
					//Debug.Log("Can see player");
					return true;
				}else{
					//Debug.Log("Can not see player");
					return false;
				}
			}
		}
		//Debug.Log("Can not see player");
		return false;

	}

	void PauseWaypoints()
	{
		if (IsWayPointPaused == false) { //only want this to happen once per state change not once per frame
			WaypointScript.Pause ();
			IsWayPointPaused = true;
		}
	}

	void ResumeWaypoints()
	{
		if (IsWayPointPaused == true) { //only want this to happen once per state change not once per frame
			 
			NavAgent.SetDestination (WaypointScript.waypoints[0].position);
			WaypointScript.Resume();
			IsWayPointPaused = false;
		}
		
	}
		
	public void StopAI()
	{
		//when the level is complete or the player dies, the AI should be stoppped
		WaypointScript.Stop();
 
	}

	public void StartAI()
	{
		AIStarted = true;
	}

	bool WithinThrowProjectileRange()
	{
		return (DistanceToPlayer > RangeForChase) && (DistanceToPlayer <= RangeForProjectile); 
	}

	void UpdateTimers()
	{
		ProjectileTimer += Time.deltaTime;
		ProjectileSeconds = (int)(ProjectileTimer % 60); 

		CantSeePlayerTimer += Time.deltaTime;
		CantSeePlayerSeconds  = (int)(CantSeePlayerTimer % 60); 


	}

	bool MinimumTimeBetweenProjectilesReached()
	{
		return (ProjectileSeconds> MinimumTimeBetweenProjectile);
	}

	void ResetTimeBetweenProjectilesTimer()
	{
		ProjectileTimer = 0;
		ProjectileSeconds = 0; 
	}

	void ResetPlayerHiddenTimer()
	{
		CantSeePlayerTimer = 0;
		CantSeePlayerSeconds = 0;
	}

	bool IsInRangeForStrike()
	{
		return (DistanceToPlayer < RangeForStrike);
	}

	void StrikePlayer()
	{
		StartCoroutine(PlayOneShotAnimation (ANIM_STATES.STRIKE_ANIM_VALE));
			

	}

	ESTATE_STATUS ProcessState()
	{
		ESTATE_STATUS CurrStateStatus = ESTATE_STATUS.RUNNING;


		if (OldState != State) {
			Debug.Log ("State is " + State);
			OldState = State;
		}


		switch (State) {

		case ESTATE.BEGIN_THROW_PROJECTILES:
		case ESTATE.BEGIN_CHASE:
			{
				PauseWaypoints(); 
				NavAgent.SetDestination (PlayerPosition.position);
				NavAgent.Resume (); //sws pause() will pause the nav agent, so it needs to be resumed
				CurrStateStatus = ESTATE_STATUS.COMPLETED;
				ResetPlayerHiddenTimer ();
				ResetTimeBetweenProjectilesTimer ();
			}
			break;
		case ESTATE.BEGIN_PATROL:
			{
				ResumeWaypoints ();
				CurrStateStatus = ESTATE_STATUS.COMPLETED;
			}
			break;
		case ESTATE.BEGIN_STRIKE:
			{
				NavAgent.Stop ();
				StrikePlayer ();
				StrikeAnimFinished = false;

				CurrStateStatus = ESTATE_STATUS.COMPLETED;

			}
			break;
		case ESTATE.STRIKING:
			{
	 
				if(StrikeAnimFinished == false)
				{
					if (CallBackFunctionCalledOnce == false) {
						CallBackFunctionCalledOnce = true;
						 
						CallBackAfterWait (StrikeAnimDuration, () => { //theres a delay between the anim is playing and the projectile is thrown
						StrikeAnimFinished = true;
							 
						});
					}
				}

				if (StrikeAnimFinished == true) {
					NavAgent.SetDestination (PlayerPosition.position);

					if (DistanceToPlayer <= (RangeForStrike+ReachForStrike)) {//check we are still in range for damage
						GameLogicScript.PlayerDamaged (DamageForStrike);
					}

					CurrStateStatus = ESTATE_STATUS.COMPLETED;
					CallBackFunctionCalledOnce = false;
					StrikeAnimFinished = false;
				} else {
					CurrStateStatus = ESTATE_STATUS.RUNNING;
				}

				 

			}
			break;
		case ESTATE.CHASING:
			{

				NavAgent.SetDestination (PlayerPosition.position);
				CurrStateStatus = ESTATE_STATUS.RUNNING;
			}
			break;
		case ESTATE.PATROLLING:
			{
				//let the waypoint manager do it's thing
				CurrStateStatus = ESTATE_STATUS.RUNNING;
			}
			break;
		case ESTATE.THROWING_PROJECTILES:
			{
				NavAgent.SetDestination (PlayerPosition.position);

			
				bool PlayerVisable = CanSeePlayer ();

				CurrStateStatus = ESTATE_STATUS.RUNNING;
				if (PlayerVisable == true) {
					if (MinimumTimeBetweenProjectilesReached ()) {
						
						StartCoroutine( PlayOneShotAnimation (ANIM_STATES.THROW_PROJECTILE_ANIM_VALE) );
						CallBackAfterWait (DelayProjectileThrownAnim, () => { //theres a delay between the anim is playing and the projectile is thrown
							FireProjectile ();
						});


						ResetTimeBetweenProjectilesTimer ();
					}
						
					ResetPlayerHiddenTimer ();
				} else if (CantSeePlayerSeconds > TimePersuePlayerForProjectileWhenHidden) {
					 
					CurrStateStatus = ESTATE_STATUS.COMPLETED; //leave this state
				}
				
			}
			break;

		}
			
		return CurrStateStatus;
	}

	bool CheckForStateThrowingProjectiles()
	{
		return (DistanceToPlayer < RangeForProjectile) && CanSeePlayer () && (State != ESTATE.STRIKING) && (State != ESTATE.CHASING) && (State != ESTATE.THROWING_PROJECTILES);
	}

	bool CheckForStateBeginChase()
	{
		return (DistanceToPlayer < RangeForChase) && (State != ESTATE.CHASING) && (State != ESTATE.STRIKING);
	}

	bool CheckForStateBeginPatrol()
	{
		if (State == ESTATE.THROWING_PROJECTILES)
			return (DistanceToPlayer >= RangeForProjectile);
		else
			return (DistanceToPlayer >= RangeForChase) && (State != ESTATE.PATROLLING);	 
	}

	bool HasCurrentAnimFinished()
	{
 		return ( !AnimController.GetCurrentAnimatorStateInfo(0).IsName(StrikeAnimNameInAnimator) && !AnimController.IsInTransition (0) );
		//return (((AnimController.GetCurrentAnimatorStateInfo (0).normalizedTime) >= 1)); //&& (!AnimController.IsInTransition (0)));
		//return (TimeInStrikeAnim >= StrikeAnimTime);
	}

	bool CheckForStrikePlayer()
	{
		if (State == ESTATE.CHASING) 
		{
			return (IsInRangeForStrike ());
		}
		return false;
	}

	void CheckForStateChange()
	{
		if (CheckForStateThrowingProjectiles ()) {
			SetState (ESTATE.BEGIN_THROW_PROJECTILES);
			 
		}
		else if (CheckForStrikePlayer ()) {
			SetState (ESTATE.BEGIN_STRIKE);
		}
		else if (CheckForStateBeginChase ()) {
			SetState (ESTATE.BEGIN_CHASE);
			 
		} else if (CheckForStateBeginPatrol ()) {
			SetState (ESTATE.BEGIN_PATROL);
			 
		} 
	}

	void SetState(ESTATE NewState)
	{
		OldState = State;
		State = NewState;
		StateStatus = ESTATE_STATUS.RUNNING;
	}

	void SetNextState()
	{
		if (State == ESTATE.BEGIN_PATROL) {
			SetState (ESTATE.PATROLLING);
		}
		else if(State == ESTATE.BEGIN_CHASE) {
			SetState (ESTATE.CHASING);
		}
		else if(State == ESTATE.BEGIN_THROW_PROJECTILES) {
			SetState (ESTATE.THROWING_PROJECTILES);
		}
		else if(State == ESTATE.BEGIN_STRIKE) {
			SetState (ESTATE.STRIKING);
		}
		else if(State == ESTATE.STRIKING) {
			SetState (ESTATE.BEGIN_CHASE);
		}
		else if(State == ESTATE.THROWING_PROJECTILES) {
			SetState (ESTATE.BEGIN_PATROL);
		}

	}

	void ProcessAI()
	{
		UpdateTimers ();
		CalculateDistanceToPlayer ();
		CheckForStateChange ();
		StateStatus = ProcessState ();


		if (StateStatus == ESTATE_STATUS.COMPLETED)
			SetNextState ();
	}

 

	void Awake() {
		GetEnemyComponents ();
	}



	// Use this for initialization
	void Start () {
		 
	}

	 

	// Update is called once per frame
	void Update () {

		//UpdateTimeSinceLastProjectileThrow ();
		if(AIStarted)
			ProcessAI ();
	}
}
