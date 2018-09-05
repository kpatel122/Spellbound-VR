using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public enum ESTATE_STATUS
{
	RUNNING,
	SUSPENDED,
	COMPLETED,
	FAILED,
	ABORTED,
	NONE
};

public class GameLogic : MonoBehaviour {

	//main game states
	private enum EGAME_STATE
	{
		INTRO,
		INITIALISE_PLAY,
		PLAYING,
		OUTRO,
		GAME_OVER
	};
		

	EGAME_STATE GameState = EGAME_STATE.INTRO;
	ESTATE_STATUS GameStateStatus = ESTATE_STATUS.RUNNING;


	public string Question;
	public string Answer;

	GameObject HealthTextMesh;
	GameObject QuesCanvas;
	GameObject AnsCanvas;

	GameObject[]  Enemies;

	Rigidbody PlayerRigidBody;
	SWS.splineMove PlayerSplineMoveScript;
 

	List<GameObject> RandomLettersPositions;
	List<GameObject> StrategicLettersPositions;


	Image WholeScreenColour;

	Image FadeImage;
	bool FadeStarted = false;
	float FadeSpeed = 0.5f;

	float FlashSpeed = 0.2f;
	//public Color DamageColour = new Color (1f, 0f, 0f, 0.9f);
	public Color DamageColour = new Color (1f, 0f, 0f, 0.9f);

	public Color FadeInColour = new Color (0f, 0f, 0f, 0.9f);
	public Color FadeOutColour = new Color (1f, 1f, 1f, 0.9f);

	public AudioClip PickUpSound;
	public AudioClip WrongPickupSound;
	public AudioClip PlayerDamageSound;
	public AudioClip PlayerWinsSound;
	public AudioClip PlayerDiesSound;

	private AudioSource AudioSource;

	private TextMesh PlayerAnswerText; 
	private TextMesh HealthText;

	private int Health = 100;
	private int HealthStep = 50;

	private string AvailableAlaphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	bool damaged = false;

	bool PlayerAlive = true;
	bool PlayerHasCollectedAllLetters = false;

	bool CameraIntroComplete = false; //Camera intro


	static bool playingMusic = false; //TODO: temp only until proper game manager is finished

	void FillLettersPositionsList(string lettersTag, List<GameObject> outputList)
	{
		//fill outputlist with all child gameobjects of letters tag

		Transform[] childs;
		GameObject LettersParent =  GameObject.FindGameObjectWithTag (lettersTag);
		childs = LettersParent.GetComponentsInChildren<Transform>();

		foreach (Transform child in childs)
		{
			if (LettersParent.GetInstanceID() != child.gameObject.GetInstanceID() ) 
			{
				outputList.Add (child.gameObject);
			}
		}
	}


	void ShuffleList(List<GameObject> ListToShuffle)
	{
		for (int i = 0; i < ListToShuffle.Count; i++) {
			GameObject temp = ListToShuffle[i];
			int randomIndex = Random.Range(i, ListToShuffle.Count);
			ListToShuffle[i] = ListToShuffle[randomIndex];
			ListToShuffle[randomIndex] = temp;
		 
		}

	}
		
	void CreateLettersList ()
	{
		RandomLettersPositions = new List<GameObject>();
		StrategicLettersPositions = new List<GameObject>();

		FillLettersPositionsList ("RandomLetters",RandomLettersPositions);
		FillLettersPositionsList  ("StrategicLetters",StrategicLettersPositions);

		ShuffleList (RandomLettersPositions);

	}
	void SetLetter(GameObject Letter, string Value)
	{
		Letter.GetComponent<TextMesh>().text = Value;
	}

	char RandomLetter()
	{
		return AvailableAlaphabet[ Random.Range (0, AvailableAlaphabet.Length) ];
	}
		

	void AssignAnswerLettersFromList(ref List<GameObject> PositionList, ref int AnswerPosition)
	{
		bool UseRandomLetters = false;
		int AnswerLength = Answer.Length;

		UseRandomLetters = (AnswerPosition >= AnswerLength);

		for (int i = 0; i < PositionList.Count; i++) 
		{ 
			if (UseRandomLetters) 
			{
				SetLetter(PositionList[i] , RandomLetter().ToString() );
			} 
			else 
			{
				SetLetter ( PositionList[i], Answer[AnswerPosition].ToString() );

				//if the letter is part of the answer make sure the same letter cant be used when placing random letters
				//as this could potentially bypass the strategic letter placement
				AvailableAlaphabet = AvailableAlaphabet.Replace (Answer [AnswerPosition].ToString (), ""); 

				AnswerPosition++;

			}
				
			if (AnswerPosition >= AnswerLength)
				UseRandomLetters = true;


		}//end for loop
			
		
	}

