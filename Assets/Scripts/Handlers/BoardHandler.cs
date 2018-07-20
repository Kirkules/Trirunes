using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;



public class BoardHandler : MonoBehaviour
{
	public GameObject[,] triangles;
	public Vector3[,] incenters;
	public bool[,] selected;

	// Board sounds
	public AudioClip explosionSound;
	public AudioClip swapSound;
	private AudioSource audioSource;

	// track matches
	public bool[,] inMatchGroup;
	//public List<Vector2>[,] matchGroupLists = new List<Vector2>[Constants.Instance ().nRows, Constants.Instance ().nCols];

	// GamePieceTypes: None, Red, Blue, Purple, White, Coin
	public List<GameObject> piecePrefabs;

	// row and column of the last triangle selected
	public int lastRow;
	public int lastCol;

	public bool slidingLeft;

	public Vector3 corner;

	private static BoardHandler board;

	public static BoardHandler Instance(){
		if (!board) {
			board = FindObjectOfType (typeof(BoardHandler)) as BoardHandler;
			if (!board) {
				Debug.LogError ("There is no active board.");
			}
		}
		return board;
	}

	void Start ()
	{
		lastRow = 0;
		lastCol = 0;

		swapSound = Resources.Load ("Sounds/mp3swap") as AudioClip;
		audioSource = gameObject.GetComponent<AudioSource> ();

		corner = transform.position;

		triangles = new GameObject[Constants.Instance ().nRows, Constants.Instance ().nCols];
		incenters = new Vector3[Constants.Instance ().nRows, Constants.Instance ().nCols];
		selected = new bool[Constants.Instance ().nRows, Constants.Instance ().nCols];

		inMatchGroup = new bool[Constants.Instance ().nRows, Constants.Instance ().nCols];

		slidingLeft = true;


		reset ();
	}

	void Update ()
	{
		switch (BattleInfo.gameState) {
		case GameState.PlayerTurn:
			playerUpdate ();
			break;
		case GameState.EnemyTurn:
			StartCoroutine (enemyUpdate ());
			break;
		default:
			break;
		}
	}

	public IEnumerator enemyUpdate(){
		// give enemy board state (or just possible matches)
		// get enemy's choice of move

		BattleInfo.gameState = GameState.EnemyThinking;

		Vector4 moveChoice = BattleInfo.currentEnemy.naiveMoveDecision(getLegalSwaps());
		int r1 = (int)moveChoice.x;
		int c1 = (int)moveChoice.y;
		int r2 = (int)moveChoice.z;
		int c2 = (int)moveChoice.w;
		// animate move choice briefly before swapping
		triangles[r1, c1].GetComponent<TriangleHandler>().startOutline();
		triangles[r2, c2].GetComponent<TriangleHandler>().startOutline();
		yield return new WaitForSeconds(1.5f);

		// stop flashing, then make move
		triangles [r1, c1].GetComponent<TriangleHandler> ().startIdle ();
		triangles [r2, c2].GetComponent<TriangleHandler> ().startIdle ();

		BattleInfo.playerWentLast = false;
		// make enemy's move
		yield return StartCoroutine(swapTriangles(r1, c1, r2, c2));
		//swapTriangles(r1, c1, r2, c2);
		BattleInfo.gameState = GameState.HandlingMatches;
		yield return StartCoroutine (handleMatches ());
		// now it's player's turn
	}

	void playerUpdate(){
		if (Input.GetMouseButtonDown (0)) {

			//what was clicked on?
			RaycastHit2D hitInformation = Physics2D.Raycast(
				Camera.main.ScreenToWorldPoint(Input.mousePosition),
				Vector2.zero);

			// clicked on a triangle?
			bool foundClickTarget = false;

			for (int row = 0; row < Constants.Instance ().nRows && !foundClickTarget; row++) {
				for (int col = 0; col < Constants.Instance ().nCols && !foundClickTarget; col++) {
					// don't check empty positions
					if (isEmptyPosition(row, col))
						continue;


					//if *some triangle* was clicked on
					if (null != hitInformation.transform) {
						// and it was this one
						if (hitInformation.transform.Equals (triangles [row, col].transform)) {
							StartCoroutine (newSelection (row, col));
							foundClickTarget = true;
						}				
					}
				}
			}

		}
	}

