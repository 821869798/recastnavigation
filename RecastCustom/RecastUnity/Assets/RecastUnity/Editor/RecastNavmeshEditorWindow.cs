using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RecastUnity.ExportEditor
{
	public class RecastNavmeshEditorWindow : EditorWindow
	{
		[MenuItem("GameEditor/RecastNavigation/RecastNavmeshEditorWindow")]
		public static void ShowExample()
		{
			RecastNavmeshEditorWindow wnd = GetWindow<RecastNavmeshEditorWindow>();
			wnd.titleContent = new GUIContent("RecastNavmeshEditorWindow");
		}

		private ObjectField objNavMeshExportConfig;

		public void CreateGUI()
		{
			// Each editor window contains a root VisualElement object
			VisualElement root = rootVisualElement;

			// Import UXML
			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/RecastUnity/Editor/RecastNavmeshEditorWindow.uxml");
			VisualElement labelFromUXML = visualTree.Instantiate();
			root.Add(labelFromUXML);

			objNavMeshExportConfig = root.Q<ObjectField>("objNavMeshExportConfig");

			root.Q<Button>("btnPreviewNavMesh").clicked += PreviewNavMesh;

			root.Q<Button>("btnGenNavMesh").clicked += ExportNavMesh;
		}

		private void PreviewNavMesh()
		{
			var navmeshSavePath = GetNavMeshReadPath();
			if (string.IsNullOrEmpty(navmeshSavePath))
			{
				EditorUtility.DisplayDialog("提示", "请选择navmesh.bytes文件", "我懂得");
				return;
			}

			RecastNavMeshDebugInfo.Instance.ClearNavMesh();

			if (!RecastNavMeshExporter.TryGetNavMeshInfo(navmeshSavePath, out var meshInfo))
			{
				EditorUtility.DisplayDialog("提示", "获取导航网格信息失败", "我懂得");
				return;
			}
			var navMesh = RecastUtility.GenMeshByMeshInfo(meshInfo);

			RecastNavMeshDebugInfo.Instance.SetMeshDraw(navMesh);

		}

		private void ExportNavMesh()
		{
			var recastConfig = objNavMeshExportConfig.value as RecastNavMeshConfig;
			if (recastConfig == null)
			{
				EditorUtility.DisplayDialog("提示", "请先选择导出配置", "我懂得");
				return;
			}

			var navmeshSavePath = GetNavMeshSavePath();
			if (string.IsNullOrEmpty(navmeshSavePath))
			{
				EditorUtility.DisplayDialog("提示", "请先选择导出保存路径", "我懂得");
				return;
			}

			RecastNavMeshDebugInfo.Instance.ClearNavMesh();

			bool result = RecastNavMeshExporter.ExportNavMeshByConfig(recastConfig, navmeshSavePath);
			if (!result)
			{
				EditorUtility.DisplayDialog("提示", "导出失败，请查看控制台错误信息", "我懂得");
				return;
			}

			if (!RecastNavMeshExporter.TryGetNavMeshInfo(navmeshSavePath, out var meshInfo))
			{
				EditorUtility.DisplayDialog("提示", "导出成功，但是获取导航网格信息失败", "我懂得");
				return;
			}
			var navMesh = RecastUtility.GenMeshByMeshInfo(meshInfo);

			RecastNavMeshDebugInfo.Instance.SetMeshDraw(navMesh);

		}

		private void OnDisable()
		{
			if (RecastNavMeshDebugInfo.HasInstance)
			{
				RecastNavMeshDebugInfo.Instance.DestroyDebug();
			}
		}

		private static string GetNavMeshSavePath()
		{
			string dataPath = Application.dataPath.Replace('\\', '/');
			string sceneName = SceneManager.GetActiveScene().name;
			return EditorUtility.SaveFilePanel("Export recast navmesh file", dataPath, sceneName, "navmesh.bytes");
		}

		private static string GetNavMeshReadPath()
		{
			string dataPath = Application.dataPath.Replace('\\', '/');
			string sceneName = SceneManager.GetActiveScene().name;
			return EditorUtility.OpenFilePanel("Read recast navmesh file", dataPath, "navmesh.bytes");
		}
	}
}