	void PlaceLetters()
	{

		int CurrentAnswerLetter = 0;
		AssignAnswerLettersFromList(ref StrategicLettersPositions, ref CurrentAnswerLetter );
		AssignAnswerLettersFromList(ref RandomLettersPositions, ref CurrentAnswerLetter );
	}

	// Use this for initialization

	void Awake()
	{
		ResetFade ();
		GetGameObjects ();
		SetupGameText ();
		CreateLettersList ();
		PlaceLetters ();
		PlayerRigidBody.isKinematic = true;
	}

	void GetGameObjects()
	{
		//all member variables that reference game objects are initialized here

		AudioSource = GetComponent<AudioSource> ();


		QuesCanvas = GameObject.FindGameObjectWithTag ("Question");
		AnsCanvas = GameObject.FindGameObjectWithTag ("Answer");
		HealthTextMesh = GameObject.FindGameObjectWithTag ("Health");
		Enemies = GameObject.FindGameObjectsWithTag ("Enemy");

		WholeScreenColour = (Image)GameObject.FindGameObjectWithTag ("Damage").GetComponent<Image>();

		PlayerRigidBody = GameObject.FindGameObjectWithTag ("Player").GetComponent<Rigidbody> ();
		//PlayerSplineMoveScript = GameObject.FindGameObjectWithTag ("Player").GetComponent<SWS.splineMove> ();


		Debug.Assert (QuesCanvas != null,"Question Canvas not assigned");
		Debug.Assert (AnsCanvas != null,"Answer Canvas not assigned");
		Debug.Assert (HealthTextMesh != null,"Health texture mesh not assigned");
		Debug.Assert (WholeScreenColour != null,"Damage image not assigned");
		Debug.Assert (Enemies != null, "No Enemies found");
		Debug.Assert (PlayerRigidBody != null, "No RigidBody for player");
		//Debug.Assert (PlayerSplineMoveScript != null, "No PlayerSplineMoveScript for player");

	}

	void SetupGameText()
	{
		QuesCanvas.GetComponent<TextMesh> ().text = Question;
		PlayerAnswerText = AnsCanvas.GetComponent<TextMesh> ();

		HealthText = HealthTextMesh.GetComponent<TextMesh> ();

		HealthText.text = Health + "%";

		Answer = Answer.ToUpper ();
		foreach (char c in Answer)
		{
			PlayerAnswerText.text += "*";
		}
	}

	void Start () {

	}

	public void PlayerDamaged(int DamgeAmount)
	{
		PlayDamageSound (); 
		RemoveHealth (DamgeAmount);

		damaged = true;
	}

	private void PlayPickupSound(bool found)
	{
		if (found) 
			AudioSource.clip = PickUpSound;
		else
			AudioSource.clip = WrongPickupSound;

		AudioSource.PlayOneShot (AudioSource.clip);
	}


	void PlayLevelWinMusic()
	{
		

		if(!playingMusic)
		{
			GameObject g = GameObject.FindGameObjectWithTag ("MainCamera");
			AudioSource  a = g.GetComponent<AudioSource> ();
			a.clip = PlayerWinsSound;
			a.Play ();
			playingMusic = true;
		}
	}

	void LevelFinished()
	{
		SetQuestionText ("You Win!");
		PlayLevelWinMusic ();
		DestroyAllEnemies ();
	}



	bool HaveAllLettersBeenCollected()
	{
		bool FoundAllLetters = true; //check if player has won this level
		for (int i = 0; i < PlayerAnswerText.text.Length; i++) {
			if (PlayerAnswerText.text[i] == '*') {
				FoundAllLetters = false;
				break;
			}
		}
		return FoundAllLetters;
	}

	public void PlayerHitWithProjectile(int Damage)
	{
		PlayerDamaged (Damage);
	}

	public void Collected(string colText)
	{
		if (!PlayerAlive || PlayerHasCollectedAllLetters)
			return;

		colText = colText.ToUpper ();
		bool letterExists = false;
	
		string res = "";
		for (int i=0; i<Answer.Length;i++)
		{
			if (Answer [i] == colText [0] && PlayerAnswerText.text [i] == '*') { //the letter collected exists in the answer

				if(!letterExists)
					res += colText [0];
				else
					res += PlayerAnswerText.text [i];

				letterExists = true;

				 
			}
			else 
			{
				if (PlayerAnswerText.text [i] == '*') { //the letter doesnt exist, add the axtrix
					res += "*"; //keep the astrixs'
				} else {
					res += PlayerAnswerText.text [i]; //keep the old text that they collected correctly

				}

			}
		}
		PlayerAnswerText.text = res;
		PlayPickupSound (letterExists);

		if(letterExists == false)
			RemoveHealth (HealthStep);

	}


	void DestroyAllEnemies()
	{
		 
		for (int i = 0; i < Enemies.Length; i++) {
			EnemyAI Ai = Enemies[i].GetComponent<EnemyAI> ();
			Ai.StopAI ();
			Destroy (Enemies[i].gameObject);
		}
	}

