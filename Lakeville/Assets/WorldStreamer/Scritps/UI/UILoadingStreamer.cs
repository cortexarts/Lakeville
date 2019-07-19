using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// User interface loading progress.
/// </summary>
using UnityEngine.Events;


public class UILoadingStreamer : MonoBehaviour
{
	/// <summary>
	/// The streamers.
	/// </summary>
	public Streamer[] streamer;
	/// <summary>
	/// The progress image.
	/// </summary>
	public Image progressImg;
	/// <summary>
	/// The wait time after end of loading.
	/// </summary>
	public float waitTime = 2;

	public UnityEvent onDone;

	/// <summary>
	/// Awake this instance, and set fill to 0.
	/// </summary>
	void Awake ()
	{
		progressImg.fillAmount = 0;
	}

	/// <summary>
	/// Update this instance, and sets current progress of streaming.
	/// </summary>
	void Update ()
	{
		if (streamer.Length > 0) {
			progressImg.fillAmount = 0;
			foreach (var item in streamer) {
				progressImg.fillAmount += item.LoadingProgress / (float)streamer.Length;

			}
			if (progressImg.fillAmount >= 1) {
				if (onDone != null)
					onDone.Invoke ();
				StartCoroutine (TurnOff ());
			}

		} else
			Debug.Log ("No streamer Attached");
	}

	public IEnumerator TurnOff ()
	{
		yield return new WaitForSeconds (waitTime);
		gameObject.SetActive (false);
	}

	/// <summary>
	/// Show progress bar and resets fill.
	/// </summary>
	public void Show ()
	{
		progressImg.fillAmount = 0;
		gameObject.SetActive (true);
	}

}
