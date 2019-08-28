using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


[CustomEditor (typeof(Streamer))]
class MyPlayerEditor : Editor
{
		
	public override void OnInspectorGUI ()
	{

		DrawDefaultInspector ();

		Streamer myTarget = (Streamer)target;

		if (myTarget.sceneCollection == null) {
			EditorGUILayout.HelpBox ("Add scene collection", MessageType.Error, true);
		} else if (myTarget.sceneCollection != null) {

			SceneCollection currentCollection = myTarget.sceneCollection;
		
			List<EditorBuildSettingsScene> scenesList = new List<EditorBuildSettingsScene> ();
			scenesList.AddRange (EditorBuildSettings.scenes);
			
			List<string> scenesToAdd = new List<string> ();
			scenesToAdd.AddRange (currentCollection.names);
			
			foreach (var item in scenesList) {
				if (scenesToAdd.Contains (item.path.Replace (currentCollection.path, ""))) {
					scenesToAdd.Remove (item.path.Replace (currentCollection.path, ""));
				}
			}

			if (scenesToAdd.Count > 0) {
				EditorGUILayout.HelpBox ("Add scenes from scene collection to build settings.", MessageType.Error, true);
				if (GUILayout.Button ("Add scenes to build settings")) {
					AddScenesToBuild (currentCollection);
				}
			}

			if (myTarget.tag != Streamer.STREAMERTAG) {
				EditorGUILayout.HelpBox ("Streamer must have " + Streamer.STREAMERTAG + " Tag.", MessageType.Error, true);
				if (GUILayout.Button ("Change tag")) {
					myTarget.tag = Streamer.STREAMERTAG;
				}

			}

			if (myTarget.deloadingRange.x < myTarget.loadingRange.x || myTarget.deloadingRange.y < myTarget.loadingRange.y || myTarget.deloadingRange.z < myTarget.loadingRange.z) {
				EditorGUILayout.HelpBox ("Streamer deloading range must >= loading range", MessageType.Error, true);
			}

		}
		

	}

	/// <summary>
	/// Adds the scenes to build.
	/// </summary>
	void AddScenesToBuild (SceneCollection sceneCollection)
	{
	
		List<EditorBuildSettingsScene> scenesList = new List<EditorBuildSettingsScene> ();
		scenesList.AddRange (EditorBuildSettings.scenes);
		
		List<string> scenesToAdd = new List<string> ();
		scenesToAdd.AddRange (sceneCollection.names);
		
		foreach (var item in scenesList) {
			if (scenesToAdd.Contains (item.path.Replace (sceneCollection.path, ""))) {
				scenesToAdd.Remove (item.path.Replace (sceneCollection.path, ""));
			}
		}
		
		foreach (var item in scenesToAdd) {
			scenesList.Add (new EditorBuildSettingsScene (sceneCollection.path + item, true));
		}
		
		EditorBuildSettings.scenes = scenesList.ToArray ();
	}


}