using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// Multi scene helper for working with multi scene editor.
/// </summary>
public class LocalAreaUpdaterEditor : EditorWindow
{
	#if UNITY_5_3
	/// <summary>
	/// The collections collapsed.
	/// </summary>
	bool collectionsCollapsed = true;
	/// <summary>
	/// The list size collections.
	/// </summary>
	int listSizeCollections = 0;
	/// <summary>
	/// The current collections.
	/// </summary>
	List<SceneCollection> currentCollections = new List<SceneCollection> ();


	/// <summary>
	/// Warning info
	/// </summary>
	string warning = "";

	/// <summary>
	/// The loaded scenes.
	/// </summary>
	List<UnityEngine.SceneManagement.Scene> loadedScenes = new List<UnityEngine.SceneManagement.Scene> ();

	/// <summary>
	/// The show loading point.
	/// </summary>
	public bool showLoadingPoint = true;

	/// <summary>
	/// The distance from center for scene loading.
	/// </summary>
	public int distanceFromCenter;

	/// <summary>
	/// Is distance meassured in tiles
	/// </summary>
	public bool tiles;

	/// <summary>
	/// The center point for scene loading.
	/// </summary>
	public Vector3 CenterPoint;

	/// <summary>
	/// The old center point.
	/// </summary>
	private Vector3 oldCenterPoint;

	/// <summary>
	/// The splits of tiles.
	/// </summary>
	List<Dictionary<string,GameObject>> splits = new List<Dictionary<string,GameObject>> ();

	/// <summary>
	/// The scroll position.
	/// </summary>
	private Vector2 scrollPos;

	/// <summary>
	/// Init this instance.
	/// </summary>
	[MenuItem ("World Streamer/Local Area Updater[BETA]")]
	static void Init ()
	{
		// Get existing open window or if none, make a new one:
		LocalAreaUpdaterEditor window = EditorWindow.GetWindow <LocalAreaUpdaterEditor> ("Local Area [BETA]");
		window.Show ();
		SceneView.onSceneGUIDelegate += window.OnSceneGUI;
		window.loadedScenes.Clear ();
		window.loadedScenes.AddRange (EditorSceneManager.GetAllScenes ());
	}

	/// <summary>
	/// Raises the focus event.
	/// </summary>
	void OnFocus ()
	{
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
		SceneView.onSceneGUIDelegate += this.OnSceneGUI;
	}

	/// <summary>
	/// Raises the destroy event.
	/// </summary>
	void OnDestroy ()
	{
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
	}

