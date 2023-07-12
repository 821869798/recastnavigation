using UnityEngine;

public static class RecastUtility
{

	public static readonly float[] DefaultHalfExtents = new float[3] { 2, 4, 2 };

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
}
