using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// Streams async scene tiles
/// </summary>
public class Streamer : MonoBehaviour
{
	/// <summary>
	/// The streamer tag.
	/// </summary>
	public static string STREAMERTAG = "SceneStreamer";

	/// <summary>
	/// The scene collection of tiles.
	/// </summary>
	public SceneCollection sceneCollection;
	/// <summary>
	/// The splits of scene collection.
	/// </summary>
	public SceneSplit[] splits;
	/// <summary>
	/// How often streamer checks player position.
	/// </summary>
	public float positionCheckTime = 3;
	/// <summary>
	/// Destroys unloaded tiles after seconds.
	/// </summary>
	public float destroyTileDelay = 2;
	/// <summary>
	/// The loading range of new tiles.
	/// </summary>
	public Vector3 loadingRange = new Vector3 (2, 2, 2);
	/// <summary>
	/// The deloading range of tiles.
	/// </summary>
	public Vector3 deloadingRange = new Vector3 (3, 3, 3);
	/// <summary>
	/// The max paralle scene loading.
	/// </summary>
	public int maxParalleSceneLoading = 1;
	/// <summary>
	/// The async scene load wait frames.
	/// </summary>
	public int sceneLoadWaitFrames = 60;
	/// <summary>
	/// The player transform.
	/// </summary>
	public Transform player;

	/// <summary>
	/// The terrain neighbours manager.
	/// </summary>
	public TerrainNeighbours terrainNeighbours;
	/// <summary>
	/// The show loading screen on start?
	/// </summary>
	public bool showLoadingScreen = true;

	/// <summary>
	/// The loading screen UI.
	/// </summary>
	public UILoadingStreamer loadingStreamer;

	/// <summary>
	/// The tiles to load.
	/// </summary>
	public int tilesToLoad = int.MaxValue;

	/// <summary>
	/// The tiles loaded.
	/// </summary>
	public int tilesLoaded;

	/// <summary>
	/// Gets the loading progress.
	/// </summary>
	/// <value>The loading progress.</value>
	public float LoadingProgress {
		get{ return  (tilesToLoad > 0) ? tilesLoaded / (float)tilesToLoad : 1; }
	}

	/// <summary>
	/// The world mover.
	/// </summary>
	[HideInInspector]
	public WorldMover
		worldMover;

	/// <summary>
	/// The current move.
	/// </summary>
	public Vector3 currentMove = Vector3.zero;

	/// <summary>
	/// The x position.
	/// </summary>
	int xPos = int.MinValue;
	/// <summary>
	/// The y position.
	/// </summary>
	int yPos = int.MinValue;
	/// <summary>
	/// The z position.
	/// </summary>
	int zPos = int.MinValue;


	/// <summary>
	/// The scenes array.
	/// </summary>
	public Dictionary<int[],SceneSplit> scenesArray;

	/// <summary>
	/// The loaded scenes.
	/// </summary>
	public List<SceneSplit> loadedScenes = new List<SceneSplit> ();

	/// <summary>
	/// The currently scene loading.
	/// </summary>
	int currentlySceneLoading = 0;

	/// <summary>
	/// The scenes to load.
	/// </summary>
	List<SceneSplit> scenesToLoad = new List<SceneSplit> ();

	/// <summary>
	/// The scene load frame next.
	/// </summary>
	int sceneLoadFrameNext = 0;

	/// <summary>
	/// The scene load frames next waited.
	/// </summary>
	bool sceneLoadFramesNextWaited = false;

	/// <summary>
	/// Is world looping on.
	/// </summary>
	public bool looping = false;



	/// <summary>
	/// Start this instance, prepares scene collection into scene array, starts player position checker
	/// </summary>
	void Start ()
	{
		if (sceneCollection != null) {

			PrepareScenesArray ();
			StartCoroutine (PositionChecker ());

		} else
			Debug.LogError ("No scene collection in streamer");

	}

