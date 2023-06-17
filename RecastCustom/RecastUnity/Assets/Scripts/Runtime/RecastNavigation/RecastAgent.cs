using System;
using Unity.VisualScripting;
using UnityEngine;

public class RecastAgent
{
	public float[] halfExtents = new float[3] { 2, 4, 2 };

	protected float[] sharedStartPos = new float[3];
	protected float[] sharedEndPos = new float[3];
	protected float[] sharedRealEndPos = new float[3];
	protected float[] smoothPath = new float[256 * 3];

	protected int nextPosIndex;
	protected int smoothPathCount;
	protected Vector3 nextPos;

	public float speed { get; set; } = 5;
	public bool pathPending { protected get; set; }

	private IntPtr navMeshScene;
	public Transform transform { private set; get; }


	private RecastAgent()
	{

	}

	public static RecastAgent Create(Transform transform, IntPtr navMeshScene)
	{
		RecastAgent recastAgent = new RecastAgent();
		recastAgent.transform = transform;
		recastAgent.navMeshScene = navMeshScene;
		return recastAgent;
	}

	public virtual void Move(Vector3 offset)
	{
		var tryEndPos = this.transform.position + offset;
		if (TryMove(this.transform.position, tryEndPos, out var realEndPos))
		{
			TurnLook(realEndPos);
			this.transform.position = realEndPos;
		}
	}


	protected virtual void TurnLook(Vector3 target)
	{
		Vector3 targetDirection = target - transform.position;
		targetDirection.y = 0; // this makes the direction strictly horizontal
		this.transform.rotation = Quaternion.LookRotation(targetDirection);
	}

	protected virtual bool TryMove(Vector3 startPos, Vector3 endPos, out Vector3 realEndPos)
	{
		sharedStartPos[0] = -startPos.x;
		sharedStartPos[1] = startPos.y;
		sharedStartPos[2] = startPos.z;

		sharedEndPos[0] = -endPos.x;
		sharedEndPos[1] = endPos.y;
		sharedEndPos[2] = endPos.z;


		int result = RecastDll.RecastTryMove(navMeshScene, halfExtents, sharedStartPos, sharedEndPos, sharedRealEndPos);

		realEndPos = new Vector3(-sharedRealEndPos[0], sharedRealEndPos[1], sharedRealEndPos[2]);

		if (result < 0)
		{
			Debug.LogError("Recast TryMove failed,result:" + result);
			return false;
		}
		return true;

	}

	public virtual bool FindNearestPoint(Vector3 startPos, out Vector3 realEndPos)
	{
		sharedStartPos[0] = -startPos.x;
		sharedStartPos[1] = startPos.y;
		sharedStartPos[2] = startPos.z;

		int result = RecastDll.RecastFindNearestPoint(navMeshScene, halfExtents, sharedStartPos, sharedRealEndPos);

		realEndPos = new Vector3(-sharedRealEndPos[0], sharedRealEndPos[1], sharedRealEndPos[2]);

		if (result < 0)
		{
			Debug.LogError("Recast FindNearestPoint failed,result:" + result);
			return false;
		}
		return true;
	}

	public virtual bool SetDestination(Vector3 target)
	{
		var startPos = transform.position;

		sharedStartPos[0] = -startPos.x;
		sharedStartPos[1] = startPos.y;
		sharedStartPos[2] = startPos.z;

		sharedEndPos[0] = -target.x;
		sharedEndPos[1] = target.y;
		sharedEndPos[2] = target.z;

		smoothPathCount = RecastDll.RecastFindFollow(navMeshScene, halfExtents, sharedStartPos, sharedEndPos, smoothPath);
		if (smoothPathCount <= 0)
		{
			return false;
		}

		pathPending = true;

		nextPosIndex = 0;
		PathNextPosition();
		TurnLook(nextPos);

		return true;
	}

	public virtual void ResetPath()
	{
		pathPending = false;
	}

	private void PathNextPosition()
	{
		nextPosIndex++;
		nextPos.Set(-smoothPath[nextPosIndex * 3], smoothPath[nextPosIndex * 3 + 1], smoothPath[nextPosIndex * 3 + 2]);
	}

	public virtual void Update()
	{
		if (!pathPending)
		{
			return;
		}

		// Calculate the movement this frame
		Vector3 dir = (nextPos - transform.position).normalized;
		Vector3 displacement = dir * speed * Time.deltaTime;

		// If the displacement is beyond the next position, move to the next position
		if (displacement.sqrMagnitude >= (nextPos - transform.position).sqrMagnitude)
		{
			transform.position = nextPos;

			if (smoothPathCount == nextPosIndex + 1)
			{
				pathPending = false;
				return;
			}
			PathNextPosition();
			TurnLook(nextPos);
		}
		else
		{
			// Otherwise, move in the direction of the next position
			transform.position += displacement;
		}
	}

	//public virtual void Update()
	//{
	//	if (!pathPending)
	//	{
	//		return;
	//	}
	//	//目标点

	//	float d = speed * Time.deltaTime;

	//	Vector3 dir = nextPos - transform.position;
	//	dir.Normalize();
	//	dir.y = 0;

	//	float d_x = dir.x * d;
	//	float d_z = dir.z * d;

	//	//本次计算到达的点
	//	Vector3 f = new Vector3(d_x, 0, d_z);
	//	Vector3 xxx = transform.position + f;
	//	float d1 = Vector3.Distance(transform.position, xxx);
	//	float d2 = Vector3.Distance(transform.position, nextPos);

	//	if (d1 >= d2)
	//	{
	//		transform.position = nextPos;

	//		if (smoothPathCount == nextPosIndex + 1)
	//		{
	//			pathPending = false;
	//			return;
	//		}

	//		PathNextPosition();
	//		TurnLook(nextPos);

	//	}
	//	else
	//	{
	//		transform.position = xxx;
	//	}

	//}
}
