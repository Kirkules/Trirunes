using UnityEngine;
using System.Collections;

public class Constants : MonoBehaviour {

	public static Constants constants;

	public static Constants Instance(){
		if (!constants) {
			constants = FindObjectOfType (typeof(Constants)) as Constants;
			if (!constants) {
				Debug.LogError ("There aren't any active Constants scripts (needs to be at least one).");
			} else{
				constants.coin_s = 0.1f * constants.s;
				constants.healthbarSize = 2 * constants.s;
				constants.h = constants.s * Mathf.Sqrt (3) / 2;
			}
		}
		return constants;
	}

	public int nRows = 6;
	public int nCols = 15;

	public float s = 50.0f;

	public float coin_s;
	public float healthbarSize;

	// TODO: reimplement constants as a singleton so these can be a Dictionary
	public float damagePieceProbability = 0.22f;
	public float healingPieceProbability = 0.22f;
	public float fervorPieceProbability = 0.23f;
	public float summoningPieceProbability = 0.23f;
	public float coinProbability = 0.1f;

	public Color damageColor;
	public Color healingColor;
	public Color fervorColor;
	public Color coinColor;
	public Color summoningColor;

	// height of equilateral triangle with side length s
	// length is in pixels
	// (Just scale up sprites to change actual size).
	public float h;
}
