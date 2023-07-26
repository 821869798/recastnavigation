using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// 通过读取Unity的Navmesh配置，直接导出RecastNavigation的Navmesh二进制数据
/// </summary>
public static class NavMeshExporter
{
	[StructLayout(LayoutKind.Sequential)]
	public struct NavMeshBuildArgs
	{
		public int vertexCount;
		public int triangleCount;
		public ushort regionId;
		public float cellSize;
		public float cellHeight;
		public float walkableHeight;
		public float walkableRadius;
		public float walkableClimb;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public float[] boundMin;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public float[] boundMax;
	};

#if UNITY_IOS && !UNITY_EDITOR
        const string NavMeshExportDLL = "__Internal";
#else
	const string NavMeshExportDLL = "NavMeshExport";
#endif

	[DllImport(NavMeshExportDLL)]
	private static extern bool NavMeshExport(ref NavMeshBuildArgs buildArgs, float[] vertices, int[] triangles, int[] area, string savePath);


	[MenuItem("GameEditor/NavMesh/Export NavMesh", priority = 10000)]
	private static void ExportNavMesh()
	{
		//临时把所有gameobject改成scale x -1
		Dictionary<Transform, Vector3> meshOriginScale = new Dictionary<Transform, Vector3>();
		Scene activeScene = SceneManager.GetActiveScene();
		GameObject[] rootObjects = activeScene.GetRootGameObjects();
		foreach (var obj in rootObjects)
		{
			var tf = obj.transform;
			var ss = tf.localScale;
			meshOriginScale[tf] = ss;
			ss.x = -ss.x;
			tf.localScale = ss;
		}

		NavMeshTriangulation triangulation = new NavMeshTriangulation();
		try
		{
#if UNITY_2017_2_OR_NEWER
			UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#endif

			triangulation = NavMesh.CalculateTriangulation();
		}
		catch (System.Exception e)
		{
			Debug.LogError("BuildNavMesh error:" + e);
			return;
		}
		finally
		{
			//还原所有更改GameOject scale
			foreach (var kv in meshOriginScale)
			{
				kv.Key.localScale = kv.Value;
			}
			meshOriginScale.Clear();
		}

		Vector3[] vertices = triangulation.vertices;
		int[] indices = triangulation.indices;
		int[] areas = triangulation.areas;
		if ((0 == vertices.Length) || (0 == indices.Length) || (0 == areas.Length))
		{
			Debug.LogError("There is no NavMesh to export!");
			return;
		}

		var curScene = SceneManager.GetActiveScene().name;

		string navmeshName = curScene + ".navmesh.bytes";
		string savePath = EditorUtility.SaveFilePanel("Export NavMesh", Path.Combine(Application.dataPath, "Res"), navmeshName, "bytes");
		if (string.IsNullOrEmpty(savePath))
		{
			return;
		}

		// Fill data.
		float[] vertBuffData = new float[vertices.Length * 3];
		int vertIdx = 0;
		foreach (var vertex in vertices)
		{
			vertBuffData[vertIdx++] = vertex.x;
			vertBuffData[vertIdx++] = vertex.y;
			vertBuffData[vertIdx++] = vertex.z;
		}

		// Get build settings.
		float voxelSize = 0.1666667f;
		float voxelHeight = 0.1666667f;
		float agentHeight = 2.0f;
		float agentRadius = 0.5f;
		float agentClimb = 0.4f;
#if UNITY_2017_2_OR_NEWER
		NavMeshBuildSettings setting = NavMesh.GetSettingsByIndex(0);
		voxelSize = setting.voxelSize;
		voxelHeight = setting.voxelSize;
		agentHeight = setting.agentHeight;
		agentRadius = setting.agentRadius;
		agentClimb = setting.agentClimb;
#endif

		NavMeshBuildArgs buildArgs = new NavMeshBuildArgs();
		buildArgs.vertexCount = vertices.Length;
		buildArgs.triangleCount = indices.Length;
		buildArgs.regionId = 0;
		buildArgs.cellSize = voxelSize;
		buildArgs.cellHeight = voxelHeight;
		buildArgs.walkableHeight = agentHeight;
		buildArgs.walkableRadius = agentRadius;
		buildArgs.walkableClimb = agentClimb;
		buildArgs.boundMin = new float[3] { 0.0f, 0.0f, 0.0f };
		buildArgs.boundMax = new float[3] { 0.0f, 0.0f, 0.0f };


		var ok = NavMeshExport(ref buildArgs, vertBuffData, indices, areas, savePath);
		if (ok)
		{
			Debug.Log($"boundMin:{string.Join(",", buildArgs.boundMin)}");
			Debug.Log($"boundMax:{string.Join(",", buildArgs.boundMax)}");
			Debug.Log("Export NavMesh to: " + savePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		else
		{
			Debug.Log("Export NavMesh faield: " + savePath);
		}

	}

	[MenuItem("GameEditor/NavMesh/ExportSceneToObj-NavStatic")]
	//[MenuItem("GameObject/ExportScene/ExportSceneToObj")]
	public static void ExportNavStatic()
	{
		ExportSceneToObjNavStaic(false);
	}

	[MenuItem("GameEditor/NavMesh/ExportSceneToObj-NavStatic(AutoCut)")]
	//[MenuItem("GameObject/ExportScene/ExportSceneToObj(AutoCut)")]
	public static void ExportNavStaticAutoCut()
	{
		ExportSceneToObjNavStaic(true);
	}

	[MenuItem("GameEditor/NavMesh/ExportSceneToObj")]
	//[MenuItem("GameObject/ExportScene/ExportSceneToObj")]
	public static void Export()
	{
		ExportSceneToObj(false);
	}

	[MenuItem("GameEditor/NavMesh/ExportSceneToObj(AutoCut)")]
	//[MenuItem("GameObject/ExportScene/ExportSceneToObj(AutoCut)")]
	public static void ExportAutoCut()
	{
		ExportSceneToObj(true);
	}

	[MenuItem("GameEditor/NavMesh/ExportSelectedObj")]
	[MenuItem("GameObject/ExportScene/ExportSelectedObj", priority = 44)]
	public static void ExportObj()
	{
		GameObject selectObj = Selection.activeGameObject;
		if (selectObj == null)
		{
			Debug.LogWarning("Select a GameObject");
			return;
		}
		string path = GetSavePath(false, selectObj);
		if (string.IsNullOrEmpty(path)) return;

		Terrain terrain = selectObj.GetComponent<Terrain>();
		MeshFilter[] mfs = selectObj.GetComponentsInChildren<MeshFilter>();
		SkinnedMeshRenderer[] smrs = selectObj.GetComponentsInChildren<SkinnedMeshRenderer>();
		Debug.Log(mfs.Length + "," + smrs.Length);
		ExportSceneToObj(path, terrain, mfs, smrs, false, false);
	}


	#region export scene to obj

	private const string CUT_LB_OBJ_PATH = "export/bound_lb";
	private const string CUT_RT_OBJ_PATH = "export/bound_rt";

	private static float autoCutMinX = 1000;
	private static float autoCutMaxX = 0;
	private static float autoCutMinY = 1000;
	private static float autoCutMaxY = 0;

	private static float cutMinX = 0;
	private static float cutMaxX = 0;
	private static float cutMinY = 0;
	private static float cutMaxY = 0;

	private static long startTime = 0;
	private static int totalCount = 0;
	private static int count = 0;
	private static int counter = 0;
	private static int progressUpdateInterval = 10000;

	public static void ExportSceneToObjNavStaic(bool autoCut)
	{
		string path = GetSavePath(autoCut, null);
		if (string.IsNullOrEmpty(path)) return;
		Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
		if (terrain && (GameObjectUtility.GetStaticEditorFlags(terrain.gameObject) & StaticEditorFlags.NavigationStatic) > 0)
		{

		}
		else
		{
			terrain = null;
		}
		MeshFilter[] mfs = UnityEngine.Object.FindObjectsOfType<MeshFilter>().Where(m => (GameObjectUtility.GetStaticEditorFlags(m.gameObject) & StaticEditorFlags.NavigationStatic) > 0).ToArray();
		SkinnedMeshRenderer[] smrs = UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>().Where(m => (GameObjectUtility.GetStaticEditorFlags(m.gameObject) & StaticEditorFlags.NavigationStatic) > 0).ToArray();
		ExportSceneToObj(path, terrain, mfs, smrs, autoCut, true);
	}

	public static void ExportSceneToObj(bool autoCut)
	{
		string path = GetSavePath(autoCut, null);
		if (string.IsNullOrEmpty(path)) return;
		Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
		MeshFilter[] mfs = UnityEngine.Object.FindObjectsOfType<MeshFilter>();
		SkinnedMeshRenderer[] smrs = UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>();
		ExportSceneToObj(path, terrain, mfs, smrs, autoCut, true);
	}

	public static void ExportSceneToObj(string path, Terrain terrain, MeshFilter[] mfs,
		SkinnedMeshRenderer[] smrs, bool autoCut, bool needCheckRect)
	{
		int vertexOffset = 0;
		string title = "export GameObject to .obj ...";
		StreamWriter writer = new StreamWriter(path);

		startTime = GetMsTime();
		UpdateCutRect(autoCut);
		counter = count = 0;
		progressUpdateInterval = 5;
		totalCount = (mfs.Length + smrs.Length) / progressUpdateInterval;
		foreach (var mf in mfs)
		{
			UpdateProgress(title);
			if (mf.GetComponent<Renderer>() != null &&
				(!needCheckRect || (needCheckRect && IsInCutRect(mf.gameObject))))
			{
				ExportMeshToObj(mf.gameObject, mf.sharedMesh, ref writer, ref vertexOffset);
			}
		}
		foreach (var smr in smrs)
		{
			UpdateProgress(title);
			if (!needCheckRect || (needCheckRect && IsInCutRect(smr.gameObject)))
			{
				ExportMeshToObj(smr.gameObject, smr.sharedMesh, ref writer, ref vertexOffset);
			}
		}
		if (terrain)
		{
			ExportTerrianToObj(terrain.terrainData, terrain.GetPosition(),
				ref writer, ref vertexOffset, autoCut);
		}
		writer.Close();
		EditorUtility.ClearProgressBar();

		long endTime = GetMsTime();
		float time = (float)(endTime - startTime) / 1000;
		Debug.Log("Export SUCCESS:" + path);
		Debug.Log("Export Time:" + time + "s");
		OpenDir(path);
	}

	private static void OpenDir(string path)
	{
		DirectoryInfo dir = Directory.GetParent(path);
		int index = path.LastIndexOf("/");
		OpenCmd("explorer.exe", dir.FullName);
	}

	private static void OpenCmd(string cmd, string args)
	{
		System.Diagnostics.Process.Start(cmd, args);
	}

	private static string GetSavePath(bool autoCut, GameObject selectObject)
	{
		string dataPath = Application.dataPath;
		string dir = dataPath.Substring(0, dataPath.LastIndexOf("/"));
		string sceneName = SceneManager.GetActiveScene().name;
		string defaultName = "";
		if (selectObject == null)
		{
			defaultName = (autoCut ? sceneName + "(autoCut)" : sceneName);
		}
		else
		{
			defaultName = (autoCut ? selectObject.name + "(autoCut)" : selectObject.name);
		}
		return EditorUtility.SaveFilePanel("Export .obj file", dir, defaultName, "obj");
	}

	private static long GetMsTime()
	{
		return System.DateTime.Now.Ticks / 10000;
		//return (System.DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
	}

	private static void UpdateCutRect(bool autoCut)
	{
		cutMinX = cutMaxX = cutMinY = cutMaxY = 0;
		if (!autoCut)
		{
			Vector3 lbPos = GetObjPos(CUT_LB_OBJ_PATH);
			Vector3 rtPos = GetObjPos(CUT_RT_OBJ_PATH);
			cutMinX = lbPos.x;
			cutMaxX = rtPos.x;
			cutMinY = lbPos.z;
			cutMaxY = rtPos.z;
		}
	}

	private static void UpdateAutoCutRect(Vector3 v)
	{
		if (v.x < autoCutMinX) autoCutMinX = v.x;
		if (v.x > autoCutMaxX) autoCutMaxX = v.x;
		if (v.z < autoCutMinY) autoCutMinY = v.z;
		if (v.z > autoCutMaxY) autoCutMaxY = v.z;
	}

	private static bool IsInCutRect(GameObject obj)
	{
		if (cutMinX == 0 && cutMaxX == 0 && cutMinY == 0 && cutMaxY == 0) return true;
		Vector3 pos = obj.transform.position;
		if (pos.x >= cutMinX && pos.x <= cutMaxX && pos.z >= cutMinY && pos.z <= cutMaxY)
			return true;
		else
			return false;
	}

	private static void ExportMeshToObj(GameObject obj, Mesh mesh, ref StreamWriter writer, ref int vertexOffset)
	{
		Quaternion r = obj.transform.localRotation;
		StringBuilder sb = new StringBuilder();
		foreach (Vector3 vertice in mesh.vertices)
		{
			Vector3 v = obj.transform.TransformPoint(vertice);
			UpdateAutoCutRect(v);
			sb.AppendFormat("v {0} {1} {2}\n", -v.x, v.y, v.z);
		}
		foreach (Vector3 nn in mesh.normals)
		{
			Vector3 v = r * nn;
			sb.AppendFormat("vn {0} {1} {2}\n", -v.x, -v.y, v.z);
		}
		foreach (Vector3 v in mesh.uv)
		{
			sb.AppendFormat("vt {0} {1}\n", v.x, v.y);
		}
		for (int i = 0; i < mesh.subMeshCount; i++)
		{
			int[] triangles = mesh.GetTriangles(i);
			for (int j = 0; j < triangles.Length; j += 3)
			{
				sb.AppendFormat("f {1} {0} {2}\n",
					triangles[j] + 1 + vertexOffset,
					triangles[j + 1] + 1 + vertexOffset,
					triangles[j + 2] + 1 + vertexOffset);
			}
		}
		vertexOffset += mesh.vertices.Length;
		writer.Write(sb.ToString());
	}

	private static void ExportTerrianToObj(TerrainData terrain, Vector3 terrainPos,
		ref StreamWriter writer, ref int vertexOffset, bool autoCut)
	{
		int tw = terrain.heightmapResolution;
		int th = terrain.heightmapResolution;

		Vector3 meshScale = terrain.size;
		meshScale = new Vector3(meshScale.x / (tw - 1), meshScale.y, meshScale.z / (th - 1));
		Vector2 uvScale = new Vector2(1.0f / (tw - 1), 1.0f / (th - 1));

		Vector2 terrainBoundLB, terrainBoundRT;
		if (autoCut)
		{
			terrainBoundLB = GetTerrainBoundPos(new Vector3(autoCutMinX, 0, autoCutMinY), terrain, terrainPos);
			terrainBoundRT = GetTerrainBoundPos(new Vector3(autoCutMaxX, 0, autoCutMaxY), terrain, terrainPos);
		}
		else
		{
			terrainBoundLB = GetTerrainBoundPos(CUT_LB_OBJ_PATH, terrain, terrainPos);
			terrainBoundRT = GetTerrainBoundPos(CUT_RT_OBJ_PATH, terrain, terrainPos);
		}

		int bw = (int)(terrainBoundRT.x - terrainBoundLB.x);
		int bh = (int)(terrainBoundRT.y - terrainBoundLB.y);

		int w = bh != 0 && bh < th ? bh : th;
		int h = bw != 0 && bw < tw ? bw : tw;

		int startX = (int)terrainBoundLB.y;
		int startY = (int)terrainBoundLB.x;
		if (startX < 0) startX = 0;
		if (startY < 0) startY = 0;

		Debug.Log(string.Format("Terrian:tw={0},th={1},sw={2},sh={3},startX={4},startY={5}",
			tw, th, bw, bh, startX, startY));

		float[,] tData = terrain.GetHeights(0, 0, tw, th);
		Vector3[] tVertices = new Vector3[w * h];
		Vector2[] tUV = new Vector2[w * h];

		int[] tPolys = new int[(w - 1) * (h - 1) * 6];

		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				Vector3 pos = new Vector3(-(startY + y), tData[startX + x, startY + y], (startX + x));
				tVertices[y * w + x] = Vector3.Scale(meshScale, pos) + terrainPos;
				tUV[y * w + x] = Vector2.Scale(new Vector2(x, y), uvScale);
			}
		}
		int index = 0;
		for (int y = 0; y < h - 1; y++)
		{
			for (int x = 0; x < w - 1; x++)
			{
				tPolys[index++] = (y * w) + x;
				tPolys[index++] = ((y + 1) * w) + x;
				tPolys[index++] = (y * w) + x + 1;
				tPolys[index++] = ((y + 1) * w) + x;
				tPolys[index++] = ((y + 1) * w) + x + 1;
				tPolys[index++] = (y * w) + x + 1;
			}
		}
		count = counter = 0;
		progressUpdateInterval = 10000;
		totalCount = (tVertices.Length + tUV.Length + tPolys.Length / 3) / progressUpdateInterval;
		string title = "export Terrain to .obj ...";
		for (int i = 0; i < tVertices.Length; i++)
		{
			UpdateProgress(title);
			StringBuilder sb = new StringBuilder(22);
			sb.AppendFormat("v {0} {1} {2}\n", tVertices[i].x, tVertices[i].y, tVertices[i].z);
			writer.Write(sb.ToString());
		}
		for (int i = 0; i < tUV.Length; i++)
		{
			UpdateProgress(title);
			StringBuilder sb = new StringBuilder(20);
			sb.AppendFormat("vt {0} {1}\n", tUV[i].x, tUV[i].y);
			writer.Write(sb.ToString());
		}
		for (int i = 0; i < tPolys.Length; i += 3)
		{
			UpdateProgress(title);
			int x = tPolys[i] + 1 + vertexOffset; ;
			int y = tPolys[i + 1] + 1 + vertexOffset;
			int z = tPolys[i + 2] + 1 + vertexOffset;
			StringBuilder sb = new StringBuilder(30);
			sb.AppendFormat("f {0} {1} {2}\n", x, y, z);
			writer.Write(sb.ToString());
		}
		vertexOffset += tVertices.Length;
	}

