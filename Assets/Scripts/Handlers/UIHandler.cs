using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using System;

public class UIHandler : MonoBehaviour {
	float edgePadding = 10;

	Dictionary<BattleStats, Dictionary<Side, GameObject>> progressBars;
	Dictionary<CounterType, GameObject> counters;

	Vector2 screenSize; // used for detecting that the screen size changed

	DialogBox dialogBox;

	private static UIHandler uiHandler;

	public static UIHandler Instance(){
		if (!uiHandler) {
			uiHandler = FindObjectOfType (typeof(UIHandler)) as UIHandler;
			if (!uiHandler) {
				Debug.LogError ("There aren't any active dialog handler scripts (needs to be at least one).");
			}
		}
		return uiHandler;
	}

	void Awake(){
		dialogBox = DialogBox.Instance ();
	}


	void Start () {

		screenSize.x = 0;
		screenSize.y = 0;

		// set up dicts containing info. for displayed objects
		progressBars = new Dictionary<BattleStats, Dictionary<Side, GameObject>>(){
			{BattleStats.Health, new Dictionary<Side, GameObject>(){
					{Side.Player, GameObject.Find ("Battle UI/Player Health Bar")},
					{Side.Enemy, GameObject.Find ("Battle UI/Enemy Health Bar")},
				}},
			{BattleStats.Fervor, new Dictionary<Side, GameObject>(){
					{Side.Player, GameObject.Find ("Battle UI/Player Fervor Bar")},
					{Side.Enemy, GameObject.Find ("Battle UI/Enemy Fervor Bar")},
				}},
			{BattleStats.Summoning, new Dictionary<Side, GameObject>(){
					{Side.Player, GameObject.Find ("Battle UI/Player Summoning Bar")},
					{Side.Enemy, GameObject.Find ("Battle UI/Enemy Summoning Bar")},
				}},
		};

		counters = new Dictionary<CounterType, GameObject> () {
			{CounterType.Coin, GameObject.Find("Battle UI/Coin Counter")},
		};

		counters [CounterType.Coin].GetComponent<Counter> ().size.x = 15;
		counters [CounterType.Coin].GetComponent<Counter> ().size.y = 15;

		StartCoroutine (screenResizeChecker ());
	}

	// detect screen size change and update the BattleUI to fit
	IEnumerator screenResizeChecker(){
		// don't resize immediately, because UI elements might not be done prepping yet
		yield return new WaitForSeconds (0.05f);

		while (BattleInfo.gameState != GameState.Exiting) {
			if (Screen.width != screenSize.x || Screen.height != screenSize.y) {
				screenSize.x = Screen.width;
				screenSize.y = Screen.height;

				float currentY = edgePadding;
				//resize/relocate progress bars
				foreach (BattleStats type in progressBars.Keys) {
					ProgressBar playerBar = progressBars [type] [Side.Player].GetComponent<ProgressBar> ();
					ProgressBar enemyBar = progressBars [type] [Side.Enemy].GetComponent<ProgressBar> ();

					playerBar.position.x = edgePadding;
					playerBar.position.y = currentY;
					enemyBar.position.x = Screen.width - enemyBar.size.x - edgePadding;
					enemyBar.position.y = currentY;
					currentY += edgePadding + playerBar.size.y;
				}

				// resize/relocate counters
				foreach (CounterType type in counters.Keys) {
					Counter theCounter = counters [type].GetComponent<Counter> ();
					theCounter.position.x = 2*edgePadding;

					theCounter.position.y = currentY + edgePadding;
					currentY += 2*edgePadding + theCounter.size.y ;
					// theCounter.position.y = Screen.height - theCounter.size.y - edgePadding;
				}

				// TODO: once characters are in the game, relocate them

				// resize the menu canvas (grey out the whole screen regardless of what size it is)



			}

			// only check and resize every second
			yield return new WaitForSeconds (1);
		}

	}

	void Update () {

	}

	// like "floating combat text", makes text that appears in a given location, then
	// rises and fades over time, deleting the text object at the end of the lifespan
	public void spawnRisingText(Vector2 position, string message, Color color, float floatingDistance = 20, 
		float lifespan=2, int fontSize = 25, FontStyle fontStyle = FontStyle.Bold){
		GameObject textCanvas = GameObject.Find("Floating Text Canvas");
		GameObject textObject = (GameObject)GameObject.Instantiate (Resources.Load("Prefabs/Floating Text"));
		textObject.transform.parent = textCanvas.transform;
		textObject.GetComponent<FloatingTextHandler> ().spawn (position, floatingDistance, lifespan, message, color, fontSize, fontStyle);
	}

	public void startNewGameDialog(){
		UnityAction yesAction = new UnityAction (() => {BoardHandler.Instance().reset();
														BattleInfo.Instance().resetStats();
														BattleInfo.gameState = GameState.PlayerTurn;});
		UnityAction noAction = new UnityAction (() => {BattleInfo.gameState = GameState.Exiting; 
													   Application.Quit();});
		dialogBox.Choice ("Would you like to battle again?", yesAction, noAction);
	}

	public void youLostDialog(){
		// yes button is just "okay"
		UnityAction yesAction = new UnityAction (startNewGameDialog);
		dialogBox.Choice ("DEFEAT!", yesAction, yesButtonText: "okay", lastDialog:false);
	}

	public void youWonDialog(){
		// yes button is just "okay"
		UnityAction yesAction = new UnityAction (startNewGameDialog);
		dialogBox.Choice ("VICTORY!", yesAction, yesButtonText: "okay", lastDialog:false);
	}

	public void instructionsPopup(){
		UnityAction yesAction = new UnityAction (() => {BattleInfo.gameState = GameState.PlayerTurn;});
		string tutorialText = "Swap adjacent triangles to match three or more pieces! Match Red pieces to damage the enemy, white to heal yourself, green to build your battle fervor (and deal more damage!), blue to build Star Power, and Coins to get richer.";
		dialogBox.ShowInfo (tutorialText, yesAction);
	}

}
