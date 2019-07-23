using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Linq;

#if UNITY_5_3
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Scene splitter editor.
/// </summary>
public class SceneSplitterEditor : EditorWindow
{
	/// <summary>
	/// The splits of tiles.
	/// </summary>
	List<Dictionary<string,GameObject>> splits = new List<Dictionary<string,GameObject>> ();

	/// <summary>
	/// The scene splitter settings.
	/// </summary>
	SceneSplitterSettings sceneSplitterSettings;

	/// <summary>
	/// Warning info
	/// </summary>
	string warning = "";

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
	/// The layers collapsed.
	/// </summary>
	bool layersCollapsed = true;
	/// <summary>
	/// The scene layers.
	/// </summary>
	List<SceneCollection> sceneLayers = new List<SceneCollection> ();
	/// <summary>
	/// The layers to remove.
	/// </summary>
	List<SceneCollection> layersToRemove = new List<SceneCollection> ();

	/// <summary>
	/// The current scene.
	/// </summary>
	private static string currentScene;
	/// <summary>
	/// The generation cancel.
	/// </summary>
	private bool cancel = false;

	/// <summary>
	/// The scroll position.
	/// </summary>
	private Vector2 scrollPos;

	/// <summary>
	///  Add menu named "Scene splitter" to the Window menu
	/// </summary>
	[MenuItem ("World Streamer/Scene splitter")]
	static void Init ()
	{
		// Get existing open window or if none, make a new one:
		SceneSplitterEditor window = EditorWindow.GetWindow <SceneSplitterEditor> ("Scene Splitter");
		window.Show ();
		currentScene = EditorApplication.currentScene;
		window.SceneChanged ();
	}