	// handle behavior when a new triangle says "i'm selected!"
	public IEnumerator newSelection(int row, int col){

		// if the last selected triangle is still selected...
		if (selected [lastRow, lastCol]) {
			foreach (Vector2 nbr in getNeighbors(lastRow, lastCol)) {
				// if last selected is a neighbor of the new one...
				if (row == (int)nbr.x && col == (int)nbr.y) {
					selected [row, col] = false;
					selected [lastRow, lastCol] = false;
					makeIdle (row, col);
					makeIdle (lastRow, lastCol);

					// initial swap
					yield return StartCoroutine(swapTriangles (row, col, lastRow, lastCol));
					// if swap doesn't make a match, swap back
					if (!(potentialMatchGroup (row, col, getType (row, col)).Count >= 3) && 
						!(potentialMatchGroup (lastRow, lastCol, getType (lastRow, lastCol)).Count >= 3) ) {
						// didn't get a match, so animate "whoops", then swap back.

						yield return StartCoroutine (swapTriangles (row, col, lastRow, lastCol, forward:false));

					} else {
						BattleInfo.playerWentLast = true;
						// if swap DOES make a match, deal with those matches
						BattleInfo.gameState = GameState.HandlingMatches;
						yield return StartCoroutine( handleMatches() );
					}

					lastRow = row;
					lastCol = col;
					yield break;
				}
			}
		}
		// deselect if you click the same piece again
		if (selected [row, col]) {
			selected [row, col] = false;
			makeIdle (row, col);
		} else {
			if (!isEmptyPosition(lastRow, lastCol)){
				selected [lastRow, lastCol] = false;
				makeIdle (lastRow, lastCol);
			}

			selected [row, col] = true;
			makeFlash (row, col);
		}


		lastRow = row;
		lastCol = col;
		yield return null;
	}

	public IEnumerator handleMatches(bool playerTurn=true){

		// wait until other stuff finishes
		while (BattleInfo.gameState != GameState.HandlingMatches);

		List<Vector2>[,] matchGroupLists = getAllMatches();
		Dictionary<GamePieceType, int> matchedPieces = new Dictionary<GamePieceType, int>();
		// start with 0 pieces of each type matched
		foreach (GamePieceType gptype in Enum.GetValues(typeof(GamePieceType)) ){
			matchedPieces.Add (gptype, 0);
		}

		bool matchedFervor = false;

		bool extraTurn = false;


		foreach (List<Vector2> matchGrp in matchGroupLists) {
			if (matchGrp.Count >= 3) {
				
				// record pieces matched so they can have their match effect
				GamePieceType matchType = triangles [(int)matchGrp [0].x, (int)matchGrp [0].y].GetComponent<TriangleHandler> ().type;

				matchedPieces [matchType] += matchGrp.Count;

				if (matchType == GamePieceType.Fervor) {
					matchedFervor = true;
				}


				// if you match enough pieces, you get an extra turn!
				if (matchGrp.Count >= 4) {
					Vector2 positionOfFirstPiece = RectTransformUtility.WorldToScreenPoint (
						Camera.main, incenters [(int)matchGrp [0].x, (int)matchGrp [0].y]);

					extraTurn = true;
					UIHandler.Instance ().spawnRisingText (positionOfFirstPiece, "Extra Turn!", Color.red, floatingDistance: 15,
						lifespan: 1.5f, fontSize: 18, fontStyle:FontStyle.Normal);
				}

				yield return explodePieces(matchGrp);
				yield return applyMatchedPieces(matchType, matchedPieces[matchType]);
			}


		}

		// if you got an extra turn, let that register briefly before giving next turn.
		// TODO: probably remove this when game pieces slide into place instead of appearing instantly


		if (!matchedFervor) {
			if (BattleInfo.playerWentLast) {
				BattleInfo.doPlayerFervorDecay ();
			} else {
				BattleInfo.currentEnemy.doFervorDecay ();
			}
		}
			
		fillBoard ();

		// if the board is left with no matches, reset it.
		while (getLegalSwaps().Count == 0){
			reset();
		}

		if (extraTurn) {
			yield return new WaitForSeconds (1.5f);
		}

		// TODO: instead of always switching turns after handling matches, wait for falling pieces
		//       to finish falling, handle their matches, and only switch turns once this process yields
		//       no more matches



		// if the game should be over now (someone's health is 0), do end-game popup
		if (BattleInfo.playerHealth == 0) {
			UIHandler.Instance ().youLostDialog ();
			yield break;
		} else if (BattleInfo.currentEnemy.health == 0) {
			UIHandler.Instance ().youWonDialog ();
			yield break;
		}


		// Display "Your turn" or "enemy's turn" for some amount of time first,
		float timeToShowTurnSwitch = 1.5f;
		if (!extraTurn) { // only show if it's the beginning of the turn, not an extra turn
			if (BattleInfo.playerWentLast) {
				Vector2 middleScreen = RectTransformUtility.WorldToScreenPoint (Camera.main, Camera.main.transform.position);
				UIHandler.Instance ().spawnRisingText (middleScreen, "Enemy Turn", Color.red, floatingDistance: 0,
					lifespan: timeToShowTurnSwitch, fontSize: 70, fontStyle: FontStyle.Normal);
			
			} else { //if ((BattleInfo.playerWentLast && extraTurn) || (!BattleInfo.playerWentLast && !extraTurn))
				Vector2 middleScreen = RectTransformUtility.WorldToScreenPoint (Camera.main, Camera.main.transform.position);
				UIHandler.Instance ().spawnRisingText (middleScreen, "Your Turn", Color.cyan, floatingDistance: 0,
					lifespan: timeToShowTurnSwitch, fontSize: 70, fontStyle: FontStyle.Normal);
			}
		}
		yield return new WaitForSeconds (timeToShowTurnSwitch);
		// then actually change turns
		if ((BattleInfo.playerWentLast && !extraTurn) || (!BattleInfo.playerWentLast && extraTurn)) {
			// wait a little longer, to feel like the enemy is "thinking"...
			yield return new WaitForSeconds (timeToShowTurnSwitch);
			BattleInfo.gameState = GameState.EnemyTurn;
		} else { // if ((BattleInfo.playerWentLast && extraTurn) || (!BattleInfo.playerWentLast && !extraTurn))
			BattleInfo.gameState = GameState.PlayerTurn;
		}





		yield return null;
	}

