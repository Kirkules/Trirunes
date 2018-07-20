using UnityEngine;
using System.Collections;

public class ResizeBGToScreen : MonoBehaviour {

	// Use this for initialization
	void Start () {
		StartCoroutine (resizeTransformToScreen ());
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public IEnumerator resizeTransformToScreen(){
		int minsize;

		while (BattleInfo.gameState != GameState.Exiting) {
			// screen may not be square, so just make the image big enough that it fits in the screen
			minsize = Mathf.Max(Screen.width, Screen.height);
			transform.localScale = new Vector3(minsize, minsize, 0);
			yield return new WaitForSeconds (1);
		}

		yield return null;
	}
}
