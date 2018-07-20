using UnityEngine;
using System.Collections;

public class TriangleHandler : MonoBehaviour {

	public God Rune;
	public int Row;
	public int Col;
	public Color color;
	public GamePieceType type;
	public TriangleMovementType movementType;
	public Vector3 movementDestination;
	public Vector3 movementSource;

	// prefab for exploding piece animation
	//public GameObject explodingTrianglePrefab;

	//Animator animator;

	public TriangleHandler(){
	}

	public void Start(){
		//animator = GetComponent<Animator> ();
		color = GetComponent<SpriteRenderer> ().color;
		movementType = TriangleMovementType.None;
		movementDestination = Vector3.zero;
		movementSource = Vector3.zero;

		// scale up
		if (type.Equals (GamePieceType.Coin)) {
			gameObject.transform.localScale = new Vector3 (Constants.Instance ().coin_s, Constants.Instance ().coin_s, 1);
		} else {
			gameObject.transform.localScale = new Vector3 (Constants.Instance ().s/4, Constants.Instance ().s/4, 1);
		}
	}
		
	public void Update (){
		// arcing toward 
	}

	public void rename(){
		gameObject.name = Row.ToString () + " " + Col.ToString () + " " + type;
	}

	public void reorient(TriangleDirection direction){
		switch (direction) {
		case TriangleDirection.Upward:
			transform.eulerAngles = new Vector3 (0, 0, -30);
			break;
		default:
			transform.eulerAngles = new Vector3 (0, 0, 30);
			break;
		}
	}



	public void startOutline(){
		GameObject mainCamera = GameObject.Find ("Main Camera");
		OutlineEffect outliner = mainCamera.GetComponent<OutlineEffect> ();
		outliner.outlineRenderers.Add (gameObject.GetComponent<SpriteRenderer> ());
	}

	public void startIdle(){
		GameObject mainCamera = GameObject.Find ("Main Camera");
		OutlineEffect outliner = mainCamera.GetComponent<OutlineEffect> ();
		outliner.outlineRenderers.Remove (gameObject.GetComponent<SpriteRenderer> ());
	}
		
	public TriangleHandler(int row, int col){
		Row = row;
		Rune = God.None;
	}
}