	public IEnumerator applyMatchedPieces(GamePieceType type, int numPieces){
		// prep floating text stuff
		GameObject playerSprite = GameObject.Find ("Player Sprite");
		Vector2 spritePos = RectTransformUtility.WorldToScreenPoint (Camera.main, playerSprite.transform.position);

		if (BattleInfo.playerWentLast) {
			switch (type) {
			case GamePieceType.Damage:
				// player deals damage
				float damageDealt = (1 + BattleInfo.playerFervorMultiplier * BattleInfo.playerFervor) * numPieces;
				if (damageDealt > 0) {
					BattleInfo.currentEnemy.takeDamage (damageDealt);
				}
				break;

			case GamePieceType.Healing:
				float prevHealth = BattleInfo.playerHealth;
				BattleInfo.playerHealth += numPieces;
				BattleInfo.playerHealth = Mathf.Min (BattleInfo.playerMaxHealth, BattleInfo.playerHealth);

				// floating healing text!
				float roundedHealing = BattleInfo.playerHealth - prevHealth;
				if (roundedHealing > 0) {
					UIHandler.Instance ().spawnRisingText (spritePos, "+" + roundedHealing.ToString (),
						Constants.Instance ().healingColor, floatingDistance: 90, lifespan: 2.5f);
				}
				break;

			case GamePieceType.Coin:
				float prevCoins = BattleInfo.Coins;
				BattleInfo.Coins += numPieces;

				// floating money text!
				float roundedCoins = BattleInfo.Coins - prevCoins;
				if (roundedCoins > 0) {
					UIHandler.Instance ().spawnRisingText (spritePos, "+" + roundedCoins.ToString (),
						Constants.Instance ().coinColor, floatingDistance: 90, lifespan: 2.5f);
				}
				break;

			case GamePieceType.Fervor:
				float prevFervor = BattleInfo.playerFervor;
				BattleInfo.playerFervor += numPieces;
				BattleInfo.playerFervor = Mathf.Min (BattleInfo.playerFervor, BattleInfo.playerMaxFervor);

				// floating fervor text!
				float roundedFervor = BattleInfo.playerFervor - prevFervor;
				if (roundedFervor > 0) {
					UIHandler.Instance ().spawnRisingText (spritePos, "+" + roundedFervor.ToString (),
						Constants.Instance ().fervorColor, floatingDistance: 90, lifespan: 2.5f);
				}
				break;

			case GamePieceType.Summoning:
				float prevSummoning = BattleInfo.playerSummoning;
				BattleInfo.playerSummoning += numPieces;
				BattleInfo.playerSummoning = Mathf.Round (Mathf.Min (BattleInfo.playerSummoning, BattleInfo.playerMaxSummoning));

				// floating summoning-gain text!
				float roundedSummoning = BattleInfo.playerSummoning - prevSummoning;
				if (roundedSummoning > 0) {
					UIHandler.Instance ().spawnRisingText (spritePos, "+" + roundedSummoning.ToString (),
						Constants.Instance().summoningColor, floatingDistance: 90, lifespan: 2.5f);
				}
				break;

			default:
				yield return null;
				break;
			}
		} else { // player didn't go last 
			switch (type) {
			case GamePieceType.Damage:
				// enemy deals damage
				float prevValue = BattleInfo.playerHealth;
				BattleInfo.playerHealth -= (1 + BattleInfo.currentEnemy.fervorMultiplier * BattleInfo.currentEnemy.fervor) * numPieces;
				BattleInfo.playerHealth = Mathf.Round (Mathf.Max (0, BattleInfo.playerHealth));
				float damageDealt = prevValue - BattleInfo.playerHealth;
				// floating damage text!
				if (damageDealt > 0) {
					UIHandler.Instance ().spawnRisingText (spritePos, "-" + damageDealt.ToString (),
						Constants.Instance ().damageColor, floatingDistance: -90, lifespan: 2.5f);
				}
				break;

			case GamePieceType.Healing:
				BattleInfo.currentEnemy.healDamage (numPieces);
				break;

			case GamePieceType.Coin:
				// nobody's gettin' that gold!

				// maybe some special enemies will collect gold and do something with it, or give more gold
				// when you defeat them.
				break;

			case GamePieceType.Fervor:
				BattleInfo.currentEnemy.addFervor (numPieces);
				break;

			case GamePieceType.Summoning:
				BattleInfo.currentEnemy.addSummoning (numPieces);
				break;

			default:
				yield return null;
				break;
			}
		}
	}


	public bool isSelected (int row, int col){
		return selected [row, col];
	}

