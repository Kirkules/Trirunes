using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

public class DialogBox : MonoBehaviour {

	public Text dialogText;
	public Image icon;
	public Button yesButton;
	public Button noButton;

	public GameObject modalPanel; // the panel containing the actual dialog elements

	private static DialogBox dialogBox; // only one dialog box at a time....

	public static DialogBox Instance(){
		if (!dialogBox) {
			dialogBox = FindObjectOfType (typeof(DialogBox)) as DialogBox;
			if (!dialogBox) {
				Debug.LogError ("There aren't any active dialog handler scripts (needs to be at least one).");
			}
		}
		return dialogBox;
	}

	public void ShowInfo(string text, UnityAction okayEvent, string yesButtonText = "okay"){

		BattleInfo.gameState = GameState.InMenu;

		yesButton.onClick.RemoveAllListeners (); // reuse buttons for each popup
		yesButton.GetComponentInChildren<Text>().text = yesButtonText;
		yesButton.GetComponentInChildren<Text>().color = Color.white;
		yesButton.onClick.AddListener (okayEvent);
		yesButton.onClick.AddListener (CloseDialog);
		this.yesButton.gameObject.SetActive (true);

		// no "no" button for this dialog

		noButton.gameObject.SetActive (false);

		this.dialogText.text = text;
		this.dialogText.fontSize = 25;

		this.icon.gameObject.SetActive (false);
		modalPanel.SetActive (true); // open dialog
	}

	public void Choice (string text, UnityAction yesEvent, UnityAction noEvent = null, string yesButtonText = "yes", 
		string noButtonText = "no", bool lastDialog = true){
		//modalPanel.SetActive (true); // open dialog

		BattleInfo.gameState = GameState.InMenu;

		yesButton.onClick.RemoveAllListeners (); // reuse buttons for each popup
		yesButton.GetComponentInChildren<Text>().text = yesButtonText;
		yesButton.GetComponentInChildren<Text>().color = Color.white;
		yesButton.onClick.AddListener (yesEvent);
		if (lastDialog) {
			yesButton.onClick.AddListener (CloseDialog);
		}
		this.yesButton.gameObject.SetActive (true);

		// no "no" button for this dialog
		if (noEvent == null) {
			noButton.gameObject.SetActive (false);
		} else {

			noButton.gameObject.SetActive (true);
			noButton.onClick.RemoveAllListeners (); // reuse buttons for each popup
			noButton.GetComponentInChildren<Text> ().text = noButtonText;
			noButton.GetComponentInChildren<Text>().color = Color.white;
			noButton.onClick.AddListener (noEvent);
			if (lastDialog) {
				noButton.onClick.AddListener (CloseDialog);
			}
		}

		this.dialogText.text = text;
		this.dialogText.fontSize = 40;

		this.icon.gameObject.SetActive (false);
		modalPanel.SetActive (true); // open dialog
	}

	public void CloseDialog(){
		modalPanel.SetActive (false);
	}

}
