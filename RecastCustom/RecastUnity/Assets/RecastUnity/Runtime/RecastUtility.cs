using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RecastUnity
{
	public static class RecastUtility
	{

		public static readonly float[] DefaultHalfExtents = new float[3] { 2, 4, 2 };

		public static bool RecastTryGetBounds(IntPtr navMeshScene, out Bounds bounds)
		{
			float[] bmin = new float[3];
			float[] bmax = new float[3];
			var result = RecastDll.RecastGetBounds(navMeshScene, bmin, bmax);
			if (result != 0)
			{
				bounds = default;
				return false;
			}

			bounds = new Bounds();
			Vector3 min = new Vector3(-bmax[0], bmin[1], bmin[2]);
			Vector3 max = new Vector3(-bmin[0], bmax[1], bmax[2]);
			bounds.SetMinMax(min, max);
			return true;
		}

		public static bool RecastTryGetBoundsRect(IntPtr navMeshScene, out Rect rect)
		{
			if (RecastTryGetBounds(navMeshScene, out var bounds))
			{
				var min = bounds.min;
				var size = bounds.size;
				rect = new Rect(min.x, min.z, size.x, size.z);
				return true;
			}
			rect = default;
			return false;
		}

		public static Vector3 RecastPos2UnityPos(float[] src)
		{
			return new Vector3(-src[0], src[1], src[2]);
		}

		public static void UnityPos2RecastPos(Vector3 src, float[] dest)
		{
			dest[0] = -src.x;
			dest[1] = src.y;
			dest[2] = src.z;
		}

		public static float[] UnityPos2RecastPosNew(Vector3 src)
		{
			float[] dest = new float[3];
			UnityPos2RecastPos(src, dest);
			return dest;
		}

		public static Vector3 IndexPathPos2Unity(float[] path, int index)
		{
			return new Vector3(-path[index * 3], path[index * 3 + 1], path[index * 3 + 2]);
		}

		public static void SetIndexPathPos2Unity(float[] path, int index, ref Vector3 pos)
		{
			pos.Set(-path[index * 3], path[index * 3 + 1], path[index * 3 + 2]);
		}

		public static void SwapRecastPos(ref float[] pos1, ref float[] pos2)
		{
			var tmp = pos1;
			pos1 = pos2;
			pos2 = tmp;
		}

		public static Mesh GenMeshByMeshInfo(RecastNavMeshInfo meshInfo)
		{
			if (meshInfo == null)
			{
				return null;
			}
			Mesh mesh = new Mesh();
			mesh.name = "RecastNavMesh";

			// Set vertices
			Vector3[] vertices = new Vector3[meshInfo.vertices.Count];
			for (int i = 0; i < meshInfo.vertices.Count; i++)
			{
				// Assuming that the vertices are 3D points
				// x need flip
				vertices[i] = new Vector3(-meshInfo.vertices[i][0], meshInfo.vertices[i][1], meshInfo.vertices[i][2]);
			}
			mesh.vertices = vertices;

			// Set triangles
			List<int> triangles = new List<int>();
			foreach (var polygon in meshInfo.triangles)
			{
				// Simple triangulation for convex polygons
				for (int i = 1; i < polygon.Count - 1; i++)
				{
					triangles.Add(polygon[0]);
					triangles.Add(polygon[i]);
					triangles.Add(polygon[i + 1]);
				}
			}
			mesh.triangles = triangles.ToArray();

			// Recalculate normals
			mesh.RecalculateNormals();
			return mesh;
		}

		public static bool RecastTryGetNavMesh(IntPtr navMeshScene, out Mesh mesh)
		{
			mesh = null;
			try
			{
				DateTimeOffset currentTimeOffset = DateTimeOffset.UtcNow;
				long timestampInSeconds = currentTimeOffset.ToUnixTimeSeconds();
				var tempJsonPath = Path.Combine(Path.GetTempPath(), "RecastUnity", timestampInSeconds.ToString() + ".json").Replace('\\', '/');

				var parentPath = Path.GetDirectoryName(tempJsonPath);
				if (!Directory.Exists(parentPath))
				{
					Directory.CreateDirectory(parentPath);
				}
				var resultCode = RecastDll.RecastGenNavMeshInfo(navMeshScene, tempJsonPath);
				if (resultCode != 0)
				{
					Debug.LogError("RecastGetNavMeshInfo failed,error code:" + resultCode);
					File.Delete(tempJsonPath);
					return false;
				}

				string json = File.ReadAllText(tempJsonPath);
				File.Delete(tempJsonPath);
				RecastNavMeshInfo navMeshData = Newtonsoft.Json.JsonConvert.DeserializeObject<RecastNavMeshInfo>(json);

				mesh = GenMeshByMeshInfo(navMeshData);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("RecastGetNavMeshInfo exception:" + e.ToString());
				return false;
			}
		}
	}

}