	public void makeFlash(int row, int col){
		triangles [row, col].GetComponent<TriangleHandler> ().startOutline ();
	}

	public void makeIdle(int row, int col){
		triangles [row, col].GetComponent<TriangleHandler> ().startIdle ();
	}

	public IEnumerator animateSwap(int r1, int c1, int r2, int c2, float seconds = 1.0f/3, bool forward=true){

		// make the triangles at the two given locations spin around each other, trading positions
		BattleInfo.gameState = GameState.AnimatingSwap;

		// the two points the triangles are swapping between
		Vector3 a = triangles [r1, c1].transform.position;
		Vector3 b = triangles [r2, c2].transform.position;


		// midpoint m between the two swap locations; this will be the pivot of the
		// temporary parent object containing them and rotating them.
		// its z-coordinate is higher than everything else so the rotating pieces are on top
		Vector3 m = new Vector3( (a.x + b.x)/2, (a.y + 	b.y)/2, 0.0f);

		// distance d between two swap locations
		Vector3 m_to_a = a - m;
		m_to_a.z = -1.0f;
		Vector3 m_to_b = b - m;
		m_to_b.z = -1.0f;

		// set up a temporary object between the triangles which will spin and carry them with it
		GameObject pivot_object = new GameObject ();
		pivot_object.transform.position = m;
		triangles [r1, c1].transform.SetParent (pivot_object.transform);
		triangles [r1, c1].transform.localPosition = m_to_a;
		triangles [r2, c2].transform.SetParent (pivot_object.transform);
		triangles [r2, c2].transform.localPosition = m_to_b;
		

		float t = 0.0f;
		float timeDelta = 1.0f / 60.0f;
		Vector3 rotation;
		if (forward) {
			rotation = new Vector3 (0, 0, timeDelta * 180 / seconds);
		} else {
			seconds = seconds / 2.0f;
			rotation = new Vector3 (0, 0, -timeDelta * 180 / seconds);
		}

		// Start Swap Sound
		audioSource.clip = swapSound;
		audioSource.Play ();

		// Begin Swapping
		while (t < seconds) {			
			pivot_object.transform.Rotate (rotation);
			yield return new WaitForSeconds (timeDelta);
			t += timeDelta;
		}


		// make board their parent again
		triangles [r1, c1].transform.SetParent (transform);
		triangles [r2, c2].transform.SetParent (transform);
		// at the end, just swap original positions
		triangles [r1, c1].transform.position = b;
		//triangles [r1, c1].GetComponent<TriangleHandler> ().reorient (getDirection(r1,c1));
		triangles [r2, c2].transform.position = a;
		//triangles [r2, c2].GetComponent<TriangleHandler> ().reorient (getDirection(r2,c2));

		// destroy the temporary pivot object
		Destroy(pivot_object);

		yield return null;
	}


	public IEnumerator swapTriangles (int r1, int c1, int r2, int c2, bool forward = true)
	{
		GameState prevState = BattleInfo.gameState;

		// animate swap first
		//TODO: do actual animation
		yield return StartCoroutine(animateSwap(r1, c1, r2, c2, forward:forward));

		BattleInfo.gameState = prevState;

		// swap indices in board
		GameObject temp_triangle = triangles [r1, c1];
		triangles [r1, c1] = triangles [r2, c2];
		triangles [r2, c2] = temp_triangle;

		// let triangles know their updated positions
		triangles [r1, c1].GetComponent<TriangleHandler> ().Row = r2;
		triangles [r1, c1].GetComponent<TriangleHandler> ().Col = c2;

		triangles [r2, c2].GetComponent<TriangleHandler> ().Row = r1;
		triangles [r2, c2].GetComponent<TriangleHandler> ().Col = c1;

		// reorient and rename if necessary
		triangles [r1, c1].GetComponent<TriangleHandler> ().reorient (getDirection (r1, c1));
		triangles [r2, c2].GetComponent<TriangleHandler> ().reorient (getDirection (r2, c2));

		triangles [r1, c1].GetComponent<TriangleHandler> ().rename ();
		triangles [r2, c2].GetComponent<TriangleHandler> ().rename ();

		yield return null;
	}

	public void slidingFillBoard(){
		// for each unfilled space, "pull pieces down" from above.
		for (int row = 0; row < Constants.Instance ().nRows; row++) {
			for (int col = 0; col < Constants.Instance ().nCols; col++) {
				// if next empty space is at the top, spawn a new one and have that "fall" in
				if (triangles [row, col] == null && !isEmptyPosition(row, col)) {
					// try pulling from above
					Vector2 positionAbove = getPositionAbove(row, col, BattleInfo.slidingDirection);
					if (positionAbove.x > 0) { // position is an actual board position
						// TODO: this
					} else { // position is outside the board; spawn new piece!
						// TODO: this
					}
				}
			}
		}
	}

