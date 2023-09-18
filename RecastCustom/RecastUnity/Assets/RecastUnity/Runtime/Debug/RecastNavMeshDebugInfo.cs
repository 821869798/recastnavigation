using System.Collections.Generic;
using UnityEngine;

namespace RecastUnity
{
	[ExecuteInEditMode]
	public class RecastNavMeshDebugInfo : MonoBehaviour
	{
		private static RecastNavMeshDebugInfo _instance;
		public static RecastNavMeshDebugInfo Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<RecastNavMeshDebugInfo>();
					if (_instance == null)
					{
						_instance = new GameObject("RecastNavMeshDebugInfo").AddComponent<RecastNavMeshDebugInfo>();
						_instance.transform.position = Vector3.zero;
						_instance.transform.rotation = Quaternion.identity;
					}
				}
				return _instance;
			}
		}

		private Mesh navMesh;
		private List<Mesh> navMeshLine = new List<Mesh>();

		private void Awake()
		{
		}

		public void SetMeshDraw(Mesh navMesh)
		{
#if UNITY_EDITOR
			ClearNavMesh();
			this.navMesh = navMesh;
			GenLineMesh();
#endif
		}

		public void ClearNavMesh()
		{
			var isPlaying = Application.isPlaying;
			if (navMesh != null)
			{
				if (isPlaying)
				{
					Object.Destroy(navMesh);
				}
				else
				{
					Object.DestroyImmediate(navMesh);
				}

				navMesh = null;
			}
			foreach (var m in navMeshLine)
			{
				if (isPlaying)
				{
					Object.Destroy(m);
				}
				else
				{
					Object.DestroyImmediate(m);
				}

			}
			navMeshLine.Clear();
		}

		void GenLineMesh()
		{
			var originTriangles = navMesh.triangles;
			var originVertices = navMesh.vertices;
			var numTriangles = originTriangles.Length / 3;
			List<Vector3> lines = new List<Vector3>();
			for (int i = 0; i < numTriangles; i++)
			{
				var a = originVertices[originTriangles[i * 3 + 0]];
				var b = originVertices[originTriangles[i * 3 + 1]];
				var c = originVertices[originTriangles[i * 3 + 2]];
				lines.Add(a);
				lines.Add(b);
				lines.Add(b);
				lines.Add(c);
				lines.Add(c);
				lines.Add(a);
			}

			const int MaxLineEndPointsPerBatch = 65532 / 2;
			int batches = (lines.Count + MaxLineEndPointsPerBatch - 1) / MaxLineEndPointsPerBatch;

			for (int batch = 0; batch < batches; batch++)
			{
				int startIndex = MaxLineEndPointsPerBatch * batch;
				int endIndex = Mathf.Min(startIndex + MaxLineEndPointsPerBatch, lines.Count);
				int lineEndPointCount = endIndex - startIndex;
				UnityEngine.Assertions.Assert.IsTrue(lineEndPointCount % 2 == 0);

				// Use pooled lists to avoid excessive allocations
				var vertices = new List<Vector3>(lineEndPointCount * 2);
				var normals = new List<Vector3>(lineEndPointCount * 2);
				var uv = new List<Vector2>(lineEndPointCount * 2);
				var tris = new List<int>(lineEndPointCount * 3);
				// Loop through each endpoint of the lines
				// and add 2 vertices for each
				for (int j = startIndex; j < endIndex; j++)
				{
					var vertex = (Vector3)lines[j];
					vertices.Add(vertex);
					vertices.Add(vertex);

					uv.Add(new Vector2(0, 0));
					uv.Add(new Vector2(1, 0));
				}

				// Loop through each line and add
				// one normal for each vertex
				for (int j = startIndex; j < endIndex; j += 2)
				{
					var lineDir = (Vector3)(lines[j + 1] - lines[j]);
					// Store the line direction in the normals.
					// A line consists of 4 vertices. The line direction will be used to
					// offset the vertices to create a line with a fixed pixel thickness
					normals.Add(lineDir);
					normals.Add(lineDir);
					normals.Add(lineDir);
					normals.Add(lineDir);
				}

				// Setup triangle indices
				// A triangle consists of 3 indices
				// A line (4 vertices) consists of 2 triangles, so 6 triangle indices
				for (int j = 0, v = 0; j < lineEndPointCount * 3; j += 6, v += 4)
				{
					// First triangle
					tris.Add(v + 0);
					tris.Add(v + 1);
					tris.Add(v + 2);

					// Second triangle
					tris.Add(v + 1);
					tris.Add(v + 3);
					tris.Add(v + 2);
				}

				var mesh = new Mesh();

				// Set all data on the mesh
				mesh.SetVertices(vertices);
				mesh.SetTriangles(tris, 0);
				//mesh.SetColors(colors);
				mesh.SetNormals(normals);
				mesh.SetUVs(0, uv);

				// Upload all data
				mesh.UploadMeshData(false);

				navMeshLine.Add(mesh);
			}
		}

		private void OnDrawGizmos()
		{
			if (navMesh == null)
			{
				return;
			}

			Material surfaceMaterial = null;
			Material lineMaterial = null;
#if UNITY_EDITOR
			// Make sure the material references are correct
			if (surfaceMaterial == null) surfaceMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath(EditorResourceHelper.editorAssets + "/Materials/Navmesh.mat", typeof(Material)) as Material;
			if (lineMaterial == null) lineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath(EditorResourceHelper.editorAssets + "/Materials/NavmeshOutline.mat", typeof(Material)) as Material;
#endif

			var position = this.transform.position;
			var rotation = this.transform.rotation;
			var localScale = this.transform.localScale;
			DrawNavMesh(surfaceMaterial, position, rotation, localScale);
			DrawNavMeshLine(lineMaterial, position, rotation, localScale);

		}

		private void DrawNavMesh(Material mat, Vector3 position, Quaternion rotation, Vector3 localScale)
		{
			for (int pass = 0; pass < mat.passCount; pass++)
			{
				mat.SetPass(pass);
				Graphics.DrawMeshNow(navMesh, Matrix4x4.TRS(position, rotation, localScale));
			}
		}

		private void DrawNavMeshLine(Material mat, Vector3 position, Quaternion rotation, Vector3 localScale)
		{
			// 画线
			for (int pass = 0; pass < mat.passCount; pass++)
			{
				mat.SetPass(pass);
				foreach (var m in navMeshLine)
				{
					Graphics.DrawMeshNow(m, Matrix4x4.TRS(position, rotation, localScale));
				}

			}
		}

		public static void DestroyDebugInfo()
		{
			if (_instance == null)
			{
				return;
			}
			if (Application.isPlaying)
			{
				Object.Destroy(_instance.gameObject);
			}
			else
			{
				Object.DestroyImmediate(_instance.gameObject);
			}
			_instance = null;
		}

		private void OnDestroy()
		{
			_instance = null;
			ClearNavMesh();
		}
	}

}