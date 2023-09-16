using System;
using System.Runtime.InteropServices;

namespace RecastUnity
{
	public static class RecastDll
	{
#if UNITY_IOS && !UNITY_EDITOR
    const string RecastDLL = "__Internal";
#else
		const string RecastDLL = "RecastDll";
#endif

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr RecastGet(int id);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr RecastLoad(int id, byte[] buffer, int n, bool isTileCache = false);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool RecastClearById(int id);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool RecastClear(IntPtr navMeshScene);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void RecastClearAll();

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastGetBounds(IntPtr navMeshScene, float[] bmin, float[] bmax);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastGenNavMeshInfo(IntPtr navMeshScene, string exportJsonPath);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool RecastFindRandomPoint(IntPtr navMeshScene, float[] pos);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastFindNearestPoint(IntPtr navMeshScene, float[] extents, float[] startPos, float[] nearestPos);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastFindRandomPointAroundCircle(IntPtr navMeshScene, float[] extents, float[] centerPos, float maxRadius, float[] pos);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastFindFollow(IntPtr navMeshScene, float[] extents, float[] startPos, float[] endPos, float[] smoothPath);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastTryMove(IntPtr navMeshScene, float[] extents, float[] startPos, float[] endPos, float[] realEndPos);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastAddAgent(IntPtr navMeshScene, float[] pos, float radius, float height, float maxSpeed, float maxAcceleration);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastRemoveAgent(IntPtr navMeshScene, int agentId);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastClearAgent(IntPtr navMeshScene);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastGetAgentPos(IntPtr navMeshScene, int agentId, float[] pos);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastGetAgentPosWithState(IntPtr navMeshScene, int agentId, float[] pos, ref int targetState);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastSetAgentPos(IntPtr navMeshScene, int agentId, float[] pos);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastSetAgentMoveTarget(IntPtr navMeshScene, int agentId, float[] pos, bool adjust = false);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void RecastUpdate(IntPtr navMeshScene, float deltaTime);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastAddObstacle(IntPtr navMeshScene, ref uint obstacleId, float[] pos, float radius, float height);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastAddBoxObstacle(IntPtr navMeshScene, ref uint obstacleId, float[] bmin, float[] bmax);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastAddBoxCenterObstacle(IntPtr navMeshScene, ref uint obstacleId, float[] center, float[] halfExtents, float yRadians);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastRemoveObstacle(IntPtr navMeshScene, uint obstacleId);

		[DllImport(RecastDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int RecastUpdateObstacles(IntPtr navMeshScene, bool isEveryFrame = false);

	}

}
