using UnityEngine;
using System.Collections;

public class BringToFront : MonoBehaviour {

	public void OnEnable(){
		transform.SetAsLastSibling ();
	}
}
