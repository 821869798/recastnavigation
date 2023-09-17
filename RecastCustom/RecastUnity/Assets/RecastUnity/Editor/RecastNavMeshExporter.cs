using Codice.Client.BaseCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace RecastUnity.ExportEditor
{
	/// <summary>
	/// 通过读取Unity的Navmesh配置，直接导出RecastNavigation的Navmesh二进制数据
	/// </summary>
	public static class RecastNavMeshExporter
	{

#if UNITY_IOS && !UNITY_EDITOR
        const string NavMeshExportDLL = "__Internal";
#else
		const string NavMeshExportDLL = "NavMeshExport";
#endif

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct RecastNavMeshBuildArgs
		{
			public float cellSize;
			public float cellHeight;
			public float agentHeight;
			public float agentRadius;
			public float agentMaxClimb;
			public float agentMaxSlope;
			public float regionMinSize;
			public float regionMergeSize;
			public float edgeMaxLen;
			public float edgeMaxError;
			public float vertsPerPoly;
			public float detailSampleDist;
			public float detailSampleMaxError;
			public float tileSize;
			public int partitionType;
		};

		[DllImport(NavMeshExportDLL)]
		private static extern bool NavMeshExport(ref NavMeshBuildArgs buildArgs, float[] vertices, int[] triangles, int[] area, string savePath);

		[DllImport(NavMeshExportDLL)]
		private static extern int TempObstaclesExport(ref RecastNavMeshBuildArgs buildArgs, string objPath, string exportPath);

		[DllImport(NavMeshExportDLL)]
		private static extern int SoleMeshExport(ref RecastNavMeshBuildArgs buildArgs, string objPath, string exportPath);

		[DllImport(NavMeshExportDLL)]
		private static extern int TempObstaclesGenMeshInfo(string binPath, string exportJsonPath);

		[DllImport(NavMeshExportDLL)]
		private static extern int SoleMeshGenMeshInfo(string binPath, string exportJsonPath);

		[DllImport(NavMeshExportDLL)]
		private static extern int AutoGenMeshInfo(string binPath, string exportJsonPath);


		public static bool ExportNavMeshByConfig(RecastNavMeshConfig recastNavMeshConfig, string outputPath)
		{
			var originActive = new Dictionary<GameObject, bool>();

			foreach (var m in recastNavMeshConfig.exportSceneModifies)
			{
				var go = GameObject.Find(m.gameObjectPath);
				if (go == null)
				{
					continue;
				}
				originActive.TryAdd(go, go.activeSelf);
				go.SetActive(m.exportActive);
			}

			var tempObjPath = Path.Combine(Path.GetTempPath(), "RecastUnity", SceneManager.GetActiveScene().name + ".obj").Replace('\\', '/');

			var parentPath = Path.GetDirectoryName(tempObjPath);
			if (!Directory.Exists(parentPath))
			{
				Directory.CreateDirectory(parentPath);
			}

			int resultCode = 0;
			try
			{
				switch (recastNavMeshConfig.sceneObjExportType)
				{
					case RecastNavMeshConfig.SceneObjExportType.NavigationStatic_AutoCut:
						ExportScene2ObjTools.ExportSceneToObjNavStaic(tempObjPath, true);
						break;
					case RecastNavMeshConfig.SceneObjExportType.NavigationStatic:
						ExportScene2ObjTools.ExportSceneToObjNavStaic(tempObjPath, false);
						break;
					case RecastNavMeshConfig.SceneObjExportType.SceneObjAll_AutoCut:
						ExportScene2ObjTools.ExportSceneToObj(tempObjPath, true);
						break;
					case RecastNavMeshConfig.SceneObjExportType.SceneObjAll:
						ExportScene2ObjTools.ExportSceneToObj(tempObjPath, false);
						break;
				}

				foreach (var kv in originActive)
				{
					kv.Key.SetActive(kv.Value);
				}
				originActive.Clear();

				if (!File.Exists(tempObjPath))
				{
					Debug.LogError("generator obj mesh failed!");
					return false;
				}

				switch (recastNavMeshConfig.navMeshType)
				{
					case RecastNavMeshConfig.RecastNavMeshType.SoleMesh:
						resultCode = SoleMeshExport(ref recastNavMeshConfig.buildArgs, tempObjPath, outputPath);
						if (resultCode != 0)
						{
							Debug.LogError("sole mesh export failed,error code:" + resultCode);
						}
						break;
					case RecastNavMeshConfig.RecastNavMeshType.TempObstacles:
						resultCode = TempObstaclesExport(ref recastNavMeshConfig.buildArgs, tempObjPath, outputPath);
						if (resultCode != 0)
						{
							Debug.LogError("temp obstacles export failed,error code:" + resultCode);
						}
						break;
					default:
						resultCode = -1;
						break;
				}

				File.Delete(tempObjPath);
			}
			catch (Exception e)
			{
				Debug.LogError("temp obstacles export exception:" + e.ToString());
				resultCode = -1;
				return false;
			}
			finally
			{
				foreach (var kv in originActive)
				{
					kv.Key.SetActive(kv.Value);
				}
			}

			if (resultCode != 0)
			{
				return false;
			}

			return true;
		}

		public static bool TryGetNavMeshInfo(string navmeshPath, out RecastNavMeshInfo meshInfo)
		{
			try
			{
				var tempJsonPath = Path.Combine(Path.GetTempPath(), "RecastUnity", Path.GetFileNameWithoutExtension(navmeshPath) + ".json").Replace('\\', '/');

				var parentPath = Path.GetDirectoryName(tempJsonPath);
				if (!Directory.Exists(parentPath))
				{
					Directory.CreateDirectory(parentPath);
				}
				var resultCode = AutoGenMeshInfo(navmeshPath, tempJsonPath);
				if (resultCode != 0)
				{
					Debug.LogError("AutoGenMeshInfo failed,error code:" + resultCode);
					meshInfo = null;
					File.Delete(tempJsonPath);
					return false;
				}

				string json = File.ReadAllText(tempJsonPath);
				File.Delete(tempJsonPath);
				RecastNavMeshInfo navMeshData = Newtonsoft.Json.JsonConvert.DeserializeObject<RecastNavMeshInfo>(json);
				meshInfo = navMeshData;
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("TryGetNavMeshInfo Exception:" + e.ToString());
			}
			meshInfo = null;
			return false;
		}

		#region Obsolete 废弃

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

		[Obsolete]
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

		#endregion

	}
}