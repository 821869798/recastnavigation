using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class RecastDll
{
	[DllImport("RecastDll")]
	public static extern IntPtr RecastLoad(int id, byte[] buffer, int n);
	[DllImport("RecastDll")]
	public static extern bool RecastClear(IntPtr navMeshScene);

	[DllImport("RecastDll")]
	public static extern int RecastFindNearestPoint(IntPtr navMeshScene, float[] extents, float[] startPos, float[] nearestPos);

	[DllImport("RecastDll")]
	public static extern int RecastTryMove(IntPtr navMeshScene, float[] extents, float[] startPos, float[] endPos, float[] realEndPos);


}