	/// <summary>
	/// Raises the GUI event.
	/// </summary>
	void OnGUI ()
	{

		if (!string.IsNullOrEmpty (warning)) {

			GUILayout.Space (20);
			var TextStyle = new GUIStyle ();
			TextStyle.normal.textColor = Color.red;
			TextStyle.alignment = TextAnchor.MiddleCenter;
			TextStyle.fontStyle = FontStyle.Bold;
			GUILayout.Label (warning, TextStyle);

		}

		scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

		EditorGUIUtility.wideMode = true;

		collectionsCollapsed = EditorGUILayout.Foldout (collectionsCollapsed, "Scene collections: ");
		if (collectionsCollapsed) {
			EditorGUI.indentLevel++;
			listSizeCollections = EditorGUILayout.IntField ("size", listSizeCollections);
			if (listSizeCollections != currentCollections.Count) {
				while (listSizeCollections > currentCollections.Count) {
					currentCollections.Add (null);
				}
				while (listSizeCollections < currentCollections.Count) {
					currentCollections.RemoveAt (currentCollections.Count - 1);
				}
			}

			for (int i = 0; i < currentCollections.Count; i++) {
				currentCollections [i] = (SceneCollection)EditorGUILayout.ObjectField (currentCollections [i], typeof(SceneCollection), true);
			}
			EditorGUI.indentLevel--;
		}
		GUILayout.Space (10);
		distanceFromCenter = EditorGUILayout.IntField ("Loading distance", distanceFromCenter);
		if (distanceFromCenter < 0)
			distanceFromCenter = 0;
		
		EditorGUILayout.BeginHorizontal ();

		tiles = EditorGUILayout.Toggle ("Tiles", tiles);
		tiles = !EditorGUILayout.Toggle ("Units", !tiles);

		EditorGUILayout.EndHorizontal ();

		GUILayout.Space (10);

		CenterPoint = EditorGUILayout.Vector3Field ("Loading Center", CenterPoint);

		EditorGUILayout.BeginHorizontal ();

		showLoadingPoint = EditorGUILayout.Toggle ("Show loading center", showLoadingPoint);

		if (GUILayout.Button ("Show center")) {

			if (SceneView.lastActiveSceneView != null) {
				SceneView.lastActiveSceneView.LookAt (CenterPoint);

	
			}

		}

		EditorGUILayout.EndHorizontal ();

		GUILayout.Space (10);
		GUILayout.Space (10);



		if (GUILayout.Button ("Load Scenes from Collections around Center Point")) {

			LoadScenesAroundCenterPoint ();

		}

		if (GUILayout.Button ("Load All Scenes from Collections")) {

			LoadScenes ();

		}
		GUILayout.Space (10);
		if (GUILayout.Button ("Unsplit Scenes")) {

			foreach (var sceneCollection in currentCollections) {
				splits.Add (new Dictionary<string, GameObject> ());
				UnSplitScene (sceneCollection);
			}

		}

		if (GUILayout.Button ("Split Scenes")) {

			UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene ();

			if (!scene.isDirty || EditorUtility.DisplayDialog ("Save warning", "For spliting Your active scene must be saved!", "Save active scene now", "No") && EditorSceneManager.SaveScene (scene)) {

			
					

				splits = new List<Dictionary<string, GameObject>> ();

				foreach (var sceneCollection in currentCollections) {
					splits.Add (new Dictionary<string, GameObject> ());
					SplitScene (sceneCollection);
				}

				EditorSceneManager.MarkAllScenesDirty ();
				EditorSceneManager.SetActiveScene (scene);

			}
		}

		GUILayout.Space (10);

		if (GUILayout.Button ("Save loaded Scenes")) {

			SaveScenes ();

		}

		if (GUILayout.Button ("Unload loaded Scenes")) {

			UnloadScenes ();

		}

		GUILayout.Space (10);

		if (GUILayout.Button ("Remove empty Scenes from collections")) {

			RemoveEmptyScenes ();

		}
		if (GUILayout.Button ("Delete empty Scenes")) {

			if (EditorUtility.DisplayDialog ("Delete warning", "Do You want do delete scene files from project folder. This operation can't be undone", "Yes, I want do delete scene files", "No"))
				RemoveEmptyScenes (true);

		}


		EditorGUILayout.EndScrollView ();
	}

	/// <summary>
	/// Raises the scene GUI event.
	/// </summary>
	/// <param name="sceneView">Scene view.</param>
	void OnSceneGUI (SceneView sceneView)
	{
		Handles.color = new Color32 (147, 225, 58, 255);

		CenterPoint = Handles.PositionHandle (CenterPoint, Quaternion.identity);

		Handles.SphereCap (0, CenterPoint, Quaternion.identity, 0.1f); 

		if (!tiles) {
			Handles.color = Color.white;
			Handles.SelectionFrame (0, CenterPoint - new Vector3 (0, 0, distanceFromCenter), Quaternion.Euler (0, 0, 0), distanceFromCenter);
			Handles.SelectionFrame (0, CenterPoint + new Vector3 (0, 0, distanceFromCenter), Quaternion.Euler (0, 0, 0), distanceFromCenter);
			Handles.SelectionFrame (0, CenterPoint - new Vector3 (distanceFromCenter, 0, 0), Quaternion.Euler (0, 90, 90), distanceFromCenter);
			Handles.SelectionFrame (0, CenterPoint + new Vector3 (distanceFromCenter, 0, 0), Quaternion.Euler (0, 90, 90), distanceFromCenter);
			Handles.SelectionFrame (0, CenterPoint, Quaternion.Euler (90, 0, 0), distanceFromCenter);
		}

		if (Vector3.Distance (CenterPoint, oldCenterPoint) > 0)
			Repaint ();
		
		oldCenterPoint = CenterPoint;

	}

