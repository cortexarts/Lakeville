using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Player position U.
/// </summary>
public class PlayerPositionUI : MonoBehaviour
{

	/// <summary>
	/// The player.
	/// </summary>
	public Transform player;

	/// <summary>
	/// The world mover.
	/// </summary>
	public WorldMover worldMover;

	/// <summary>
	/// The text.
	/// </summary>
	public Text text;

	/// <summary>
	/// Update this instance and shows player real position and player position after move.
	/// </summary>
	public void Update ()
	{
		text.text = "Player position: " + player.transform.position + "\nPlayer real position: " + (player.transform.position - worldMover.currentMove);
	}


}
