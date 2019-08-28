using UnityEngine;
using System.Collections;

/// <summary>
/// Player teleport which reloads tiles at teleport destination.
/// </summary>
public class PlayerTeleport : MonoBehaviour
{
	
	/// <summary>
	/// The loading screen UI.
	/// </summary>
	public UILoadingStreamer loadingStreamer;

	/// <summary>
	/// The world streamer.
	/// </summary>
	public Streamer[] streamers;
	/// <summary>
	/// The player transform.
	/// </summary>
	public Transform player;
	/// <summary>
	/// The world mover.
	/// </summary>
	public WorldMover worldMover;

	/// <summary>
	/// Teleport the player and shows loading screen.
	/// </summary>
	/// <param name="showLoadingScreen">If set to <c>true</c> shows loading screen after teleport.</param>
	public void Teleport (bool showLoadingScreen)
	{
		player.position = transform.position + ((worldMover == null) ? Vector3.zero : worldMover.currentMove);
		player.rotation = transform.rotation;
		foreach (var streamer in streamers) {
			streamer.showLoadingScreen = showLoadingScreen;
			streamer.CheckPositionTiles ();

		}
		if (loadingStreamer != null)
			loadingStreamer.Show ();
	}

	/// <summary>
	/// Raises the draw gizmos selected event.
	/// </summary>
	void OnDrawGizmosSelected ()
	{
		// Display the explosion radius when selected
		Gizmos.color = new Color (0.4f, 0.7f, 1, 0.5f);
		Gizmos.DrawSphere (transform.position, 1);
	}
}
