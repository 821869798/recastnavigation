using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecastUnity.ExportEditor
{
	[CreateAssetMenu(fileName = "RecastNavMeshConfig", menuName = "Create Scriptable Config/Recast Navagation/RecastNavMeshConfig")]
	public class RecastNavMeshConfig : ScriptableObject
	{
		public enum RecastNavMeshType
		{
			// 支持动态障碍物
			TempObstacles,
			// 不支持动态障碍物
			SoleMesh
		}

		[Tooltip("导出的寻路网格类型")]
		public RecastNavMeshType navMeshType = RecastNavMeshType.TempObstacles;

		public enum SceneObjExportType
		{
			NavigationStatic_AutoCut,
			NavigationStatic,
			SceneObjAll_AutoCut,
			SceneObjAll,
		}

		[Tooltip("导出场景中mesh的类型")]
		public SceneObjExportType sceneObjExportType = SceneObjExportType.NavigationStatic_AutoCut;

		[Tooltip("导出的寻路网格参数")]
		[SerializeField]
		public RecastNavMeshExporter.RecastNavMeshBuildArgs buildArgs = new RecastNavMeshExporter.RecastNavMeshBuildArgs()
		{
			cellSize = 0.3f,
			cellHeight = 0.2f,
			agentHeight = 2.0f,
			agentRadius = 0.6f,
			agentMaxClimb = 0.9f,
			agentMaxSlope = 45.0f,
			regionMinSize = 8.0f,
			regionMergeSize = 20.0f,
			edgeMaxLen = 12.0f,
			edgeMaxError = 1.3f,
			vertsPerPoly = 6.0f,
			detailSampleDist = 6.0f,
			detailSampleMaxError = 1.0f,
			partitionType = 0,
			tileSize = 48,
		};

		[Serializable]
		public class ExportSceneModify
		{
			public string gameObjectPath;
			public bool exportActive;
		}

		public List<ExportSceneModify> exportSceneModifies = new List<ExportSceneModify>();
	}
}