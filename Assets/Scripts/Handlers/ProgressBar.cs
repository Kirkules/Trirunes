using UnityEngine;
using System.Collections.Generic;

// Borrowed (original/basic) Idea from "duck" on Unity answers
public class ProgressBar : MonoBehaviour {


	// position is relative to the UI canvas
	public Vector2 position;
	public Vector2 size;

	public Texture2D emptyTexture;
	public Texture2D fullTexture;
	public string title;
	public RectTransform.Edge titleSide;
	public float padding = 10;

	public ProgressBarType barType;

	public float barFill;
	public Vector2 barInfo;

	void Start(){
		size = new Vector2 (Constants.Instance ().healthbarSize, Constants.Instance ().healthbarSize * 0.15f);
		barInfo = new Vector2 (0, 1);
		barFill = 0;
	}

	// get game values to use in display
	void Update () {
		if (BattleInfo.progressBarInfo != null) {
			barInfo = BattleInfo.progressBarInfo [barType];
			barFill = barInfo.x / barInfo.y;
			title = BattleInfo.progressBarTitles [barType];
		}
	}

	void OnGUI(){
		// draw the bar to the correct fill level
		GUI.BeginGroup(new Rect(position.x, position.y, size.x, size.y));
		{
			
			GUI.DrawTexture (new Rect (0, 0, size.x, size.y), emptyTexture);
			//draw the filled-in part:
			GUI.BeginGroup (new Rect (0, 0, size.x * barFill, size.y));
			GUI.DrawTexture (new Rect (0, 0, size.x, size.y), fullTexture);
			GUI.EndGroup ();
		}
		GUI.EndGroup();

		// configure the bar's title
		GUI.skin.font = (Font)Resources.Load ("Fonts/olivers barney");
		GUI.skin.label.clipping = TextClipping.Overflow;
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.fontSize = 12;

		// put the bar's title in the right place
		switch (titleSide){
		case RectTransform.Edge.Right:
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;

			// put title label in
			GUI.Label (new Rect (position.x + size.x + padding, position.y, 150, size.y), title);
			break;
		case RectTransform.Edge.Left:
			GUI.skin.label.alignment = TextAnchor.MiddleRight;
			GUI.Label (new Rect (0, position.y, position.x - padding, size.y), title);
			break;
		} 

		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.alignment = TextAnchor.MiddleLeft;
		// put lower/upper amounts
		string labelText = barInfo.x.ToString() + " / " + barInfo.y.ToString();
		GUI.Label (new Rect (position.x + padding, position.y + padding, size.x, size.y), labelText);
	}
}
