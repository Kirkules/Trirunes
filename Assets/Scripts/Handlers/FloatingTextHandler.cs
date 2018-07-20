using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FloatingTextHandler : MonoBehaviour {

	Vector2 position;
	float floatingDistance;
	float lifespan;
//	float lifetime_remaining;
//	float start_time;
	float death_time;
	string message;
	Color color;

	// prefab for floating text

	Text textObject;

	public void spawn(Vector2 worldPosition, float floatingDistance, float lifespan, string message, Color color, int fontSize, 
		FontStyle fontStyle){
		// world coordinate position
		this.position = worldPosition;

		// because the "position" of the text canvas is its center, and it is screen-sized....
		this.position.x -= Screen.width / 2;
		this.position.y -= Screen.height / 2;

		this.floatingDistance = floatingDistance;
		this.lifespan = lifespan;

//		this.start_time = Time.time;
		this.death_time = Time.time + lifespan;

//		this.lifetime_remaining = lifespan;
		this.message = message;
		this.color = color;


		this.textObject = GetComponentInParent<Text> ();

		textObject.rectTransform.anchoredPosition = new Vector2 (position.x, position.y);

		// anchored position is actually the center of the rect...
		// so draw the text there
		textObject.alignment = TextAnchor.MiddleCenter;
		textObject.fontSize = fontSize;
		textObject.fontStyle = fontStyle;

		textObject.text = this.message;
		textObject.color = this.color;

		StartCoroutine (this.animateFloatingText ());
	}

	public IEnumerator animateFloatingText(){
		while (Time.time < death_time) {
			yield return new WaitForSeconds (Time.deltaTime);
			Vector2 change_vector = new Vector2(0, (Time.deltaTime / lifespan) * floatingDistance);
			textObject.rectTransform.anchoredPosition += change_vector;
		}

		disappear ();
		yield return null;
	}


	void disappear(){
		// delete the Text object and this handler
		Destroy(gameObject);
	}
}
