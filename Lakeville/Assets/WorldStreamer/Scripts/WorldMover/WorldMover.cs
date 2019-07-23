using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// World mover - movesw the world when moving out of chosen range.
/// </summary>
public class WorldMover : MonoBehaviour
{

	/// <summary>
	/// The x tile range based on main streamer.
	/// </summary>
	public float xTileRange = 2;
	/// <summary>
	/// The y tile range  based on main streamer.
	/// </summary>
	public float yTileRange = 2;
	/// <summary>
	/// The z tile range  based on main streamer.
	/// </summary>
	public float zTileRange = 2;
	
	/// <summary>
	/// The x current tile move.
	/// </summary>
	public float xCurrentTile = 0;
	/// <summary>
	/// The y current tile move.
	/// </summary>
	public float yCurrentTile = 0;
	/// <summary>
	/// The z current tile move.
	/// </summary>
	public float zCurrentTile = 0;

	/// <summary>
	/// The streamer main for checking range.
	/// </summary>
	public Streamer streamerMain;
	/// <summary>
	/// The additional streamers to move tiles.
	/// </summary>
	public Streamer[] streamersAdditional;

	/// <summary>
	/// The current move vector.
	/// </summary>
	public Vector3 currentMove = Vector3.zero;

	/// <summary>
	/// The objects to move with tiles.
	/// </summary>
	[HideInInspector]
	public List<ObjectToMove>
		objectsToMove = new List<ObjectToMove> ();

	/// <summary>
	/// Start this instance and sets main streamer field for world mover.
	/// </summary>
	public void Start ()
	{
		streamerMain.worldMover = this;
		List<Streamer> streamersTemp = new List<Streamer> ();
		streamersTemp.AddRange (streamersAdditional);
		streamersTemp.Remove (streamerMain);
		streamersAdditional = streamersTemp.ToArray ();
	}

	/// <summary>
	/// Checks the mover distance.
	/// </summary>
	/// <param name="xPosCurrent">X position current in tiles.</param>
	/// <param name="yPosCurrent">Y position current in tiles.</param>
	/// <param name="zPosCurrent">Z position current in tiles.</param>
	public void CheckMoverDistance (int xPosCurrent, int yPosCurrent, int zPosCurrent)
	{

		if (Mathf.Abs (xPosCurrent - xCurrentTile) > xTileRange || Mathf.Abs (yPosCurrent - yCurrentTile) > yTileRange || Mathf.Abs (zPosCurrent - zCurrentTile) > zTileRange) {

			MoveWorld (xPosCurrent, yPosCurrent, zPosCurrent);

		}
	}

	/// <summary>
	/// Moves the world.
	/// </summary>
	/// <param name="xPosCurrent">X position current in tiles.</param>
	/// <param name="yPosCurrent">Y position current in tiles.</param>
	/// <param name="zPosCurrent">Z position current in tiles.</param>
	void MoveWorld (int xPosCurrent, int yPosCurrent, int zPosCurrent)
	{

		Vector3 moveVector = new Vector3 ((xPosCurrent - xCurrentTile) * streamerMain.sceneCollection.xSize, (yPosCurrent - yCurrentTile) * streamerMain.sceneCollection.ySize, (zPosCurrent - zCurrentTile) * streamerMain.sceneCollection.zSize);

		currentMove -= moveVector;

		streamerMain.player.position -= moveVector;
		foreach (var item in streamerMain.loadedScenes) {
			if (item.loaded && item.sceneGo != null)
				item.sceneGo.transform.position -= moveVector;
		}

		foreach (var item in objectsToMove) {
			item.transform.position -= moveVector;
		}

		xCurrentTile = xPosCurrent;
		yCurrentTile = yPosCurrent;
		zCurrentTile = zPosCurrent;

		streamerMain.currentMove = currentMove;

		foreach (var item in streamersAdditional) {

			item.currentMove = currentMove;

			foreach (var scene in item.loadedScenes) {
				if (scene.loaded && scene.sceneGo != null)
					scene.sceneGo.transform.position -= moveVector;
			}
		}

	}

	/// <summary>
	/// Moves the object.
	/// </summary>
	/// <param name="objectTransform">Object transform.</param>
	public void MoveObject (Transform objectTransform)
	{
		objectTransform.position += currentMove;
	}

	/// <summary>
	/// Adds the object to move.
	/// </summary>
	/// <param name="objectToMove">Object to move.</param>
	public void AddObjectToMove (ObjectToMove objectToMove)
	{
		objectToMove.transform.position += currentMove;
		objectsToMove.Add (objectToMove);
	}
}