	// returns row, col of position above the given one, in the given direction
	public Vector2 getPositionAbove(int row, int col, SlidingDirection direction){
		int candidateRow, candidateCol;

		if (getDirection (row, col) == TriangleDirection.Upward) { // if triangle is "pointing" upward
			// "above" now means to the right or left, in the same row
			candidateRow = row;
			switch (BattleInfo.slidingDirection) {
			case SlidingDirection.Left:
				candidateCol = col - 1;
				break;
			case SlidingDirection.Right:
				candidateCol = col + 1;
				break;
			default:
				candidateCol = -1;
				break;
			}

			if (!isEmptyPosition (candidateRow, candidateCol)) {
				return new Vector2 (candidateRow, candidateCol);
			} else {
				return new Vector2 (-1, -1); // indicate no piece above
			}
		} else { // triangle is "pointing" downward
			// "above" now means directly above; doesn't depend on sliding direction
			candidateRow = row - 1;
			candidateCol = col;

			if (!isEmptyPosition (candidateRow, candidateCol)) {
				return new Vector2 (candidateRow, candidateCol);
			} else {
				return new Vector2 (-1, -1); // indicate no piece above
			}
		}
	}




	public void fillBoard(){
		UnityEngine.Random.seed = (int)System.DateTime.Now.Ticks;


		for (int row = 0; row < Constants.Instance ().nRows; row++) {
			for (int col = 0; col < Constants.Instance ().nCols; col++) {

				// skip over positions that already have triangles
				if (triangles [row, col] != null) {
					continue;
				}


				// don't make triangles in empty positions
				if (isEmptyPosition (row, col)) {
					continue;
				}

				// initialize incenters
				incenters [row, col] = getIncenter (row, col);


				// instantiate a game object that will not cause a match
				GamePieceType type_choice = getRandomType();

				while (potentialMatchGroup(row, col, type_choice).Count >= 3) { // pick at random until no match is made
					type_choice = getRandomType();
				}


				triangles [row, col] = GameObject.Instantiate (piecePrefabs [(int)type_choice]);

				triangles [row, col].GetComponent<TriangleHandler> ().type = type_choice;

				// make this board the parent of the game piece
				triangles [row, col].transform.SetParent(transform);

				triangles [row, col].GetComponent<TriangleHandler> ().Row = row;
				triangles [row, col].GetComponent<TriangleHandler> ().Col = col;

				// default is no rune
				triangles [row, col].GetComponent<TriangleHandler> ().Rune = God.None;

				// setup name to indicate where the piece is and what it is
				triangles [row, col].GetComponent<TriangleHandler> ().rename();

				// move incenter to correct location
				triangles [row, col].transform.localPosition = incenters [row, col];

				// rotate based on position
				triangles [row, col].GetComponent<TriangleHandler>().reorient (getDirection(row, col));
			}
		}
	}


	public IEnumerator removePiece(int row, int col){
		// because Destroying a game object doesn't actually remove it from containers
		Destroy (triangles [row, col]);
		triangles [row, col] = null;
		yield return null;
	}

	public IEnumerator removePieces(List<Vector2> pieces){
		// because Destroying a game object doesn't actually remove it from containers
		foreach (Vector2 piece in pieces) {
			Destroy (triangles [(int)piece.x, (int)piece.y]);
			triangles [(int)piece.x, (int)piece.y] = null;
		}
		yield return null;
	}

	public IEnumerator explodePieces(List<Vector2> pieces, float seconds = 0.5f, IEnumerator doAfter = null){
		if (pieces.Count < 1) {
			yield break;
		}
		// set exploder material color to this triangle's color
		print("starting explosion SETUP");
		Color initialColor = triangles[(int)pieces[0].x, (int)pieces[0].y].GetComponent<SpriteRenderer>().color;
		Material explodingMaterial = Resources.Load ("Materials/Exploding Material") as Material;
		explodingMaterial.color = initialColor;

		List<SpriteRenderer> renderers = new List<SpriteRenderer> ();
		foreach (Vector2 piece in pieces) {
			renderers.Add (triangles [(int)piece.x, (int)piece.y].GetComponent<SpriteRenderer> ());
		}

		// set emissive materials
		foreach (SpriteRenderer renderer in renderers){
			renderer.material = explodingMaterial;
		}

		Color currentColor = initialColor;

		int updatesPerSecond = 60;
		float timeDelta = 1.0f / updatesPerSecond;
		float t = 0.0f;

		// Le Sound!
		// audioSource.clip = explosionSound;


		// Explosion animation starts here!
		while (t < seconds/2) {
			currentColor = Color.Lerp (initialColor, Color.white, t / (seconds / 2));

			foreach (SpriteRenderer renderer in renderers){
				renderer.color = currentColor;
			}
			explodingMaterial.SetColor ("_EmissiveColor", currentColor);
			t += timeDelta;
			yield return new WaitForSeconds (timeDelta);
		}

		t = 0.0f;
		while (t < seconds/2) {

			currentColor = Color.Lerp (Color.white, Color.clear, t / (seconds / 2));
			foreach (SpriteRenderer renderer in renderers) {
				renderer.color = currentColor;
			}
			explodingMaterial.color = currentColor;
			explodingMaterial.SetColor ("_EmissionColor", currentColor);
			t += timeDelta;
			yield return new WaitForSeconds (timeDelta);
		}

		yield return removePieces(pieces);
	}