	/// <summary>
	/// Raises the GUI event.
	/// </summary>
	void OnGUI ()
	{

		if (currentScene != EditorApplication.currentScene) {
			SceneChanged ();
		}

		GUILayout.Label (currentScene, EditorStyles.boldLabel);

		scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

		GUILayout.Label ("Scene layers", EditorStyles.boldLabel);
		if (GUILayout.Button ("Add layer")) {
			CreateLayer ();
		}

		layersCollapsed = EditorGUILayout.Foldout (layersCollapsed, "Layers: " + sceneLayers.Count);
		if (layersCollapsed) {
			int layerNum = 0;
			EditorGUI.indentLevel++;
			foreach (var layer in sceneLayers) {

				EditorGUILayout.BeginHorizontal ();
				layer.collapsed = EditorGUILayout.Foldout (layer.collapsed, "Layer: " + layer.prefixScene);
				if (GUILayout.Button (new GUIContent ("S", "Split Scene by layer"), GUILayout.Width (25))) {
					SplitScene (layer);
				}
				if (GUILayout.Button (new GUIContent ("G", "Generate Scene by layer"), GUILayout.Width (25))) {
					#if UNITY_5_3
					GenerateScenesMulti (layer, 0, 1);
					#else
					GenerateScenes (layer, 0, 1);
					#endif

					listSizeCollections = currentCollections.Count;
					EditorUtility.ClearProgressBar ();
				}
				if (GUILayout.Button (new GUIContent ("C", "Clear Scene by layer"), GUILayout.Width (25))) {
					ClearSplitScene (layer);
				}
				if (GUILayout.Button (new GUIContent ("X", "Delete layer"), GUILayout.Width (25))) {
					DeleteLayer (layer);
					continue;
				}
				layer.color = EditorGUILayout.ColorField (layer.color, GUILayout.Width (60));

				EditorGUILayout.EndHorizontal ();
				EditorGUI.indentLevel++;
				if (layer.collapsed) {

					layer.xSplitIs = EditorGUILayout.Toggle ("Split x", layer.xSplitIs);
				
					if (layer.xSplitIs) {
						layer.xSize = EditorGUILayout.IntField ("X size", layer.xSize);
					} else
						layer.xSize = 0;
				
					layer.ySplitIs = EditorGUILayout.Toggle ("Split y", layer.ySplitIs);
					if (layer.ySplitIs) {
						layer.ySize = EditorGUILayout.IntField ("Y size", layer.ySize);
					} else
						layer.ySize = 0;
				
					layer.zSplitIs = EditorGUILayout.Toggle ("Split z", layer.zSplitIs);
					if (layer.zSplitIs) {
						layer.zSize = EditorGUILayout.IntField ("Z size", layer.zSize);
					} else
						layer.zSize = 0;
				
					layer.prefixName = EditorGUILayout.TextField ("GameObject Prefix", layer.prefixName);
					layer.prefixScene = EditorGUILayout.TextField ("Scene Prefix", layer.prefixScene);
					layer.layerNumber = layerNum;
					GUILayout.Space (10);
					layerNum++;
				}
				EditorGUI.indentLevel--;
			}
			EditorGUI.indentLevel--;

		}

		foreach (var item in layersToRemove) {
			sceneLayers.Remove (item);
			DestroyImmediate (item.gameObject);
		}
		layersToRemove.Clear ();
		



		if (sceneSplitterSettings == null)
			CreateSettings ();

		GUILayout.Space (10);
		sceneSplitterSettings.scenesPath = EditorGUILayout.TextField ("Scene create folder", sceneSplitterSettings.scenesPath);


		GUILayout.Space (10);


		if (GUILayout.Button ("Split Scene")) {
			foreach (var layer in sceneLayers) {
				SplitScene (layer);
			}
		}

		if (GUILayout.Button ("Clear Scene Split")) {
			foreach (var layer in sceneLayers) {
				ClearSplitScene (layer);
			}
		}
				
		if (GUILayout.Button ("Generate Scenes")) {
			currentCollections.Clear ();

			EditorUtility.DisplayProgressBar ("Creating Scenes", "Preparing scene", 0);
			int currentLayerID = 0;

			foreach (var layer in sceneLayers) {
				if (cancel)
					break;
				if (EditorUtility.DisplayCancelableProgressBar ("Preparing Scenes " + (currentLayerID + 1) + '/' + sceneLayers.Count, "Preparing scene " + layer.prefixScene, (currentLayerID / (float)sceneLayers.Count))) {
					cancel = true;
					break;
				}
				#if UNITY_5_3
				GenerateScenesMulti (layer, currentLayerID, sceneLayers.Count);
				#else
				GenerateScenes (layer, currentLayerID, sceneLayers.Count);
				#endif

				currentLayerID++;
			}
			if (cancel) {
				cancel = false;
			} else
				listSizeCollections = currentCollections.Count;

			EditorUtility.ClearProgressBar ();
		}



		if (!string.IsNullOrEmpty (warning)) {
			
			GUILayout.Space (20);
			var TextStyle = new GUIStyle ();
			TextStyle.normal.textColor = Color.red;
			TextStyle.alignment = TextAnchor.MiddleCenter;
			TextStyle.fontStyle = FontStyle.Bold;
			GUILayout.Label (warning, TextStyle);
			
		}
		
		GUILayout.Space (20);

		GUILayout.Box ("", new GUILayoutOption[]{ GUILayout.ExpandWidth (true), GUILayout.Height (1) });
		GUILayout.Space (10);
		GUILayout.Label ("Build Settings", EditorStyles.boldLabel);
		GUILayout.Space (10);

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
		
		if (GUILayout.Button ("Add scenes to build settings")) {

			AddScenesToBuild ();
		}
		if (GUILayout.Button ("Remove scenes from build settings")) {
			RemoveScenesFromBuild ();
		}
		if (GUILayout.Button ("Find scene collections prefabs")) {
			foreach (var layer in sceneLayers) {
				FindCollection (layer);
			}
		}
//		if (GUILayout.Button ("Rob - Generate Random Scene")) {
//			foreach (var layer in sceneLayers) {
//				GenerateRandomSceneObjects (layer);
//			}
//		}

		
		EditorGUILayout.EndScrollView ();

	}

	/// <summary>
	/// Scenes the changed.
	/// </summary>
	void SceneChanged ()
	{
		currentScene = EditorApplication.currentScene;

		sceneSplitterSettings = FindObjectOfType (typeof(SceneSplitterSettings)) as SceneSplitterSettings;

		if (sceneSplitterSettings == null)
			CreateSettings ();


		SceneCollection[] sceneCollections = FindObjectsOfType (typeof(SceneCollection)) as SceneCollection[];
		currentCollections.Clear ();
		listSizeCollections = 0;
		sceneLayers.Clear ();
		if (sceneCollections.Length > 0) {
			if (sceneLayers == null)
				sceneLayers = new List<SceneCollection> ();
			sceneLayers.AddRange (sceneCollections);



			splits = new List<Dictionary<string,GameObject>> ();
			foreach (var item in sceneLayers) {
			
				if (item.transform.parent != sceneSplitterSettings.transform)
					item.transform.parent = sceneSplitterSettings.transform;
				splits.Add (new Dictionary<string, GameObject> ());
			}
		}
		foreach (var layer in sceneLayers) {
			FindCollection (layer);
		}

	}