	/// <summary>
	/// Loads the scenes around center point within range.
	/// </summary>
	void LoadScenesAroundCenterPoint ()
	{
		foreach (var item in currentCollections) {
			foreach (var sceneName in item.names) {
				
				int posX = 0;
				int posY = 0;
				int posZ = 0;

				Vector3 pos = CenterPoint;


				int xPosCurrent = (item.xSize != 0) ? (int)(Mathf.FloorToInt (pos.x / item.xSize)) : 0;
				int yPosCurrent = (item.ySize != 0) ? (int)(Mathf.FloorToInt (pos.y / item.ySize)) : 0;
				int zPosCurrent = (item.zSize != 0) ? (int)(Mathf.FloorToInt (pos.z / item.zSize)) : 0;

				Streamer.SceneNameToPos (item, sceneName, out posX, out posY, out posZ);

				if (tiles) {

					if (Mathf.Abs (posX - xPosCurrent) < distanceFromCenter
					    && Mathf.Abs (posY - yPosCurrent) < distanceFromCenter
					    && Mathf.Abs (posZ - zPosCurrent) < distanceFromCenter)
						loadedScenes.Add (EditorSceneManager.OpenScene (item.path + sceneName, OpenSceneMode.Additive));
				} else {
					if (Mathf.Abs (posX * item.xSize - pos.x) < distanceFromCenter
					    && Mathf.Abs (posY * item.ySize - pos.y) < distanceFromCenter
					    && Mathf.Abs (posZ * item.zSize - pos.z) < distanceFromCenter)
						loadedScenes.Add (EditorSceneManager.OpenScene (item.path + sceneName, OpenSceneMode.Additive));
				}

			}
		}

	}

	/// <summary>
	/// Loads all the scenes from collections.
	/// </summary>
	void LoadScenes ()
	{

		foreach (var item in currentCollections) {
			foreach (var sceneName in item.names) {
				loadedScenes.Add (EditorSceneManager.OpenScene (item.path + sceneName, OpenSceneMode.Additive));
			}
		}
	}

	/// <summary>
	/// Saves the scenes.
	/// </summary>
	void SaveScenes ()
	{
		foreach (var layer in currentCollections) {
			
		
			foreach (var item in loadedScenes) {
				
				if (item.name.StartsWith (layer.prefixScene) && item.isLoaded) {
					if (GameObject.Find (item.name) == null) {

						GameObject split = new GameObject (item.name);
						SceneSplitManager sceneSplitManager = split.AddComponent<SceneSplitManager> ();
						sceneSplitManager.sceneName = split.name;

						sceneSplitManager.size = new Vector3 (layer.xSize != 0 ? layer.xSize : 100, layer.ySize != 0 ? layer.ySize : 100, layer.zSize != 0 ? layer.zSize : 100);
						int posx;
						int posy;
						int posz;
						Streamer.SceneNameToPos (layer, item.name, out posx, out posy, out posz);
						posx *= layer.xSize;
						posy *= layer.ySize;
						posz *= layer.zSize;
						sceneSplitManager.position = GetSplitPosition (new Vector3 (posx, posy, posz), layer);
						sceneSplitManager.color = layer.color;
						EditorSceneManager.MoveGameObjectToScene (split, item);

					}
				}
			}
		}
		EditorSceneManager.SaveModifiedScenesIfUserWantsTo (loadedScenes.ToArray ());

	}

	/// <summary>
	/// Removes or deletes the empty scenes.
	/// </summary>
	/// <param name="delete">If set to <c>true</c> will delete scene files.</param>
	void RemoveEmptyScenes (bool delete = false)
	{
		
		List<UnityEngine.SceneManagement.Scene> scenesToDelete = new List<UnityEngine.SceneManagement.Scene> ();
		foreach (var scene in loadedScenes) {
			if (scene.rootCount == 0) {
				scenesToDelete.Add (scene);
			}
		}

		foreach (var sceneCollection in currentCollections) {
			List<string> sceneList = new List<string> ();
			sceneList.AddRange (sceneCollection.names);
			foreach (var scene in scenesToDelete) {
				loadedScenes.Remove (scene);
				sceneList.Remove (scene.name + ".unity");
			}
			sceneCollection.names = sceneList.ToArray ();
		}
		if (!delete) {
			foreach (var scene in scenesToDelete) {
				EditorSceneManager.CloseScene (scene, true);
			}
		} else {
			List<string> scenesToDeletePaths = new List<string> ();
			foreach (var scene in scenesToDelete) {
				scenesToDeletePaths.Add (scene.path);
				EditorSceneManager.CloseScene (scene, true);

			}
			scenesToDelete.Clear ();
			foreach (var scenePath in scenesToDeletePaths) {
				FileUtil.DeleteFileOrDirectory (scenePath);
			}
		}
	}