	// Put a triangle, correctly oriented, in each nonempty position
	public void reset ()
	{
		// remove triangles
		for (int row = 0; row < Constants.Instance ().nRows; row++) {
			for (int col = 0; col < Constants.Instance ().nCols; col++) {
				// blah 
				print (row);
				print (col);
				print (triangles);
				if (triangles [row, col] == null) {
					continue;
				}
				StartCoroutine (removePiece (row, col));
			}
		}

		fillBoard ();

	}

	// Is the corresponding triangle pointing downward or upward?
	public TriangleDirection getDirection (int row, int col)
	{
		if ((row + col) % 2 == 1) {
			return TriangleDirection.Upward;
		} else {
			return TriangleDirection.Downward;
		}
	}

	public bool hypotheticalMatch (Vector4 swap){
		int r1 = (int)swap.x;
		int c1 = (int)swap.y;
		int r2 = (int)swap.z;
		int c2 = (int)swap.w;

		// can't swap an empty position
		if (isEmptyPosition (r1, c1) || isEmptyPosition (r2, c2)) {
			return false;
		}



		// Determine whether putting the given piece at the given position would cause a match
		// Do a BFS from the given position and record all pieces with the same piece type

		List<Vector2> closedList = new List<Vector2> ();
		Stack<Vector2> openList = new Stack<Vector2> ();
		openList.Push (new Vector2(r1, c1));

		Vector2 current;
		List<Vector2> neighbors;
		while (openList.Count > 0) {
			current = openList.Pop ();
			if (isEmptyPosition ((int)current.x, (int)current.y)) {
				continue;
			}
			// record that current has been grouped into a match group
			if (!isEmptyPosition((int)current.x, (int)current.y)) {
				inMatchGroup [(int)current.x, (int)current.y] = true;
			}

			neighbors = getNeighbors ((int)current.x, (int)current.y);
			foreach (Vector2 nbr in neighbors){
				// if nothing was put there yet, skip checking
				if (triangles [(int)nbr.x, (int)nbr.y] == null || isEmptyPosition((int)nbr.x, (int)nbr.y)) {
					continue;
				}

				// if piece was already seen in search, skip checking it again
				if (closedList.Contains(nbr)){
					continue;
				}

				if (getType ((int)nbr.x, (int)nbr.y).Equals (null)) {
					//include it in the result of the search
					openList.Push (nbr);
				}

			}
			closedList.Add (current);

		}
		// matches are 3 or more items.
		return closedList.Count > 0;
		
	}

	// returns the list of positions that would match if this piece were the given type
	List<Vector2> potentialMatchGroup(int row, int col, GamePieceType pieceType, bool recording=false){
		if (isEmptyPosition (row, col)) {
			return new List<Vector2> ();
		}
		// Determine whether putting the given piece at the given position would cause a match
		// Do a BFS from the given position and record all pieces with the same piece type

		List<Vector2> closedList = new List<Vector2> ();
		Stack<Vector2> openList = new Stack<Vector2> ();
		openList.Push (new Vector2(row, col));

		Vector2 current;
		List<Vector2> neighbors;
		while (openList.Count > 0) {
			current = openList.Pop ();
			if (isEmptyPosition ((int)current.x, (int)current.y)) {
				continue;
			}
			// record that current has been grouped into a match group
			if (recording && !isEmptyPosition((int)current.x, (int)current.y)) {
				inMatchGroup [(int)current.x, (int)current.y] = true;
			}

			neighbors = getNeighbors ((int)current.x, (int)current.y);
			foreach (Vector2 nbr in neighbors){
				// if nothing was put there yet, skip checking
				if (triangles [(int)nbr.x, (int)nbr.y] == null || isEmptyPosition((int)nbr.x, (int)nbr.y)) {
					continue;
				}

				// if piece was already seen in search, skip checking it again
				if (closedList.Contains(nbr)){
					continue;
				}

				if (getType ((int)nbr.x, (int)nbr.y).Equals (pieceType)) {
					//include it in the result of the search
					openList.Push (nbr);
				}

			}
			closedList.Add (current);

		}
		// matches are 3 or more items.
		return closedList;
	}

	bool isEmptyPosition (int row, int col)
	{
		if (row < 0 || col < 0 || row >= Constants.Instance ().nRows || col >= Constants.Instance ().nCols) {
			return true;
		}
		// Empty triangle in each corner in order to make board nicely shaped
		int empty_corner_size = Constants.Instance ().nRows / 2;
		// Top Left Corner empty positions
		if (row < empty_corner_size && col < empty_corner_size) {
			if (row + col < empty_corner_size) {
				return true;
			}
		}

		// Top Right Corner empty positions
		else if (row < empty_corner_size && col >= Constants.Instance ().nCols - empty_corner_size) {
			if (row + (Constants.Instance ().nCols - 1 - col) < empty_corner_size) {
				return true;
			}
		}

		// Bottom Left Corner empty positions
		else if (row >= empty_corner_size && col < empty_corner_size) {
			if ((Constants.Instance ().nRows - 1 - row) + col < empty_corner_size) {
				return true;
			}
		}

		// Bottom Right Corner empty positions
		else if (row >= empty_corner_size && col >= Constants.Instance ().nCols - empty_corner_size) {
			if ((Constants.Instance ().nRows - 1 - row) + (Constants.Instance ().nCols - 1 - col) < empty_corner_size) {
				return true;
			}
		}

		return false;
	}
		