	/// <summary>
	/// Creates the settings gameObject.
	/// </summary>
	void CreateSettings ()
	{
		
		sceneSplitterSettings = FindObjectOfType (typeof(SceneSplitterSettings)) as SceneSplitterSettings;
		
		if (sceneSplitterSettings == null) {

			GameObject gameObject = new GameObject ("_SceneSplitterSettings");
			sceneSplitterSettings = gameObject.AddComponent<SceneSplitterSettings> (); 

		}
	}

	/// <summary>
	/// Deletes the layer.
	/// </summary>
	/// <param name="layer">Layer.</param>
	void DeleteLayer (SceneCollection layer)
	{
		layersToRemove.Add (layer);
		splits.RemoveAt (0);
	}

	/// <summary>
	/// Creates the layer.
	/// </summary>
	void CreateLayer ()
	{
		GameObject sceneCollectionGO = new GameObject ("SC_" + sceneLayers.Count);
		
		sceneCollectionGO.transform.parent = sceneSplitterSettings.transform;

		SceneCollection newSceneCollection = sceneCollectionGO.AddComponent<SceneCollection> ();
		newSceneCollection.color = new Color (Random.value, Random.value, Random.value, 255);
		sceneLayers.Add (newSceneCollection);
		splits.Add (new Dictionary<string, GameObject> ());
	}

	/// <summary>
	/// Adds the scenes to build.
	/// </summary>
	void AddScenesToBuild ()
	{
		warning = "";
		List<EditorBuildSettingsScene> scenesList = new List<EditorBuildSettingsScene> ();
		scenesList.AddRange (EditorBuildSettings.scenes);
		foreach (var currentCollection in currentCollections) {
			
		
                            
			List<string> scenesToAdd = new List<string> ();
			scenesToAdd.AddRange (currentCollection.names);

			foreach (var item in scenesList) {
				if (scenesToAdd.Contains (item.path.Replace (currentCollection.path, ""))) {
					scenesToAdd.Remove (item.path.Replace (currentCollection.path, ""));
				}
			}

			foreach (var item in scenesToAdd) {
				scenesList.Add (new EditorBuildSettingsScene (currentCollection.path + item, true));
			}
		}
		EditorBuildSettings.scenes = scenesList.ToArray ();
	}

	/// <summary>
	/// Removes the scenes from build.
	/// </summary>
	void RemoveScenesFromBuild ()
	{
		warning = "";
		List<EditorBuildSettingsScene> scenesList = new List<EditorBuildSettingsScene> ();
		scenesList.AddRange (EditorBuildSettings.scenes);



		foreach (var currentCollection in currentCollections) {

			List<string> scenesToAdd = new List<string> ();
			scenesToAdd.AddRange (currentCollection.names);

			List<EditorBuildSettingsScene> newScenesList = new List<EditorBuildSettingsScene> ();
			foreach (var item in scenesList) {
				if (scenesToAdd.Contains (item.path.Replace (currentCollection.path, ""))) {
					newScenesList.Add (item);
				}
			}
			foreach (var removeScene in newScenesList) {
				scenesList.Remove (removeScene);
			}

		}
        
		EditorBuildSettings.scenes = scenesList.ToArray ();
	}