	/// <summary>
	/// Unloads all scenes.
	/// </summary>
	void UnloadScenes ()
	{
		UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene ();
		foreach (var item in EditorSceneManager.GetAllScenes()) {
			if (item != scene)
				EditorSceneManager.CloseScene (item, true);
		}

	}

	/// <summary>
	/// Unsplits the scene from tiles.
	/// </summary>	
	void UnSplitScene (SceneCollection layer)
	{

		warning = "";
		splits [layer.layerNumber] = new Dictionary<string, GameObject> ();




		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();

		FindSceneGO (layer.prefixScene, allObjects, splits [layer.layerNumber]);

		ClearSceneGO (layer);

	}

	/// <summary>
	/// Splits the scene into tiles.
	/// </summary>	
	void SplitScene (SceneCollection layer)
	{
		warning = "";
		splits [layer.layerNumber] = new Dictionary<string, GameObject> ();


	


		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();

		FindSceneGO (layer.prefixScene, allObjects, splits [layer.layerNumber]);

		ClearSceneGO (layer);


		foreach (var item in allObjects) {
			if (item == null || item.transform.parent != null || !item.name.StartsWith (layer.prefixName)
			    || item.GetComponent<SceneSplitManager> () != null || item.GetComponent<SceneCollection> () != null || item.GetComponent<SceneSplitterSettings> () != null)
				continue;
			
			string splitName = layer.prefixScene + GetID (item.transform.position, layer);
		
			UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneByName (splitName);//EditorSceneManager.NewScene (NewSceneSetup.EmptyScene, NewSceneMode.Additive);



			if (scene.name == null) {

				string splitSceneName = splitName + ".unity";
				string sceneName = layer.path + splitSceneName;

				bool contains = false;

				foreach (var nameSplit in layer.names) {
					if (nameSplit == splitSceneName) {
						contains = true;
						break;
					}
				}

				if (!contains) {
					List<string> names = new List<string> ();
					names.AddRange (layer.names);
					names.Add (splitSceneName);
					layer.names = names.ToArray ();

					scene = EditorSceneManager.NewScene (NewSceneSetup.EmptyScene, NewSceneMode.Additive);

					EditorSceneManager.SaveScene (scene, sceneName);
					loadedScenes.Add (scene);

				} else {
					scene = EditorSceneManager.OpenScene (sceneName, OpenSceneMode.Additive);
					loadedScenes.Add (scene);
				}



			}
		}

		allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();

		FindSceneGO (layer.prefixScene, allObjects, splits [layer.layerNumber]);

		ClearSceneGO (layer);

		foreach (var item in allObjects) {
			if (item == null || item.transform.parent != null || !item.name.StartsWith (layer.prefixName)
			    || item.GetComponent<SceneSplitManager> () != null || item.GetComponent<SceneCollection> () != null || item.GetComponent<SceneSplitterSettings> () != null)
				continue;
			string itemId = GetID (item.transform.position, layer);

			GameObject split = null;
			if (!splits [layer.layerNumber].TryGetValue (itemId, out split)) {
				split = new GameObject (layer.prefixScene + itemId);
				SceneSplitManager sceneSplitManager = split.AddComponent<SceneSplitManager> ();
				sceneSplitManager.sceneName = split.name;

				sceneSplitManager.size = new Vector3 (layer.xSize != 0 ? layer.xSize : 100, layer.ySize != 0 ? layer.ySize : 100, layer.zSize != 0 ? layer.zSize : 100);
				sceneSplitManager.position = GetSplitPosition (item.transform.position, layer);
				sceneSplitManager.color = layer.color;
				
				splits [layer.layerNumber].Add (itemId, split);

				Vector3 splitPosId = GetSplitPositionID (item.transform.position, layer);

				if (layer.xSplitIs) {
					if (splitPosId.x < layer.xLimitsx) {
						layer.xLimitsx = (int)splitPosId.x;
					}
					if (splitPosId.x > layer.xLimitsy) {
						layer.xLimitsy = (int)splitPosId.x;
					}
				} else {
					layer.xLimitsx = 0;
					layer.xLimitsy = 0;
				}

				if (layer.ySplitIs) {
					if (splitPosId.y < layer.yLimitsx) {
						layer.yLimitsx = (int)splitPosId.y;
					}
					if (splitPosId.y > layer.yLimitsy) {
						layer.yLimitsy = (int)splitPosId.y;
					}
				} else {
					layer.yLimitsx = 0;
					layer.yLimitsy = 0;
				}

				if (layer.zSplitIs) {
					if (splitPosId.z < layer.zLimitsx) {
						layer.zLimitsx = (int)splitPosId.x;
					}
					if (splitPosId.z > layer.zLimitsy) {
						layer.zLimitsy = (int)splitPosId.z;
					}
				} else {
					layer.zLimitsx = 0;
					layer.zLimitsy = 0;
				}

				UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneByName (sceneSplitManager.sceneName);


			

				EditorSceneManager.MoveGameObjectToScene (split, scene);

			}



			item.transform.SetParent (split.transform);
		}

		if (splits.Count == 0) {
			warning = "No objects to split. Check GameObject or Scene Prefix.";
		}

		foreach (var item in loadedScenes) {
			if (!string.IsNullOrEmpty(item.name) && GameObject.Find (item.name) == null) {

				GameObject split = new GameObject (item.name);
				SceneSplitManager sceneSplitManager = split.AddComponent<SceneSplitManager> ();
				sceneSplitManager.sceneName = split.name;

				sceneSplitManager.size = new Vector3 (layer.xSize != 0 ? layer.xSize : 100, layer.ySize != 0 ? layer.ySize : 100, layer.zSize != 0 ? layer.zSize : 100);
				int posx;
				int posy;
				int posz;
                
				Streamer.SceneNameToPos (layer, item.name, out posx, out posy, out posz);
                
				posx *= layer.xSize;
				posy *= layer.ySize;
				posz *= layer.zSize;
				sceneSplitManager.position = GetSplitPosition (new Vector3 (posx, posy, posz), layer);
				sceneSplitManager.color = layer.color;

				splits [layer.layerNumber].Add (split.name.Replace (layer.prefixScene, ""), split);
				EditorSceneManager.MoveGameObjectToScene (split, item);

			}
		}


	}