	public List<Vector2> getNeighbors (int row, int col)
	{
		List<Vector2> neighbors = new List<Vector2> ();

		if (!isEmptyPosition (row, col)) {// empty triangles don't get to "have neighbors"

			// check left
			int candidate_row = row;
			int candidate_col = col - 1;
			if (!isEmptyPosition (candidate_row, candidate_col)) {
				neighbors.Add (new Vector2 (candidate_row, candidate_col));
			}

			// check right
			candidate_row = row;
			candidate_col = col + 1;
			if (!isEmptyPosition (candidate_row, candidate_col)) {
				neighbors.Add (new Vector2 (candidate_row, candidate_col));
			}

			// check below if the pointy side of the piece is up

			if (getDirection (row, col) == TriangleDirection.Upward) {
				if (row < Constants.Instance ().nRows - 1) {
					candidate_row = row + 1;
					candidate_col = col;
					if (!isEmptyPosition (candidate_row, candidate_col)) {
						neighbors.Add (new Vector2 (candidate_row, candidate_col));
					}
				}
			} else { // check above if the pointy side of the piece is down
				if (row > 0) {
					candidate_row = row - 1;
					candidate_col = col;
					if (!isEmptyPosition (candidate_row, candidate_col)) {
						neighbors.Add (new Vector2 (candidate_row, candidate_col));
					}
				}
			}
		} else { // position is empty

		}
		return neighbors;
	}


	// where center of triangle should be, **relative to board coordinates**
	Vector3 getIncenter (int row, int col)
	{
		// NOTE: This calculation depends on nRows = 2*(odd number)
		// a, b, c, are the corner points of the triangle
		float a_y, a_x, b_y, b_x, c_y, c_x;
		if (getDirection (row, col) == TriangleDirection.Upward) {
			// every other row is offset by half a triangle side length
			a_y = (Constants.Instance ().s / 2) * ((row + 1) % 2) + Mathf.Floor (col / 2) * Constants.Instance ().s;
			a_x = -1 * (row) * Constants.Instance ().h - Constants.Instance ().h/3;
		} else { // Direction is Downward
			a_y = (Constants.Instance ().s / 2) * (row % 2) + Mathf.Floor (col / 2) * Constants.Instance ().s;
			a_x = -1 * (row) * Constants.Instance ().h;
		}

		b_y = a_y + 1*Constants.Instance ().s;
		b_x = a_x;

		c_y = a_y + (Constants.Instance ().s / 2);
		c_x = a_x + 1*Constants.Instance ().s;
		Vector3 incenter = new Vector3 ((a_x + b_x + c_x) / 3, (a_y + b_y + c_y) / 3);


		//incenter = incenter + corner;

		// rotate based on the rotation of the board
		incenter = Quaternion.Euler(0, 0, BoardHandler.Instance().transform.rotation.eulerAngles.z) * incenter;

		return incenter;
	}



	List<Vector2>[,] getAllMatches(){
		//initialize match groups
		List<Vector2>[,] matchGroupLists = new List<Vector2>[Constants.Instance ().nRows, Constants.Instance ().nCols];
		for (int row = 0; row < Constants.Instance ().nRows; row++){
			for (int col = 0; col < Constants.Instance ().nCols; col++) {
				matchGroupLists [row, col] = new List<Vector2> ();
				inMatchGroup [row, col] = false;
			}
		}


		for (int row = 0; row < Constants.Instance ().nRows; row++) {
			for (int col = 0; col < Constants.Instance ().nCols; col++) {
				// if this piece hasn't been put in a group yet...
				if (!inMatchGroup [row, col] && !isEmptyPosition(row, col)) {
					// put it, and its matching group, into its group list
					matchGroupLists [row, col] = potentialMatchGroup (row, col, getType (row, col), recording:true);
				}
			}
		}

		return matchGroupLists;
	}

	void doubleCheckMatchGroups(){
		List<Vector2>[,] matchGroupLists = getAllMatches ();
		foreach (var matchGrp in matchGroupLists) {
			if (matchGrp == null || matchGrp.Count == 0) {
				continue;
			}
			GamePieceType groupType = getType ((int)matchGrp [0].x, (int)matchGrp [0].y);
			foreach (var gamePiece in matchGrp) {
				if (getType ((int)gamePiece.x, (int)gamePiece.y) != groupType) {
					print ("BADDIE AT " + gamePiece.ToString ());
				}
			}
		}
	}