	void PlayDamageSound()
	{
		AudioSource.clip = PlayerDamageSound;
		AudioSource.PlayOneShot (AudioSource.clip);
	}

	void PlayDeathSound()
	{
		AudioSource.clip = PlayerDiesSound;
		AudioSource.PlayOneShot (AudioSource.clip);

	}

	void SetQuestionText(string QuestionText)
	{
		QuesCanvas.GetComponent<TextMesh> ().text = QuestionText;
	}

	void PlayerDies()
	{
		SetQuestionText ("You Lose!");
		PlayDeathSound ();
		DestroyAllEnemies ();
	}

	void RemoveHealth(int Amount)
	{
		Health -= Amount;
		HealthText.text = Health + "%";
	}

	bool IsPlayerAlive()
	{
		PlayerAlive = (Health <= 0) ? false : true; 
		return PlayerAlive;
	}

	void ProcessDamage()
	{
		if (damaged) 
		{
			damaged = false;
			WholeScreenColour.color = DamageColour; 

			if (! IsPlayerAlive() ) {
				PlayerDies ();
			}
		} 
		else 
		{
			//fade from damage colour back to normal
			WholeScreenColour.color = Color.Lerp (WholeScreenColour.color, Color.clear, FlashSpeed * Time.deltaTime);
		}
	}

	bool HasFadeFinished()
	{
		return (WholeScreenColour.color.a < 0.1f);
	}
	void ResetFade()
	{
		FadeStarted = false;
	}

	ESTATE_STATUS FadeIntoScene()
	{
		ESTATE_STATUS FadeStatus = ESTATE_STATUS.RUNNING;

		if (FadeStarted == false) {
			WholeScreenColour.color = FadeInColour;
			FadeStarted = true; //do this once
		} else {
			WholeScreenColour.color = Color.Lerp (WholeScreenColour.color, Color.clear, FadeSpeed * Time.deltaTime);

			if (HasFadeFinished () == true) {
				FadeStatus = ESTATE_STATUS.COMPLETED;
			}
		}

		return FadeStatus;
	}

	void StartAllEnemyAI(){
		 
		for (int i = 0; i < Enemies.Length; i++) {
			EnemyAI Ai = Enemies[i].GetComponent<EnemyAI> ();
			Ai.StartAI ();
		 
		}
	}

	public void CameraIntroFinished()
	{
		CameraIntroComplete = true;
		PlayerSplineMoveScript.Stop ();
		PlayerSplineMoveScript.enabled = false;
		Debug.Log ("CameraIntroFinished() called ");
	}

	ESTATE_STATUS HasCameraIntroFinished ()
	{
		return CameraIntroComplete == true ? ESTATE_STATUS.COMPLETED : ESTATE_STATUS.RUNNING;
	}
 
 

	ESTATE_STATUS ProcessGameState()
	{
		ESTATE_STATUS StateStatus = ESTATE_STATUS.RUNNING; //the default state status
 

		switch (GameState) {

		case EGAME_STATE.INTRO:
			{
				StateStatus = ESTATE_STATUS.RUNNING;
				FadeIntoScene ();

				//if (HasCameraIntroFinished () == ESTATE_STATUS.COMPLETED) {
					StateStatus = ESTATE_STATUS.COMPLETED;
					PlayerRigidBody.isKinematic = false;
				//}
					
			}
			break;
		case EGAME_STATE.INITIALISE_PLAY:
			{
				//one time init before the game loop start
				StartAllEnemyAI();
				StateStatus = ESTATE_STATUS.COMPLETED;
			 
			}
			break;
		case EGAME_STATE.PLAYING:
			{
				PlayerHasCollectedAllLetters = HaveAllLettersBeenCollected ();
				if (PlayerHasCollectedAllLetters) 
				{
					LevelFinished ();
					StateStatus = ESTATE_STATUS.COMPLETED;
				 
				}
				ProcessDamage ();
			}
			break;
		case EGAME_STATE.OUTRO:
			{
			}
			break;
		}

		return StateStatus;
	}

	void SetNextGameState()
	{
		if (GameState == EGAME_STATE.INTRO) {
			GameState = EGAME_STATE.INITIALISE_PLAY;
		} 
		else if (GameState == EGAME_STATE.INITIALISE_PLAY) {
			GameState = EGAME_STATE.PLAYING;
		}
		else if (GameState == EGAME_STATE.PLAYING) {
			GameState = EGAME_STATE.OUTRO;
		}


	}

	// Update is called once per frame
	void Update () 
	{

		if (GameStateStatus == ESTATE_STATUS.COMPLETED) {
			SetNextGameState ();
		}
	 
		GameStateStatus = ProcessGameState ();
	}
}