	/// <summary>
	/// Clears the scene Game objects.
	/// </summary>
	void ClearSceneGO (SceneCollection layer)
	{
		List<string> toRemove = new List<string> ();

		foreach (var item in splits [layer.layerNumber]) {
			if (item.Value.GetComponent<SceneSplitManager> ()) {
				Transform splitTrans = item.Value.transform;
				foreach (Transform splitChild in splitTrans) {
					splitChild.parent = null;
				}

				while (splitTrans.childCount > 0) {

					foreach (Transform splitChild in splitTrans) {
						splitChild.parent = null;
					}

				}
				GameObject.DestroyImmediate (splitTrans.gameObject);
				toRemove.Add (item.Key);
			}
		}
		foreach (var item in toRemove) {
			splits [layer.layerNumber].Remove (item);
		}
	}

	/// <summary>
	/// Finds the scene stream splits.
	/// </summary>
	/// <param name="allObjects">All objects in scene.</param>
	/// <param name="destroyOther">If set to <c>true</c> destroy other objects in scene.</param>
	void FindSceneGO (string prefixScene, GameObject[] allObjects, Dictionary<string, GameObject> splits, bool destroyOther = false)
	{
		foreach (var item in allObjects) {
			if (item == null)
				continue;

			if (item != null && (item.transform.parent != null || !item.name.StartsWith (prefixScene))) {
				if (destroyOther && item.transform.parent == null) {
					DestroyImmediate (item);
				}
				continue;
			}

			GameObject go;
			string sceneID = "";

			sceneID = item.name.Replace (prefixScene, "");
			if (!splits.TryGetValue (sceneID, out go))
				splits.Add (sceneID, item);
		}
	}