	// returns a dictionary of swaps (key) and the match type they create (value)
	public Dictionary<Vector4, GamePieceType> getLegalSwaps(){
		// TODO: TODO: TODO:
		// this usually gives correct moves, but sometimes gives wrong swaps in the sense that:
		// - makes a swap that does not result in a match....

		List<Vector2>[,] matchGroupLists = getAllMatches ();

		Dictionary<Vector4, GamePieceType> legalSwaps = new Dictionary<Vector4, GamePieceType> ();
		GamePieceType groupType;

		// first do simple swaps (not including swaps that connect three entirely isolated pieces
		foreach (List<Vector2> matchGrp in matchGroupLists) {
			// DEBUGGING: I'm pretty sure matchGroupLists is correct.
			if (matchGrp.Count < 2) {
				continue;
			}
			groupType = getType ((int)matchGrp [0].x, (int)matchGrp [0].y);
			// DEBUGGING: I'm pretty sure getType is correct.
			// if any one-step neighbors (outside the group) have a one-step neighbor same as the matchgroup,
			// report a swap there.

			// gather the neighbors of the match group
			List<Vector2> neighbors = new List<Vector2> ();
			foreach (Vector2 gamePiece in matchGrp) {
				List<Vector2> gamePieceNeighbors = getNeighbors ((int)gamePiece.x, (int)gamePiece.y);
				foreach (Vector2 nbr in gamePieceNeighbors) {
					if (!neighbors.Contains (nbr) && !matchGrp.Contains (nbr)) {
						neighbors.Add (nbr);
					} else if (matchGrp.Contains (nbr)) {
					}
				}
			}

			foreach (Vector2 nbr in neighbors) {
				// test swapping that neighbor with one of its neighbors.
				// if it yields a match (same type as group type), record it
				List<Vector2> two_away_nbrs = getNeighbors ((int)nbr.x, (int)nbr.y);
				foreach (Vector2 two_away_nbr in two_away_nbrs) {
					// don't use two_away_nbrs in the match group itself
					if (matchGrp.Contains (two_away_nbr)) {
						continue;
					}

					if (getType ((int)two_away_nbr.x, (int)two_away_nbr.y).Equals (groupType)) {
						Vector4 theSwap = new Vector4 (nbr.x, nbr.y, two_away_nbr.x, two_away_nbr.y);
						if (!legalSwaps.ContainsKey (theSwap)) {
							legalSwaps.Add (theSwap, groupType);
						}
					}
				}
			}
		}

		// add "triangle swaps"; game pieces with all neighbors the same color. any of these neighbors
		// can be swapped with the central one to make a match
		// TODO: finish this
		for	 (int row = 0; row < Constants.Instance ().nRows; row++) { // top and bottom row can't be the center
			for (int col = 0; col < Constants.Instance ().nCols; col++) {
				// if a piece has 3 neighbors and all are the same color, return all 3 swaps
				List<Vector2> neighbors = getNeighbors(row, col);
				if (neighbors.Count < 3) {
					continue;
				}
				GamePieceType firstNeighborType = getType ((int)neighbors [0].x, (int)neighbors [0].y);
				bool allSame = true;
				foreach (Vector2 nbr in neighbors) {
					if (isEmptyPosition ((int)nbr.x, (int)nbr.y) ||
						getType ((int)nbr.x, (int)nbr.y) != firstNeighborType) {
						allSame = false;
					}
				}
				if (allSame) {
					// add swaps with each neighbor
					foreach (Vector2 nbr in neighbors) {
						Vector4 theSwap = new Vector4 (nbr.x, nbr.y, row, col);
						if (!legalSwaps.ContainsKey (theSwap)) {
							legalSwaps.Add (theSwap, firstNeighborType);
						}
					}
				}
			}
		}

		return legalSwaps;
	}


	GamePieceType getType(int row, int col){
		if (triangles [row, col] != null) {
			return triangles [row, col].GetComponent<TriangleHandler> ().type;
		}
		return GamePieceType.None;
	}

	GamePieceType getRandomType(){
		//TODO: make Constants a singleton instead of class with static fields so we can 
		//      more easily use a Dictionary instead of this
		float choice = UnityEngine.Random.Range (0.0f, 1.0f);

		// assuming the probabilities in Constants actually add to 1, this chooses a coin at random
		// using those probabilities.
		if (choice < Constants.Instance ().damagePieceProbability) {
			return GamePieceType.Damage;
		} else if (choice < Constants.Instance ().damagePieceProbability + Constants.Instance ().healingPieceProbability){
			return GamePieceType.Healing;
		} else if (choice < Constants.Instance ().damagePieceProbability + Constants.Instance ().healingPieceProbability + Constants.Instance ().fervorPieceProbability){
			return GamePieceType.Fervor;
		} else if (choice < Constants.Instance ().damagePieceProbability + Constants.Instance ().healingPieceProbability + Constants.Instance ().fervorPieceProbability
			+ Constants.Instance ().summoningPieceProbability){
			return GamePieceType.Summoning;
		} else { // last option is a coin
			return GamePieceType.Coin;
		}
		//return (GamePieceType)UnityEngine.Random.Range (1, Enum.GetNames (typeof(GamePieceType)).Length);
	}

}
