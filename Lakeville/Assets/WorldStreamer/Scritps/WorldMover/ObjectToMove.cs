using UnityEngine;
using System.Collections;

/// <summary>
/// Object to move by world mover.
/// </summary>
public class ObjectToMove : MonoBehaviour
{
	/// <summary>
	/// The world mover.
	/// </summary>
	public WorldMover worldMover;

	void Start ()
	{
		if (worldMover == null) {
			GameObject[] streamerGO = GameObject.FindGameObjectsWithTag (Streamer.STREAMERTAG);
			foreach (var item in streamerGO) {
				WorldMover mover = item.GetComponent<Streamer> ().worldMover;
				if (mover != null) {
					worldMover = mover;

				}
			}
		}

		worldMover.AddObjectToMove (this);

	}
}