	/// <summary>
	/// Generates the random scene objects.
	/// </summary>
	void GenerateRandomSceneObjects (SceneCollection layer)
	{
		warning = "";
		for (int i = 0; i < 100; i++) {
			GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
			cube.transform.position = Random.insideUnitSphere * 100;
			cube.name = layer.prefixName + "_" + i;
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
	/// Splits the scene into tiles.
	/// </summary>	
	void SplitScene (SceneCollection layer)
	{
		warning = "";
		splits [layer.layerNumber] = new Dictionary<string, GameObject> ();


		layer.xLimitsx = int.MaxValue;
		layer.xLimitsy = int.MinValue;
		layer.yLimitsx = int.MaxValue;
		layer.yLimitsy = int.MinValue;
		layer.zLimitsx = int.MaxValue;
		layer.zLimitsy = int.MinValue;


		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();

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
			}

						
			item.transform.SetParent (split.transform);
		}

		if (splits.Count == 0) {
			warning = "No objects to split. Check GameObject or Scene Prefix.";
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
	/// Clears the split scene.
	/// </summary>
	void ClearSplitScene (SceneCollection layer)
	{
		warning = "";
		splits [layer.layerNumber] = new Dictionary<string, GameObject> ();
		
		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();
		
		FindSceneGO (layer.prefixScene, allObjects, splits [layer.layerNumber]);
		ClearSceneGO (layer);
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

#if UNITY_5_3
	/// <summary>
	/// Generates scenes from splits with multi scene.
	/// </summary>
	void GenerateScenesMulti (SceneCollection layer, int currentLayerID, int layersCount)
	{
		if (cancel)
			return;
		warning = "";

		string scenesPath = this.sceneSplitterSettings.scenesPath;
		if (!Directory.Exists (scenesPath)) {

			warning = "Scene create folder doesn't exist.";

			return;
		}

		scenesPath += layer.prefixScene + "/";
		if (!Directory.Exists (scenesPath)) {
			Directory.CreateDirectory (scenesPath);
		}

		List<string> sceneNames = new List<string> ();

		EditorApplication.SaveScene ();

		Dictionary<string, GameObject> mainSplits = new Dictionary<string, GameObject> ();

		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();

		FindSceneGO (layer.prefixScene, allObjects, mainSplits);
		string currentScene = EditorApplication.currentScene;

		Dictionary<string,string> scenes = new Dictionary<string,string> ();


		List<string> splitsNames = new List<string> ();
		foreach (var split in mainSplits) {
			splitsNames.Add (split.Value.name);

		}

		if (splits.Count == 0) {

			warning = "No objects to build scenes.";
			return;
		}

		int i = 0;
		foreach (var split in splitsNames) {
			if (cancel)
				return;
			sceneNames.Add (split + ".unity");
			string sceneName = scenesPath + split + ".unity";

			Scene scene = EditorSceneManager.NewScene (NewSceneSetup.EmptyScene, NewSceneMode.Additive);

			SceneManager.MoveGameObjectToScene (GameObject.Find (split), scene);

			EditorSceneManager.SaveScene (scene, sceneName);
				
			scenes.Add (split, sceneName);

			if (EditorUtility.DisplayCancelableProgressBar ("Creating Scenes " + (currentLayerID + 1) + '/' + layersCount + " (" + layer.prefixScene + ")", "Creating scene " + Path.GetFileNameWithoutExtension (EditorApplication.currentScene) + " " + i + " from " + splitsNames.Count, (currentLayerID + (i / (float)splitsNames.Count)) / (float)layersCount)) {
				cancel = true;
				EditorUtility.ClearProgressBar ();
				return;
			}
			i++;
		}
		

		EditorSceneManager.OpenScene (currentScene, OpenSceneMode.Single);

		SceneCollection sceneCollection;
		GameObject createdCollectionGO;
		if (AssetDatabase.LoadAssetAtPath (scenesPath + "SC_" + layer.prefixScene + ".prefab", typeof(GameObject))) {

			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath (scenesPath + "SC_" + layer.prefixScene + ".prefab", typeof(GameObject));

			sceneCollection = prefab.GetComponent<SceneCollection> ();
			sceneCollection.path = scenesPath;
			sceneCollection.names = sceneNames.ToArray ();
			sceneCollection.prefixName = layer.prefixName;
			sceneCollection.prefixScene = layer.prefixScene;
			sceneCollection.xSplitIs = layer.xSplitIs;
			sceneCollection.ySplitIs = layer.ySplitIs;
			sceneCollection.zSplitIs = layer.zSplitIs;
			sceneCollection.xSize = layer.xSize;
			sceneCollection.ySize = layer.ySize;
			sceneCollection.zSize = layer.zSize;
			sceneCollection.xLimitsx = layer.xLimitsx;
			sceneCollection.xLimitsy = layer.xLimitsy;
			sceneCollection.yLimitsx = layer.yLimitsx;
			sceneCollection.yLimitsy = layer.yLimitsy;
			sceneCollection.zLimitsx = layer.zLimitsx;
			sceneCollection.zLimitsy = layer.zLimitsy;
			sceneCollection.color = layer.color;

			createdCollectionGO = prefab;
			if (!currentCollections.Contains (sceneCollection)) {
				currentCollections.Add (createdCollectionGO.GetComponent<SceneCollection> ());


			}
			EditorUtility.SetDirty (prefab);
			AssetDatabase.SaveAssets ();

		} else {

			GameObject sceneCollectionGO = new GameObject ("SC_" + layer.prefixScene);

			sceneCollection = sceneCollectionGO.AddComponent<SceneCollection> ();
			sceneCollection.path = scenesPath;
			sceneNames.Sort ();
			sceneCollection.names = sceneNames.ToArray ();
			sceneCollection.prefixName = layer.prefixName;
			sceneCollection.prefixScene = layer.prefixScene;
			sceneCollection.xSplitIs = layer.xSplitIs;
			sceneCollection.ySplitIs = layer.ySplitIs;
			sceneCollection.zSplitIs = layer.zSplitIs;
			sceneCollection.xSize = layer.xSize;
			sceneCollection.ySize = layer.ySize;
			sceneCollection.zSize = layer.zSize;
			sceneCollection.xLimitsx = layer.xLimitsx;
			sceneCollection.xLimitsy = layer.xLimitsy;
			sceneCollection.yLimitsx = layer.yLimitsx;
			sceneCollection.yLimitsy = layer.yLimitsy;
			sceneCollection.zLimitsx = layer.zLimitsx;
			sceneCollection.zLimitsy = layer.zLimitsy;
			sceneCollection.color = layer.color;

			createdCollectionGO = PrefabUtility.CreatePrefab (scenesPath + sceneCollectionGO.name + ".prefab", sceneCollectionGO);
			currentCollections.Add (createdCollectionGO.GetComponent<SceneCollection> ());
			GameObject.DestroyImmediate (sceneCollection.gameObject);
		}

	}
#endif
    /// <summary>
    /// Generates scenes from splits.
    /// </summary>
    void GenerateScenes (SceneCollection layer, int currentLayerID, int layersCount)
	{
		if (cancel)
			return;
		warning = "";

		string scenesPath = this.sceneSplitterSettings.scenesPath;
		if (!Directory.Exists (scenesPath)) {

			warning = "Scene create folder doesn't exist.";

			return;
		}

		scenesPath += layer.prefixScene + "/";
		if (!Directory.Exists (scenesPath)) {
			Directory.CreateDirectory (scenesPath);
		}

		List<string> sceneNames = new List<string> ();

		EditorApplication.SaveScene ();


		Dictionary<string, GameObject> mainSplits = new Dictionary<string, GameObject> ();
		
		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();

		FindSceneGO (layer.prefixScene, allObjects, mainSplits);
		string currentScene = EditorApplication.currentScene;

		Dictionary<string,string> scenes = new Dictionary<string,string> ();

		
		List<string> splitsNames = new List<string> ();
		foreach (var split in mainSplits) {
			splitsNames.Add (split.Value.name);
		
		}

		if (splits.Count == 0) {
			
			warning = "No objects to build scenes.";
			return;
		}

		int i = 0;
		foreach (var split in splitsNames) {
			if (cancel)
				return;
			sceneNames.Add (split + ".unity");
			string sceneName = scenesPath + split + ".unity";
			EditorApplication.SaveScene (sceneName, true);
			scenes.Add (split, sceneName);

		
			EditorApplication.OpenScene (sceneName);

			splits [layer.layerNumber] = new Dictionary<string, GameObject> ();
			allObjects = UnityEngine.Object.FindObjectsOfType<GameObject> ();
			FindSceneGO (layer.prefixScene, allObjects, splits [layer.layerNumber], true);


						

			foreach (var item in splits [layer.layerNumber]) {

				if (item.Value.name != split)
					GameObject.DestroyImmediate (item.Value);
			}

			Transform[] disabledTransf = GetAllDisabledSceneObjects ();
			foreach (var item in disabledTransf) {
				GameObject.DestroyImmediate (item.gameObject);
			}

			EditorApplication.SaveScene ();
			
			EditorApplication.OpenScene (currentScene);

			if (EditorUtility.DisplayCancelableProgressBar ("Creating Scenes " + (currentLayerID + 1) + '/' + layersCount + " (" + layer.prefixScene + ")", "Creating scene " + Path.GetFileNameWithoutExtension (EditorApplication.currentScene) + " " + i + " from " + splitsNames.Count, (currentLayerID + (i / (float)splitsNames.Count)) / (float)layersCount)) {
				cancel = true;
				EditorUtility.ClearProgressBar ();
				return;
			}
			i++;
		}

	
		EditorApplication.OpenScene (currentScene);

		SceneCollection sceneCollection;
		GameObject createdCollectionGO;
		if (AssetDatabase.LoadAssetAtPath (scenesPath + "SC_" + layer.prefixScene + ".prefab", typeof(GameObject))) {

			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath (scenesPath + "SC_" + layer.prefixScene + ".prefab", typeof(GameObject));

			sceneCollection = prefab.GetComponent<SceneCollection> ();
			sceneCollection.path = scenesPath;
			sceneCollection.names = sceneNames.ToArray ();
			sceneCollection.prefixScene = layer.prefixScene;
			sceneCollection.xSplitIs = layer.xSplitIs;
			sceneCollection.ySplitIs = layer.ySplitIs;
			sceneCollection.zSplitIs = layer.zSplitIs;
			sceneCollection.xSize = layer.xSize;
			sceneCollection.ySize = layer.ySize;
			sceneCollection.zSize = layer.zSize;
			sceneCollection.xLimitsx = layer.xLimitsx;
			sceneCollection.xLimitsy = layer.xLimitsy;
			sceneCollection.yLimitsx = layer.yLimitsx;
			sceneCollection.yLimitsy = layer.yLimitsy;
			sceneCollection.zLimitsx = layer.zLimitsx;
			sceneCollection.zLimitsy = layer.zLimitsy;
			sceneCollection.color = layer.color;


			createdCollectionGO = prefab;
			if (!currentCollections.Contains (sceneCollection))
				currentCollections.Add (createdCollectionGO.GetComponent<SceneCollection> ());

			AssetDatabase.SaveAssets ();
		} else {

			GameObject sceneCollectionGO = new GameObject ("SC_" + layer.prefixScene);
			
			sceneCollection = sceneCollectionGO.AddComponent<SceneCollection> ();
			sceneCollection.path = scenesPath;
			sceneNames.Sort ();
			sceneCollection.names = sceneNames.ToArray ();
			sceneCollection.prefixScene = layer.prefixScene;
			sceneCollection.xSplitIs = layer.xSplitIs;
			sceneCollection.ySplitIs = layer.ySplitIs;
			sceneCollection.zSplitIs = layer.zSplitIs;
			sceneCollection.xSize = layer.xSize;
			sceneCollection.ySize = layer.ySize;
			sceneCollection.zSize = layer.zSize;
			sceneCollection.xLimitsx = layer.xLimitsx;
			sceneCollection.xLimitsy = layer.xLimitsy;
			sceneCollection.yLimitsx = layer.yLimitsx;
			sceneCollection.yLimitsy = layer.yLimitsy;
			sceneCollection.zLimitsx = layer.zLimitsx;
			sceneCollection.zLimitsy = layer.zLimitsy;
			sceneCollection.color = layer.color;

			createdCollectionGO = PrefabUtility.CreatePrefab (scenesPath + sceneCollectionGO.name + ".prefab", sceneCollectionGO);
			currentCollections.Add (createdCollectionGO.GetComponent<SceneCollection> ());
			GameObject.DestroyImmediate (sceneCollection.gameObject);
		}




	}

	/// <summary>
	/// Finds the collections.
	/// </summary>
	public void FindCollection (SceneCollection layer)
	{
		string scenesPath = this.sceneSplitterSettings.scenesPath + layer.prefixScene + "/";
		GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath (scenesPath + "SC_" + layer.prefixScene + ".prefab", typeof(GameObject));

		if (prefab != null) {
			SceneCollection sceneCollection = prefab.GetComponent<SceneCollection> ();
			if (!currentCollections.Contains (sceneCollection))
				currentCollections.Add (sceneCollection);
			listSizeCollections = currentCollections.Count;
		}
		
	}

	/// <summary>
	/// Gets all disabled scene objects.
	/// </summary>
	/// <returns>The all disabled scene objects.</returns>
	public static Transform[] GetAllDisabledSceneObjects ()
	{
		var allTransforms = GameObject.FindObjectsOfTypeAll (typeof(Transform));
		
		var previousSelection = Selection.objects;
		
		Selection.objects = allTransforms.Cast<Transform> ()
			.Where (x => x != null && x.parent == null)
				.Select (x => x.gameObject)
				.Where (x => x != null && !x.activeSelf)
				.Cast<UnityEngine.Object> ().ToArray ();
		
		
		var selectedTransforms = Selection.GetTransforms (SelectionMode.TopLevel | SelectionMode.Editable | SelectionMode.ExcludePrefab);
		
		Selection.objects = previousSelection;
		
		return selectedTransforms;
	}
}