	/// <summary>
	/// Adds the scene game object to collection
	/// </summary>
	/// <param name="sceneName">Scene name.</param>
	/// <param name="sceneGO">Scene Game object</param>
	public void AddSceneGO (string sceneName, GameObject sceneGO)
	{
		int posX = 0;
		int posY = 0;
		int posZ = 0;
		
		SceneNameToPos (sceneCollection, sceneName, out posX, out posY, out posZ);
		int[] posInt = new int[] { posX, posY, posZ };

		if (scenesArray.ContainsKey (posInt)) {
			scenesArray [posInt].sceneGo = sceneGO;

			//Debug.Log (currentMove + " " + new Vector3 (scenesArray [posInt].posXLimitMove, 0, 0));
			sceneGO.transform.position += currentMove + new Vector3 (scenesArray [posInt].posXLimitMove, scenesArray [posInt].posYLimitMove, scenesArray [posInt].posZLimitMove);

		}

		tilesLoaded++;
		currentlySceneLoading--;
		if (terrainNeighbours)
			terrainNeighbours.CreateNeighbours ();
	}

	/// <summary>
	/// Update this instance, starts load level async
	/// </summary>
	void Update ()
	{
		LoadLevelAsyncManage ();
	}

	/// <summary>
	/// Manages async scene loading
	/// </summary>
	void LoadLevelAsyncManage ()
	{
		if (scenesToLoad.Count > 0 && currentlySceneLoading <= 0) {

			if (LoadingProgress < 1 || sceneLoadFramesNextWaited && sceneLoadFrameNext <= 0) {
				sceneLoadFramesNextWaited = false;
				sceneLoadFrameNext = sceneLoadWaitFrames;
				while (currentlySceneLoading < maxParalleSceneLoading && scenesToLoad.Count > 0) {
					SceneSplit split = scenesToLoad [0];

///					if (!Application.isWebPlayer || Application.isWebPlayer && Application.CanStreamedLevelBeLoaded (split.sceneName)) {
///						scenesToLoad.Remove (split);
///						currentlySceneLoading++;
///						Application.LoadLevelAdditiveAsync (split.sceneName);
///					}

				}
			} else {
				sceneLoadFramesNextWaited = true;
				sceneLoadFrameNext--;
			}
		}
	}

	/// <summary>
	/// Coroutine checks player position
	/// </summary>
	/// <returns>The checker.</returns>
	IEnumerator PositionChecker ()
	{
		while (true) {
			CheckPositionTiles ();

			yield return new WaitForSeconds (positionCheckTime);
		}
	}

	/// <summary>
	/// Checks the position of player in tiles.
	/// </summary>
	public void CheckPositionTiles ()
	{


		Vector3 pos = player.position;

		pos -= currentMove;
	
		int xPosCurrent = (sceneCollection.xSize != 0) ? (int)(Mathf.FloorToInt (pos.x / sceneCollection.xSize)) : 0;
		int yPosCurrent = (sceneCollection.ySize != 0) ? (int)(Mathf.FloorToInt (pos.y / sceneCollection.ySize)) : 0;
		int zPosCurrent = (sceneCollection.zSize != 0) ? (int)(Mathf.FloorToInt (pos.z / sceneCollection.zSize)) : 0;
		if (xPosCurrent != xPos || yPosCurrent != yPos || zPosCurrent != zPos) {

			xPos = xPosCurrent;
			yPos = yPosCurrent;
			zPos = zPosCurrent;

			SceneLoading ();
			Invoke ("SceneUnloading", destroyTileDelay);

			if (worldMover != null) {
				worldMover.CheckMoverDistance (xPosCurrent, yPosCurrent, zPosCurrent);
			}
		}
	}

