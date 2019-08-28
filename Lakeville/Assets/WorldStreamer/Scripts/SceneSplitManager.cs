using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// Scene split manager, finds streamer and adds scene.
/// </summary>
public class SceneSplitManager : MonoBehaviour
{
	/// <summary>
	/// The name of the scene.
	/// </summary>
	public string sceneName;

	
	/// <summary>
	/// The gizmos color.
	/// </summary>
	public  Color
		color;

	/// <summary>
	/// The split position.
	/// </summary>
	[HideInInspector]
	public Vector3
		position;

	/// <summary>
	/// The size of split.
	/// </summary>
	[HideInInspector]
	public Vector3
		size = new Vector3 (10, 10, 10);


	/// <summary>
	/// Start this instance, finds streamer and adds scene.
	/// </summary>
	void Start ()
	{
		AddToStreamer ();

	}

	void AddToStreamer ()
	{
		GameObject[] streamerGO = GameObject.FindGameObjectsWithTag (Streamer.STREAMERTAG);
		foreach (var item in streamerGO) {
			Streamer streamer = item.GetComponent<Streamer> ();
			if (streamer != null) {
				foreach (var name in streamer.sceneCollection.names) {
				
					if (name.Replace (".unity", "") == sceneName) {
						streamer.AddSceneGO (sceneName, this.gameObject);
						return;
					}
				}
			}
		
		}
	}

	void  OnDrawGizmosSelected ()
	{
		// Display the explosion radius when selected
		Gizmos.color = color;
		Gizmos.DrawWireCube (position + size * 0.5f, size);
	}
}
