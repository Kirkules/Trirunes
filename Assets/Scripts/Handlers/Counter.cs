using UnityEngine;
using System.Collections;

public class Counter : MonoBehaviour {


	public Vector2 position;
	public Vector2 size;
	public Texture2D icon;

	//public string title;

	public int count;
	public RectTransform.Edge textSide;
	public float padding = 10;
	public CounterType counterType;


	void Start () {
		count = 0;
		textSide = RectTransform.Edge.Right;
	}

	void Update () {
		if (BattleInfo.counterInfo != null) {
			count = BattleInfo.counterInfo [counterType];
		}
	}

	void OnGUI(){

		// draw the counter icon
		GUI.BeginGroup(new Rect(position.x, position.y, size.x, size.y));
		{
			GUI.DrawTexture (new Rect (0, 0, size.x, size.y), icon);
		}
		GUI.EndGroup();

		// configure the counter's text
		GUI.skin.font = (Font)Resources.Load ("Fonts/olivers barney");
		GUI.skin.label.clipping = TextClipping.Overflow;
		GUI.skin.label.normal.textColor = Color.yellow;
		GUI.skin.label.fontSize = 12;

		// put the counter's text in the right place
		switch (textSide){
		case RectTransform.Edge.Right:
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;

			// put title label in
			GUI.Label (new Rect (position.x + size.x + padding, position.y, 150, size.y), count.ToString());
			break;
		case RectTransform.Edge.Left:
			GUI.skin.label.alignment = TextAnchor.MiddleRight;
			GUI.Label (new Rect (0, position.y, position.x - padding, size.y), count.ToString());
			break;
		default:
			print ("ain't no counter text side");
			break;
		} 

		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.label.alignment = TextAnchor.MiddleLeft;
	}

}
