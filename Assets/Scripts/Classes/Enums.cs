using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum CounterType {
	None,
	Coin
}

public enum GamePieceType {
	None,
	Damage,
	Healing,
	Fervor,
	Summoning,
	Coin
}

public enum ProgressBarType {
	None,
	PlayerHealth,
	EnemyHealth,
	PlayerFervor,
	EnemyFervor,
	PlayerSummoning,
	EnemySummoning
}

public enum SlidingDirection {
	Left,
	Right
}

public enum BattleStats {
	None,
	Health,
	Fervor,
	Summoning
}

public enum Side {
	Player,
	Enemy,
}

public enum God {
	None,
	Odin,
	Thor,
	Loki,
	Freya
}

public enum TriangleMovementType {
	None,
	Arc,
	Straight
}



public enum TriangleDirection { 
	Upward, 
	Downward 
}

public enum GameState {
	None,
	PlayerTurn,
	PlayerAction,
	EnemyTurn,
	EnemyThinking,
	EnemyDoneThinking,
	AnimatingSwap,
	HandlingMatches,
	DestroyingMatches,
	SlidingPieces,
	InMenu,
	DialogBox,
	Tutorial,
	BattleOver,
	Exiting
}