	private static Vector2 GetTerrainBoundPos(string path, TerrainData terrain, Vector3 terrainPos)
	{
		var go = GameObject.Find(path);
		if (go)
		{
			Vector3 pos = go.transform.position;
			return GetTerrainBoundPos(pos, terrain, terrainPos);
		}
		return Vector2.zero;
	}

	private static Vector2 GetTerrainBoundPos(Vector3 worldPos, TerrainData terrain, Vector3 terrainPos)
	{
		Vector3 tpos = worldPos - terrainPos;
		return new Vector2((int)(tpos.x / terrain.size.x * terrain.heightmapResolution),
			(int)(tpos.z / terrain.size.z * terrain.heightmapResolution));
	}

	private static Vector3 GetObjPos(string path)
	{
		var go = GameObject.Find(path);
		if (go)
		{
			return go.transform.position;
		}
		return Vector3.zero;
	}

	private static void UpdateProgress(string title)
	{
		if (counter++ == progressUpdateInterval)
		{
			counter = 0;
			float process = Mathf.InverseLerp(0, totalCount, ++count);
			long currTime = GetMsTime();
			float sec = ((float)(currTime - startTime)) / 1000;
			string text = string.Format("{0}/{1}({2:f2} sec.)", count, totalCount, sec);
			EditorUtility.DisplayProgressBar(title, text, process);
		}
	}

	#endregion
}