	/// <summary>
	/// Unloads tiles out of range
	/// </summary>
	void SceneUnloading ()
	{
		
		List<SceneSplit> scenesToDestroy = new List<SceneSplit> ();
		foreach (var item in loadedScenes) {

			if (Mathf.Abs (item.posX + item.xDeloadLimit - xPos) > (int)deloadingRange.x
			    || Mathf.Abs (item.posY + item.yDeloadLimit - yPos) > (int)deloadingRange.y
			    || Mathf.Abs (item.posZ + item.zDeloadLimit - zPos) > (int)deloadingRange.x)
			if (item.sceneGo != null)
				scenesToDestroy.Add (item);

		}

		foreach (var item in scenesToDestroy) {

			loadedScenes.Remove (item);

			if (item.sceneGo != null) {
				Terrain childTerrain = item.sceneGo.GetComponentInChildren<Terrain> ();
				if (childTerrain) {
					GameObject childTerrainGO = childTerrain.gameObject;

					Destroy (childTerrain);
					childTerrain = null;
					Destroy (childTerrainGO);
					childTerrainGO = null;

				}
			}
		
			#if UNITY_5_3
			try {
				SceneManager.UnloadScene (item.sceneGo.scene.name);
			} catch (System.Exception ex) {
				Debug.Log (item.sceneName);
				Debug.Log (item.sceneGo.name);
				Debug.Log (item.sceneGo.scene.name);
				Debug.LogError (ex.Message);
			}


			#else
			GameObject.Destroy (item.sceneGo);
			#endif

			item.sceneGo = null;
			item.loaded = false;
			
		}
		scenesToDestroy.Clear ();



		if (terrainNeighbours)
			terrainNeighbours.CreateNeighbours ();

		
		Resources.UnloadUnusedAssets ();
		System.GC.Collect ();
	}

	/// <summary>
	/// Loads tiles in range
	/// </summary>
	void SceneLoading ()
	{
		//Debug.Log (showLoadingScreen);
		if (showLoadingScreen && loadingStreamer != null) {
			showLoadingScreen = false;
			if (tilesLoaded >= tilesToLoad) {
				tilesToLoad = int.MaxValue;
				tilesLoaded = 0;
			}
		}


		int tilesToLoadNew = 0;


		int[] sceneIDPlayer = new int[] {
			xPos,
			yPos,
			zPos
		};

		if (scenesArray.ContainsKey (sceneIDPlayer)) {
			SceneSplit split = scenesArray [sceneIDPlayer];
			if (!split.loaded) {
				split.loaded = true;
				
				scenesToLoad.Add (split);
				loadedScenes.Add (split);
				tilesToLoadNew++;
			}
		}


		for (int x = -(int)loadingRange.x + xPos; x <= (int)loadingRange.x + xPos; x++) {
			for (int y = -(int)loadingRange.y + yPos; y <= (int)loadingRange.y + yPos; y++) {
				for (int z = -(int)loadingRange.z + zPos; z <= (int)loadingRange.z + zPos; z++) {
					int[] sceneID = new int[] {
						x,
						y,
						z
					};
					float xMoveLimit = 0;
					int xDeloadLimit = 0;
					
					float yMoveLimit = 0;
					int yDeloadLimit = 0;
					
					float zMoveLimit = 0;
					int zDeloadLimit = 0;

					if (looping) {

					

						if (sceneCollection.xSplitIs) {

							if (x > sceneCollection.xLimitsy) {
								int xLimit = (int)sceneCollection.xLimitsy + 1;

								xMoveLimit = ((int)(x / xLimit)) * xLimit * sceneCollection.xSize;
								xDeloadLimit = ((int)(x / xLimit)) * xLimit;
								sceneID [0] = x % xLimit;

							}


							if (x < sceneCollection.xLimitsx) {

								int xLimit = (int)sceneCollection.xLimitsy + 1;
						
								xMoveLimit = ((int)((x + 1) / xLimit) - 1) * xLimit * sceneCollection.xSize;
								xDeloadLimit = ((int)((x + 1) / xLimit) - 1) * xLimit;
								sceneID [0] = (xLimit + (x % xLimit)) % xLimit;

							
							}
						}
						if (sceneCollection.ySplitIs) {

							if (y > sceneCollection.yLimitsy) {
								int yLimit = (int)sceneCollection.yLimitsy + 1;

								yMoveLimit = ((int)(y / yLimit)) * yLimit * sceneCollection.ySize;
								yDeloadLimit = ((int)(y / yLimit)) * yLimit;
								sceneID [1] = y % yLimit;
						
						
							}

							if (y < sceneCollection.yLimitsx) {

								int yLimit = (int)sceneCollection.yLimitsy + 1;
						
								yMoveLimit = ((int)((y + 1) / yLimit) - 1) * yLimit * sceneCollection.ySize;
								yDeloadLimit = ((int)((y + 1) / yLimit) - 1) * yLimit;
								sceneID [1] = (yLimit + (y % yLimit)) % yLimit;
							}
						}

						if (sceneCollection.zSplitIs) {


							if (z > sceneCollection.zLimitsy) {
								int zLimit = (int)sceneCollection.zLimitsy + 1;
						
								zMoveLimit = ((int)(z / zLimit)) * zLimit * sceneCollection.zSize;
								zDeloadLimit = ((int)(z / zLimit)) * zLimit;
								sceneID [2] = z % zLimit;
						
						
							}
					

							if (z < sceneCollection.zLimitsx) {
						
								int zLimit = (int)sceneCollection.zLimitsy + 1;
						
								zMoveLimit = ((int)((z + 1) / zLimit) - 1) * zLimit * sceneCollection.zSize;
								zDeloadLimit = ((int)((z + 1) / zLimit) - 1) * zLimit;
								sceneID [2] = (zLimit + (z % zLimit)) % zLimit;

							}

						}
					}

					if (scenesArray.ContainsKey (sceneID)) {
						SceneSplit split = scenesArray [sceneID];
						if (!split.loaded) {
							split.loaded = true;

							split.posXLimitMove = xMoveLimit;
							split.xDeloadLimit = xDeloadLimit;

							split.posYLimitMove = yMoveLimit;
							split.yDeloadLimit = yDeloadLimit;

							split.posZLimitMove = zMoveLimit;
							split.zDeloadLimit = zDeloadLimit;

							scenesToLoad.Add (split);
							loadedScenes.Add (split);
							tilesToLoadNew++;
						}
					}
				}
			}
		}
	
		tilesToLoad = tilesToLoadNew;


	}