	/// <summary>
	/// Gets the ID of tile by position.
	/// </summary>
	/// <returns>The ID of tile.</returns>
	/// <param name="position">Position of tile.</param>
	/// <param name="position">Position of tile.</param>
	/// <param name="layer">Layer.</param>
	string GetID (Vector3 position, SceneCollection layer)
	{
		int xId = (int)(Mathf.FloorToInt (position.x / layer.xSize));

		if (Mathf.Abs ((position.x / layer.xSize) - Mathf.RoundToInt (position.x / layer.xSize)) < 0.001f) {
			xId = (int)Mathf.RoundToInt (position.x / layer.xSize);
		}


		int yId = (int)(Mathf.FloorToInt (position.y / layer.ySize));

		if (Mathf.Abs ((position.y / layer.ySize) - Mathf.RoundToInt (position.y / layer.ySize)) < 0.001f) {
			yId = (int)Mathf.RoundToInt (position.y / layer.ySize);
		}

		int zId = (int)(Mathf.FloorToInt (position.z / layer.zSize));

		if (Mathf.Abs ((position.z / layer.zSize) - Mathf.RoundToInt (position.z / layer.zSize)) < 0.001f) {
			zId = (int)Mathf.RoundToInt (position.z / layer.zSize);
		}


		return (layer.xSplitIs ? "_x" + xId : "") +
		(layer.ySplitIs ? "_y" + yId : "")
		+ (layer.zSplitIs ? "_z" + zId : "");
	}

	/// <summary>
	/// Gets the split position.
	/// </summary>
	/// <returns>The split position.</returns>
	/// <param name="position">Position of tile.</param>
	/// <param name="layer">Layer.</param>
	Vector3 GetSplitPosition (Vector3 position, SceneCollection layer)
	{
		int x = (int)(Mathf.FloorToInt (position.x / layer.xSize));

		if (Mathf.Abs ((position.x / layer.xSize) - Mathf.RoundToInt (position.x / layer.xSize)) < 0.001f) {
			x = (int)Mathf.RoundToInt (position.x / layer.xSize);
		}


		int y = (int)(Mathf.FloorToInt (position.y / layer.ySize));

		if (Mathf.Abs ((position.y / layer.ySize) - Mathf.RoundToInt (position.y / layer.ySize)) < 0.001f) {
			y = (int)Mathf.RoundToInt (position.y / layer.ySize);
		}

		int z = (int)(Mathf.FloorToInt (position.z / layer.zSize));

		if (Mathf.Abs ((position.z / layer.zSize) - Mathf.RoundToInt (position.z / layer.zSize)) < 0.001f) {
			z = (int)Mathf.RoundToInt (position.z / layer.zSize);
		}


		return new Vector3 (x * layer.xSize, y * layer.ySize, z * layer.zSize);
	}

	/// <summary>
	/// Gets the split position divided by size.
	/// </summary>
	/// <returns>The split position I.</returns>
	/// <param name="position">Position.</param>
	/// <param name="layer">Layer.</param>
	Vector3 GetSplitPositionID (Vector3 position, SceneCollection layer)
	{
		int x = (int)(Mathf.FloorToInt (position.x / layer.xSize));

		if (Mathf.Abs ((position.x / layer.xSize) - Mathf.RoundToInt (position.x / layer.xSize)) < 0.001f) {
			x = (int)Mathf.RoundToInt (position.x / layer.xSize);
		}


		int y = (int)(Mathf.FloorToInt (position.y / layer.ySize));

		if (Mathf.Abs ((position.y / layer.ySize) - Mathf.RoundToInt (position.y / layer.ySize)) < 0.001f) {
			y = (int)Mathf.RoundToInt (position.y / layer.ySize);
		}

		int z = (int)(Mathf.FloorToInt (position.z / layer.zSize));

		if (Mathf.Abs ((position.z / layer.zSize) - Mathf.RoundToInt (position.z / layer.zSize)) < 0.001f) {
			z = (int)Mathf.RoundToInt (position.z / layer.zSize);
		}


		return new Vector3 (x, y, z);
	}
	

	
	

	#else
	
	[MenuItem ("World Streamer/Multi Scene Helper (UNITY 5.3 NEEDED)")]
	static void Init ()
	{
		Debug.LogError ("You need Unity version 5.3 or newer!");
	}

	/// <summary>
	/// Raises the GUI event.
	/// </summary>
	void OnGUI ()
	{



		GUILayout.Space (20);
		var TextStyle = new GUIStyle ();
		TextStyle.normal.textColor = Color.red;
		TextStyle.alignment = TextAnchor.MiddleCenter;
		TextStyle.fontStyle = FontStyle.Bold;
		GUILayout.Label ("You need Unity version 5.3 or newer!", TextStyle);


	}
		#endif

}