	/// <summary>
	/// Prepares the scenes array from collection
	/// </summary>
	void PrepareScenesArray ()
	{
		scenesArray = new Dictionary<int[], SceneSplit> (new IntArrayComparer ());


		foreach (var sceneName in sceneCollection.names) {
		
			int posX = 0;
			int posY = 0;
			int posZ = 0;

			SceneNameToPos (sceneCollection, sceneName, out posX, out posY, out posZ);

			SceneSplit sceneSplit = new SceneSplit ();
			sceneSplit.posX = posX;
			sceneSplit.posY = posY;
			sceneSplit.posZ = posZ;
			sceneSplit.sceneName = sceneName.Replace (".unity", "");
			scenesArray.Add (new int[] {
				posX,
				posY,
				posZ
			}, sceneSplit);
		}
	}

	/// <summary>
	/// Converts scene name into position
	/// </summary>
	/// <param name="sceneName">Scene name.</param>
	/// <param name="posX">Position x.</param>
	/// <param name="posY">Position y.</param>
	/// <param name="posZ">Position z.</param>
	public static void SceneNameToPos (SceneCollection sceneCollection, string sceneName, out int posX, out int posY, out int posZ)
	{
		posX = 0;
		posY = 0;
		posZ = 0;

		string[] values = sceneName.Replace (sceneCollection.prefixScene, "").Replace (".unity", "").Split (new char[] {
			'_'
		}, System.StringSplitOptions.RemoveEmptyEntries);

		foreach (var item in values) {
			if (item [0] == 'x') {
				posX = int.Parse (item.Replace ("x", ""));
			}
			if (item [0] == 'y') {
				posY = int.Parse (item.Replace ("y", ""));
			}
			if (item [0] == 'z') {
				posZ = int.Parse (item.Replace ("z", ""));
			}
		}

	}
}